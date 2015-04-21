using System;
using System.IO;
using Akka.Actor;

namespace Core.Storage.Projections
{
    public class ReadModelProjectionSupervisorStrategy : OneForOneStrategy
    {
        private ReadModelProjectionSupervisorStrategy() : base(maxNrOfRetries: 10, withinTimeRange: TimeSpan.FromSeconds(30), decider: new LocalOnlyDecider(
                e =>
                {
                    Console.Out.WriteLine("ERROR: {0}", e.GetType().Name);

                    if (e is IOException || e.InnerException is IOException)
                        return Directive.Restart;

                    return Directive.Stop;
                })){}

        public static readonly SupervisorStrategy Instance = new ReadModelProjectionSupervisorStrategy();
    }
}