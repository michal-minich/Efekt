using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Specer
    {
        private readonly Stack<Spec> returns = new Stack<Spec>();
        private readonly Prog prog;
        private bool isImportContext;

        
        public Specer(Prog prog)
        {
            this.prog = prog;
        }


        public void Spec()
        {
            spec(prog.RootElement, Env.CreateSpecRoot(prog));
        }


        [JetBrains.Annotations.Pure]
        private static Spec common([NotNull] params Spec[] specs)
        {
            C.Nn(specs);
            C.ReturnsNn();
            
            var possible = new List<Spec>();

            foreach (var s in specs)
            {
                if (s == null || s is AnySpec || s is UnknownSpec)
                    continue;
                if (s is AnyOfSpec anyOf)
                {
                    possible.AddRange(anyOf.Possible);
                    possible.Remove(VoidSpec.Instance);
                }
                else
                {
                    possible.Add(s);
                }
            }

            C.Assume(!possible.Contains(AnySpec.Instance));

            var c = possible.Distinct(new SpecComparer()).ToList();

            if (c.Count == 0)
                return AnySpec.Instance;

            if (c.Count == 1)
                return c[0];
            else
                return new AnyOfSpec(c);
        }


        private static void setFromTop(Ident ident, Spec spec, Env<Spec> env)
        {
            C.Req(ident.Spec == null);
            C.Req(spec != AnySpec.Instance);
            ident.Spec = spec;
            set(ident, spec, env);
        }


        private static void set(Ident ident, Spec spec, Env<Spec> env)
        {
            env.Set(ident, spec);
            setSources(ident, spec);
        }


        private static void setSources(Ident ident, Spec spec1)
        {
        }


        private static void setFromUsage(Ident ident, Spec spec, Env<Spec> env)
        {
        }


        private static void update(Ident ident, Spec spec, Env<Spec> env)
        {
            var oldS = env.GetFromThisEnvOnly(ident, true).Value;
            C.Assume(/*ident.Spec == null ||*/ ReferenceEquals(ident.Spec, oldS));
            set(ident, common(oldS, spec), env);
        }


        private void read(Element e)
        {
            if (wantsRead == null)
                return;
            e.Spec = common(e.Spec, wantsRead);
            wantsRead = null;
        }


        [CanBeNull]
        private Spec wantsRead;
        private Spec spec(Element e, Env<Spec> env, Spec specToBeRead)
        {
            if (e is Ident)
                wantsRead = specToBeRead;
            var s = spec(e, env);
            wantsRead = null;
            return s;
        }


        private Spec spec(Element e, Env<Spec> env)
        {
            C.Nn(e, env);
            Contract.Requires(e.Spec == null || e.Spec is SimpleSpec);
            C.EnsNn(e.Spec);
            
            switch (e)
            {
                case Declr d:
                    spec(d.Exp, env);
                    d.Ident.Spec = d.Exp.Spec;
                    env.Declare(d, d.Ident.Spec);
                    C.Assume(d.Spec == VoidSpec.Instance);
                    return VoidSpec.Instance;
                case Assign a:
                    spec(a.Exp, env);
                    switch (a.To)
                    {
                        case Ident ident:
                            setFromTop(ident, a.Exp.Spec, env);
                            break;
                        case MemberAccess ma:
                            var obj = spec(ma.Exp, env);
                            var o2 = obj.As<ObjSpec>(ma, prog);
                            setFromTop(ma.Ident, a.Exp.Spec, o2.Env);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    return a.Spec;
                case Ident i:
                    read(i);
                    Spec s;
                    if (isImportContext)
                        s = env.GetWithoutImports(i).Value;
                    else
                        s = env.Get(i).Value;
                    var c = common(i.Spec, s);
                    if (!isImportContext)
                    if (env.GetWithoutImportOrNull(i) != null)
                        set(i, c, env);
                    if (i.Spec == null)
                        i.Spec = c;
                    else if (i.Spec.ToDebugString() != c.ToDebugString())
                    {
                        i.Spec = c;
                    }

                    return c;
                case Return r:
                    var retS = spec(r.Exp, env);
                    returns.Push(common(returns.Pop(), retS));
                    return r.Spec;
                case FnApply fna:
                    if (!(fna.Fn is Ident fnI) || fnI.Name != "typeof")
                        spec(fna.Fn, env);
                    var fnS = fna.Fn.Spec as FnSpec;
                    var ix = 0;
                    if (fnS != null && fnS.ParameterSpec.Count != fna.Arguments.Count)
                        throw prog.RemarkList.ParameterArgumentCountMismatch(fna, fnS.ParameterSpec.Count);
                    foreach (var aa in fna.Arguments)
                    {
                        if (fnS != null)
                            spec(aa, env, fnS.ParameterSpec[ix++]);
                        else
                            spec(aa, env);
                    }

                    if (fnS != null)
                        fna.Spec = fnS.ReturnSpec;
                    else
                        fna.Spec = UnknownSpec.Instance;
                    return fna.Spec;
                case Fn f:
                    returns.Push(UnknownSpec.Instance);
                    var paramsEnv = Env.Create(prog, env);
                    foreach (var p in f.Parameters)
                    {
                        p.Ident.Spec = UnknownSpec.Instance;
                        paramsEnv.Declare(p, p.Ident.Spec);
                    }

                    var fnEnv = Env.Create(prog, paramsEnv);
                    if (f.Sequence.Count == 0)
                    {
                        returns.Pop();
                        f.Spec = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value).Append(VoidSpec.Instance).ToList());
                        return f.Spec;
                    }
                    else if (f.Sequence.Count == 1 && f.Sequence[0] is Exp)
                    {
                        var ret = spec(f.Sequence[0], fnEnv);
                        returns.Pop();
                        f.Spec = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value).Append(ret).ToList());
                        return f.Spec;
                    }
                    foreach (var fnItem in f.Sequence)
                    {
                        spec(fnItem, fnEnv);
                    }

                    var ret2 = returns.Pop();
                    f.Spec = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value).Append(ret2 ?? VoidSpec.Instance).ToList());
                    return f.Spec;
                case When w:
                    var test = spec(w.Test, env, BoolSpec.Instance);
                    //var _ = test.As<BoolSpec>(test, prog); // todo
                    var specT = spec(w.Then, Env.Create(prog, env));
                    if (w.Otherwise == null)
                    {
                        w.Spec = VoidSpec.Instance;
                        return w.Spec;
                    }
                    else
                    {
                        var specO = spec(w.Otherwise, Env.Create(prog, env));
                        w.Spec = common(specT, specO);
                        return w.Spec;
                    }
                case Loop l:
                    var loopEnv = Env.Create(prog, env);
                    foreach (var be in l.Body)
                    {
                        spec(be, loopEnv);
                    }
                    return VoidSpec.Instance;
                case Break br:
                    return br.Spec;
                case Continue ct:
                    return ct.Spec;
                case ArrConstructor ae:
                    var items = ae.Arguments.Select(a => spec(a, env)).ToArray();
                    ae.Spec = new ArrSpec(common(items));
                    return ae.Spec;
                case MemberAccess ma:
                    spec(ma.Exp, env);
                    var mai = ma.Ident;
                    if (ma.Exp.Spec is AnySpec)
                    {
                        var objSEnv = Env.Create(prog, env);
                        var objS = new ObjSpec(new List<ObjSpecMember>(), objSEnv, true);
                        objSEnv.Declare(new Var(new Ident(mai.Name, mai.TokenType).CopInfoFrom(mai, true), Void.Instance), UnknownSpec.Instance);
                        spec(mai, objSEnv);
                        objS.Members.Add(new ObjSpecMember(mai, mai.Spec, false));
                        ma.Exp.Spec = objS;
                        if (ma.Exp is Ident i)
                        {
                            set(i, ma.Exp.Spec, env);
                        }
                    }
                    
                    var objS2 = ma.Exp.Spec as ObjSpec;
                    if (objS2 == null)
                        throw prog.RemarkList.OnlyObjectsCanHaveMembers(ma);
                    if (objS2.FromUsage)
                    {
                        var member = objS2.Members.FirstOrDefault(m => m.Ident.Name == mai.Name); // also use type and var/let?
                        if (member == null)
                        {
                            objS2.Env.Declare(new Var(new Ident(mai.Name, mai.TokenType).CopInfoFrom(mai, true), Void.Instance), UnknownSpec.Instance);
                            spec(mai, objS2.Env);
                            objS2.Members.Add(new ObjSpecMember(mai, mai.Spec, false));
                        }
                    }

                    //var objSpec = exp.As<ObjSpec>(ma.Exp, prog); // todo
                    if (ma.Exp.Spec is ObjSpec o)
                        ma.Spec = o.Env.GetFromThisEnvOnly(mai, null).Value;
                    else
                        ma.Spec = UnknownSpec.Instance;
                    return ma.Spec;
                case New n:
                    var objEnv = Env.Create(prog, env);
                    var members = new List<ObjSpecMember>();
                    foreach (var v in n.Body)
                    {
                        spec(v, objEnv);
                        if (v is Declr d)
                        {
                            var ss = d.Exp == null ? d.Spec : d.Exp.Spec;
                            members.Add(new ObjSpecMember(d.Ident, ss, d is Let));
                        }
                    }
                    n.Spec = new ObjSpec(members, objEnv) { Parent = n.Parent };
                    return n.Spec;
                case Value v:
                    return v.Spec;
                case Sequence seq:
                    var scopeEnv = Env.Create(prog, env);
                    if (seq.Count == 1)
                        return spec(seq.First(), scopeEnv);
                    foreach (var item in seq)
                    {
                        spec(item, scopeEnv);
                    }
                    return seq.Spec;
                case Toss ts:
                    spec(ts.Exception, env);
                    return ts.Spec;
                case Attempt att:
                    spec(att.Body, env);
                    if (att.Grab != null)
                    {
                        var grabEnv = Env.Create(prog, env);
                        var exDeclr = new Param(new Ident("exception", TokenType.Ident) {Spec = AnySpec.Instance});
                        grabEnv.Declare(exDeclr, exDeclr.Ident.Spec);
                        spec(att.Grab, grabEnv);
                    }
                    if (att.AtLast != null)
                        spec(att.AtLast, env);
                    return att.Spec;
                case Import imp:
                    isImportContext = true;
                    var modImpEl = spec(imp.QualifiedIdent, env);
                    isImportContext = false;
                    var modImp = modImpEl.As<ObjSpec>(imp, prog);
                    env.AddImport(imp.QualifiedIdent, modImp.Env, (Declr)modImp.Parent);
                    return imp.Spec;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}