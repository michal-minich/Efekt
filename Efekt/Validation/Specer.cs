using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Specer
    {
        [CanBeNull] private Spec ret;
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
            env.Set(ident, spec);
        }


        private static void setFromUsage(Ident ident, Spec spec, Env<Spec> env)
        {
        }


        private static void update(Ident ident, Spec spec, Env<Spec> env)
        {
            var oldS = env.Get(ident);
            C.Assume(/*ident.Spec == null ||*/ ReferenceEquals(ident.Spec, oldS));
            env.Set(ident, common(oldS, spec));
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
            Contract.Requires(e.Spec == null || e.Spec  is SimpleSpec);
            C.Ens(e.Spec != null);
            
            switch (e)
            {
                case Declr d:
                    spec(d.Exp, env);
                    d.Ident.Spec = d.Exp.Spec;
                    env.Declare(d.Ident, d.Ident.Spec, d is Let);
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
                        s = env.Get(i);
                    else
                        s = env.GetWithImport(i);
                    var c = common(i.Spec, s);
                    if (!isImportContext)
                    if (env.GetOrNull(i) != null)
                        env.Set(i, c);
                    if (i.Spec == null)
                        i.Spec = c;
                    return c;
                case Return r:
                    var retS = spec(r.Exp, env);
                    ret = common(ret, retS);
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
                    var paramsEnv = Env<Spec>.Create(prog, env);
                    foreach (var p in f.Parameters)
                    {
                        p.Ident.Spec = UnknownSpec.Instance;
                        paramsEnv.Declare(p.Ident, p.Ident.Spec, true);
                    }

                    var fnEnv = Env<Spec>.Create(prog, paramsEnv);
                    if (f.Sequence.Count == 1)
                    {
                        var r = evalSequenceItem(f.Sequence[0], fnEnv);
                        if (ret == null)
                            ret = r;
                        var tmp = ret;
                        ret = null;
                        //callStack.Pop();
                        f.Spec = new FnSpec(f.Parameters.Select(p => fnEnv.Get(p.Ident)).Append(tmp).ToList());
                        return f.Spec;
                    }
                    foreach (var fnItem in f.Sequence)
                    {
                        evalSequenceItemFull(fnItem, fnEnv);
                    }

                    f.Spec = new FnSpec(f.Parameters.Select(p => fnEnv.Get(p.Ident)).Append(ret ?? VoidSpec.Instance).ToList());
                    return f.Spec;
                case When w:
                    var test = spec(w.Test, env, BoolSpec.Instance);
                    //var _ = test.As<BoolSpec>(test, prog); // todo
                    var specT = spec(w.Then, Env<Spec>.Create(prog, env));
                    if (w.Otherwise == null)
                    {
                        w.Spec = VoidSpec.Instance;
                        return w.Spec;
                    }
                    else
                    {
                        var specO = spec(w.Otherwise, Env<Spec>.Create(prog, env));
                        w.Spec = common(specT, specO);
                        return w.Spec;
                    }
                case Loop l:
                    var loopEnv = Env<Spec>.Create(prog, env);
                    foreach (var be in l.Body)
                    {
                        evalSequenceItemFull(be, loopEnv);
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
                    if (ma.Exp.Spec is AnySpec)
                    {
                        var objSEnv = Env<Spec>.Create(prog, env);
                        var objS = new ObjSpec(new List<ObjSpecMember>(), objSEnv, true);
                        objSEnv.Declare(ma.Ident, UnknownSpec.Instance, false);
                        spec(ma.Ident, objSEnv);
                        objS.Members.Add(new ObjSpecMember(ma.Ident, ma.Ident.Spec, false));
                        ma.Exp.Spec = objS;
                        if (ma.Exp is Ident i)
                        {
                            env.Set(i, ma.Exp.Spec);
                        }
                    }
                    
                    var objS2 = ma.Exp.Spec as ObjSpec;
                    if (objS2 == null)
                        throw prog.RemarkList.OnlyObjectsCanHaveMembers(ma);
                    if (objS2.FromUsage)
                    {
                        var member = objS2.Members.FirstOrDefault(m => m.Ident.Name == ma.Ident.Name); // also use type and var/let?
                        if (member == null)
                        {
                            objS2.Env.Declare(ma.Ident, UnknownSpec.Instance, false);
                            spec(ma.Ident, objS2.Env);
                            objS2.Members.Add(new ObjSpecMember(ma.Ident, ma.Ident.Spec, false));
                        }
                    }

                    //var objSpec = exp.As<ObjSpec>(ma.Exp, prog); // todo
                    if (ma.Exp.Spec is ObjSpec o)
                        ma.Spec = o.Env.Get(ma.Ident);
                    else
                        ma.Spec = UnknownSpec.Instance;
                    return ma.Spec;
                case New n:
                    var objEnv = Env<Spec>.Create(prog, env);
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
                    n.Spec = new ObjSpec(members, objEnv);
                    return n.Spec;
                case Value v:
                    return v.Spec;
                case Sequence seq:
                    var scopeEnv = Env<Spec>.Create(prog, env);
                    if (seq.Count == 1)
                        return spec(seq.First(), scopeEnv);
                    foreach (var item in seq)
                    {
                        evalSequenceItemFull(item, scopeEnv);
                    }
                    return seq.Spec;
                case Toss ts:
                    spec(ts.Exception, env);
                    return ts.Spec;
                case Attempt att:
                    spec(att.Body, env);
                    if (att.Grab != null)
                    {
                        var grabEnv = Env<Spec>.Create(prog, env);
                        var exIdent = new Ident("exception", TokenType.Ident) {Spec = AnySpec.Instance};
                        grabEnv.Declare(exIdent, exIdent.Spec, true);
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
                    env.AddImport(imp.QualifiedIdent, modImp.Env);
                    return imp.Spec;
                default:
                    throw new NotSupportedException();
            }
        }


        private void evalSequenceItemFull(Element bodyElement, Env<Spec> env)
        {
            var bodyVal = evalSequenceItem(bodyElement, env);
            // ReSharper disable once PossibleUnintendedReferenceComparison
           /* if (bodyVal != VoidSpec.Instance)
            {
                if (bodyElement is FnApply fna2)
                    prog.RemarkList.ValueReturnedFromFunctionNotUsed(fna2);
                else
                    prog.RemarkList.ValueIsNotAssigned(bodyElement);
            }*/
        }


        private Spec evalSequenceItem(Element bodyElement, Env<Spec> env)
        {
            C.ReturnsNn();
            var bodyVal = spec(bodyElement, env);
            return bodyVal;
        }
    }
}