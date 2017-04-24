using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public static class Utils
    {
        public static string RepeatString(string value, int count)
        {
            return string.Concat(Enumerable.Repeat(value, count));
        }
    }

    public static class C
    {
        [ContractAnnotation("condition:false => halt", true)]
        // ReSharper disable once UnusedParameter.Global
        public static void Requires(bool condition)
        {
            if (!condition)
                throw new Exception();
        }

        public static bool ForAll<T>(IEnumerable<T> collection, Predicate<T> predicate)
        {
            foreach (var item in collection)
                if (!predicate(item))
                    return false;
            return true;
        }
    }
}