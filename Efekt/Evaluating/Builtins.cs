    using System.Collections.Generic;

namespace Efekt
{
    public static class Builtins
    {
        public static readonly StringWriter Writer = new StringWriter();

        public static readonly IReadOnlyList<Builtin> Values = new List<Builtin>
        {
            new Builtin("+", (remark, @params) =>
            {
                C.Nn(@params);
                C.Assume(@params.Count == 2);
                var a = @params[0].AsInt(remark);
                var b = @params[1].AsInt(remark);
                return new Int(a.Value + b.Value);
            }),

            new Builtin("*", (remark, @params)  =>
            {
                C.Nn(@params);
                C.Assume(@params.Count == 2);
                var a = @params[0].AsInt(remark);
                var b = @params[1].AsInt(remark);
                return new Int(a.Value * b.Value);
            }),

            new Builtin("print", (remark, @params)  =>
            {
                C.Nn(@params);
                C.Assume(@params.Count == 1);
                var exp = @params[0];
                C.Nn(exp);
                Writer.Write(exp.ElementToString());
                return Void.Instance;
            }),

            new Builtin("cons", (remark, @params)  =>
            {
                C.AllNotNull(@params);
                var xs = @params[0].AsArr(remark).Values;
                var list = new List<Value>(xs.Count + 1);
                list.AddRange(xs);
                list.Add((Value) @params[1]);
                return new Arr(new Values(list.ToArray()));
            })
        };


        private static Int AsInt(this Exp exp, Remark remark)
        {
            return exp is Int i ? i : throw remark.Error.DifferntTypeExpected(exp, typeof(Int).Name);
        }


        private static Arr AsArr(this Exp exp, Remark remark)
        {
            return exp is Arr a ? a : throw remark.Error.DifferntTypeExpected(exp, typeof(Arr).Name);
        }


        private static readonly StringWriter sw = new StringWriter();
        private static readonly PlainTextCodeWriter ctw = new PlainTextCodeWriter(sw);
        private static readonly Printer cw = new Printer(ctw);


        private static string ElementToString(this Element e)
        {
            cw.Write(e);
            return e.GetType().Name + ": " + sw.GetAndReset();
        }
    }
}