using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
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
        private readonly Dictionary<QualifiedIdent, Obj> imports = new Dictionary<QualifiedIdent, Obj>();
        [CanBeNull] private readonly Env parent;
        private readonly Prog prog;


        private Env(Prog prog)
        {
            this.prog = prog;
            parent = null;
        }
        

        private Env(Prog prog, Env parent)
        {
            this.prog = prog;
            this.parent = parent;
        }


        public static Env CreateRoot(Prog prog)
        {
            var env = new Env(prog);
            foreach (var b in new Builtins(prog).Values)
                env.dict.Add(b.Name, new EnvValue(b, true));
            return env;
        }


        public static Env Create(Prog prog, Env parent) => new Env(prog, parent);


        public Value Get(Ident ident)
        {
            var v = GetOrNull(ident);
            if (v != null)
                return v;
            throw prog.RemarkList.Except.VariableIsNotDeclared(ident);
        }


        [CanBeNull]
        public Value GetDirectlyOrNull(Ident ident)
        {
            if (dict.TryGetValue(ident.Name, out var envValue))
                return envValue.Value;
            return null;
        }


        [CanBeNull]
        public Value GetOrNull(Ident ident)
        {
            if (dict.TryGetValue(ident.Name, out var envValue))
                return envValue.Value;
            if (parent != null)
                return parent.GetOrNull(ident);
            return null;
        }


        [CanBeNull]
        public Value GetWithImportOrNull(Ident ident)
        {
            var candidates = new Dictionary<QualifiedIdent, Value>();

            var local = GetOrNull(ident);
            if (local != null)
                candidates.Add(new Ident("local", TokenType.Ident), local);

            GetFromImports(ident, candidates);

            if (candidates.Count == 1)
                return candidates.First().Value;
            if (candidates.Count == 0)
                return null;
            throw prog.RemarkList.Except.MoreVariableCandidates(candidates, ident);
        }

        private void GetFromImports(Ident ident, Dictionary<QualifiedIdent, Value> candidates)
        {
            foreach (var i in imports)
            {
                var x = i.Value.Env.GetDirectlyOrNull(ident);
                if (x != null && !candidates.Any(c => c.Key.ToDebugString() == i.Key.ToDebugString()))
                    candidates.Add(i.Key, x);
            }
            if (parent != null)
                parent.GetFromImports(ident, candidates);
        }


        public Value GetWithImport(Ident ident)
        {
            var v  = GetWithImportOrNull(ident);
            if (v == null)
                throw prog.RemarkList.Except.VariableIsNotDeclared(ident);
            return v;
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
                    if (old.Value != Void.Instance && old.Value.GetType() != value.GetType())
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


        public void AddImport(QualifiedIdent qi, Obj module)
        {
            imports.Add(qi, module);
        }
    }
}