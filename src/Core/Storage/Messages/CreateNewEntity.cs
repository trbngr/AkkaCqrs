using System;

namespace Core.Storage.Messages
{
    public class CreateNewEntity
    {
        public CreateNewEntity(Guid key, object entity)
        {
            Key = key;
            Entity = entity;
        }

        public Guid Key { get; private set; }
        public object Entity { get; private set; }
    }

    public class NewEntityCreated { 
        public NewEntityCreated(Guid key, object entity)
        {
            Key = key;
            Entity = entity;
        }

        public Guid Key { get; private set; }
        public object Entity { get; private set; }}
}