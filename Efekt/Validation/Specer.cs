using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Specer
    {
        private readonly Prog prog;
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
            C.Req(e.Spec == null);
            C.Ens(e.Spec != null);
            e.Spec = spec;
            return spec;
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
                    set(p.Ident, AnySpec.Instance);
                    env.Declare(p.Ident, AnySpec.Instance, true);
                    return p.Spec;
                case Declr d:
                    var s = spec(d.Exp, env);
                    set(d, AnySpec.Instance);
                    set(d.Ident, s);
                    env.Declare(d.Ident, s, !(d is Var));
                    return s;
                case Assign a:
                    switch (a.To)
                    {
                        case Ident ident:
                            var s2 = spec(a.Exp, env);
                            set(a, VoidSpec.Instance);
                            var d = ident.DeclareBy.Spec;
                            if (d != s2)
                                throw prog.RemarkList.ExpectedDifferentType(a.Exp, s2, d.ToString());
                            set(a.To, s2);
                            env.Set(ident, s2);
                            return set(a, VoidSpec.Instance);
                        case MemberAccess ma:
                            return set(ma, VoidSpec.Instance); // TODO
                        default:
                            throw new NotSupportedException();
                    }
                case Ident i:
                    //C.Assume(i.DeclareBy != null);
                    return set(i, env.Get(i));
                case Return r:
                    return set(r, VoidSpec.Instance);
                case FnApply fna:
                    spec(fna.Fn, env);
                    foreach (var a in fna.Arguments)
                        spec(a, env);
                    return set(fna, VoidSpec.Instance); // TODO
                case Fn f:
                    var fnParamsEnv = Env<Spec>.Create(prog, env);
                    foreach (var p in f.Parameters)
                        spec(p, fnParamsEnv);
                    var fnBodyEnv = Env<Spec>.Create(prog, fnParamsEnv);
                    spec(f.Sequence, fnBodyEnv);
                    return set(f, new FnSpec(new List<Spec>())); // TODO
                case When w:
                    var test = spec(w.Test, env);
                    if (test != BoolSpec.Instance)
                        throw prog.RemarkList.ExpectedDifferentType(w.Test, test, "Bool");
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
                case Int i:
                    return set(i, IntSpec.Instance);
                case Void v:
                    return set(v, VoidSpec.Instance);
                case Sequence seq:
                    var scopeEnv = Env<Spec>.Create(prog, env);
                    foreach (var si in seq)
                        spec(si, scopeEnv);
                    return set(seq, VoidSpec.Instance);
                case Attempt att:
                    return set(att, VoidSpec.Instance);
                case Import imp:
                    return set(imp, VoidSpec.Instance);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}