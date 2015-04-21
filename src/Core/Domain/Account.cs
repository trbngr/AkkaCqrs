using Akka.Actor;
using Core.Extensions;
using Core.Messages.Account;

namespace Core.Domain
{
    public class Account : AggregateRootActor
    {
        private AccountState _state;

        public Account(AggregateRootCreationParameters parameters)
            : base(parameters)
        {
            _state = new AccountState(this);
        }

        protected override bool Handle(ICommand command)
        {
            if (command.ReadMessage<GetCurrentBalance>().HasValue)
            {
                Sender.Tell(new CurrentBalanceResponse(command.AggregateId, _state.Balance));
                return true;
            }

            _state.Handle(command);
            return true;
        }

        protected override bool Apply(IEvent @event)
        {
            _state.Mutate(@event);
            return true;
        }

        protected override bool RecoverState(object state)
        {
            if (state.CanHandle<AccountState>(x =>
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