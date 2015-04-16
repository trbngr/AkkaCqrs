using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence;
using Akka.Persistence.Journal;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

namespace EventStore.Persistence
{
    public class EventStoreJournal : AsyncWriteJournal
    {
        private const int BatchSize = 500;
        private readonly Lazy<Task<IEventStoreConnection>> _connection;
        private readonly JsonSerializerSettings _serializerSettings;

        public EventStoreJournal()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                Formatting = Formatting.Indented,
                Converters =
                {
                    new ActorRefConverter(Context)
                }
            };

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
            var connection = await GetConnection();

            var slice = await connection.ReadStreamEventsBackwardAsync(persistenceId, StreamPosition.End, 1, false);

            long sequence = 0;

            if (slice.Events.Any())
                sequence = slice.Events.First().OriginalEventNumber + 1;

            return sequence;
        }

        public override async Task ReplayMessagesAsync(string persistenceId, long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> replayCallback)
        {
            var connection = await GetConnection();

            var sequenceNr = ((int)fromSequenceNr -1);
            if (sequenceNr < 0)
                sequenceNr = 0;

            StreamEventsSlice slice;
            do
            {
                slice = await connection.ReadStreamEventsForwardAsync(persistenceId, sequenceNr, BatchSize, false);

                foreach (var @event in slice.Events)
                {
                    var json = Encoding.UTF8.GetString(@event.OriginalEvent.Data);
                    var representation = JsonConvert.DeserializeObject<IPersistentRepresentation>(json, _serializerSettings);
                    replayCallback(representation);
                }
                
                sequenceNr = slice.NextEventNumber;

            } while (!slice.IsEndOfStream);
        }

        protected override async Task WriteMessagesAsync(IEnumerable<IPersistentRepresentation> messages)
        {
            var connection = await GetConnection();

            foreach (var grouping in messages.GroupBy(x => x.PersistenceId))
            {
                var persistenceId = grouping.Key;

                var representations = grouping.OrderBy(x => x.SequenceNr).ToArray();
                var expectedVersion = (int)representations.Last().SequenceNr - 2;

                var events = representations.Select(x =>
                {
                    var eventId = GuidUtility.Create(GuidUtility.IsoOidNamespace, string.Concat(persistenceId, x.SequenceNr));
                    var json = JsonConvert.SerializeObject(x, _serializerSettings);
                    var data = Encoding.UTF8.GetBytes(json);
                    var meta = new byte[0];
                    return new EventData(eventId, x.GetType().FullName, true, data, meta);
                });

                await connection.AppendToStreamAsync(persistenceId, expectedVersion, events);
            }
        }

        protected override Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr, bool isPermanent)
        {
            return Task.FromResult<object>(null);
        }

        class ActorRefConverter : JsonConverter
        {
            private readonly IActorContext _context;

            public ActorRefConverter(IActorContext context)
            {
                _context = context;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((IActorRef)value).Path.ToStringWithAddress());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var value = reader.Value.ToString();

                ActorSelection selection = _context.ActorSelection(value);
                return selection.Anchor;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof (IActorRef).IsAssignableFrom(objectType);
            }
        }
    }
}