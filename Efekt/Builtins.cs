using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public static class Builtins
    {
        public static readonly StringWriter Writer = new StringWriter();

        public static readonly IReadOnlyList<Builtin> Values = new List<Builtin>
        {
            new Builtin("+", @params =>
            {
                var a = (Int)@params[0];
                var b = (Int)@params[1];
                return new Int(a.Value + b.Value);
            }),

            new Builtin("*", @params =>
            {
                var a = (Int)@params[0];
                var b = (Int)@params[1];
                return new Int(a.Value * b.Value);
            }),

            new Builtin("print", @params =>
            {
                Writer.Write(@params[0].ElementToString());
                return Void.Instance;
            }),

            new Builtin("cons", @params =>
            {
                var xs = ((Arr) @params[0]).Values;
                var list = new List<Value>(xs.Count + 1);
                list.AddRange(xs);
                list.Add((Value)@params[1]);
                return new Arr(new Values(list.ToArray()));
            })
        };

        public static readonly StringWriter sw = new StringWriter();
        private static readonly PlainTextCodeWriter ctw = new PlainTextCodeWriter(sw);
        public static readonly Printer cw = new Printer(ctw);

        private static string ElementToString([NotNull] this Element e)
        {
            cw.Write(e);
            return e.GetType().Name + ": " + sw.GetAndReset();
        }
    }
}