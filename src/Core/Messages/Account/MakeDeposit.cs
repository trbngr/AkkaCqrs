using System;

namespace Core.Messages.Account
{
    public class MakeDeposit : ICommand
    {
        public MakeDeposit(Guid aggregateId, decimal amount)
        {
            AggregateId = aggregateId;
            Amount = amount;
        }

        public decimal Amount { get; private set; }
        public Guid AggregateId { get; private set; }
    }
}