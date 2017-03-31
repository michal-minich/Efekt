using System;
using System.Linq;

namespace Efekt
{
    public static class Utils
    {
        public static string RepeatString(string value, int count)
        {
            return String.Concat(Enumerable.Repeat(value, count));
        }
    }
}