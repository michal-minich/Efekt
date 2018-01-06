using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Specer
    {
        [CanBeNull] private Spec wantsRead;
        private List<Spec> rets;
        private bool isBreak;
        private bool isContinue;
        private Prog prog;
        private bool isImportContext;

        public Specer(Prog prog)
        {
            this.prog = prog;
        }


        public void Spec()
        {
            spec(prog.RootElement, Env<Declr>.CreateSpecRoot(prog));
        }


        private Spec set(Element e, [NotNull] Spec spec)
        {
            C.Nn(e, spec);
            C.Req(e.Spec == null);
            C.Ens(e.Spec != null);
            e.Spec = spec;
            return spec;
        }


        [Pure]
        private Spec declareAs(Ident i, [NotNull] Spec spec)
        {
            C.Nn(i, spec);
            C.Ens(i.Spec != null);

            i.Spec = addTypeUsage(i.Spec, spec);
            return i.Spec;
        }


        [Pure]
        private Spec readAs(Ident i, [NotNull] Spec spec)
        {
            C.Nn(i, spec);
            C.Ens(i.Spec != null);

            if (spec == VoidSpec.Instance)
                throw prog.RemarkList.VariableIsNotYetInitializied(i);

            if (i.Spec is AnyOfSpec anyOf && anyOf.Possible.Contains(VoidSpec.Instance))
                throw prog.RemarkList.VariableMightNotYetBeInitialized(i);

            i.Spec = addTypeUsage(i.Spec, spec);
            return i.Spec;
        }


        [Pure]
        private Spec writeAs(Ident i, [NotNull] Spec spec)
        {
            C.Nn(i, spec);
            //C.Req(spec != VoidSpec.Instance);
            C.Ens(i.Spec != null);

            if (spec == VoidSpec.Instance)
                i.Spec = null;

            if (i.Spec == AnySpec.Instance)
                i.Spec = null;

            i.Spec = addTypeUsage(i.Spec, spec);
            return i.Spec;
        }


        [Pure]
        private static Spec addTypeUsage([CanBeNull] Spec current, Spec toAdd)
        {
            C.Nn(toAdd);
            C.ReturnsNn();

            if (current == null)
                return toAdd;

            if (current == toAdd) // todo value comparison
                return current;

            if (current is AnyOfSpec anyOf)
            {
                C.Assume(!anyOf.Possible.Contains(AnySpec.Instance));
                foreach (var ao in anyOf.Possible)
                    if (ao == toAdd) // todo value comparison
                        return current;
                anyOf.Possible.Add(toAdd);
            }
            else
            {
                current = new AnyOfSpec(new List<Spec> {current, toAdd});
            }

            return current;
        }


        [Pure]
        private static bool isAssignable([CanBeNull] Spec current, Spec toBeAssigned)
        {
            C.Nn(current, toBeAssigned);
            
            if (current == AnySpec.Instance || current == toBeAssigned) // todo value comparison
                return true;

            if (current is AnyOfSpec anyOf)
            {
                C.Assume(!anyOf.Possible.Contains(AnySpec.Instance));
                if (anyOf.Possible.Contains(toBeAssigned))
                    return true;
            }

            return false;
        }


        private static Spec simplify(List<Spec> specs)
        {
            Spec s = null;
            foreach (var ss in specs)
                s = addTypeUsage(s, ss);
            return s;
        }


        private Spec spec(Element e, Env<Spec> env)
        {
            C.Nn(e, env);
            C.Req(e.Spec == null);
            C.Ens(e.Spec != null);
            
            switch (e)
            {
                case Param p:
                    set(p, AnySpec.Instance);
                    declareAs(p.Ident, AnySpec.Instance);
                    env.Declare(p.Ident, AnySpec.Instance, true);
                    return p.Spec;
                case Declr d:
                    var s = spec(d.Exp, env);
                    set(d, AnySpec.Instance);
                    declareAs(d.Ident, s);
                    env.Declare(d.Ident, s, !(d is Var));
                    return s;
                case Assign a:
                    switch (a.To)
                    {
                        case Ident ident:
                            var expSpec = spec(a.Exp, env);
                            writeAs(ident, expSpec);
                            ident.DeclareBy.Spec = ident.Spec;
                            env.Set(ident, ident.Spec);
                            break;
                        case MemberAccess ma:
                            // TODO
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    return set(a, VoidSpec.Instance);
                case Ident i:
                    //C.Assume(i.DeclareBy != null);
                    set(i, env.Get(i));
                    if (wantsRead != null)
                    {
                        writeAs(i, wantsRead);
                        wantsRead = null;
                    }
                    if (i.DeclareBy == null || i.DeclareBy.Spec == null)
                        return i.Spec;
                    if (!isAssignable(i.DeclareBy.Spec, i.Spec))
                        prog.RemarkList.ExpectedDifferentType(i, i.DeclareBy.Spec, i.Spec);
                    return i.Spec;
                case Return r:
                    rets.AddValue(spec(r.Exp, env));
                    return VoidSpec.Instance;
                case FnApply fna:
                    spec(fna.Fn, env);
                    foreach (var a in fna.Arguments)
                        spec(a, env);
                    return set(fna, VoidSpec.Instance); // TODO
                case Fn f:
                    rets = new List<Spec>();
                    var fnParamsEnv = Env<Spec>.Create(prog, env);
                    var sig = new List<Spec>();
                    foreach (var p in f.Parameters)
                        spec(p, fnParamsEnv);
                    var fnBodyEnv = Env<Spec>.Create(prog, fnParamsEnv);
                    spec(f.Sequence, fnBodyEnv);
                    C.Assume(rets != null && rets.Count >= 1);
                    foreach (var p in f.Parameters)
                        sig.AddValue(p.Ident.DeclareBy.Spec);
                    var sret = simplify(rets);
                    sig.AddValue(sret);
                    return set(f, new FnSpec(sig)); // TODO
                case When w:
                    wantsRead = BoolSpec.Instance;
                    var test = spec(w.Test, env);
                    if (test != BoolSpec.Instance)
                        prog.RemarkList.ExpectedDifferentType(w.Test, BoolSpec.Instance, w.Test.Spec);
                    var th = spec(w.Then, env);
                    set(w, VoidSpec.Instance);
                    if (w.Otherwise == null)
                    {
                        return th;
                    }
                    else
                    {
                        var os = spec(w.Otherwise, env);
                        if (th == os) // TODO value coparison
                            return th;
                        return new AnyOfSpec(new List<Spec> {th, os});
                    }
                case Loop l:
                    spec(l.Body, env);
                    return set(l, VoidSpec.Instance); // TODO
                case Break br:
                    return set(br, VoidSpec.Instance);
                case Continue ct:
                    return set(ct, VoidSpec.Instance);
                case ArrConstructor ae:
                    return set(ae, new ArrSpec()); // TODO
                case MemberAccess ma:
                    return set(ma, VoidSpec.Instance); // TODO
                case New n:
                    var objEnv = Env<Spec>.Create(prog, env);
                    foreach (var v in n.Body)
                        spec(v, objEnv);
                    return set(n, new ObjSpec()); // TODO
                case Bool b:
                    return set(b, BoolSpec.Instance);
                case Builtin bu:
                    return set(bu, bu.FnSpec); // TODO
                case Char ch:
                    return set(ch, CharSpec.Instance);
                case Text t:
                    return set(t, TextSpec.Instance);
                case Int i:
                    return set(i, IntSpec.Instance);
                case Void v:
                    return set(v, VoidSpec.Instance);
                case Sequence seq:
                    var scopeEnv = Env<Spec>.Create(prog, env);
                    if (seq.Count == 1)
                    {
                        rets.AddValue(spec(seq.First(), scopeEnv));
                        return set(seq, VoidSpec.Instance);
                    }
                    foreach (var si in seq)
                        spec(si, scopeEnv);
                    return set(seq, VoidSpec.Instance);
                case Toss ts:
                    spec(ts.Exception, env);
                    return set(ts, VoidSpec.Instance);
                case Attempt att:
                    spec(att.Body, env);
                    if (att.Grab != null)
                        spec(att.Grab, env);
                    if (att.AtLast != null)
                        spec(att.AtLast, env);
                    return set(att, VoidSpec.Instance);
                case Import imp:
                    return set(imp, VoidSpec.Instance);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}