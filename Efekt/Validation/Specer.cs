using System;
using System.Collections.Generic;
using System.Linq;

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
            var env = Env.CreateSpecRoot(prog);
            if (prog.RootElement is Exp e)
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                specExp(e, env, AnySpec.Instance);
            else
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                specEl(prog.RootElement, env, VoidSpec.Instance);
        }


        
        [System.Diagnostics.Contracts.Pure]
        private Spec specExp(Exp exp, Env<Spec> env, Spec slot)
        {
            C.Nn(exp, env, slot);
            C.Req(slot != UnknownSpec.Instance);

            var s = specEl(exp, env, slot);
            if (!isAssignable(s, slot))
                prog.RemarkList.CannotConvertType(s, slot, exp);
            return s;
        }


        private void specStm(Stm e, Env<Spec> env)
        {
            C.Nn(e, env);
            var s = specEl(e, env, VoidSpec.Instance);
            C.Assert(s == VoidSpec.Instance);
        }


        private bool isAssignable(Spec s, Spec slot)
        {
            C.Nn(s, slot);
            C.Req(slot != UnknownSpec.Instance);

            switch (slot)
            {
                case AnySpec _:
                case VoidSpec _:
                    return true;
                case FnSpec fnSlot:
                    return s is FnSpec fnS
                           && areAssignable(fnSlot.ParameterSpec, fnS.ParameterSpec)
                           && isAssignable(fnS.ReturnSpec, fnSlot.ReturnSpec);
                case ObjSpec objSlot:
                    return s is ObjSpec objS && isObjAssignable(objS, objSlot);
                case IntSpec _:
                    return s is IntSpec;
                case CharSpec _:
                    return s is CharSpec;
                case BoolSpec _:
                    return s is BoolSpec;
                case TextSpec _:
                    return s is TextSpec;
                case ArrSpec arrSlot:
                    return s is ArrSpec arrS && areSame(arrS.ItemSpec, arrSlot.ItemSpec);
                default:
                    return false;
            }
        }


        private bool isObjAssignable(ObjSpec objS, ObjSpec objSlot)
        {
            foreach (var oM in objS.Members)
            {
                var slotM = objSlot.Members.FirstOrDefault(m => m.Name == oM.Name);
                if (slotM == null || !isAssignable(oM.Spec, slotM.Spec))
                    return false;
            }

            return true;
        }


        private bool areAssignable(IReadOnlyList<Spec> ss, IReadOnlyList<Spec> slots)
        {
            if (ss.Count != slots.Count)
                return false;

            var n = 0;
            foreach (var s in ss)
            {
                if (!isAssignable(s, slots[n++]))
                    return false;
            }

            return true;
        }


        [System.Diagnostics.Contracts.Pure]
        private bool areSame(Spec a, Spec b)
        {
            return a.ToDebugString() == b.ToDebugString();
        }
        
        
        [System.Diagnostics.Contracts.Pure]
        private Spec specEl(Element e, Env<Spec> env, Spec slot)
        {
            C.Nn(e, env);
            C.ReturnsNn();
            
            Spec ss;
            switch (e)
            {
                case Declr d:
                    ss = specExp(d.Exp, env, AnySpec.Instance);
                    env.Declare(d, ss);
                    return VoidSpec.Instance;

                case Assign a:
                    switch (a.To)
                    {
                        case Ident ident:
                            specIdent(a.Exp, env, ident);
                            break;
                        case MemberAccess ma:
                            var maExpS = specExp(ma.Exp, env, new ObjSpec(
                                new List<ObjSpecMember>
                                {
                                    new ObjSpecMember(ma.Ident.Name, AnySpec.Instance)
                                },
                                env));
                            var objS = maExpS.As<ObjSpec>(ma.Exp, prog);
                            var maSlotS = objS.Env.Get(ma.Ident).Value;
                            ss = specExp(a.Exp, env, maSlotS);
                            objS.Env.Set(ma.Ident, ss);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    return VoidSpec.Instance;

                case Ident i:
                    ss = isImportContext
                        ? env.GetWithoutImports(i).Value
                        : env.Get(i).Value;
                    if (isImportContext || !isAssignable(slot, ss) || ss != AnySpec.Instance)
                        return ss;
                    if (slot == VoidSpec.Instance)
                        prog.RemarkList.CannotConvertType(ss, slot, i);
                    env.Set(i, slot);
                    return slot;

                case Return r:
                    var retS = specExp(r.Exp, env, returns.Peek());
                    returns.Push(retS);
                    return VoidSpec.Instance;

                case FnApply fna:
                    var fnI = fna.Fn as Ident;
                    if (fnI == null || fnI.Name != "typeof")
                        ss = specExp(fna.Fn, env, AnySpec.Instance); // todo
                    else
                        ss = null;
                    var fnS = ss as FnSpec;
                    if (fnS == null)
                    {
                        var sigS = new List<Spec>();
                        foreach (var aa in fna.Arguments)
                        {
                            var aS = specExp(aa, env, AnySpec.Instance);
                            sigS.Add(aS);
                        }
                        sigS.Add(slot);
                        var newFnSpec = new FnSpec(sigS);
                        if (fnI != null && fnI.Name != "typeof")
                        {
                            env.Set(fnI, newFnSpec);
                        }
                        fnS = newFnSpec;
                    }
                    else
                    {
                        if (fnS.ParameterSpec.Count != fna.Arguments.Count)
                            throw prog.RemarkList.ParameterArgumentCountMismatch(fna, fnS.ParameterSpec.Count);

                        var ix = 0;
                        foreach (var aa in fna.Arguments)
                        {
                            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                            specExp(aa, env, fnS.ParameterSpec[ix++]);
                        }
                    }

                    if (fnI != null && fnI.Name == "typeof")
                    {
                        fna.Arguments = new FnArguments(new List<Exp> {fnS.ParameterSpec[0]});
                    }

                    return fnS.ReturnSpec;

                case Fn f:
                    var paramsEnv = Env.Create(prog, env);
                    foreach (var p in f.Parameters)
                    {
                        ss = AnySpec.Instance;
                        paramsEnv.Declare(p, ss);
                    }

                    var fnEnv = Env.Create(prog, paramsEnv);
                    if (f.Sequence.Count == 0)
                    {
                        ss = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                            .Append(VoidSpec.Instance).ToList());
                        return ss;
                    }
                    else if (f.Sequence.Count == 1 && f.Sequence[0] is Exp exp1)
                    {
                        returns.Push(VoidSpec.Instance);
                        var ret = specExp(exp1, fnEnv, AnySpec.Instance);
                        returns.Pop();
                        ss = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                            .Append(ret).ToList());
                        return ss;
                    }

                    returns.Push(VoidSpec.Instance);
                    foreach (var fnItem in f.Sequence)
                    {
                        var _ = specEl(fnItem, fnEnv, VoidSpec.Instance);
                    }
                    var ret2 = returns.Pop();

                    ss = new FnSpec(f.Parameters.Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                        .Append(ret2).ToList());
                    return ss;

                case When w:
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    specExp(w.Test, env, BoolSpec.Instance);
                    var thenS = specEl(w.Then, Env.Create(prog, env), slot);
                    if (w.Otherwise == null)
                    {
                        ss = VoidSpec.Instance;
                        return ss;
                    }
                    else
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var otherwiseS = specEl(w.Otherwise, Env.Create(prog, env), slot);
                        if (!areSame(thenS, otherwiseS))
                            throw prog.RemarkList.TypeError();
                        ss = thenS;
                        return ss;
                    }

                case Loop l:
                    specStm(l.Body, env);
                    return VoidSpec.Instance;

                case Break _:
                    return VoidSpec.Instance;

                case Continue _:
                    return VoidSpec.Instance;

                case ArrConstructor ae:
                    Spec prevS = AnySpec.Instance;
                    foreach (var a in ae.Arguments)
                    {
                        ss = specExp(a, env, prevS);
                        prevS = ss;
                    }
                    return new ArrSpec(prevS);

                case MemberAccess ma:
                    ss = specExp(ma.Exp, env, AnySpec.Instance);
                    var mai = ma.Ident;
                    if (ss is AnySpec)
                    {
                        var objSEnv = Env.Create(prog, env);
                        var objS = new ObjSpec(new List<ObjSpecMember>(), objSEnv) {FromUsage = true};
                        var declr = new Var(new Ident(mai.Name, mai.TokenType).CopInfoFrom(mai, true), Void.Instance);
                        objSEnv.Declare(declr, slot);
                        var maiS = specExp(mai, objSEnv, slot);
                        objS.Members.Add(new ObjSpecMember(mai.Name, maiS));
                        ss = objS;
                        if (ma.Exp is Ident i)
                        {
                            env.Set(i, ss);
                        }
                    }

                    var objS2 = ss as ObjSpec;
                    if (objS2 == null)
                        throw prog.RemarkList.OnlyObjectsCanHaveMembers(ma, ss);
                    if (objS2.FromUsage)
                    {
                        var member = objS2.Members.FirstOrDefault(m => m.Name == mai.Name); // also use type and var/let?
                        if (member == null)
                        {
                            var declr = new Var(new Ident(mai.Name, mai.TokenType).CopInfoFrom(mai, true), Void.Instance);
                            objS2.Env.Declare(declr, slot);
                            var maiS = specExp(mai, objS2.Env, slot);
                            objS2.Members.Add(new ObjSpecMember(mai.Name, maiS));
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
                        var _ = specEl(classItem, objEnv, AnySpec.Instance);
                        if (classItem is Declr d)
                            members.Add(new ObjSpecMember(d.Ident.Name, objEnv.Get(d.Ident).Value));
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
                    if (seq.Count == 1 && seq[0] is Exp exp)
                    {
                        ss = specExp(exp, scopeEnv, AnySpec.Instance); // todo use FN spec return type instead of any
                        returns.UpdateTop(ss);
                        return ss;
                    }
                    else
                    {
                        foreach (var item in seq)
                        {
                            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                            specEl(item, scopeEnv, VoidSpec.Instance);
                        }

                        return VoidSpec.Instance;
                    }

               case Toss ts:
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    specExp(ts.Exception, env, AnySpec.Instance);
                    // TODO requires exp to be of some type?
                    return VoidSpec.Instance;

                case Attempt att:
                    specStm(att.Body, env);
                    if (att.Grab != null)
                    {
                        var grabEnv = Env.Create(prog, env);
                        var exDeclr = new Param(new Ident("exception", TokenType.Ident));
                        grabEnv.Declare(exDeclr, AnySpec.Instance);
                        // ReSharper disable once AssignNullToNotNullAttribute
                        specStm(att.Grab, grabEnv);
                    }

                    if (att.AtLast != null)
                        // ReSharper disable once AssignNullToNotNullAttribute
                        specStm(att.AtLast, env);
                    return VoidSpec.Instance;

                case Import imp:
                    isImportContext = true;
                    var modImpEl = specExp(imp.QualifiedIdent, env, AnySpec.Instance); // should be some obj, but it is checked below
                    isImportContext = false;
                    var modImp = modImpEl.As<ObjSpec>(imp, prog);
                    env.AddImport(imp.QualifiedIdent, modImp.Env, (Declr) modImp.Parent);
                    return VoidSpec.Instance;

                default:
                    throw new NotSupportedException();
            }
        }


        private void specIdent(Exp exp, Env<Spec> env, Ident i)
        {
            var slot = env.Get(i).Value;
            var s = specExp(exp, env, slot);
            env.Set(i, s);
        }
    }
}