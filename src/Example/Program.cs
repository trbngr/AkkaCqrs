using System;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Core;
using Core.Domain;
using Core.Messages;
using Core.Messages.Account;
using Debug = System.Diagnostics.Debug;

namespace Example
{
    internal class Program
    {
        private static void Main()
        {
            var system = ActorSystem.Create(SystemData.SystemName);

            var id = Guid.Parse("bd75ef4da4b44330b2a0683895572055");

            var stats = system.ActorOf(Props.Create<StatsActor>(), "statscollector");
            system.EventStream.Subscribe(stats, typeof (IEvent));

            var actor = system.AccountAggregate(id);

//            actor.Tell(new CreateAccount(id, "savings"));

            Thread.Sleep(500);
            actor.Tell(new MakeDeposit(id, 40));

            Thread.Sleep(500);
            actor.Tell(new MakeWithdrawal(id, 20));

            Thread.Sleep(500);
            actor.Tell(new MakeWithdrawal(id, 20));

            Thread.Sleep(500);
            actor.Tell(new MakeDeposit(id, 100));

            Thread.Sleep(500);
            actor.Tell(new MakeWithdrawal(id, 20));

            Thread.Sleep(500);
            actor.Tell(new MakeWithdrawal(id, 20));

            Thread.Sleep(500);
            actor.Tell(new MakeWithdrawal(id, 20));

            Thread.Sleep(500);
            actor.Tell(new MakeDeposit(id, 1500));

            var response = actor.Ask<CurrentBalanceResponse>(new GetCurrentBalance(id)).Result;

            Console.Out.WriteLine("Current balance is: {0:c}", response.Balance);
//            Debug.Assert(response.Balance == 1540m);

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
