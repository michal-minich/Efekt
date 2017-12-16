using System;

namespace Efekt
{
    public class EfektException : Exception
    {
        public EfektException(string message) : base(message)
        {
        }
    }


    public class EfektProgramException : Exception
    {
        public Value Value { get; }

        public EfektProgramException(string message, Value value) : base(message)
        {
            Value = value;
        }
    }
}