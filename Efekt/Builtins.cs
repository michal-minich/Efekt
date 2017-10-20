using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public static class Builtins
    {
        [NotNull]
        public static readonly StringWriter Writer = new StringWriter();

        [NotNull]
        public static readonly IReadOnlyList<Builtin> Values = new List<Builtin>
        {
            new Builtin("+", @params =>
            {
                C.Nn(@params);
                C.Assume(@params.Count == 2);
                var a = @params[0].AsInt();
                var b = @params[1].AsInt();
                return new Int(a.Value + b.Value);
            }),

            new Builtin("*", @params =>
            {
                C.Nn(@params);
                C.Assume(@params.Count == 2);
                var a = @params[0].AsInt();
                var b = @params[1].AsInt();
                return new Int(a.Value * b.Value);
            }),

            new Builtin("print", @params =>
            {
                C.Nn(@params);
                C.Assume(@params.Count == 1);
                var exp = @params[0];
                C.Nn(exp);
                Writer.Write(exp.ElementToString());
                return Void.Instance;
            }),

            new Builtin("cons", @params =>
            {
                C.AllNotNull(@params);
                var xs = @params[0].AsArr().Values;
                var list = new List<Value>(xs.Count + 1);
                list.AddRange(xs);
                list.Add((Value)@params[1]);
                return new Arr(new Values(list.ToArray()));
            })
        };


        [NotNull]
        private static Int AsInt(this Exp exp)
        {
            return exp is Int i ? i : throw new Exception();
        }


        [NotNull]
        private static Arr AsArr(this Exp exp)
        {
            return exp is Arr a ? a : throw new Exception();
        }


        [NotNull] private static readonly StringWriter sw = new StringWriter();
        [NotNull] private static readonly PlainTextCodeWriter ctw = new PlainTextCodeWriter(sw);
        [NotNull] private static readonly Printer cw = new Printer(ctw);

        [NotNull]
        private static string ElementToString([NotNull] this Element e)
        {
            cw.Write(e);
            return e.GetType().Name + ": " + sw.GetAndReset();
        }
    }
}