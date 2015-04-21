using System;
using Akka.Actor;
using Core;
using Core.Messages.Account;

namespace Example
{
    internal class Program
    {
        private static void Main()
        {
            var system = ActorSystem.Create(SystemData.SystemName);
            
            var stats = system.ActorOf(Props.Create<StatsActor>(), "statscollector");
            var opWatch = system.ActorOf<OperationWatcher>("operations");
            system.EventStream.Subscribe(stats, typeof (IEvent));
            system.EventStream.Subscribe(opWatch, typeof(IEvent));

            IActorRef actor = system.ActorOf<AccountWorker>();
            
            Console.Clear();
            Console.Out.Write("Enter a new account name: ");
            var name = Console.ReadLine();
            actor.Tell(new CreateAccount(Guid.NewGuid(), name));
            
            system.AwaitTermination();
        }

        class OperationWatcher : ReceiveActor
        {
            public OperationWatcher()
            {
                Receive<IEvent>(x =>
                {
                    Console.Out.WriteLine(x);
                });
            }
        }
    }
}
