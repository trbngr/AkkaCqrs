using System;

namespace Core.Storage.Messages
{
    public class AddNewEntity
    {
        public AddNewEntity(Guid key, object entity)
        {
            Key = key;
            Entity = entity;
        }

        public Guid Key { get; private set; }
        public object Entity { get; private set; }
    }
}