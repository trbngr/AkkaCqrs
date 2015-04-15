using System;

namespace Core
{
    public interface IEvent
    {
        Guid AggregateId { get; }
        DateTime UtcDate { get; }
    }
}