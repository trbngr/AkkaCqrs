using System;
using System.IO;
using Akka.Actor;
using Akka.Event;
using Core.Storage.Messages;
using Newtonsoft.Json;

namespace Core.Storage.FileSystem
{
    public class FileSystemStorage : TypedActor, IStorageProvider
    {
        private readonly DirectoryInfo _directory;
        private readonly ILoggingAdapter _log;
        private readonly JsonSerializerSettings _serializerSettings;

        public FileSystemStorage()
        {
            _directory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "entities"));
            _directory.Create();
            _log = Context.GetLogger();

            _serializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.Indented
            };
        }

        public void Handle(GetEntity message)
        {
            var entityType = message.EntityType;
            var fileName = GetFileName(message.Key, entityType);

            var info = new FileInfo(fileName);
            if (!info.Exists)
                Sender.Tell(new EntityNotFound(message.Key));

            _log.Info("Get entity: {0}/{1:n}", entityType.Name, message.Key);

            object entity;
            using (var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            using (var reader = new StreamReader(stream))
            {
                entity = JsonConvert.DeserializeObject(reader.ReadToEnd(), entityType, _serializerSettings);
            }

            Sender.Tell(new EntityRetrieved(message.Key, entity));
        }

        public void Handle(UpdateEntity message)
        {
            var fileName = GetFileName(message.Key, message.Entity.GetType());
            var info = new FileInfo(fileName);

            using (var file = new FileStream(info.FullName, FileMode.Open, FileAccess.Write, FileShare.None))
                WriteToFile(file, message.Entity);

            _log.Info("Update entity: {0}/{1:n}", message.Entity.GetType().Name, message.Key);
            Sender.Tell(new EntityUpdated(message.Key, message.Entity));
        }

        public void Handle(CreateNewEntity message)
        {
            var fileName = GetFileName(message.Key, message.Entity.GetType());

            var info = new FileInfo(fileName);
            if (info.Exists)
                Sender.Tell(new EntityAlreadyExists());

            _log.Info("Create entity: {0}/{1:n}", message.Entity.GetType().Name, message.Key);

            Directory.CreateDirectory(info.Directory.FullName);

            using (var file = new FileStream(info.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                WriteToFile(file, message.Entity);

            Sender.Tell(new NewEntityCreated(message.Key, message.Entity));
        }

        private string GetFileName(Guid key, Type entityType)
        {
            var folder = entityType.Name.ToLowerInvariant();
            return Path.Combine(_directory.FullName, folder, string.Format("{0:n}.json", key));
        }

        private void WriteToFile(FileStream file, object entity)
        {
            using (var destination = new MemoryStream())
            {
                Serialize(entity, destination);
                byte[] data = destination.ToArray();
                file.Seek(0, SeekOrigin.Begin);
                file.Write(data, 0, data.Length);
                // truncate this file
                file.SetLength(data.Length);
            }
        }

        private void Serialize(object instance, Stream destination)
        {
            var sw = new StreamWriter(destination);
            sw.Write(JsonConvert.SerializeObject(instance, _serializerSettings));
            sw.Flush();
        }
    }
}