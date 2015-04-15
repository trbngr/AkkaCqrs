using System;

namespace Core.Messages.Account
{
    public class MakeWithdrawal : ICommand
    {
        public MakeWithdrawal(Guid aggregateId, decimal amount)
        {
            AggregateId = aggregateId;
            Amount = amount;
        }

        public decimal Amount { get; private set; }
        public Guid AggregateId { get; private set; }
    }

    public class GetCurrentBalance : ICommand
    {
        public GetCurrentBalance(Guid aggregateId)
        {
            AggregateId = aggregateId;
        }

        public Guid AggregateId { get; private set; }
    }

    public class CurrentBalanceResponse
    {
        public CurrentBalanceResponse(Guid aggregateId, decimal balance)
        {
            AggregateId = aggregateId;
            Balance = balance;
        }

        public decimal Balance { get; private set; }
        public Guid AggregateId { get; private set; }
    }
}