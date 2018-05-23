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
                specElement(prog.RootElement, env, VoidSpec.Instance);
        }


        private bool isAssignable(Spec s, Spec slot)
        {
            C.Nn(s, slot);

            switch (slot)
            {
                case UnknownSpec _:
                case AnySpec _:
                    return true;
                case VoidSpec _:
                    return s is VoidSpec;
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
                case FnSpec fnSlot:
                    return s is FnSpec fnS
                           && areAssignable(fnSlot.ParameterSpec, fnS.ParameterSpec)
                           && (fnSlot.ReturnSpec is VoidSpec || isAssignable(fnS.ReturnSpec, fnSlot.ReturnSpec));
                case ObjSpec objSlot:
                    return s is ObjSpec objS && isObjAssignable(objS, objSlot);
                default:
                    return false;
            }
        }

        
        private bool isObjAssignable(ObjSpec objS, ObjSpec objSlot)
        {
            foreach (var objSlotM in objSlot.Members)
            {
                var oM = objS.Members.FirstOrDefault(m => m.Name == objSlotM.Name);
                if (oM == null)
                    return false;
                if (objSlotM.IsLet)
                {
                    if (!isAssignable(oM.Spec, objSlotM.Spec))
                        return false;
                }
                else
                {
                    if (!areSame(oM.Spec, objSlotM.Spec))
                        return false;
                }
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
        private Spec specExp2(Exp exp, Env<Spec> env, Spec slot)
        {
            C.Nn(exp, env, slot);
            C.ReturnsNn();
            
            Spec ss;
            switch (exp)
            {
               case Ident i:
                    if (isImportContext)
                    {
                        return env.GetWithoutImports(i).Value;
                    }
                    else
                    {
                        var orig = env.Get(i).Value;
                        if (orig == UnknownSpec.Instance)
                        {
                            //prog.RemarkList.AttemptToReadUninitializedVariableWarn(i);
                            env.Set(i, slot);
                            return slot;
                        }
                        if (!isAssignable(orig, slot))
                            prog.RemarkList.CannotConvertType(orig, slot, i);
                        return orig;
                    }

                case FnApply fna:
                {
                    if (fna.Fn is Ident fnI && fnI.Name == "typeof")
                    {
                        fna.Arguments = new FnArguments(new List<Exp> {specExp(fna.Arguments[0], env, AnySpec.Instance) });
                        return VoidSpec.Instance;
                    }

                    var orig = specExp(fna.Fn, env, AnySpec.Instance);

                    if (orig is FnSpec fnS)
                    {
                        if (fnS.ParameterSpec.Count != fna.Arguments.Count)
                            throw prog.RemarkList.ParameterArgumentCountMismatch(fna, fnS.ParameterSpec.Count);

                        var ix = 0;
                        foreach (var aa in fna.Arguments)
                        {
                            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                            specExp(aa, env, fnS.ParameterSpec[ix++]);
                        }

                        if (!isAssignable(fnS.ReturnSpec, slot))
                            prog.RemarkList.CannotConvertType(fnS.ReturnSpec, slot, fna);

                        return fnS.ReturnSpec;
                    }

                    var sigS = new List<Spec>();
                    foreach (var aa in fna.Arguments)
                    {
                        var aS = specExp(aa, env, AnySpec.Instance);
                        sigS.Add(aS);
                    }
                    sigS.Add(slot == UnknownSpec.Instance ? VoidSpec.Instance : slot);

                    var newFnSpec = new FnSpec(sigS);
                    if (fna.Fn is Ident fnI2)
                        env.Set(fnI2, newFnSpec);

                    return slot;
                }

                case Fn f:
                    var paramsEnv = Env.Create(prog, env);
                    foreach (var p in f.Parameters)
                        paramsEnv.Declare(p, UnknownSpec.Instance);

                    var fnEnv = Env.Create(prog, paramsEnv);
                    FnSpec ret;
                    if (f.Sequence.Count == 0)
                    {
                        validateUnusedParams(paramsEnv);
                        ret = makeFnSpec(f, paramsEnv, VoidSpec.Instance);
                    }
                    else if (f.Sequence.Count == 1 && f.Sequence[0] is Exp exp1)
                    {
                        ss = specExp(exp1, fnEnv, AnySpec.Instance);
                        validateUnusedParams(paramsEnv);
                        ret = makeFnSpec(f, paramsEnv, ss == UnknownSpec.Instance ? AnySpec.Instance : ss);
                    }
                    else
                    {
                        returns.Push(UnknownSpec.Instance);
                        foreach (var fnItem in f.Sequence)
                            specElement(fnItem, fnEnv, VoidSpec.Instance);
                        ss = returns.Pop();
                        validateUnusedParams(paramsEnv);
                        ret = makeFnSpec(f, paramsEnv, ss == UnknownSpec.Instance ? VoidSpec.Instance : ss);
                    }

                    validateUnusedVariables(fnEnv);

                    return ret;

                case When w:
                    var _ = specExp(w.Test, env, BoolSpec.Instance);
                    var thenS = specWhenSequence(w.Then, env, slot);
                    if (w.Otherwise == null)
                    {
                        return VoidSpec.Instance;
                    }
                    else
                    {
                        var otherwiseS = specWhenSequence(w.Otherwise, env, slot);
                        if (!areSame(thenS, otherwiseS))
                            prog.RemarkList.CannotConvertType(thenS, otherwiseS, w.Otherwise);
                        return thenS;
                    }

                case ArrConstructor ae:
                    Spec prevS = AnySpec.Instance;
                    foreach (var a in ae.Arguments)
                        prevS = specExp(a, env, prevS);
                    return new ArrSpec(prevS);

                case MemberAccess ma:
                    ss = specExp(ma.Exp, env, UnknownSpec.Instance);
                    var mai = ma.Ident;
                    if (ss is UnknownSpec)
                    {
                        var objSEnv = Env.Create(prog, env);
                        var objS = new ObjSpec(new List<ObjSpecMember>(), objSEnv) { FromUsage = true };
                        var declr = new Var(new Ident(mai.Name, mai.TokenType).CopInfoFrom(mai, true), Void.Instance);
                        objSEnv.Declare(declr, slot);
                        var maiS = specExp(mai, objSEnv, slot);
                        objS.Members.Add(new ObjSpecMember(mai.Name, maiS));
                        ss = objS;
                        if (ma.Exp is Ident i)
                        {
                            env.Set(i, objS);
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
                        specElement(classItem, objEnv, AnySpec.Instance);
                        if (classItem is Declr d)
                            members.Add(new ObjSpecMember(d.Ident.Name, objEnv.Get(d.Ident).Value));
                    }
                    return new ObjSpec(members, objEnv) { Parent = n.Parent };

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

                default:
                    throw new NotSupportedException();
            }
        }

        private void validateUnusedParams(Env<Spec> paramsEnv)
        {
            foreach (var item in paramsEnv.Items)
            {
                if (item.Value.Value == UnknownSpec.Instance)
                {
                    prog.RemarkList.UnusedParameter(item.Key);
                    item.Value.Value = AnySpec.Instance;
                }
            }
        }

        private void validateUnusedVariables(Env<Spec> fnEnv)
        {
            foreach (var item in fnEnv.Items)
            {
                if (item.Value.Value == UnknownSpec.Instance)
                {
                    prog.RemarkList.UnusedVariable(item.Key);
                    item.Value.Value = VoidSpec.Instance;
                }
            }
        }


        [System.Diagnostics.Contracts.Pure]
        private Spec specExp(Exp exp, Env<Spec> env, Spec slot)
        {
            C.Nn(exp, env, slot);

            var s = specExp2(exp, env, slot);
            if (!isAssignable(s, slot))
                prog.RemarkList.CannotConvertType(s, slot, exp);
            return s;
        }



        private void specElement(Element el, Env<Spec> env, Spec slot)
        {
            C.Nn(el, env, slot);

            switch (el)
            {
                case Exp exp:
                    var _ = specExp(exp, env, slot);
                    break;

                case Stm stm:
                    specStm(stm, env);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }



        private void specStm(Stm e, Env<Spec> env)
        {
            C.Nn(e, env);

            switch (e)
            {
                case Declr d:
                    if (d.Exp == null)
                    {
                        env.Declare(d, UnknownSpec.Instance);
                    }
                    else
                    {
                        env.Declare(d, specExp(d.Exp, env, AnySpec.Instance));
                    }

                    break;

                case Assign a:
                    switch (a.To)
                    {
                        case Ident ident:
                        {
                            var orig = env.Get(ident, true).Value;
                            var s = specExp(a.Exp, env, orig);
                            if (orig == UnknownSpec.Instance)
                                env.Set(ident, s);
                            break;
                        }

                        case MemberAccess ma:
                        {
                            var minObjEnv = Env.Create(prog, env);
                            var declr = new Var(new Ident(ma.Ident.Name, ma.Ident.TokenType).CopInfoFrom(ma.Ident, true), Void.Instance);
                            minObjEnv.Declare(declr, AnySpec.Instance);
                            var minObj = new ObjSpec(
                                new List<ObjSpecMember> {new ObjSpecMember(ma.Ident.Name, AnySpec.Instance) },
                                minObjEnv);
                            var maExpS = specExp(ma.Exp, env, minObj);
                            if (maExpS is ObjSpec objS)
                            {
                                var orig = objS.Env.Get(ma.Ident, true).Value;
                                var s = specExp(a.Exp, env, orig);
                                if (orig == AnySpec.Instance)
                                {
                                    objS.Members.First(m => m.Name == ma.Ident.Name).Spec = s;
                                    objS.Env.Set(ma.Ident, s);
                                }
                            }

                            break;
                        }

                        default:
                            throw new NotSupportedException();
                    }

                    break;

                case Return r:
                    var retS = specExp(r.Exp, env, returns.Peek());
                    returns.UpdateTop(retS);
                    break;

                case Loop l:
                    specSequence(l.Body, env);
                    break;

                case Break _:
                    break;

                case Continue _:
                    break;
                    
                case Toss ts:
                    var _ = specExp(ts.Exception, env, AnySpec.Instance);
                    // TODO requires exp to be of some type?
                    break;

                case Attempt att:
                    specSequence(att.Body, env);
                    if (att.Grab != null)
                    {
                        var grabEnv = Env.Create(prog, env);
                        var exDeclr = new Param(new Ident("exception", TokenType.Ident));
                        grabEnv.Declare(exDeclr, AnySpec.Instance);
                        specSequence(att.Grab, grabEnv);
                    }

                    if (att.AtLast != null)
                        specSequence(att.AtLast, env);
                    break;

                case Import imp:
                    isImportContext = true;
                    var objSpecWithNoMembers = new ObjSpec(new List<ObjSpecMember>(), Env.Create(prog, env));
                    var modImpEl = specExp(imp.QualifiedIdent, env, objSpecWithNoMembers);
                    isImportContext = false;
                    if (modImpEl is ObjSpec modImp)
                        env.AddImport(imp.QualifiedIdent, modImp.Env, (Declr) modImp.Parent);
                    break;

                case Sequence seq:
                    specSequence(seq, env);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }


        private Spec specWhenSequence(SequenceItem si, Env<Spec> env, Spec slot)
        {
            switch (si)
            {
                case Sequence sequence:
                    var scopeEnv = Env.Create(prog, env);

                    if (sequence.Count == 1 && sequence[0] is Exp exp)
                        return specExp(exp, scopeEnv, slot);

                    foreach (var item in sequence)
                    {
                        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                        specElement(item, scopeEnv, VoidSpec.Instance);
                    }

                    validateUnusedVariables(scopeEnv);

                    return VoidSpec.Instance;

                case Exp exp1:
                    return specExp(exp1, env, slot);

                default:
                    specElement(si, env, slot);
                    return VoidSpec.Instance;
            }
        }


        private void specSequence(Sequence sequence, Env<Spec> env)
        {
            var scopeEnv = Env.Create(prog, env);

            foreach (var item in sequence)
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                specElement(item, scopeEnv, VoidSpec.Instance);
            }

            validateUnusedVariables(scopeEnv);
        }


        private static FnSpec makeFnSpec(Fn fn, Env<Spec> paramsEnv, Spec retSpec)
        {
            return new FnSpec(
                fn.Parameters
                    .Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                    .Append(retSpec)
                    .ToList());
        }
    }
}