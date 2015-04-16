using System;
using Akka.Actor;
using Core;
using Core.Domain;
using Core.Messages;
using Core.Messages.Account;

namespace Example
{
    internal class Program
    {
        private static void Main()
        {
            var system = ActorSystem.Create(SystemData.SystemName);

            var id = Guid.Parse("7a609341bf8a45f4805ff3ad775af851");

            var stats = system.ActorOf(Props.Create<StatsActor>());
            system.EventStream.Subscribe(stats, typeof (IEvent));

            var actor = system.AccountAggregate(id);

            actor.Tell(new CreateAccount(id, "savings"));
            actor.Tell(new MakeDeposit(id, 40));
            actor.Tell(new MakeWithdrawal(id, 20));
//            actor.Tell(new MakeWithdrawal(id, 20));
//            actor.Tell(new MakeDeposit(id, 100));
//            actor.Tell(new MakeWithdrawal(id, 20));
//            actor.Tell(new MakeWithdrawal(id, 20));
//
//            actor.Tell(new MakeWithdrawal(id, 20));
//            actor.Tell(new MakeDeposit(id, 1500));
//            var response = actor.Ask<CurrentBalanceResponse>(new GetCurrentBalance(id)).Result;
//            Console.Out.WriteLine("Current balance is: {0:c}", response.Balance);

//            WriteResponse(actor.Ask<CommandResponse>(new ChangeAccountName(id, "checking")).Result);

            actor.Tell(SaveAggregate.Message);


            Console.Out.WriteLine("Press Enter to exit");
            Console.ReadLine();
            system.Shutdown();
        }

        private static void WriteResponse(CommandResponse response)
        {
            if (response == null)
                return;

            var exception = response.Exception;
            if (exception != null)
            {
                exception.Flatten().Handle(x =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(x.Message);
                    Console.ResetColor();
                    return true;
                });
            }
        }
    }
}