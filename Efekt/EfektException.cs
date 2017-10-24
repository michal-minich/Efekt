using System;

namespace Efekt
{
    internal class EfektException : Exception
    {
        public readonly Element Element;

        public EfektException(string message, Element element) : base(message)
        {
            Element = element;
        }

        public EfektException(string message, Exception innerException, Element element) : base(message, innerException)
        {
            Element = element;
        }
    }
}