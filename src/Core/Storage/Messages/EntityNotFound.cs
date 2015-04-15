using System;

namespace Core.Storage.Messages
{
    public class EntityNotFound
    {
        public EntityNotFound(Guid key)
        {
            Key = key;
        }

        public Guid Key { get; private set; }
    }
}