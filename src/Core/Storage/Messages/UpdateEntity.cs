using System;

namespace Core.Storage.Messages
{
    public class UpdateEntity
    {
        public UpdateEntity(Guid key, object entity)
        {
            Key = key;
            Entity = entity;
        }

        public Guid Key { get; private set; }
        public object Entity { get; private set; }
    }

    public class EntityUpdated
    {
        public EntityUpdated(Guid key, object entity)
        {
            Key = key;
            Entity = entity;
        }

        public Guid Key { get; private set; }
        public object Entity { get; private set; }
    }
}