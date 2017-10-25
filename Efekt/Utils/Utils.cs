using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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


        public static IEnumerable<TSource> DistinctBy<TSource, TDistictKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TDistictKey> selector)
        {
            return source.GroupBy(selector).Select(g => g.First());
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
                throw new Exception();
        }
        

        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("false => halt", true)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Assume(bool condition)
        {
            if (!condition)
                throw new Exception();
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [ContractAnnotation("null => halt", true)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        public static void Nn([CanBeNull] object value)
        {
            if (value == null)
                throw new Exception(); ;
        }


        [DebuggerStepThrough]
        [Conditional("DEBUG")]
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