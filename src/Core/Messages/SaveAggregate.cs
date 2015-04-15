namespace Core.Messages
{
    public sealed class SaveAggregate
    {
        private static readonly SaveAggregate Instance = new SaveAggregate();

        public static SaveAggregate Message
        {
            get { return Instance; }
        }
    }
}