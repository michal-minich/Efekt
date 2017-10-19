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

        public static String SubstringAfter(this String value, String after)
        {
            Contract.Requires(value.Length >= after.Length);

            var startIx = value.IndexOf(after, StringComparison.Ordinal);
            Contract.Assume(startIx != -1);
            var ix = startIx + after.Length;
            return value.Substring(ix);
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

        [ContractAnnotation("null => halt", true)]
        [ContractAbbreviator]
        // ReSharper disable once UnusedParameter.Global
        public static void Nn(object value)
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