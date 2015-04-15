using System;
using Akka.Actor;
using Akka.Event;
using Core.Storage.FileSystem;
using Core.Storage.Messages;

namespace Core.Storage.Projections.Account
{
    public class AccountCreated : TypedActor, IHandle<Core.Messages.Account.AccountCreated>, IHandle<EntityAlreadyExists>, IHandle<NewEntityCreated>
    {
        private readonly IActorRef _storage;
        private readonly ILoggingAdapter _log;

        public AccountCreated()
        {
            _storage = Context.ActorOf(Props.Create<FileSystemStorage>(), SystemData.StorageActor.Name);
            _log = Context.GetLogger();
        }

        public void Handle(Core.Messages.Account.AccountCreated message)
        {
            _storage.Tell(new CreateNewEntity(message.AggregateId, new Entities.Account
            {
                Name = message.Name
            }));
        }

        public void Handle(EntityAlreadyExists message)
        {
            throw new Exception("Entity already exists");
        }

        public void Handle(NewEntityCreated message)
        {
            _log.Info("Entity {0}/{1:n} created.", message.Entity.GetType().Name, message.Key);
        }
    }
}