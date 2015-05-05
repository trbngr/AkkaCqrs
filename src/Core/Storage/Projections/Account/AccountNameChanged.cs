using Akka.Actor;
using Akka.Event;
using Core.Storage.FileSystem;
using Core.Storage.Messages;

namespace Core.Storage.Projections.Account
{
    public class AccountNameChanged : ReceiveActor, IWithUnboundedStash
    {
        private readonly IActorRef _storage;
        private readonly ILoggingAdapter _log;
        private Entities.Account _entity;

        public AccountNameChanged()
        {
            _storage = Context.ActorOf(Props.Create<FileSystemStorage>(), SystemData.StorageActor.Name);
            _log = Context.GetLogger();
            Become(RetrievingEntity);
        }

        private void RetrievingEntity()
        {
            Receive<Core.Messages.Account.AccountNameChanged>(x =>
            {
                _log.Info("Retrieve account entity");
                Stash.Stash();
                _storage.Tell(new GetEntity(x.AggregateId, typeof(Entities.Account)));
            });

            Receive<EntityRetrieved>(x =>
            {
                _log.Info("Account entity retrieved");
                _entity = (Entities.Account)x.Entity;
                Become(EntityAvailable);
                Stash.Unstash();
            });
        }

        private void EntityAvailable()
        {
            Receive<Core.Messages.Account.AccountNameChanged>(x =>
            {
                _log.Info("Update account entity");
                _entity.Name = x.Name;
                _storage.Tell(new UpdateEntity(x.AggregateId, _entity));
            });

            Receive<EntityUpdated>(x =>
            {
                _log.Info("Account updated.");
                Become(RetrievingEntity);
            });
        }

        public IStash Stash { get; set; }
    }
}