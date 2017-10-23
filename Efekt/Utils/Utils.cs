using System.Collections.Generic;
using System.Diagnostics;
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
        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("false => halt", true)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Assert(bool condition)
        {
            if (!condition)
                throw Error.Fail();
        }
        

        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("false => halt", true)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Assume(bool condition)
        {
            if (!condition)
                throw Error.Fail();
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("null => halt", true)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Nn([CanBeNull] object value)
        {
            if (value == null)
                throw Error.Fail();
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("null => halt")]
        public static void AllNotNull<T>([CanBeNull] IEnumerable<T> items)
        {
            Nn(items);
            foreach (var i in items)
                if (i == null)
                    throw Error.Fail();
        }
    }
}