using System;

namespace Core.Messages.Account
{
    public class CreateAccount : ICommand
    {
        public CreateAccount(Guid aggregateId, string name)
        {
            AggregateId = aggregateId;
            Name = name;
        }

        public Guid AggregateId { get; private set; }
        public string Name { get; private set; }
    }
}