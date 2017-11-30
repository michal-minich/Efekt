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

                new Builtin("-", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(inExp, prog);
                    var b = @params[1].AsInt(inExp, prog);
                    return new Int(a.Value - b.Value);
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
                    var a = @params[0].AsValue(inExp, prog);
                    var b = @params[1].AsValue(inExp, prog);
                    return new Bool(a.ToDebugString() == b.ToDebugString());
                }),

                new Builtin("<", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(inExp, prog);
                    var b = @params[1].AsInt(inExp, prog);
                    return new Bool(a.Value < b.Value);
                }),

                new Builtin("print", (@params, inExp) =>
                {
                    C.Assume(@params.Count == 1);
                    var exp = @params[0];
                    prog.OutputPrinter.Write(exp);
                    return Void.Instance;
                }),

                new Builtin("at", (@params, inExp) =>
                {
                    var items = @params[0].AsArr(inExp, prog).Values;
                    var at = @params[1].AsInt(inExp, prog);
                    // todo check index vs lenght
                    return items[at.Value];
                }),

                new Builtin("setAt", (@params, inExp) =>
                {
                    var items = @params[0].AsArr(inExp, prog).Values;
                    var at = @params[1].AsInt(inExp, prog);
                    var value = @params[2].AsValue(inExp, prog);
                    // todo check index vs lenght
                    return items[at.Value] = value;
                }),

                new Builtin("count", (@params, inExp) =>
                {
                    var items = @params[0].AsArr(inExp, prog).Values;
                    return new Int(items.Count);
                })
            };
        }
    }
}