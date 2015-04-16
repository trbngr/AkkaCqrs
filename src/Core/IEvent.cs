using System;
using Akka.Routing;

namespace Core
{
    public interface IEvent
    {
        Guid AggregateId { get; }
        DateTime UtcDate { get; }
    }

    public abstract class Event : IEvent, IConsistentHashable
    {
        protected Event(Guid aggregateId)
        {
            AggregateId = aggregateId;
            UtcDate = SystemClock.UtcNow;
        }

        public Guid AggregateId { get; private set; }

        public DateTime UtcDate { get; private set; }

        object IConsistentHashable.ConsistentHashKey
        {
            get { return AggregateId; }
        }
    }
}