using System;

namespace Core.Messages.Account
{
    public class AccountNameChanged : IEvent
    {
        public Guid AggregateId { get; private set; }
        public string Name { get; private set; }

        public AccountNameChanged(Guid aggregateId, string name)
        {
            AggregateId = aggregateId;
            Name = name;
            UtcDate = SystemClock.UtcNow;
        }

        public override string ToString()
        {
            return string.Format("Account with id {0:n} has had its name changed to {1}", AggregateId, Name);
        }

        public DateTime UtcDate { get; private set; }
    }
}