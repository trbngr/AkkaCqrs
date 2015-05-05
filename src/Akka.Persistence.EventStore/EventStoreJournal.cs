using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Akka.Persistence.Journal;
using Akka.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

namespace EventStore.Persistence
{
    public class EventStoreJournal : AsyncWriteJournal
    {
        private const int BatchSize = 500;
        private readonly Lazy<Task<IEventStoreConnection>> _connection;
        private readonly ILoggingAdapter _log;
        private readonly Serializer _serializer;

        public EventStoreJournal()
        {
            _log = Context.GetLogger();

            var serialization = Context.System.Serialization;
            _serializer = serialization.FindSerializerForType(typeof(IPersistentRepresentation));

            _connection = new Lazy<Task<IEventStoreConnection>>(async () =>
            {
                var settings = ConnectionSettings.Create()
                    .KeepReconnecting()
                    .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));

                var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4532);
                
                IEventStoreConnection connection = EventStoreConnection.Create(settings, endPoint, "akka.net");
                await connection.ConnectAsync();
                return connection;
            });
        }

        private Task<IEventStoreConnection> GetConnection()
        {
            return _connection.Value;
        }

        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            try
            {
                var connection = await GetConnection();

                var slice = await connection.ReadStreamEventsBackwardAsync(persistenceId, StreamPosition.End, 1, false);

                long sequence = 0;

                if (slice.Events.Any())
                    sequence = slice.Events.First().OriginalEventNumber + 1;

                return sequence;
            }
            catch (Exception e)
            {
                _log.Error(e, e.Message);
                throw;
            }
        }

        public override async Task ReplayMessagesAsync(string persistenceId, long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> replayCallback)
        {
            try
            {
                var connection = await GetConnection();

                var start = ((int) fromSequenceNr - 1);

                StreamEventsSlice slice;
                do
                {
                    slice = await connection.ReadStreamEventsForwardAsync(persistenceId, start, BatchSize, false);

                    foreach (var @event in slice.Events)
                    {
                        var representation = (IPersistentRepresentation)_serializer.FromBinary(@event.OriginalEvent.Data, typeof(IPersistentRepresentation));
                        replayCallback(representation);
                    }
                
                    start = slice.NextEventNumber;

                } while (!slice.IsEndOfStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected override async Task WriteMessagesAsync(IEnumerable<IPersistentRepresentation> messages)
        {
            var connection = await GetConnection();
            
            foreach (var grouping in messages.GroupBy(x => x.PersistenceId))
            {
                var stream = grouping.Key;

                var representations = grouping.OrderBy(x => x.SequenceNr).ToArray();
                var expectedVersion = (int)representations.First().SequenceNr - 2;

                var events = representations.Select(x =>
                {
                    var data = _serializer.ToBinary(x);
                    var eventId = GuidUtility.Create(GuidUtility.IsoOidNamespace, string.Concat(stream, x.SequenceNr));
                    
                    var meta = new byte[0];
                    return new EventData(eventId, x.GetType().FullName, true, data, meta);
                });

                await connection.AppendToStreamAsync(stream, expectedVersion < 0 ? ExpectedVersion.NoStream : expectedVersion, events);
            }
        }

        protected override Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr, bool isPermanent)
        {
            return Task.FromResult<object>(null);
        }
    }
}