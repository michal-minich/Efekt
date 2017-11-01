using System.Collections.Generic;

namespace Efekt
{
    public class Builtins
    {
        public readonly IReadOnlyList<Builtin> Values;

        public Builtins(Prog prog)
        {
            Values = new List<Builtin>
            {
                new Builtin("+", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(inExp, prog);
                    var b = @params[1].AsInt(inExp, prog);
                    return new Int(a.Value + b.Value);
                }),

                new Builtin("*", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(inExp, prog);
                    var b = @params[1].AsInt(inExp, prog);
                    return new Int(a.Value * b.Value);
                }),

                new Builtin("==", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(inExp, prog);
                    var b = @params[1].AsInt(inExp, prog);
                    return new Bool(a.Value == b.Value);
                }),

                new Builtin("and", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsBool(inExp, prog);
                    var b = @params[1].AsBool(inExp, prog);
                    return new Bool(a.Value && b.Value);
                }),

                new Builtin("or", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsBool(inExp, prog);
                    var b = @params[1].AsBool(inExp, prog);
                    return new Bool(a.Value || b.Value);
                }),

                new Builtin("print", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 1);
                    var exp = @params[0];
                    prog.OutputPrinter.Write(exp);
                    return Void.Instance;
                }),

                new Builtin("cons", (@params, inExp) =>
                {
                    var xs = @params[0].AsArr(inExp, prog).Values;
                    var list = new List<Value>(xs.Count + 1);
                    list.AddRange(xs);
                    list.Add(@params[1].AsValue(inExp, prog));
                    return new Arr(new Values(list.ToArray()));
                })
            };
        }
    }
}