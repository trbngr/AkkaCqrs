using System;
using System.Threading.Tasks;
using Akka.Actor;
using Autofac;
using Core;
using Core.Domain;
using Core.Messages;
using Core.Messages.Account;
using Core.Storage.FileSystem;
using Core.Storage.Projections;
using Nito.AsyncEx;

namespace Example
{
    internal class Program
    {
        private static void Main()
        {
            var system = ActorSystem.Create(SystemData.SystemName);

            var id = Guid.Parse("1c765aa2553c42c9a2f4dfb87cc197ee");

            var projections = system.ActorOf(Props.Create<ReadModelProjections>(), SystemData.ProjectionsActor.Name);

            var actor = system.AccountAggregate(id, projections);

            actor.Tell(new CreateAccount(id, "savings"));
//            actor.Tell(new MakeDeposit(id, 40));
//            actor.Tell(new MakeWithdrawal(id, 20));
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