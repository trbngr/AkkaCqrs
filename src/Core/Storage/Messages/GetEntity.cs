using System;

namespace Core.Storage.Messages
{
    public class GetEntity
    {
        public GetEntity(Guid key, Type entityType)
        {
            Key = key;
            EntityType = entityType;
        }

        public Guid Key { get; private set; }
        public Type EntityType { get; private set; }
    }
}