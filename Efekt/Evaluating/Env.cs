using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class EvnItem<T> where T : class, Element
    {
        public T Value { get; set; }
        public readonly bool IsLet;

        public EvnItem(T value, bool isLet)
        {
            C.Nn(value);

            Value = value;
            IsLet = isLet;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Value != null);
        }
    }


    public abstract class Env
    {
        public static Env<Value> CreateValueRoot(Prog prog)
        {
            return CreateRoot<Value>(prog, b => b);
        }


        public static Env<Spec> CreateSpecRoot(Prog prog)
        {
            return CreateRoot(prog, b => b.Spec);
        }


        public static Env<Declr> CreateDeclrRoot(Prog prog)
        {
            return CreateRoot<Declr>(prog, b =>
            {
                var tt = b.Name.Any(ch => ch >= 'a' && ch <= 'z') ? TokenType.Ident : TokenType.Op;
                return new Let(new Ident(b.Name, tt), b);
            });
        }


        public static Env<TA> CreateRoot<TA>(Prog prog, Func<Builtin, TA> selector) where TA : class, Element
        {
            C.Nn(prog);
            C.ReturnsNn();

            var dict = new Dictionary<string, EvnItem<TA>>();
            foreach (var b in new Builtins(prog).Values)
                dict.Add(b.Name, new EvnItem<TA>(selector(b), true));
            return new Env<TA>(prog,dict);
        }


        public static Env<T> Create<T>(Prog prog, Env<T> parent) where T : class, Element
        {
            C.Nn(prog, parent);
            C.ReturnsNn();

            return new Env<T>(prog, parent);
        }
    }


    public sealed class Env<T> : Env where T : class, Element
    {
        private readonly Dictionary<string, EvnItem<T>> dict;
        private readonly Dictionary<QualifiedIdent, Env<T>> imports = new Dictionary<QualifiedIdent, Env<T>>();
        [CanBeNull] private readonly Env<T> parent;
        private readonly Prog prog;


        public Env(Prog program, Dictionary<string, EvnItem<T>> initialDictionary)
        {
            C.Nn(program, initialDictionary);
            C.AllNotNull(initialDictionary);
            prog = program;
            dict = initialDictionary;
            parent = null;
        }


        public Env(Prog prog, Env<T> parent)
        {
            C.Nn(prog, parent);
            this.prog = prog;
            this.parent = parent;
            dict = new Dictionary<string, EvnItem<T>>();
        }


        public EvnItem<T> GetWithoutImports(Ident ident)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v = GetWithoutImportOrNull(ident);
            return v ?? throw prog.RemarkList.VariableIsNotDeclared(ident);
        }


        public EvnItem<T> GetFromThisEnvOnly(Ident ident)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v = GetFromThisEnvOnlyOrNull(ident);
            return v ?? throw prog.RemarkList.VariableIsNotDeclared(ident);
        }


        [CanBeNull]
        public EvnItem<T> GetFromThisEnvOnlyOrNull(Ident ident)
        {
            C.Nn(ident);
            if (dict.TryGetValue(ident.Name, out var envValue))
                return envValue;
            return null;
        }


        [CanBeNull]
        public EvnItem<T> GetWithoutImportOrNull(Ident ident)
        {
            C.Nn(ident);
            if (dict.TryGetValue(ident.Name, out var envValue))
                return envValue;
            if (parent != null)
                return parent.GetWithoutImportOrNull(ident);
            return null;
        }


        [CanBeNull]
        public EvnItem<T> GetOrNull(Ident ident)
        {
            C.Nn(ident);
            var candidates = new Dictionary<QualifiedIdent, EvnItem<T>>();

            var local = GetWithoutImportOrNull(ident);
            if (local != null)
                candidates.Add(new Ident("(local)", TokenType.Ident), local);

            loadFromImports(ident, candidates);

            if (candidates.Count == 1)
                return candidates.First().Value;
            if (candidates.Count == 0)
                return null;
            throw prog.RemarkList.MoreVariableCandidates(candidates, ident);
        }


        private void loadFromImports(Ident ident, Dictionary<QualifiedIdent, EvnItem<T>> candidates)
        {
            C.Nn(ident);

            foreach (var i in imports)
            {
                var x = i.Value.GetFromThisEnvOnlyOrNull(ident);
                if (x != null && !candidates.Any(c => c.Key.ToDebugString() == i.Key.ToDebugString()))
                    candidates.Add(i.Key, x);
            }
            if (parent != null)
                parent.loadFromImports(ident, candidates);
        }


        public EvnItem<T> Get(Ident ident)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v  = GetOrNull(ident);
            if (v == null)
                throw prog.RemarkList.VariableIsNotDeclared(ident);
            return v;
        }


        public void Declare(Ident ident, T value, bool isLet)
        {
            C.Nn(ident, value);

            if (dict.ContainsKey(ident.Name))
                throw prog.RemarkList.VariableIsAlreadyDeclared(ident);
            dict.Add(ident.Name, new EvnItem<T>(value, isLet));
        }

        
        public void Set(Ident ident, T value)
        {
            C.Nn(ident, value);

            var old = Get(ident);
           
            if (old.Value != Void.Instance && (
                    old.Value.Spec != null &&
                    old.Value.Spec != UnknownSpec.Instance &&
                    old.Value.Spec != UnknownSpec.Instance &&
                    old.Value.Spec != AnySpec.Instance) &&
                old.Value.GetType() != value.GetType())
            {
                prog.RemarkList.AssigningDifferentType(ident, old.Value, value);
            }

            if (!(value is Spec) && old.IsLet)
                prog.RemarkList.ReasigingLet(ident);
            old.Value = value;
        }
        

        public void AddImport(QualifiedIdent qi, Env<T> module)
        {
            C.Nn(qi, module);

            imports.Add(qi, module);
        }
    }
}