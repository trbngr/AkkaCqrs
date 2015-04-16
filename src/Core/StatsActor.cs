using Akka.Actor;
using Akka.Event;

namespace Core
{
    public class StatsActor : ReceiveActor
    {
        public StatsActor()
        {
            var logger = Context.GetLogger();

            Receive<IEvent>(x =>
            {
                logger.Info("Collect event: {0}", x);
            });
        }
    }
}