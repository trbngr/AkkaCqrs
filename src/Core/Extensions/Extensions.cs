using System;
using Akka.Actor;

namespace Core.Extensions
{
    public static class Extensions
    {
        public static Optional<TMessage> ReadMessage<TMessage>(this object message)
        {
            return message is TMessage ? (TMessage)message : default(TMessage);
        }

        public static bool Recieve<TMessage>(this object message, Action<TMessage> action)
        {
            if (!(message is TMessage))
                return false;

            action((TMessage)message);
            return true;
        }

        public static bool Recieve<TMessage>(this object message, Func<TMessage, bool> action)
        {
            if (!(message is TMessage))
                return false;

            return action((TMessage)message);
        }
    }
}