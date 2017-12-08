using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public static class Utils
    {
        public static string RepeatString(string value, int count)
        {
            return String.Concat(Enumerable.Repeat(value, count));
        }


        public static string GetFilePathRelativeToBase(string filePath)
        {
            return GetFilePathRelativeToBase(Directory.GetCurrentDirectory(), filePath);
        }


        public static string GetFilePathRelativeToBase(string basePath, string filePath)
        {
            var fromUri = new Uri(basePath);
            var toUri = new Uri(fromUri, filePath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return relativePath;
        }


        public static IEnumerable<TSource> DistinctBy<TSource, TDistinctKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TDistinctKey> selector)
        {
            return source.GroupBy(selector).Select(g => g.First());
        }


        [JetBrains.Annotations.Pure]
        public static IEnumerable<TSouce> Prepend<TSouce>(this IEnumerable<TSouce> source, TSouce element)
        {
            return new[] {element}.Concat(source);
        }


        [JetBrains.Annotations.Pure]
        public static IEnumerable<TSouce> Append<TSouce>(this IEnumerable<TSouce> source, TSouce element)
        {
            return source.Concat(new[] {element});
        }
    }


    public static class C
    {
        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("false => halt", true)]
        //[ContractAbbreviator]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Assert(bool condition)
        {
            Contract.Assert(condition);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("false => halt", true)]
        //[ContractAbbreviator]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Assume(bool condition)
        {
            Contract.Assume(condition);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("false => halt", true)]
        [ContractAbbreviator]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Req(bool condition)
        {
            Contract.Requires(condition);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("null => halt", true)]
        [ContractAbbreviator]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Nn([CanBeNull] object value)
        {
            Contract.Requires(value != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAbbreviator]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void EnsNn<T>()
        {
            Contract.Ensures(Contract.Result<T>() != null);
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("CONTRACTS_FULL")]
        [ContractAnnotation("null => halt")]
        [ContractAbbreviator]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void AllNotNull<T>([CanBeNull] IEnumerable<T> items)
        {
            Contract.Requires(items != null);
            Contract.Requires(Contract.ForAll(items, i => i != null));
        }
    }
}