using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;

namespace Efekt
{
    // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Global
    public static class C
    {
        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("false => halt", true)]
        [AssertionMethod]
        //[ContractAbbreviator]
        public static void Assert(bool condition)
        {
            Contract.Assert(condition);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("false => halt", true)]
        [AssertionMethod]
        //[ContractAbbreviator]
        public static void Assume(bool condition)
        {
            Contract.Assume(condition);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("false => halt", true)]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void Req(bool condition)
        {
            Contract.Requires(condition);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("false => halt", true)]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void Ens(bool condition)
        {
            Contract.Ensures(condition);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        //[ContractAnnotation("null => halt", true)]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void EnsNn([CanBeNull] object value)
        {
            Contract.Ensures(value != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("null => halt", true)]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void Nn([CanBeNull] object value)
        {
            Contract.Requires(value != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("value1:null => halt", true)]
        [ContractAnnotation("value2:null => halt", true)]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void Nn([CanBeNull] object value1, [CanBeNull] object value2)
        {
            Contract.Requires(value1 != null);
            Contract.Requires(value2 != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("value1:null => halt", true)]
        [ContractAnnotation("value2:null => halt", true)]
        [ContractAnnotation("value3:null => halt", true)]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void Nn([CanBeNull] object value1, [CanBeNull] object value2, [CanBeNull] object value3)
        {
            Contract.Requires(value1 != null);
            Contract.Requires(value2 != null);
            Contract.Requires(value3 != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("value1:null => halt", true)]
        [ContractAnnotation("value2:null => halt", true)]
        [ContractAnnotation("value3:null => halt", true)]
        [ContractAnnotation("value4:null => halt", true)]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void Nn([CanBeNull] object value1, [CanBeNull] object value2, [CanBeNull] object value3, [CanBeNull] object value4)
        {
            Contract.Requires(value1 != null);
            Contract.Requires(value2 != null);
            Contract.Requires(value3 != null);
            Contract.Requires(value4 != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void ReturnsNn()
        {
            Contract.Ensures(Contract.Result<object>() != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("null => halt")]
        [AssertionMethod]
        [ContractAbbreviator]
        public static void AllNotNull<T>([CanBeNull] IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(Contract.ForAll(items, i => i != null));
        }
    }
    // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Global
}