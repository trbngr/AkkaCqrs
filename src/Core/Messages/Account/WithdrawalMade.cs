using System;

namespace Core.Messages.Account
{
    public class WithdrawalMade : IEvent
    {
        public WithdrawalMade(Guid aggregateId, decimal amount, decimal newBalance)
        {
            AggregateId = aggregateId;
            Amount = amount;
            NewBalance = newBalance;
            UtcDate = SystemClock.UtcNow;
        }

        public decimal Amount { get; private set; }
        public decimal NewBalance { get; private set; }
        public Guid AggregateId { get; private set; }

        public DateTime UtcDate { get; private set; }

        public override string ToString()
        {
            return string.Format("Withdrew {0:c} from account {1:n}", Amount, AggregateId);
        }
    }
}