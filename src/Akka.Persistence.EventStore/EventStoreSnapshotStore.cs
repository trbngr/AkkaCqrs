using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Akka.Persistence;
using Akka.Persistence.Serialization;
using Akka.Persistence.Snapshot;
using Akka.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;

namespace EventStore.Persistence
{
    public class EventStoreSnapshotStore : SnapshotStore
    {
        private readonly Lazy<Task<IEventStoreConnection>> _connection;

        private readonly Serializer _serializer;

        public EventStoreSnapshotStore()
        {
            var serialization = Context.System.Serialization;
            _serializer = serialization.FindSerializerForType(typeof(SelectedSnapshot));

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

        protected override async Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            var connection = await GetConnection();
            var streamName = GetStreamName(persistenceId);
            var slice = await connection.ReadStreamEventsBackwardAsync(streamName, StreamPosition.End, 1, false);

            if (slice.Status == SliceReadStatus.StreamNotFound)
            {
                await connection.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, StreamMetadata.Data);
                return null;
            }

            if (slice.Events.Any())
            {
                var @event = slice.Events.First().OriginalEvent;

                var snapshot = (SelectedSnapshot)_serializer.FromBinary(@event.Data, typeof(SelectedSnapshot));

                return snapshot;
            }

            return null;
        }

        private static string GetStreamName(string persistenceId)
        {
            return string.Format("snapshot-{0}", persistenceId);
        }

        protected override async Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            var connection = await GetConnection();
            var streamName = GetStreamName(metadata.PersistenceId);
            var data = _serializer.ToBinary(new SelectedSnapshot(metadata, snapshot));
            var eventData = new EventData(Guid.NewGuid(), typeof(Snapshot).Name, false, data, new byte[0]);

            await connection.AppendToStreamAsync(streamName, ExpectedVersion.Any, eventData);
        }

        protected override void Saved(SnapshotMetadata metadata)
        {}

        protected override void Delete(SnapshotMetadata metadata)
        {}

        protected override void Delete(string persistenceId, SnapshotSelectionCriteria criteria)
        {}

        
        public class StreamMetadata
        {
            [JsonProperty("$maxCount")]
            public int MaxCount = 1;

            private StreamMetadata()
            {
            }

            private static readonly StreamMetadata Instance = new StreamMetadata();

            public static byte[] Data
            {
                get
                {
                    return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Instance));
                }
            }
        }
    }
}