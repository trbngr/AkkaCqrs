using Akka.Actor;
using Core.Storage.Messages;

namespace Core.Storage
{
    public interface IStorageProvider : IHandle<GetEntity>, IHandle<UpdateEntity>, IHandle<CreateNewEntity>
    {
    }
}