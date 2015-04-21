using System;
using Akka.Actor;
using Akka.Routing;
using Core.Storage.Projections;

namespace Core.Domain
{
    public static class AggregateFactory
    {
        public static IActorRef AccountAggregate(this ActorSystem system, Guid id, int snapshotThreshold = 250)
        {
            var projectionsProps = new ConsistentHashingPool(5).Props(Props.Create<ReadModelProjections>());

            var projections = system.ActorOf(projectionsProps, SystemData.ProjectionsActor.Name);

            var creationParams = new AggregateRootCreationParameters(id, projections, snapshotThreshold);

            return system.ActorOf(Props.Create<Account>(creationParams), "aggregates(account)");
        }
    }
}