using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Env
    {
        private readonly Dictionary<string, Value> dict = new Dictionary<string, Value>();
        [CanBeNull] private readonly Env parent;

        private Env()
        {
            parent = null;
            foreach (var b in Builtins.Values)
                dict.Add(b.Name, b);
        }

        private Env(Env parent)
        {
            this.parent = parent;
        }

        public static Env CreateRoot()
        {
            return new Env();
        }

        public static Env Create(Env parent)
        {
            return new Env(parent);
        }

        public Value Get(Ident ident)
        {
            if (dict.TryGetValue(ident.Name, out var value))
                return value;
            if (parent != null)
                return parent.Get(ident);
            throw Error.VariableIsNotDeclared(ident);
        }

        public void Declare(Ident ident, Value value)
        {
            if (dict.ContainsKey(ident.Name))
                throw Error.VariableIsAlreadyDeclared(ident);
            dict.Add(ident.Name, value);
        }

        public void Set(Ident ident, Value value)
        {
            var e = this;
            do
            {
                if (e.dict.ContainsKey(ident.Name))
                {
                    e.dict[ident.Name] = value;
                    return;
                }
                e = e.parent;
            } while (e != null);
            throw Error.VariableIsNotDeclared(ident);
        }
    }
}