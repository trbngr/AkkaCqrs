using System;

namespace Core.Messages.Account
{
    public class AccountCreated : IEvent
    {
        public Guid AggregateId { get; private set; }
        public string Name { get; private set; }

        public AccountCreated(Guid aggregateId, string name)
        {
            AggregateId = aggregateId;
            Name = name;
            UtcDate = SystemClock.UtcNow;
        }

        public override string ToString()
        {
            return string.Format("Account {0} created with id {1:n}", Name, AggregateId);
        }

        public DateTime UtcDate { get; private set; }
    }
}