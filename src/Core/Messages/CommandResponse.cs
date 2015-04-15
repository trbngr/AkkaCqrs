using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Messages
{
    public class CommandResponse
    {
        private readonly bool _success;
        private readonly Exception[] _exceptions;

        public CommandResponse(bool success, IEnumerable<Exception> exceptions)
        {
            _success = success;
            _exceptions = exceptions.ToArray();
        }

        public bool Success { get { return _success && _exceptions.Length == 0; } }

        public AggregateException Exception
        {
            get
            {
                if (Success)
                    return null;
                return new AggregateException(_exceptions);
            }
        }
    }
}