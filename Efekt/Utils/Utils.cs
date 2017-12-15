using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Efekt
{
    public static class Utils
    {
        public static string RepeatString(string value, int count)
        {
            C.ReturnsNn();

            return String.Concat(Enumerable.Repeat(value, count));
        }


        public static string GetFilePathRelativeToBase(string filePath)
        {
            C.ReturnsNn();

            return GetFilePathRelativeToBase(Directory.GetCurrentDirectory(), filePath);
        }


        public static string GetFilePathRelativeToBase(string basePath, string filePath)
        {
            C.ReturnsNn();

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
            C.ReturnsNn();

            return source.GroupBy(selector).Select(g => g.First());
        }


        [JetBrains.Annotations.Pure]
        public static IEnumerable<TSouce> Prepend<TSouce>(this IEnumerable<TSouce> source, TSouce element)
        {
            C.ReturnsNn();

            return new[] {element}.Concat(source);
        }


        [JetBrains.Annotations.Pure]
        public static IEnumerable<TSouce> Append<TSouce>(this IEnumerable<TSouce> source, TSouce element)
        {
            C.ReturnsNn();

            return source.Concat(new[] {element});
        }


        public static void AddValue<T>(this IList<T> list, T value)
        {
            C.Nn(list, value);

            list.Add(value);
        }
    }
}