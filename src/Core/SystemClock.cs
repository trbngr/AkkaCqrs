using System;

namespace Core
{
    public static class SystemClock
    {
        private static Func<DateTime> GetUtcNow = () => DateTime.UtcNow;
        private static Func<DateTimeOffset> GetOffsetUtcNow = () => DateTimeOffset.UtcNow;

        public static DateTime UtcNow
        {
            get { return GetUtcNow(); }
        }

        public static DateTimeOffset OffsetUtcNow
        {
            get { return GetOffsetUtcNow(); }
        }

        public static IDisposable Set(DateTime dateTime)
        {
            GetUtcNow = () => dateTime;
            return new DisposableStub();
        }

        public static IDisposable Set(DateTimeOffset dateTimeOffset)
        {
            GetOffsetUtcNow = () => dateTimeOffset;
            return new DisposableStub();
        }

        private class DisposableStub : IDisposable
        {
            public void Dispose()
            {
                GetUtcNow = () => DateTime.UtcNow;
                GetOffsetUtcNow = () => DateTimeOffset.UtcNow;
            }
        }
    }
}