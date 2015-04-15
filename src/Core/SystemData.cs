using System;
using Akka.Actor;

namespace Core
{
    public class SystemData
    {
        public const string SystemName = "sys";
        public static ActorMetaData StorageActor = new ActorMetaData("storage", string.Format("akka://{0}/user/storage", SystemName));
        public static ActorMetaData ProjectionsActor = new ActorMetaData("projections", string.Format("akka://{0}/user/projections", SystemName));

        public class ActorMetaData
        {
            public ActorMetaData(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public string Name { get; private set; }
            public string Path { get; private set; }
        }

        public static ActorPath Select(IActorRef parent, Type actorType)
        {
            return parent.Path.Child(actorType.Name.ToLowerInvariant());
        }
    }
}