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
}