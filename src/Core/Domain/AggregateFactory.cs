using System;
using Akka.Actor;
using Akka.Routing;
using Core.Storage.Projections;

namespace Core.Domain
{
    public static class AggregateFactory
    {
        public static IActorRef AccountAggregate(this IUntypedActorContext context, Guid id)
        {
            return context.ActorOf(Props.Create<Account>(id), "aggregates(account)");
        }

        public static IActorRef AccountAggregate(this ActorSystem system, Guid id)
        {
            var projectionsProps = new ConsistentHashingPool(2).Props(Props.Create<ReadModelProjections>());

            var projections = system.ActorOf(projectionsProps, SystemData.ProjectionsActor.Name);

            return system.ActorOf(Props.Create<Account>(id, projections), "aggregates(account)");
        }
    }
}