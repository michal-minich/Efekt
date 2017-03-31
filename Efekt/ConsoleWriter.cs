using System;

namespace Efekt
{
    public class ConsoleWriter : TextWriter
    {
        public void Write(string value)
        {
            Console.Write(value);
        }
    }
}