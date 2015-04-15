using System;
using Akka.Actor;

namespace Core.Domain
{
    public static class AggregateFactory
    {
        public static IActorRef AccountAggregate(this IUntypedActorContext context, Guid id)
        {
            return context.ActorOf(Props.Create<Account>(id), "aggregates(account)");
        }

        public static IActorRef AccountAggregate(this ActorSystem system, Guid id, IActorRef projections)
        {
            return system.ActorOf(Props.Create<Account>(id, projections), "aggregates(account)");
        }
    }
}