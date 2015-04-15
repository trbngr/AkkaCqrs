using System;
using Core.Messages.Account;

namespace Core.Domain
{
    internal class AccountState
    {
        public AccountState(IEventSink events)
        {
            Events = events;
        }

        public bool Created { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        internal IEventSink Events { get; set; }

        public void Apply(AccountCreated message)
        {
            Created = true;
            Name = message.Name;
        }

        public void Handle(CreateAccount message)
        {
            if (Created)
                throw new DomainException(string.Format("Account {0:n} already exists.", message.AggregateId));

            Events.Publish(new AccountCreated(message.AggregateId, message.Name));
        }

        public void Handle(ChangeAccountName message)
        {
            if (!Created)
                throw new DomainException("Can not change the name of a non-existing account.");

            Events.Publish(new AccountNameChanged(message.AggregateId, message.Name));
        }

        public void Apply(AccountNameChanged message)
        {
            Name = message.Name;
        }

        public void Handle(MakeDeposit message)
        {
            if(message.Amount < 0)
                throw new NegativeDepositException();

            Events.Publish(new DepositMade(message.AggregateId, message.Amount));

        }

        public void Apply(DepositMade message)
        {
            Balance += message.Amount;
        }

        public void Handle(MakeWithdrawal message)
        {
            if(Balance < Math.Abs(message.Amount))
                throw new InsufficientFundsException(Balance);

            Events.Publish(new WithdrawalMade(message.AggregateId, message.Amount, Balance - message.Amount));
        }

        public void Apply(WithdrawalMade message)
        {
            Balance -= Math.Abs(message.Amount);
        }
    }

    public class NegativeDepositException : DomainException
    {
        public NegativeDepositException() : base("Can not make a negative deposit. Did you mean to withdraw the funds?")
        {
        }
    }

    public class InsufficientFundsException : DomainException
    {
        public InsufficientFundsException(decimal balance)
            : base(string.Format("Insufficient funds available: {0:c}", balance))
        {
        }
    }
}