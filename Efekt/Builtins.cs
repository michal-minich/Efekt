using System.Collections.Generic;

namespace Efekt
{
    public static class Builtins
    {
        public static readonly StringWriter Writer = new StringWriter();

        public static readonly IReadOnlyList<Builtin> Values = new List<Builtin>
        {
            new Builtin("+", @params =>
            {
                return new Int(((Int)@params[0]).Value + ((Int)@params[1]).Value);
            }),
            new Builtin("print", @params =>
            {
                Writer.Write(@params[0].ToString());
                return Void.Instance;
            })
        };
    }
}