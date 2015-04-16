using System;

namespace Core.Messages.Account
{
    public class AccountNameChanged : Event
    {
        public AccountNameChanged(Guid aggregateId, string name) : base(aggregateId)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public override string ToString()
        {
            return string.Format("Account with id {0:n} has had its name changed to {1}", AggregateId, Name);
        }
    }
}