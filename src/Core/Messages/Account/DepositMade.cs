using System;

namespace Core.Messages.Account
{
    public class DepositMade : Event
    {
        public DepositMade(Guid aggregateId, decimal amount) : base(aggregateId)
        {
            Amount = amount;
        }

        public decimal Amount { get; private set; }

        public override string ToString()
        {
            return string.Format("Deposited {0:c} into account {1:n}", Amount, AggregateId);
        }
    }
}