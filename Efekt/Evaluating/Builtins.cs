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
                new Builtin("+", (@params, fna) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(fna.Arguments[0], prog);
                    var b = @params[1].AsInt(fna.Arguments[1], prog);
                    return new Int(a.Value + b.Value);
                }),

                new Builtin("-", (@params, fna) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(fna.Arguments[0], prog);
                    var b = @params[1].AsInt(fna.Arguments[1], prog);
                    return new Int(a.Value - b.Value);
                }),

                new Builtin("*", (@params, fna) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(fna.Arguments[0], prog);
                    var b = @params[1].AsInt(fna.Arguments[1], prog);
                    return new Int(a.Value * b.Value);
                }),

                new Builtin("==", (@params, fna) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsValue(fna.Arguments[0], prog);
                    var b = @params[1].AsValue(fna.Arguments[1], prog);
                    return new Bool(a.ToDebugString() == b.ToDebugString());
                }),

                new Builtin("<", (@params, fna) =>
                {
                    C.Assume(@params.Count == 2);
                    var a = @params[0].AsInt(fna.Arguments[0], prog);
                    var b = @params[1].AsInt(fna.Arguments[1], prog);
                    return new Bool(a.Value < b.Value);
                }),

                new Builtin("print", (@params, fna) =>
                {
                    C.Assume(@params.Count == 1);
                    var exp = @params[0];
                    prog.OutputPrinter.Write(exp);
                    return Void.Instance;
                }),

                new Builtin("at", (@params, fna) =>
                {
                    var items = @params[0].AsArr(fna.Arguments[0], prog).Values;
                    var at = @params[1].AsInt(fna.Arguments[1], prog);
                    // todo check index vs lenght
                    return items[at.Value];
                }),

                new Builtin("setAt", (@params, fna) =>
                {
                    var items = @params[0].AsArr(fna.Arguments[0], prog).Values;
                    var at = @params[1].AsInt(fna.Arguments[1], prog);
                    var value = @params[2].AsValue(fna.Arguments[2], prog);
                    // todo check index vs lenght
                    return items[at.Value] = value;
                }),

                new Builtin("count", (@params, fna) =>
                {
                    var items = @params[0].AsArr(fna.Arguments[0], prog).Values;
                    return new Int(items.Count);
                })
            };
        }
    }
}