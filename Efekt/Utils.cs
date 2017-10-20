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
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Assert(bool condition)
        {
            if (!condition)
                throw new Exception();
        }


        [ContractAnnotation("false => halt", true)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Assume(bool condition)
        {
            if (!condition)
                throw new Exception();
        }


        [ContractAnnotation("null => halt", true)]
        [ContractAbbreviator]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Nn([CanBeNull] object value)
        {
            if (value == null)
                throw new Exception();
        }


        [ContractAbbreviator]
        [ContractAnnotation("null => halt")]
        public static void AllNotNull<T>([CanBeNull] IEnumerable<T> items)
        {
            Nn(items);
            foreach (var i in items)
                if (i == null)
                    throw new Exception();
        }
    }
}