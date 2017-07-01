using System.Collections.Generic;

namespace Efekt
{
    public static class Builtins
    {
        public static IReadOnlyList<Builtin> Values = new List<Builtin>
        {
            new Builtin("+", @params =>
            {
                return new Int(3);
            })
        };
    }
}