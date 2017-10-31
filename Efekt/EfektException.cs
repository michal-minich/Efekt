using System;

namespace Efekt
{
    public class EfektException : Exception
    {
        public EfektException(string message) : base(message)
        {
        }

        public EfektException(string message, Element inExp) : base(message)
        {
        }
    }


    public class EfektInterpretedException : EfektException
    {
        public Value Value { get; }

        public EfektInterpretedException(Value value) : base("Interpreted excpetion " + value.ToDebugString())
        {
            Value = value;
        }
    }
}