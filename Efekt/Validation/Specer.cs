using System;
using System.Collections.Generic;
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
            var _ = spec(prog.RootElement, Env.CreateSpecRoot(prog));
        }

        

        [System.Diagnostics.Contracts.Pure]
        private Spec moreSpecific([CanBeNull] Spec s1, [NotNull] Spec s2, [NotNull] Exp exp)
        {
            C.ReturnsNn();

            if (s1 == null)
                return s2;

            if (s1.ToDebugString() == s2.ToDebugString())
                return s1;

            var arr = new List<Spec> {s1, s2};

            arr.Remove(VoidSpec.Instance);
            arr.Remove(AnySpec.Instance);
            arr.Remove(UnknownSpec.Instance);

            if (arr.Count == 0)
                return AnySpec.Instance;
            if (arr.Count == 1)
                return arr[0];

            prog.RemarkList.CannotConvertType(s1, s2, exp);

            return arr[1];
        }


        private void setFromTop(Ident ident, Spec spec, Env<Spec> env)
        {
            C.Nn(ident, spec, env);
            var oldS = env.Get(ident).Value;
            if (oldS != UnknownSpec.Instance 
                && oldS != VoidSpec.Instance 
                && oldS != AnySpec.Instance
                && oldS.ToDebugString() != moreSpecific(oldS, spec, ident).ToDebugString())
                throw new Exception();
            env.Set(ident, spec);
        }

        
        private void setFromUsage(Exp exp, Spec s, Env<Spec> env)
        {
            C.Nn(exp, s, env);

            switch (exp)
            {
                case Ident i:
                    var oldS = env.Get(i).Value;
                    var newS = moreSpecific(oldS, s, exp);
                    env.Set(i, newS);
                    break;
                case MemberAccess ma:
                    setFromUsage(ma.Ident, s, spec(ma.Exp, env).As<ObjSpec>(ma.Exp, prog).Env);
                    break;
                default:
                    return;
            }
        }


        [System.Diagnostics.Contracts.Pure]
        private Spec spec(Element e, Env<Spec> env)
        {
            C.Nn(e, env);
            C.ReturnsNn();
            
            Spec ss;
            switch (e)
            {
                case Declr d:
                    ss = spec(d.Exp, env);
                    env.Declare(d, ss);
                    return VoidSpec.Instance;

                case Assign a:
                    ss = spec(a.Exp, env);
                    switch (a.To)
                    {
                        case Ident ident:
                            setFromTop(ident, ss, env);
                            break;
                        case MemberAccess ma:
                            var maExpS = spec(ma.Exp, env);
                            var objS = maExpS.As<ObjSpec>(ma.Exp, prog);
                            setFromTop(ma.Ident, ss, objS.Env);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    return VoidSpec.Instance;

                case Ident i:
                    ss = isImportContext
                        ? env.GetWithoutImports(i).Value
                        : env.Get(i).Value;
                    return ss;

                case Return r:
                    var retS = spec(r.Exp, env);
                    returns.Push(moreSpecific(returns.Pop(), retS, r.Exp));
                    return VoidSpec.Instance;

                case FnApply fna:
                    var fnI = fna.Fn as Ident;
                    if (fnI == null || fnI.Name != "typeof")
                        ss = spec(fna.Fn, env);
                    else
                        ss = null;
                    var fnS = ss as FnSpec;
                    if (fnS == null)
                    {
                        var argsS = new List<Spec>();
                        foreach (var aa in fna.Arguments)
                        {
                            var aS = spec(aa, env);
                            setFromUsage(aa, aS, env);
                            argsS.Add(aS);
                        }
                        argsS.Add(UnknownSpec.Instance);
                        var newFnSpec = new FnSpec(argsS);
                        if (fnI != null && fnI.Name != "typeof")
                            setFromUsage(fnI, newFnSpec, env);
                        fnS = newFnSpec;
                    }
                    else
                    {
                        if (fnS.ParameterSpec.Count != fna.Arguments.Count)
                            throw prog.RemarkList.ParameterArgumentCountMismatch(fna, fnS.ParameterSpec.Count);

                        var ix = 0;
                        foreach (var aa in fna.Arguments)
                        {
                            var aS = spec(aa, env);
                            var pS = fnS.ParameterSpec[ix++];
                            var bestArgS = moreSpecific(aS, pS, aa);
                            var debugString = bestArgS.ToDebugString();
                            if (pS != AnySpec.Instance && debugString != pS.ToDebugString())
                            {
                                prog.RemarkList.CannotConvertArgumentToParameter(aa, aS, pS);
                            }

                            setFromUsage(aa, pS, env);
                        }
                    }

                    if (fnI != null && fnI.Name == "typeof")
                    {
                        fna.Arguments = new FnArguments(new List<Exp> {fnS.ParameterSpec[0]});
                    }

                    return fnS.ReturnSpec;

                case Fn f:
                    returns.Push(null);
                    var paramsEnv = Env.Create(prog, env);
                    foreach (var p in f.Parameters)
                    {
                        ss = AnySpec.Instance;
                        paramsEnv.Declare(p, ss);
                    }

                    var fnEnv = Env.Create(prog, paramsEnv);
                    if (f.Sequence.Count == 0)
                    {
                        returns.Pop();
                        ss = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                            .Append(VoidSpec.Instance).ToList());
                        return ss;
                    }
                    else if (f.Sequence.Count == 1 && f.Sequence[0] is Exp)
                    {
                        var ret = spec(f.Sequence[0], fnEnv);
                        returns.Pop();
                        ss = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                            .Append(ret).ToList());
                        return ss;
                    }

                    foreach (var fnItem in f.Sequence)
                    {
                        var _ = spec(fnItem, fnEnv);
                    }

                    var ret2 = returns.Pop();
                    ss = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                        .Append(ret2 ?? VoidSpec.Instance).ToList());
                    return ss;

                case When w:
                    setFromUsage(w.Test, BoolSpec.Instance, env);
                    var testS = spec(w.Test, env);
                    if (testS != BoolSpec.Instance)
                        prog.RemarkList.CannotConvertType(testS, BoolSpec.Instance, w.Test);
                    var thenS = spec(w.Then, Env.Create(prog, env));
                    if (w.Otherwise == null)
                    {
                        ss = VoidSpec.Instance;
                        return ss;
                    }
                    else
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var otherwiseS = spec(w.Otherwise, Env.Create(prog, env));
                        if (thenS.ToDebugString() != otherwiseS.ToDebugString())
                            throw prog.RemarkList.TypeError();
                        ss = thenS;// moreSpecific(thenS, otherwiseS);
                        return ss;
                    }

                case Loop l:
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    spec(l.Body, env);
                    return VoidSpec.Instance;

                case Break _:
                    return VoidSpec.Instance;

                case Continue _:
                    return VoidSpec.Instance;

                case ArrConstructor ae:
                    var items = ae.Arguments.Select(a => spec(a, env)).ToArray();
                    ss = new ArrSpec(items.Aggregate((s1, s2) => moreSpecific(s1, s2, ae)));
                    return ss;

                case MemberAccess ma:
                    ss = spec(ma.Exp, env);
                    var mai = ma.Ident;
                    if (ss is AnySpec)
                    {
                        var objSEnv = Env.Create(prog, env);
                        var objS = new ObjSpec(new List<ObjSpecMember>(), objSEnv) {FromUsage = true};
                        var declr = new Var(new Ident(mai.Name, mai.TokenType).CopInfoFrom(mai, true), Void.Instance);
                        objSEnv.Declare(declr, UnknownSpec.Instance);
                        var maiS = spec(mai, objSEnv);
                        objS.Members.Add(new ObjSpecMember(declr, maiS));
                        ss = objS;
                        if (ma.Exp is Ident i)
                        {
                            setFromUsage(i, ss, env);
                        }
                    }

                    var objS2 = ss as ObjSpec;
                    if (objS2 == null)
                        throw prog.RemarkList.OnlyObjectsCanHaveMembers(ma, ss);
                    if (objS2.FromUsage)
                    {
                        var member = objS2.Members.FirstOrDefault(m => m.Declr.Ident.Name == mai.Name); // also use type and var/let?
                        if (member == null)
                        {
                            var declr = new Var(new Ident(mai.Name, mai.TokenType).CopInfoFrom(mai, true), Void.Instance);
                            objS2.Env.Declare(declr, UnknownSpec.Instance);
                            var maiS = spec(mai, objS2.Env);
                            objS2.Members.Add(new ObjSpecMember(declr, maiS));
                        }
                    }

                    var objSpec = (ObjSpec)ss;
                    ss = objSpec.Env.GetFromThisEnvOnly(mai, null).Value;
                    return ss;

                case New n:
                    var objEnv = Env.Create(prog, env);
                    var members = new List<ObjSpecMember>();
                    foreach (var classItem in n.Body)
                    {
                        var _ = spec(classItem, objEnv);
                        if (classItem is Declr d)
                            members.Add(new ObjSpecMember(d, objEnv.Get(d.Ident).Value));
                    }

                    ss = new ObjSpec(members, objEnv) {Parent = n.Parent};
                    return ss;

                case Text _:
                    return TextSpec.Instance;
                    
                case Bool _:
                    return BoolSpec.Instance;

                case Builtin bu:
                    return bu.FixedSpec;

                case Char _:
                    return CharSpec.Instance;
                    
                case Int _:
                    return IntSpec.Instance;
                    
                case Void _:
                    return VoidSpec.Instance;

                case Sequence seq:
                    var scopeEnv = Env.Create(prog, env);
                    ss = null;
                    foreach (var item in seq)
                        ss = spec(item, scopeEnv);
                    if (seq.Count == 1 && seq[0] is Exp)
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        returns.UpdateTop(ss);
                        return ss;
                    }
                    return VoidSpec.Instance;

               case Toss ts:
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    spec(ts.Exception, env);
                    // TODO requires exp to be of some type?
                    return VoidSpec.Instance;

                case Attempt att:
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    spec(att.Body, env);
                    if (att.Grab != null)
                    {
                        var grabEnv = Env.Create(prog, env);
                        var exDeclr = new Param(new Ident("exception", TokenType.Ident));
                        grabEnv.Declare(exDeclr, AnySpec.Instance);
                        // ReSharper disable once AssignNullToNotNullAttribute
                        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                        spec(att.Grab, grabEnv);
                    }

                    if (att.AtLast != null)
                        // ReSharper disable once AssignNullToNotNullAttribute
                        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                        spec(att.AtLast, env);
                    return VoidSpec.Instance;

                case Import imp:
                    isImportContext = true;
                    var modImpEl = spec(imp.QualifiedIdent, env);
                    isImportContext = false;
                    var modImp = modImpEl.As<ObjSpec>(imp, prog);
                    env.AddImport(imp.QualifiedIdent, modImp.Env, (Declr) modImp.Parent);
                    return VoidSpec.Instance;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}