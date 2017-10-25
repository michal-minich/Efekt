using System;

namespace Efekt
{
    internal static class Warn
    {
        internal static void ValueReturnedFromFunctionNotUsed(FnApply fna)
        {
            w("Value returned from function is not used");
        }

        private static void w(string message)
        {
            Console.WriteLine("Warning: " + message);
        }
    }
}
