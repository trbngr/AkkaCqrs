using System;
using System.IO;
using Akka.Actor;
using Akka.Event;
using Core.Storage.FileSystem;
using Core.Storage.Messages;

namespace Core.Storage.Projections.Account
{
    public class WithdrawalMade : ReceiveActor, IWithUnboundedStash
    {
        private readonly IActorRef _storage;
        private Entities.Account _accountEntity;
        private readonly ILoggingAdapter _log;

        public WithdrawalMade()
        {
            _storage = Context.ActorOf(Props.Create<FileSystemStorage>(), SystemData.StorageActor.Name);
            BecomeStacked(RetrievingEntity);
            _log = Context.GetLogger();
        }

        private void RetrievingEntity()
        {
            Receive<Core.Messages.Account.WithdrawalMade>(x =>
            {
                _log.Info("Retrieve account entity");
                Stash.Stash();
                _storage.Tell(new GetEntity(x.AggregateId, typeof(Entities.Account)));
            });

            Receive<EntityRetrieved>(x =>
            {
                _log.Info("Account entity retrieved");
                _accountEntity = (Entities.Account) x.Entity;
                BecomeStacked(EntityAvailable);
                Stash.Unstash();
            });
        }

        private void EntityAvailable()
        {
            Receive<Core.Messages.Account.WithdrawalMade>(x =>
            {
                _log.Info("Update account entity");
                _accountEntity.Balance -= Math.Abs(x.Amount);
                _storage.Tell(new UpdateEntity(x.AggregateId, _accountEntity));
            });

            Receive<EntityUpdated>(x =>
            {
                _log.Info("Account updated.");
            });
        }

        public IStash Stash { get; set; }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(maxNrOfRetries: 10, withinTimeRange: TimeSpan.FromSeconds(30), decider: new LocalOnlyDecider(
                e =>
                {
                    _log.Info("{0}", e.GetType().Name);

                    if (e is IOException)
                        return Directive.Restart;

                    return Directive.Stop;
                }));
        }
    }
}