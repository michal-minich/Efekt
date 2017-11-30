using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class StackItem
    {
        private readonly Element e;
        private string fnName;
        public int LineIndex { get; set; }
        public string FilePath => e.FilePath;
        public string FnName => fnName ?? (fnName = (((Fn) e).Parent is Var v ? v.Ident.Name : null) ?? "(anonymous)");

        public StackItem(Fn fn) => e = fn;

        public StackItem(Builtin b, string fnName)
        {
            e = b;
            this.fnName = fnName;
        }
    }

    public sealed class Interpreter
    {
        [CanBeNull] private Value ret;
        private bool isBreak;
        private bool isContinue;
        private Prog prog;
        private bool IsImportContext;

        private Stack<StackItem> callStack { get; set; }

        public IReadOnlyList<StackItem> CallStack => callStack.ToList();


        public Value Eval(Prog program)
        {
            prog = program;
            callStack = new Stack<StackItem>();
            return eval(prog.RootElement, Env.CreateRoot(prog));
        }


        private Value eval(Element se, Env env)
        {
            switch (se)
            {
                case Let l:
                    var val2 = eval(l.Exp, env);
                    env.Declare(l.Ident, val2, true);
                    return Void.Instance;
                case Var v:
                    var val = eval(v.Exp, env);
                    env.Declare(v.Ident, val, false);
                    return Void.Instance;
                case Assign a:
                    var newValue = eval(a.Exp, env);
                    switch (a.To)
                    {
                        case Ident ident:
                            env.Set(ident, newValue);
                            break;
                        case MemberAccess ma:
                            var obj = eval(ma.Exp, env);
                            var o2 = obj.AsObj(ma, prog);
                            o2.Env.Set(ma.Ident, newValue);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    return Void.Instance;
                case Ident i:
                    if (IsImportContext)
                        return env.Get(i);
                    else
                        return env.GetWithImport(i);
                case Return r:
                    ret = eval(r.Exp, env);
                    return Void.Instance;
                case FnApply fna:
                    Exp fn;
                    if (fna.Fn is MemberAccess extMemAcc)
                    {
                        var exp2 = eval(extMemAcc.Exp, env);
                        if (exp2 is Obj o2)
                        {
                            var v = o2.Env.GetDirectlyOrNull(extMemAcc.Ident);
                            if (v != null)
                            {
                                fn = v;
                                goto noExtMethodApply;
                            }
                        }
                        var envV = env.GetWithImportOrNull(extMemAcc.Ident);
                        if (envV != null)
                        {
                            var extFn = envV.AsFn(fna, prog);
                            if (extFn.Parameters.Count == 0)
                                throw prog.RemarkList.Except.ExtensionFuncHasNoParameters(extFn, extMemAcc);
                            var newArgs = new FnArguments(new[] {exp2}.Concat(fna.Arguments).ToList());
                            var newFna = new FnApply(extFn, newArgs)
                            {
                                LineIndex = fna.LineIndex,
                                FilePath = fna.FilePath,
                                IsBraced = fna.IsBraced,
                                Parent = fna.Parent
                            };
                            return eval(newFna, env);
                        }
                        throw prog.RemarkList.Except.VariableIsNotDeclared(extMemAcc.Ident);
                    }
                    fn = eval(fna.Fn, env);
                    noExtMethodApply:
                    var builtin = fn as Builtin;
                    var eArgs = fna.Arguments.Select(a => eval(a, env)).ToList();
                    if (builtin != null)
                    {
                        callStack.Push(new StackItem(builtin, builtin.Name));
                        var res = builtin.Fn(new FnArguments(eArgs.Cast<Exp>().ToList()), fna);
                        callStack.Pop();
                        return res;
                    }
                    var fn2 = fn.AsFn(fna, prog);
                    var paramsEnv = Env.Create(prog, fn2.Env);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                    {
                        var eArg = eArgs[ix++];
                        paramsEnv.Declare(p.Ident, eArg, true);
                    }
                    var fnEnv = Env.Create(prog, paramsEnv);
                    callStack.Push(new StackItem(fn2));
                    if (fn2.Sequence.Count == 1)
                    {
                        var r = evalSequenceItem(fn2.Sequence.First(), fnEnv);
                        if (ret == null)
                            return r;
                        var tmp = ret;
                        ret = null;
                        callStack.Pop();
                        return tmp;
                    }
                    else
                    {
                        foreach (var bodyElement in fn2.Sequence)
                        {
                            evalSequenceItemFull(bodyElement, fnEnv);
                            if (ret != null)
                            {
                                var tmp = ret;
                                ret = null;
                                callStack.Pop();
                                return tmp;
                            }
                        }
                    }
                    callStack.Pop();
                    return Void.Instance;
                case Fn f:
                    var fn3 = new Fn(f.Parameters, f.Sequence, env)
                    {
                        Parent = f.Parent,
                        LineIndex = f.LineIndex,
                        FilePath = f.FilePath,
                        IsBraced = f.IsBraced
                    };
                    return fn3;
                case When w:
                    var test = eval(w.Test, env);
                    var testB = test.AsBool(w.Test, prog);
                    if (testB.Value)
                        return eval(w.Then, Env.Create(prog, env));
                    else if (w.Otherwise != null)
                        return eval(w.Otherwise, Env.Create(prog, env));
                    else
                        return Void.Instance;
                case Loop l:
                    var loopEnv = Env.Create(prog, env);
                    while (true)
                    {
                        continueLoop:
                        foreach (var e in l.Body)
                        {
                            evalSequenceItemFull(e, loopEnv);
                            if (isContinue)
                            {
                                isContinue = false;
                                goto continueLoop;
                            }
                            if (isBreak)
                            {
                                isBreak = false;
                                return Void.Instance;
                            }
                            if (ret != null)
                                return Void.Instance;
                        }
                    }
                case Continue _:
                    isContinue = true;
                    return Void.Instance;
                case Break _:
                    isBreak = true;
                    return Void.Instance;
                case ArrConstructor ae:
                    return new Arr(new Values(ae.Arguments.Select(e => eval(e, env)).ToList()));
                case MemberAccess ma:
                    var exp = eval(ma.Exp, env);
                    var o = exp.AsObj(ma, prog);
                    return o.Env.Get(ma.Ident);
                case New n:
                    var objEnv = Env.Create(prog, env);
                    foreach (var v in n.Body)
                        eval(v, objEnv);
                    return new Obj(n.Body, objEnv);
                case Value ve:
                    return ve;
                case Sequence seq:
                    var scopeEnv = Env.Create(prog, env);
                    if (seq.Count == 1)
                        return eval(seq.First(), scopeEnv);
                    foreach (var item in seq)
                    {
                        evalSequenceItemFull(item, scopeEnv);
                        if (ret != null)
                            return Void.Instance;
                    }
                    return Void.Instance;
                case Toss ts:
                    var exVal = eval(ts.Exception, env);
                    throw prog.RemarkList.AddInterpretedException(new Remark(
                        RemarkSeverity.InterpretedException,
                        "Interpreted Exception",
                        ts.FilePath,
                        ts.LineIndex,
                        exVal,
                        ts,
                        callStack.ToList()));
                case Attempt att:
                    try
                    {
                        return eval(att.Body, env);
                    }
                    catch (EfektInterpretedException ex)
                    {
                        var grabEnv = Env.Create(prog, env);
                        grabEnv.Declare(new Ident("exception", TokenType.Ident), ex.Value, true);
                        if (att.Grab != null)
                            eval(att.Grab, grabEnv);
                        return Void.Instance;
                    }
                    finally
                    {
                        if (att.AtLast != null)
                            eval(att.AtLast, env);
                    }
                case Import imp:
                    IsImportContext = true;
                    var modImpEl = eval(imp.QualifiedIdent, env);
                    IsImportContext = false;
                    var modImp = modImpEl.AsObj(imp, prog);
                    env.AddImport(imp.QualifiedIdent, modImp);
                    return Void.Instance;
                default:
                    throw new NotSupportedException();
            }
        }

        private void evalSequenceItemFull(Element bodyElement, Env env)
        {
            var bodyVal = evalSequenceItem(bodyElement, env);
            if (bodyVal != Void.Instance)
            {
                if (bodyElement is FnApply fna2)
                    prog.RemarkList.Warn.ValueReturnedFromFunctionNotUsed(fna2);
                else
                    prog.RemarkList.Warn.ValueIsNotAssigned(bodyElement);
            }
        }

        private Value evalSequenceItem(Element bodyElement, Env env)
        {
            callStack.Peek().LineIndex = bodyElement.LineIndex;
            var bodyVal = eval(bodyElement, env);
            return bodyVal;
        }
    }
}