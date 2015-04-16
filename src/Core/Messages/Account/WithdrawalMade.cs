using System;

namespace Core.Messages.Account
{
    public class WithdrawalMade : Event
    {
        public WithdrawalMade(Guid aggregateId, decimal amount, decimal newBalance) : base(aggregateId)
        {
            Amount = amount;
            NewBalance = newBalance;
        }

        public decimal Amount { get; private set; }
        public decimal NewBalance { get; private set; }

        public override string ToString()
        {
            return string.Format("Withdrew {0:c} from account {1:n}", Amount, AggregateId);
        }
    }
}