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

        public FileSystemStorage()
        {
            _directory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "entities"));
            _directory.Create();
            _log = Context.GetLogger();
        }

        public void Handle(GetEntity message)
        {
            var entityType = message.EntityType;
            var fileName = GetFileName(message.Key, entityType);

            var info = new FileInfo(fileName);
            if (!info.Exists)
                Sender.Tell(new EntityNotFound(message.Key));

            using (var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                var entity = JsonConvert.DeserializeObject(json, entityType);
                _log.Info("Get entity: {0}/{1:n}", entityType.Name, message.Key);
                Sender.Tell(new EntityRetrieved(message.Key, entity));
            }
        }

        public void Handle(UpdateEntity message)
        {
            var fileName = GetFileName(message.Key, message.Entity.GetType());
            var info = new FileInfo(fileName);

            using (var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonConvert.SerializeObject(message.Entity, Formatting.Indented));
                _log.Info("Update entity: {0}/{1:n}", message.Entity.GetType().Name, message.Key);
                Sender.Tell(new EntityUpdated(message.Key, message.Entity));
            }
        }

        public void Handle(CreateNewEntity message)
        {
            var fileName = GetFileName(message.Key, message.Entity.GetType());

            var info = new FileInfo(fileName);
            if (info.Exists)
                Sender.Tell(new EntityAlreadyExists());

            Directory.CreateDirectory(info.Directory.FullName);

            using (var stream = new FileStream(info.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonConvert.SerializeObject(message.Entity, Formatting.Indented));
                _log.Info("Create entity: {0}/{1:n}", message.Entity.GetType().Name, message.Key);
                Sender.Tell(new NewEntityCreated(message.Key, message.Entity));
            }
        }

        private string GetFileName(Guid key, Type entityType)
        {
            var folder = entityType.Name.ToLowerInvariant();
            return Path.Combine(_directory.FullName, folder, string.Format("{0:n}.json", key));
        }
    }
}