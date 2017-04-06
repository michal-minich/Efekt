using System;

namespace Efekt
{
    public sealed class ConsoleWriter : TextWriter
    {
        public void Write(string value)
        {
            Console.Write(value);
        }
    }
}