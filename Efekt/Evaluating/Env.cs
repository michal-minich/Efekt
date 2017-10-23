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

        public Value Get(string name)
        {
            if (dict.TryGetValue(name, out var value))
                return value;
            if (parent != null)
                return parent.Get(name);
            throw Error.Fail();
        }

        public void Declare(string name, Value value)
        {
            if (dict.ContainsKey(name))
                throw Error.Fail();
            dict.Add(name, value);
        }

        public void Set(string name, Value value)
        {
            var e = this;
            do
            {
                if (e.dict.ContainsKey(name))
                {
                    e.dict[name] = value;
                    return;
                }
                e = e.parent;
            } while (e != null);
            throw Error.Fail();
        }
    }
}