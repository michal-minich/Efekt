using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Env
    {
        private readonly Dictionary<string, Value> dict = new Dictionary<string, Value>();
        [CanBeNull] private readonly Env parent;
        private readonly Prog prog;

        private Env(Prog prog)
        {
            this.prog = prog;
            parent = null;
            foreach (var b in new Builtins(prog).Values)
                dict.Add(b.Name, b);
        }

        private Env(Prog prog, Env parent)
        {
            this.prog = prog;
            this.parent = parent;
        }

        public static Env CreateRoot(Prog prog)
        {
            return new Env(prog);
        }

        public static Env Create(Prog prog, Env parent)
        {
            return new Env(prog, parent);
        }

        public Value Get(Ident ident)
        {
            if (dict.TryGetValue(ident.Name, out var value))
                return value;
            if (parent != null)
                return parent.Get(ident);
            throw prog.RemarkList.Except.VariableIsNotDeclared(ident);
        }

        public void Declare(Ident ident, Value value)
        {
            if (dict.ContainsKey(ident.Name))
                throw prog.RemarkList.Except.VariableIsAlreadyDeclared(ident);
            dict.Add(ident.Name, value);
        }

        public void Set(Ident ident, Value value)
        {
            var e = this;
            do
            {
                if (e.dict.ContainsKey(ident.Name))
                {
                    var old = e.dict[ident.Name];
                    if (old.GetType() != value.GetType())
                        prog.RemarkList.Warn.AssigningDifferentType(ident, old, value);
                    e.dict[ident.Name] = value;
                    return;
                }
                e = e.parent;
            } while (e != null);
            throw prog.RemarkList.Except.VariableIsNotDeclared(ident);
        }
    }
}