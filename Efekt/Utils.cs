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
        [ContractAnnotation("false => halt", true)]
        // ReSharper disable once UnusedParameter.Global
        public static void Req(bool condition)
        {
            if (!condition)
                throw new Exception();
        }

        [ContractAbbreviator]
        // ReSharper disable once UnusedParameter.Global
        public static void Nn<T>([NotNull] T value) where T : class
        {
            Contract. /*Requires*/Assert(value != null);
        }


        [ContractAbbreviator]
        [ContractAnnotation("null => halt")]
        public static void AllNotNull<T>(IEnumerable<T> items)
        {
            Req(Contract.ForAll(items, i => i != null));
        }
    }
}