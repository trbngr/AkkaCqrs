using System;
using Akka.Actor;
using Core.Extensions;
using Core.Messages.Account;

namespace Core.Domain
{
    public class Account : AggregateRoot
    {
        private AccountState _state;

        public Account(Guid id, IActorRef projections)
            : base(id, projections)
        {
            _state = new AccountState(this);
        }

        protected override bool Handle(ICommand command)
        {
            if (command.Recieve<CreateAccount>(x => _state.Handle(x)))
                return true;

            if (command.Recieve<ChangeAccountName>(x => _state.Handle(x)))
                return true;

            if (command.Recieve<MakeDeposit>(x => _state.Handle(x)))
                return true;

            if (command.Recieve<MakeWithdrawal>(x => _state.Handle(x)))
                return true;

            if (command.ReadMessage<GetCurrentBalance>().HasValue)
            {
                Sender.Tell(new CurrentBalanceResponse(command.AggregateId, _state.Balance));
                return true;
            }
            
            return false;
        }

        protected override bool Apply(IEvent @event)
        {
            if (@event.Recieve<AccountCreated>(x => _state.Apply(x)))
                return true;

            if (@event.Recieve<AccountNameChanged>(x => _state.Apply(x)))
                return true;

            if (@event.Recieve<WithdrawalMade>(x => _state.Apply(x)))
                return true;

            if (@event.Recieve<DepositMade>(x => _state.Apply(x)))
                return true;

            return false;
        }

        protected override bool RecoverState(object state)
        {
            if (state.Recieve<AccountState>(x =>
            {
                x.Events = this;
                _state = x;
            }))
                return true;

            return false;
        }

        protected override object GetState()
        {
            return _state;
        }
    }
}