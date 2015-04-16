using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Event;

namespace Core.Storage.Projections
{
    public class ReadModelProjections : ReceiveActor
    {
        public static readonly Type[] ProjectionActors;
        private readonly ILoggingAdapter _log;
        private readonly HashSet<ActorPath> _paths = new HashSet<ActorPath>();

        static ReadModelProjections()
        {
            var root = typeof(ReadModelProjections);
            ProjectionActors = root.Assembly.GetTypes()
                // ReSharper disable once PossibleNullReferenceException
                // ReSharper disable once AssignNullToNotNullAttribute
                .Where(type => type.Namespace.StartsWith(root.Namespace))
                .Where(type => typeof (IInternalActor).IsAssignableFrom(type))
                .ToArray();
        }

        public ReadModelProjections()
        {
            _log = Context.GetLogger();

            Receive<IEvent>(x =>
            {
                var eventType = x.GetType();
                var actorType = ProjectionActors.FirstOrDefault(a => a.Name == eventType.Name);
                if (actorType != null)
                {
                    var path = SystemData.Select(Self, actorType);
                    if (!_paths.Contains(path))
                    {
                        var @ref = Context.ActorOf(Props.Create(actorType), actorType.Name.ToLowerInvariant());
                        _paths.Add(@ref.Path);
                    }
                    Context.ActorSelection(path).Tell(x);
                }

                Context.System.EventStream.Publish(x);
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(maxNrOfRetries: 10, withinTimeRange: TimeSpan.FromSeconds(30), decider: new LocalOnlyDecider(
                e =>
                {
                    _log.Info("{0}", e.GetType().Name);

                    if (e is IOException || e.InnerException is IOException)
                        return Directive.Restart;

                    return Directive.Stop;
                }));
        }
    }
}