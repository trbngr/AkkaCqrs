using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Core.Extensions;
using Core.Messages;

namespace Core.Domain
{
    public abstract class AggregateRoot : PersistentActor, IEventSink
    {
        private readonly IActorRef _projections;
        private readonly ICollection<Exception> _exceptions;
        private readonly ILoggingAdapter _log;

        protected AggregateRoot(Guid id, IActorRef projections)
        {
            _projections = projections;
            Id = id;
            _exceptions = new List<Exception>();
            _log = Context.GetLogger();
        }

        public override string PersistenceId
        {
            get { return string.Format("{0}-agg-{1:n}", GetType().Name, Id).ToLowerInvariant(); }
        }

        public Guid Id { get; private set; }

        void IEventSink.Publish(IEvent @event)
        {
            Persist(@event, e =>
            {
                Apply(e);
                _projections.Tell(e);
            });
        }

        protected override bool ReceiveRecover(object message)
        {
            if (message.Recieve<RecoveryCompleted>(x =>
            {
                _log.Debug("Recovered state to version {0}", LastSequenceNr);
            }))
                return true;

            var snapshot = message.ReadMessage<SnapshotOffer>();
            if (snapshot.HasValue)
            {
                var offer = snapshot.Value;
                _log.Debug("State loaded from snapshot");
                return RecoverState(offer.Snapshot);
            }

            if (message.Recieve<IEvent>(@event =>
            {
                Apply(@event);
            }))
                return true;

            return false;
        }

        protected override bool ReceiveCommand(object message)
        {
            if (message.Recieve<SaveAggregate>(x => Save()))
                return true;

            if (message.Recieve<ICommand>(command =>
            {
                var handled = HandleAndRecordExceptions(command);
                Sender.Tell(new CommandResponse(handled, _exceptions));
                return handled;
            }))
                return true;

            return false;
        }

        private bool Save()
        {
//            if ((Version - LastSequenceNr) > 2)
                SaveSnapshot(GetState());

            return true;
        }

        private bool HandleAndRecordExceptions(ICommand command)
        {
            try
            {
                return Handle(command);
            }
            catch (Exception e)
            {
                _exceptions.Add(e);
            }

            return false;
        }

        protected abstract bool Handle(ICommand command);
        protected abstract bool Apply(IEvent @event);
        protected abstract bool RecoverState(object state);
        protected abstract object GetState();
    }
}