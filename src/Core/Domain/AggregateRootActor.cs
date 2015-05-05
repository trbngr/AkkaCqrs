using System;
using System.Collections.Generic;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Core.Messages;

namespace Core.Domain
{
    public class AggregateRootCreationParameters
    {
        public AggregateRootCreationParameters(Guid id, IActorRef projections, int snapshotThreshold = 250)
        {
            Id = id;
            Projections = projections;
            SnapshotThreshold = snapshotThreshold;
        }

        public Guid Id { get; private set; }
        public IActorRef Projections { get; private set; }
        public int SnapshotThreshold { get; private set; }
    }

    public abstract class AggregateRootActor : PersistentActor, IEventSink
    {
        private readonly Guid _id;
        private readonly IActorRef _projections;
        private readonly int _snapshotThreshold;
        private readonly ILoggingAdapter _log;

        protected AggregateRootActor(AggregateRootCreationParameters parameters)
        {
            _id = parameters.Id;
            _projections = parameters.Projections;
            _snapshotThreshold = parameters.SnapshotThreshold;

            _log = Context.GetLogger();
        }

        public override string PersistenceId
        {
            get { return string.Format("{0}-agg-{1:n}", GetType().Name, _id).ToLowerInvariant(); }
        }

        private long LastSnapshottedVersion { get; set; }

        void IEventSink.Publish(IEvent @event)
        {
            Persist(@event, e =>
            {
                Apply(e);
                _projections.Tell(@event);
            });
        }

        protected override bool ReceiveRecover(object message)
        {
            return message.Match()
                .With<RecoveryCompleted>(x =>
                {
                    _log.Debug("Recovered state to version {0}", LastSequenceNr);
                })
                .With<SnapshotOffer>(offer =>
                {
                    _log.Debug("State loaded from snapshot");
                    LastSnapshottedVersion = offer.Metadata.SequenceNr;
                    RecoverState(offer.Snapshot);
                })
                .With<IEvent>(x => Apply(x))
                .WasHandled;
        }

        protected override bool ReceiveCommand(object message)
        {
            return message.Match()
                .With<SaveAggregate>(x => Save())
                .With<ICommand>(command =>
                {
                    try
                    {
                        var handled = Handle(command);
                        Sender.Tell(new CommandResponse(handled));
                    }
                    catch (DomainException e)
                    {
                        Sender.Tell(e);
                    }
                }).WasHandled;
        }

        private bool Save()
        {
            if ((LastSequenceNr - LastSnapshottedVersion) >= _snapshotThreshold)
                SaveSnapshot(GetState());

            return true;
        }

        protected abstract bool Handle(ICommand command);
        protected abstract bool Apply(IEvent @event);
        protected abstract void RecoverState(object state);
        protected abstract object GetState();
    }
}