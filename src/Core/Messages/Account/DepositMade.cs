using System;

namespace Core.Messages.Account
{
    public class DepositMade : IEvent
    {
        public DepositMade(Guid aggregateId, decimal amount)
        {
            AggregateId = aggregateId;
            Amount = amount;
            UtcDate = SystemClock.UtcNow;
        }

        public decimal Amount { get; private set; }
        public Guid AggregateId { get; private set; }

        public DateTime UtcDate { get; private set; }

        public override string ToString()
        {
            return string.Format("Deposited {0:c} into account {1:n}", Amount, AggregateId);
        }
    }
}