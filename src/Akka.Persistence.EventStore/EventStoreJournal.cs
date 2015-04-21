﻿using System;
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
                Console.WriteLine(e);
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
                        var json = Encoding.UTF8.GetString(@event.OriginalEvent.Data);
                        var representation = JsonConvert.DeserializeObject<IPersistentRepresentation>(json, _serializerSettings);
                        Console.Out.WriteLine("Replay: {0}", representation.Payload.GetType());
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
            Console.Out.WriteLine("BEGIN");
            foreach (var grouping in messages.GroupBy(x => x.PersistenceId))
            {
                var stream = grouping.Key;
                Console.Out.WriteLine("\t{0}", stream);

                var representations = grouping.OrderBy(x => x.SequenceNr).ToArray();
                var expectedVersion = (int)representations.First().SequenceNr - 2;

                var events = representations.Select(x =>
                {
                    Console.Out.WriteLine("\tSequenceNr: {0}", x.SequenceNr);

                    var eventId = GuidUtility.Create(GuidUtility.IsoOidNamespace, string.Concat(stream, x.SequenceNr));
                    var json = JsonConvert.SerializeObject(x, _serializerSettings);
                    var data = Encoding.UTF8.GetBytes(json);
                    var meta = new byte[0];
                    return new EventData(eventId, x.GetType().FullName, true, data, meta);
                });

                Console.Out.WriteLine("\tExpect version: {0}", expectedVersion);

                await connection.AppendToStreamAsync(stream, expectedVersion < 0 ? ExpectedVersion.NoStream : expectedVersion, events);
                Console.Out.WriteLine("END");
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