using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

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


        [Pure]
        public static IEnumerable<TSouce> Prepend<TSouce>(this IEnumerable<TSouce> source, TSouce element)
        {
            C.ReturnsNn();

            return new[] {element}.Concat(source);
        }


        [Pure]
        public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            C.ReturnsNn();

            return source.Concat(new[] {element});
        }


        public static void AddValue<T>(this IList<T> list, T value)
        {
            C.Nn(list, value);

            list.Add(value);
        }


        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count = 1)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                    "Argument n should be non-negative.");

            return InternalSkipLast(source, count);
        }


        private static IEnumerable<T> InternalSkipLast<T>(IEnumerable<T> source, int count)
        {
            Queue<T> buffer = new Queue<T>(count + 1);

            foreach (T x in source)
            {
                buffer.Enqueue(x);

                if (buffer.Count == count + 1)
                    yield return buffer.Dequeue();
            }
        }


        public static void AddUnique<T>(this IList<T> list, T item)
        {
            if (!list.Contains(item))
                list.Add(item);
        }


        public static void UpdateTop<T>(this Stack<T> stack, T item)
        {
            stack.Pop();
            stack.Push(item);
        }
    }
}