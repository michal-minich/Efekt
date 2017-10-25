using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Env
    {
        private readonly Dictionary<string, Value> dict = new Dictionary<string, Value>();
        [CanBeNull] private readonly Env parent;
        private readonly Remark remark;

        private Env(Remark remark)
        {
            this.remark = remark;
            parent = null;
            foreach (var b in Builtins.Values)
                dict.Add(b.Name, b);
        }

        private Env(Remark remark, Env parent)
        {
            this.remark = remark;
            this.parent = parent;
        }

        public static Env CreateRoot(Remark remark)
        {
            return new Env(remark);
        }

        public static Env Create(Remark remark, Env parent)
        {
            return new Env(remark, parent);
        }

        public Value Get(Ident ident)
        {
            if (dict.TryGetValue(ident.Name, out var value))
                return value;
            if (parent != null)
                return parent.Get(ident);
            throw remark.Error.VariableIsNotDeclared(ident);
        }

        public void Declare(Ident ident, Value value)
        {
            if (dict.ContainsKey(ident.Name))
                throw remark.Error.VariableIsAlreadyDeclared(ident);
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
                    {
                        remark.Warn.AssigningDifferntType(ident, old, value);
                    }
                    e.dict[ident.Name] = value;
                    return;
                }
                e = e.parent;
            } while (e != null);
            throw remark.Error.VariableIsNotDeclared(ident);
        }
    }
}