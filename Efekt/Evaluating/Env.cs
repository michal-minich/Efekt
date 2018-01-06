using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class EnvValue<T> where T : class
    {
        public readonly T Value;
        public readonly bool IsLet;

        public EnvValue(T value, bool isLet)
        {
            Value = value;
            IsLet = isLet;
        }
    }


    public sealed class Env<T> where T : class
    {
        private readonly Dictionary<string, EnvValue<T>> dict = new Dictionary<string, EnvValue<T>>();
        private readonly Dictionary<QualifiedIdent, Env<T>> imports = new Dictionary<QualifiedIdent, Env<T>>();
        [CanBeNull] private readonly Env<T> parent;
        private readonly Prog prog;


        private Env(Prog prog)
        {
            C.Nn(prog);
            this.prog = prog;
            parent = null;
        }
        

        private Env(Prog prog, Env<T> parent)
        {
            C.Nn(prog, parent);
            this.prog = prog;
            this.parent = parent;
        }


        public static Env<Value> CreateValueRoot(Prog prog)
        {
            C.Nn(prog);
            C.ReturnsNn();

            var env = new Env<Value>(prog);
            foreach (var b in new Builtins(prog).Values)
                env.dict.Add(b.Name, new EnvValue<Value>(b, true));
            return env;
        }


        public static Env<Spec> CreateSpecRoot(Prog prog)
        {
            C.Nn(prog);
            C.ReturnsNn();

            var env = new Env<Spec>(prog);
            foreach (var b in new Builtins(prog).Values)
                env.dict.Add(b.Name, new EnvValue<Spec>(b.FnSpec, true));
            return env;
        }


        public static Env<Declr> CreateDeclrRoot(Prog prog)
        {
            C.Nn(prog);
            C.ReturnsNn();

            var env = new Env<Declr>(prog);
            foreach (var b in new Builtins(prog).Values)
                env.dict.Add(b.Name, new EnvValue<Declr>(new Let(new Ident(b.Name, TokenType.Ident), b), true));
            // TODO toke type ident/op
            return env;
        }



        public static Env<T> Create(Prog prog, Env<T> parent)
        {
            C.Nn(prog, parent);
            C.ReturnsNn();

            return new Env<T>(prog, parent);
        }

        public T Get(Ident ident)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v = GetOrNull(ident);
            if (v != null)
                return v;
            throw prog.RemarkList.VariableIsNotDeclared(ident);
        }


        [CanBeNull]
        public T GetDirectlyOrNull(Ident ident)
        {
            C.Nn(ident);
            if (dict.TryGetValue(ident.Name, out var envValue))
                return envValue.Value;
            return null;
        }


        [CanBeNull]
        public T GetOrNull(Ident ident)
        {
            C.Nn(ident);
            if (dict.TryGetValue(ident.Name, out var envValue))
                return envValue.Value;
            if (parent != null)
                return parent.GetOrNull(ident);
            return null;
        }


        [CanBeNull]
        public T GetWithImportOrNull(Ident ident)
        {
            C.Nn(ident);
            var candidates = new Dictionary<QualifiedIdent, T>();

            var local = GetOrNull(ident);
            if (local != null)
                candidates.Add(new Ident("local", TokenType.Ident), local);

            GetFromImports(ident, candidates);

            if (candidates.Count == 1)
                return candidates.First().Value;
            if (candidates.Count == 0)
                return null;
            throw prog.RemarkList.MoreVariableCandidates(candidates, ident);
        }

        private void GetFromImports(Ident ident, Dictionary<QualifiedIdent, T> candidates)
        {
            C.Nn(ident);

            foreach (var i in imports)
            {
                var x = i.Value.GetDirectlyOrNull(ident);
                if (x != null && !candidates.Any(c => c.Key.ToDebugString() == i.Key.ToDebugString()))
                    candidates.Add(i.Key, (T)x);
            }
            if (parent != null)
                parent.GetFromImports(ident, candidates);
        }


        public T GetWithImport(Ident ident)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v  = GetWithImportOrNull(ident);
            if (v == null)
                throw prog.RemarkList.VariableIsNotDeclared(ident);
            return v;
        }


        public void Declare(Ident ident, T value, bool isLet)
        {
            C.Nn(ident, value);

            if (dict.ContainsKey(ident.Name))
                throw prog.RemarkList.VariableIsAlreadyDeclared(ident);
            dict.Add(ident.Name, new EnvValue<T>(value, isLet));
        }


        public void Set(Ident ident, T value)
        {
            C.Nn(ident, value);

            var e = this;
            do
            {
                if (e.dict.ContainsKey(ident.Name))
                {
                    var old = e.dict[ident.Name];
                    if (old.Value != Void.Instance && old.Value.GetType() != value.GetType())
                        prog.RemarkList.AssigningDifferentType(ident, old.Value, value);
                    if (old.IsLet)
                        prog.RemarkList.ReasigingLet(ident);
                    e.dict[ident.Name] = new EnvValue<T>(value, old.IsLet);
                    return;
                }
                e = e.parent;
            } while (e != null);
            throw prog.RemarkList.VariableIsNotDeclared(ident);
        }


        public void AddImport(QualifiedIdent qi, Env<T> module)
        {
            C.Nn(qi, module);

            imports.Add(qi, module);
        }
    }
}