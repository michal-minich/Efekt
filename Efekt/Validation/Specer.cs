using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Specer
    {
        private readonly Stack<Spec> returns = new Stack<Spec>();
        private Prog prog;
        private bool isImportContext;


        public Env<Spec> Env { get; private set; }


        // ReSharper disable once UnusedMethodReturnValue.Global
        public Spec Spec(Prog program)
        {
            C.Nn(program);
            C.ReturnsNn();

            prog = program;
            isImportContext = false;
            returns.Clear();
            Env = Efekt.Env.CreateSpecRoot(prog);
            return specSequenceItem(prog.RootElement, Env, VoidSpec.Instance);
        }


        private bool isAssignable(Spec s, Spec slot)
        {
            C.Nn(s, slot);

            switch (slot)
            {
                case NotSetSpec _:
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
                case ArrSpec slotArr:
                    return s is ArrSpec sArr && areSame(sArr.ItemSpec, slotArr.ItemSpec);
                case FnSpec slotFn:
                    return s is FnSpec sFn
                           && areAssignable(slotFn.ParameterSpec, sFn.ParameterSpec)
                           && (slotFn.ReturnSpec is VoidSpec || isAssignable(sFn.ReturnSpec, slotFn.ReturnSpec));
                case ObjSpec slotObj:
                    return s is ObjSpec sObj && isObjAssignable(sObj, slotObj);
                default:
                    return false;
            }
        }


        private bool isObjAssignable(ObjSpec sObj, ObjSpec slotObj)
        {
            C.Nn(sObj, slotObj);

            foreach (var slotMember in slotObj.Env.Items)
            {
                var sMember = sObj.Env.GetFromThisEnvOnlyOrNull(slotMember.Key.Ident, null);
                if (sMember == null || !isAssignable(sMember.Value, slotMember.Value.Value))
                    return false;
            }

            return true;
        }


        private bool areAssignable(IReadOnlyCollection<Spec> ss, IReadOnlyCollection<Spec> slots)
        {
            C.AllNotNull(ss);
            C.AllNotNull(slots);

            return ss.Count == slots.Count && ss.Zip(slots, isAssignable).All(a => a);
        }


        [System.Diagnostics.Contracts.Pure]
        private static bool areSame(Spec a, Spec b)
        {
            C.Nn(a, b);
            return a.ToCodeString() == b.ToCodeString();
        }


        [System.Diagnostics.Contracts.Pure]
        private Spec commonType(IEnumerable<Exp> exps, Env<Spec> env)
        {
            C.Nn(exps, env);
            C.ReturnsNn();

            return exps.Aggregate((Spec) AnySpec.Instance, (slot, a) => specExp(a, env, slot));
        }


        private Spec specSequenceItem(SequenceItem si, Env<Spec> env, Spec slot)
        {
            C.Nn(si, env, slot);
            C.ReturnsNn();

            switch (si)
            {
                case Sequence sequence:
                    if (sequence.Count == 1 && sequence[0] is Exp e)
                        return specExp(e, env, slot);
                    specSequence(sequence, env);
                    return VoidSpec.Instance;

                case Exp exp:
                    return specExp(exp, env, slot);

                default:
                    specElement(si, env, slot);
                    return VoidSpec.Instance;
            }
        }


        private Env<Spec> specSequence(Sequence sequence, Env<Spec> env)
        {
            C.Nn(sequence, env);

            var scopeEnv = env.Create();
            foreach (var item in sequence)
                specElement(item, scopeEnv, VoidSpec.Instance);
            validateUnusedVariables(scopeEnv);
            return scopeEnv;
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


        [System.Diagnostics.Contracts.Pure]
        private Spec specExp(Exp exp, Env<Spec> env, Spec slot)
        {
            C.Nn(exp, env, slot);
            C.ReturnsNn();

            var s = specExp2(exp, env, slot);
            exp.Spec = s;
            if (!isAssignable(s, slot))
                prog.RemarkList.CannotConvertType(s, slot, exp);
            return s;
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

                        if (orig == NotSetSpec.Instance || orig == AnySpec.Instance)
                        {
                            //prog.RemarkList.AttemptToReadUninitializedVariableWarn(i);
                            setIdentAndAssignedFrom(env, i, slot);
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
                        if (!(fna.Arguments[0] is Spec))
                            fna.Arguments = new FnArguments(new List<Exp> {specExp(fna.Arguments[0], env, AnySpec.Instance)});
                        return VoidSpec.Instance;
                    }

                    var orig = specExp(fna.Fn, env, AnySpec.Instance);

                    if (orig is FnSpec fnS)
                    {
                        if (fnS.ParameterSpec.Count != fna.Arguments.Count)
                            throw prog.RemarkList.ParameterArgumentCountMismatch(fna, fnS.ParameterSpec.Count);

                        var argumentSpecs = new List<Spec>();
                        var ix = 0;
                        foreach (var aa in fna.Arguments)
                        {
                            var pS = fnS.ParameterSpec[ix];
                            var aS = specExp(aa, env, pS);
                            argumentSpecs.Add(isAssignable(aS, pS) ? aS : pS);
                            ++ix;
                        }

                        if (fna.Arguments.Count != 0 && fnS.Fn?.SpecEnv != null)
                        {
                            var newFn = new Fn(fnS.Fn.Parameters, fnS.Fn.Sequence, fnS.Fn.SpecEnv).CopyInfoFrom(fnS.Fn);
                            var newFnS = specFn(newFn, null, argumentSpecs);
                            fnS = newFnS;
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

                    sigS.Add(slot == NotSetSpec.Instance ? AnySpec.Instance : slot);

                    var newFnSpec = new FnSpec(sigS, null);
                    if (fna.Fn is Ident fnI2)
                        env.Set(fnI2, newFnSpec);

                    return slot;
                }

                case Fn f:
                    return specFn(f, env);

                case When w:
                    var _ = specExp(w.Test, env, BoolSpec.Instance);
                    var thenS = specSequenceItem(w.Then, env, slot);
                    if (w.Otherwise == null)
                    {
                        return VoidSpec.Instance;
                    }
                    else
                    {
                        var otherwiseS = specSequenceItem(w.Otherwise, env, slot);
                        if (!areSame(thenS, otherwiseS))
                            prog.RemarkList.CannotConvertType(thenS, otherwiseS, w.Otherwise);
                        return thenS;
                    }

                case ArrConstructor ae:
                    var s = new ArrSpec(commonType(ae.Arguments, env));
                    var notSetArgs = ae.Arguments.OfType<Ident>().ToList();
                    foreach (var a in notSetArgs)
                    {
                        var orig2 = env.Get(a).Value;
                        if (isAssignable(s.ItemSpec, orig2))
                            setIdentAndAssignedFrom(env, a, s.ItemSpec);
                    }

                    return s;

                case MemberAccess ma:
                    ss = specExp(ma.Exp, env, NotSetSpec.Instance);
                    var mai = ma.Ident;
                    if (ss is NotSetSpec || ss is AnySpec)
                    {
                        var objSEnv = env.Create();
                        var objS = new ObjSpec(objSEnv) {FromUsage = true};
                        var declr = new Var(new Ident(mai.Name, mai.TokenType).CopyInfoFrom(mai, true));
                        objSEnv.Declare(declr, slot);
                        var __ = specExp(mai, objSEnv, slot);
                        ss = objS;
                        if (ma.Exp is Ident i)
                        {
                            setIdentAndAssignedFrom(env, i, objS);
                        }
                    }

                    var objS2 = ss as ObjSpec;
                    if (objS2 == null)
                        throw prog.RemarkList.OnlyObjectsCanHaveMembers(ma, ss);
                    if (objS2.FromUsage)
                    {
                        var member = objS2.Env.GetFromThisEnvOnlyOrNull(mai, null); // also use type and var/let?
                        if (member == null)
                        {
                            var declr = new Var(new Ident(mai.Name, mai.TokenType).CopyInfoFrom(mai, true));
                            objS2.Env.Declare(declr, slot);
                            var __ = specExp(mai, objS2.Env, slot);
                        }
                    }

                    var objSpec = (ObjSpec) ss;
                    ss = objSpec.Env.GetFromThisEnvOnly(mai, null).Value;
                    return ss;

                case New n:
                    var objEnv = env.Create();
                    foreach (var classItem in n.Body)
                        specStm(classItem, objEnv);
                    return new ObjSpec(objEnv) {Parent = n.Parent};

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


        private FnSpec specFn(Fn fn, [CanBeNull] Env<Spec> env, IReadOnlyList<Spec> argumentSpecs = null)
        {
            var e = env ?? fn.SpecEnv;
            C.Assert(e != null);
            var paramsEnv = e.Create();
            if (argumentSpecs == null)
            {
                foreach (var p in fn.Parameters)
                    paramsEnv.Declare(p, NotSetSpec.Instance);
            }
            else
            {
                C.Assert(argumentSpecs.Count == fn.Parameters.Count);
                var ix = 0;
                foreach (var p in fn.Parameters)
                    paramsEnv.Declare(p, argumentSpecs[ix++]);
            }

            if (fn.Sequence.Count == 0)
            {
                fn.SpecEnv = paramsEnv;
                validateUnusedVariables(paramsEnv);
                return makeFnSpec(fn, paramsEnv, VoidSpec.Instance);
            }
            else if (fn.Sequence.Count == 1 && fn.Sequence[0] is Exp exp1)
            {
                fn.SpecEnv = paramsEnv.Create();
                var retS = specExp(exp1, fn.SpecEnv, AnySpec.Instance);
                validateUnusedVariables(paramsEnv);
                validateUnusedVariables(fn.SpecEnv);
                return makeFnSpec(fn, paramsEnv, retS == NotSetSpec.Instance ? AnySpec.Instance : retS);
            }
            else
            {
                returns.Push(NotSetSpec.Instance);
                fn.SpecEnv = specSequence(fn.Sequence, paramsEnv);
                var retS = returns.Pop();
                validateUnusedVariables(paramsEnv);
                return makeFnSpec(fn, paramsEnv, retS == NotSetSpec.Instance ? VoidSpec.Instance : retS);
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
                        env.Declare(d, NotSetSpec.Instance);
                    }
                    else
                    {
                        env.Declare(d, specExp(d.Exp, env, AnySpec.Instance));
                        if (d.Exp is Ident fromIdent)
                            d.AssignedFrom.Add(fromIdent);
                        d.Ident.Spec = d.Exp.Spec;
                    }

                    break;

                case Assign a:
                    switch (a.To)
                    {
                        case Ident ident:
                        {
                            var orig = env.Get(ident, true).Value;
                            var slot3 = orig == NotSetSpec.Instance ? AnySpec.Instance : orig;
                            var s = specExp(a.Exp, env, slot3);
                            if (orig == NotSetSpec.Instance)
                                env.Set(ident, s);
                            if (a.Exp is Ident fromIdent)
                                a.AssignedFrom.Add(fromIdent);

                            break;
                        }

                        case MemberAccess ma:
                        {
                            var minObjEnv = env.Create();
                            var declr = new Let(
                                new Ident(ma.Ident.Name, ma.Ident.TokenType)
                                    .CopyInfoFrom(ma.Ident, true), Void.Instance);
                            minObjEnv.Declare(declr, AnySpec.Instance);
                            var minObj = new ObjSpec(minObjEnv);
                            var maExpS = specExp(ma.Exp, env, minObj);
                            if (maExpS is ObjSpec objS)
                            {
                                var orig = objS.Env.Get(ma.Ident, true).Value;
                                var slot3 = orig == NotSetSpec.Instance ? AnySpec.Instance : orig;
                                var s = specExp(a.Exp, env, slot3);
                                if (orig == AnySpec.Instance)
                                {
                                    objS.Env.Set(ma.Ident, s);
                                    a.AssignedFrom.Add(ma.Ident);
                                }
                            }

                            break;
                        }

                        default:
                            throw new NotSupportedException();
                    }

                    break;

                case Return r:
                    var slot2 = returns.Peek();
                    var retS = specExp(r.Exp, env, slot2 == NotSetSpec.Instance ? AnySpec.Instance : slot2);
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
                        var grabEnv = env.Create();
                        var exDeclr = new Let(new Ident("exception", TokenType.Ident));
                        grabEnv.Declare(exDeclr, AnySpec.Instance);
                        specSequence(att.Grab, grabEnv);
                    }

                    if (att.AtLast != null)
                        specSequence(att.AtLast, env);
                    break;

                case Import imp:
                    isImportContext = true;
                    var objSpecWithNoMembers = new ObjSpec(env.Create());
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


        private static void setIdentAndAssignedFrom(Env<Spec> env, Ident ident, Spec spec)
        {
            env.Set(ident, spec);
            foreach (var af in ident.DeclareBy.AssignedFrom)
                env.Set(af, spec);
        }


        private static FnSpec makeFnSpec(Fn fn, Env<Spec> paramsEnv, Spec retSpec)
        {
            return new FnSpec(
                fn.Parameters
                    .Select(p => paramsEnv.GetFromThisEnvOnly(p.Ident, null).Value)
                    .Append(retSpec)
                    .ToList(),
                fn);
        }


        private void validateUnusedVariables(Env<Spec> env)
        {
            var notSetOrRead = env.Items
                .Where(i => i.Value.Value == NotSetSpec.Instance || i.Key.ReadBy.Count == 0)
                .ToList();

            foreach (var item in notSetOrRead)
            {
                if (item.Key is Param)
                {
                    item.Value.Value = AnySpec.Instance;
                    prog.RemarkList.UnusedParameter(item.Key);
                }
                else
                {
                    item.Value.Value = VoidSpec.Instance;
                    prog.RemarkList.UnusedVariable(item.Key);
                }

                item.Key.Spec = item.Value.Value;
                item.Key.Ident.Spec = item.Value.Value;
            }
        }
    }
}