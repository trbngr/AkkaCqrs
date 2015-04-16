using System;

namespace Core.Messages.Account
{
    public class AccountCreated : Event
    {
        public string Name { get; private set; }

        public AccountCreated(Guid aggregateId, string name) : base(aggregateId)
        {
            Name = name;
        }

        public override string ToString()
        {
            return string.Format("Account {0} created with id {1:n}", Name, AggregateId);
        }
    }
}