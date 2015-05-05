using Akka;
using Akka.Actor;
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
            var handled = command.Match().With<GetCurrentBalance>(_ =>
            {
                Sender.Tell(new CurrentBalanceResponse(command.AggregateId, _state.Balance));
            }).WasHandled;

            if (!handled)
            {
                _state.Handle(command);
            }

            return true;
        }

        protected override bool Apply(IEvent @event)
        {
            _state.Mutate(@event);
            return true;
        }

        protected override void RecoverState(object state)
        {
            state.Match()
                .With<AccountState>(x =>
                {
                    x.Events = this;
                    _state = x;
                });
        }

        protected override object GetState()
        {
            return _state;
        }
    }
}