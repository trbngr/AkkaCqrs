namespace Core.Domain
{
    public interface IEventSink
    {
        void Publish(IEvent @event);
    }
}