using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class EnvValue
    {
        public readonly Value Value;
        public readonly bool IsLet;

        public EnvValue(Value value, bool isLet)
        {
            Value = value;
            IsLet = isLet;
        }
    }


    public sealed class Env
    {
        private readonly Dictionary<string, EnvValue> dict = new Dictionary<string, EnvValue>();
        [CanBeNull] private readonly Env parent;
        private readonly Prog prog;


        private Env(Prog prog)
        {
            this.prog = prog;
            parent = null;
            foreach (var b in new Builtins(prog).Values)
                dict.Add(b.Name, new EnvValue(b, true));
        }


        private Env(Prog prog, Env parent)
        {
            this.prog = prog;
            this.parent = parent;
        }


        public static Env CreateRoot(Prog prog) => new Env(prog);


        public static Env Create(Prog prog, Env parent) => new Env(prog, parent);


        public Value Get(Ident ident)
        {
            if (dict.TryGetValue(ident.Name,out var envValue))
                return envValue.Value;
            if (parent != null)
                return parent.Get(ident);
            throw prog.RemarkList.Except.VariableIsNotDeclared(ident);
        }


        public void Declare(Ident ident, Value value, bool isLet)
        {
            if (dict.ContainsKey(ident.Name))
                throw prog.RemarkList.Except.VariableIsAlreadyDeclared(ident);
            dict.Add(ident.Name, new EnvValue(value, isLet));
        }


        public void Set(Ident ident, Value value)
        {
            var e = this;
            do
            {
                if (e.dict.ContainsKey(ident.Name))
                {
                    var old = e.dict[ident.Name];
                    if (old.Value.GetType() != value.GetType())
                        prog.RemarkList.Warn.AssigningDifferentType(ident, old.Value, value);
                    if (old.IsLet)
                        prog.RemarkList.Warn.ReasigingLet(ident);
                    e.dict[ident.Name] = new EnvValue(value, old.IsLet);
                    return;
                }
                e = e.parent;
            } while (e != null);
            throw prog.RemarkList.Except.VariableIsNotDeclared(ident);
        }
    }
}