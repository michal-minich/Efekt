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
            return CreateRoot(prog, b => b.FixedSpec, true);
        }


        public static Env<TA> CreateRoot<TA>(Prog prog, Func<Builtin, TA> selector, bool buildUsages = false) where TA : class, Element
        {
            C.Nn(prog);
            C.ReturnsNn();

            var dict = new Dictionary<Declr, EvnItem<TA>>();
            foreach (var b in new Builtins(prog).Values)
            {
                var tt = b.Name.Any(ch => ch >= 'a' && ch <= 'z') ? TokenType.Ident : TokenType.Op;
                dict.Add(new Let(new Ident(b.Name, tt), Void.Instance), new EvnItem<TA>(selector(b), true));
            }

            return new Env<TA>(prog, dict, buildUsages);
        }
    }


    public sealed class Env<T> : Env where T : class, Element
    {
        public readonly Dictionary<Declr, EvnItem<T>> Items;
        private readonly bool buildUsages;
        private readonly Dictionary<QualifiedIdent, Env<T>> imports = new Dictionary<QualifiedIdent, Env<T>>();
        [CanBeNull] private readonly Env<T> parent;
        private readonly Prog prog;


        public Env(Prog program, Dictionary<Declr, EvnItem<T>> initialDictionary, bool buildUsages)
        {
            C.Nn(program, initialDictionary);
            C.AllNotNull(initialDictionary);
            prog = program;
            Items = initialDictionary;
            this.buildUsages = buildUsages;
            parent = null;
        }


        public Env(Prog prog, [CanBeNull] Env<T> parent, bool buildUsages)
        {
            C.Nn(prog);
            this.prog = prog;
            this.parent = parent;
            this.buildUsages = buildUsages;
            Items = new Dictionary<Declr, EvnItem<T>>();
        }


        public Env<T> Create()
        {
            C.ReturnsNn();

            return new Env<T>(prog, this, buildUsages);
        }


        public EvnItem<T> GetWithoutImports(Ident ident)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v = GetWithoutImportOrNull(ident);
            return v ?? throw prog.RemarkList.VariableIsNotDeclared(ident);
        }


        public EvnItem<T> GetFromThisEnvOnly(Ident ident, bool? forWrite)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v = GetFromThisEnvOnlyOrNull(ident, forWrite);
            return v ?? throw prog.RemarkList.VariableIsNotDeclared(ident);
        }


        [CanBeNull]
        public EvnItem<T> GetFromThisEnvOnlyOrNull(Ident ident, bool? forWrite)
        {
            C.Nn(ident);
            var kvp = Items.FirstOrDefault(kv => kv.Key.Ident.Name == ident.Name);
            if (kvp.Key == null)
                return null;
            addUsage(kvp.Key, ident, forWrite);
            return kvp.Value;
        }


        [CanBeNull]
        public EvnItem<T> GetWithoutImportOrNull(Ident ident, bool forWrite = false)
        {
            C.Nn(ident);
            var kvp = Items.FirstOrDefault(kv => kv.Key.Ident.Name == ident.Name);

            if (kvp.Key != null)
            {
                addUsage(kvp.Key, ident, forWrite);
                return kvp.Value;
            }

            if (parent != null)
                return parent.GetWithoutImportOrNull(ident, forWrite);
            return null;
        }


        private void addUsage(Declr declr, Ident ident, bool? forWrite)
        {
            if (buildUsages && forWrite != null)
            {
                ident.DeclareBy = declr;
                if (forWrite == true)
                    declr.WrittenBy.Add(ident);
                else
                    declr.ReadBy.Add(ident);
            }
        }


        [CanBeNull]
        public EvnItem<T> GetOrNull(Ident ident, bool forWrite = false)
        {
            C.Nn(ident);
            var candidates = new Dictionary<QualifiedIdent, EvnItem<T>>();

            var local = GetWithoutImportOrNull(ident, forWrite);
            if (local != null)
                candidates.Add(new Ident("(local)", TokenType.Ident), local);

            loadFromImports(ident, candidates, forWrite);

            if (candidates.Count == 1)
                return candidates.First().Value;
            if (candidates.Count == 0)
                return null;
            throw prog.RemarkList.MoreVariableCandidates(candidates, ident);
        }


        private void loadFromImports(Ident ident, Dictionary<QualifiedIdent, EvnItem<T>> candidates, bool forWrite)
        {
            C.Nn(ident);

            foreach (var i in imports)
            {
                var x = i.Value.GetFromThisEnvOnlyOrNull(ident, forWrite);
                if (x != null && !candidates.Any(c => c.Key.ToCodeString() == i.Key.ToCodeString()))
                    candidates.Add(i.Key, x);
            }
            if (parent != null)
                parent.loadFromImports(ident, candidates, forWrite);
        }


        public EvnItem<T> Get(Ident ident, bool forWrite = false)
        {
            C.Nn(ident);
            C.ReturnsNn();

            var v  = GetOrNull(ident, forWrite);
            if (v == null)
                throw prog.RemarkList.VariableIsNotDeclared(ident);
            return v;
        }


        public void Declare(Declr declr, T value)
        {
            C.Nn(declr, value);

            if (Items.Any(kvp => kvp.Key.Ident.Name == declr.Ident.Name))
                throw prog.RemarkList.VariableIsAlreadyDeclared(declr.Ident);
            Items.Add(declr, new EvnItem<T>(value, !(declr is Var)));
        }


        public void Set(Ident ident, T value)
        {
            C.Nn(ident, value);

            var old = Get(ident, true);
           
            // todo move to specer
            /*if (old.Value != Void.Instance && (
                    old.Value.Spec != null &&
                    old.Value.Spec != UnknownSpec.Instance &&
                    old.Value.Spec != UnknownSpec.Instance &&
                    old.Value.Spec != AnySpec.Instance) &&
                old.Value.GetType() != value.GetType())
            {
                prog.RemarkList.AssigningDifferentType(ident, old.Value, value);
            }*/

            if (!(value is Spec) && old.IsLet)
                prog.RemarkList.ReasigingLet(ident);
            old.Value = value;
        }


        public void AddImport(QualifiedIdent qi, Env<T> module, Declr declr)
        {
            C.Nn(qi, module, declr);

            imports.Add(qi, module);
        }
    }
}