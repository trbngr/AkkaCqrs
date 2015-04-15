using System;

namespace Core.Storage.Messages
{
    public class EntityRetrieved
    {
        public Guid Key { get; private set; }
        public object Entity { get; private set; }

        public EntityRetrieved(Guid key, object entity)
        {
            Key = key;
            Entity = entity;
        }
    }
}