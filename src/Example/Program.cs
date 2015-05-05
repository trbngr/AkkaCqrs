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
            system.EventStream.Subscribe(stats, typeof (IEvent));
            
            IActorRef actor = system.ActorOf<AccountWorker>();
            
            Console.Clear();
            Console.Out.Write("Enter a new account name: ");
            actor.Tell(new CreateAccount(Guid.NewGuid(), Console.ReadLine()));
            
            system.AwaitTermination();
        }
    }
}
