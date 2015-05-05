using System;
using System.Threading.Tasks;
using Akka.Actor;
using Core;
using Core.Domain;
using Core.Messages;
using Core.Messages.Account;

namespace Example
{
    public class AccountWorker : ReceiveActor
    {
        private IActorRef _account;
        private Guid _id;

        public AccountWorker()
        {
            Context.System.EventStream.Subscribe(Self, typeof (IEvent));

            Receive<CreateAccount>(x =>
            {
                _id = x.AggregateId;
                _account = Context.System.AccountAggregate(x.AggregateId);

                _account.Tell(x);

                Become(Created);
            });
        }

        private void Created()
        {
            Receive<ExitApp>(x => Context.System.Shutdown());

            Receive<DomainException>(async x =>
            {
                Console.Out.WriteLine("ERROR: {0}", x.Message);
                Console.Out.WriteLine("Press [Enter] to continue.");
                Console.ReadLine();
                await RunOp();
            });

            Receive<ExitApp>(x => Context.System.Shutdown());

            Receive<IEvent>(async x => await RunOp());
        }

        private async Task RunOp()
        {
            var response = await _account.Ask<CurrentBalanceResponse>(new GetCurrentBalance(_id));
            Console.Clear();
            Console.Out.WriteLine("Current Balance: {0:C}", response.Balance);
            Console.Out.WriteLine("-------------");
            Console.Out.WriteLine("1: Withdrawal");
            Console.Out.WriteLine("2: Deposit");
            Console.Out.WriteLine("3: Change Name");
            Console.Out.WriteLine("4: Exit");
            int choice;
            if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out choice))
            {
                switch (choice)
                {
                    case 1:
                    {
                        Console.Write("Amount?: ");
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var amount = decimal.Parse(Console.ReadLine());
                        _account.Tell(new MakeWithdrawal(_id, amount));
                        break;
                    }
                    case 2:
                    {
                        Console.Write("Amount?: ");
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var amount = decimal.Parse(Console.ReadLine());
                        _account.Tell(new MakeDeposit(_id, amount));
                        break;
                    }
                    case 3:
                    {
                        Console.Write("New account name?: ");
                        var name = Console.ReadLine();
                        _account.Tell(new ChangeAccountName(_id, name));
                        break;
                    }
                    case 4:
                    {
                        Self.Tell(new ExitApp());
                        break;
                    }
                }

                _account.Tell(SaveAggregate.Message);
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(maxNrOfRetries: 10, withinTimeRange: TimeSpan.FromSeconds(30), decider: new LocalOnlyDecider(
                e =>
                {
                    if (e is DomainException || e.InnerException is DomainException)
                    {
                        Self.Tell((DomainException)e);
                        return Directive.Resume;
                    }

                    return Directive.Stop;
                }));
        }

        class ExitApp { }
    }
}