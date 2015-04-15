using System;

namespace Core
{
    public interface ICommand 
    {
        Guid AggregateId { get; }
    }
}