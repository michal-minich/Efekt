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
        public int ColumnIndex { get; set; }
        public string FilePath => e.FilePath;
        public string FnName => fnName ?? (fnName = (((Fn) e).Parent is Declr d ? d.Ident.Name : null) ?? "(anonymous)");

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
        private bool isImportContext;

        private Stack<StackItem> callStack { get; set; }

        public IReadOnlyList<StackItem> CallStack => callStack?.ToList();


        public Value Eval(Prog program)
        {
            prog = program;
            callStack = new Stack<StackItem>();
            return eval(prog.RootElement, Env<Value>.CreateValueRoot(prog));
        }


        private Value eval(Element se, Env<Value> env)
        {
            C.Nn(se, env);
            C.ReturnsNn();

            switch (se)
            {
                case Declr d:
                    var val = eval(d.Exp, env);
                    env.Declare(d.Ident, val, d is Let);
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
                    if (isImportContext)
                        return env.Get(i);
                    else
                        return env.GetWithImport(i);
                case Return r:
                    ret = eval(r.Exp, env);
                    return Void.Instance;
                case FnApply fna:
                    if (fna.Fn is Ident fnI && fnI.Name == "typeof")
                    {
                        var ofS = fna.Arguments[0].Spec.ToString();
                        prog.OutputWriter.Write(ofS);
                        return Void.Instance;
                    }
                    Exp fn;
                    if (fna.Fn is MemberAccess extMemAcc)
                    {
                        var exp2 = eval(extMemAcc.Exp, env);
                        if (exp2 is Obj o2)
                        {
                            var v = o2.Env.GetDirectlyOrNull(extMemAcc.Ident);
                            if (v != null)
                            {
                                fn = v.Value;
                                goto noExtMethodApply;
                            }
                        }
                        var envV = env.GetWithImportOrNull(extMemAcc.Ident);
                        if (envV != null)
                        {
                            var extFn = envV.Value.AsFn(fna, prog);
                            if (extFn.Parameters.Count == 0)
                                throw prog.RemarkList.ExtensionFuncHasNoParameters(extFn, extMemAcc);
                            var newArgs = new FnArguments(new[] {exp2}.Concat(fna.Arguments).ToList());
                            var newFna = new FnApply(extFn, newArgs)
                            {
                                LineIndex = fna.LineIndex,
                                ColumnIndex = fna.ColumnIndex,
                                LineIndexEnd = fna.LineIndexEnd,
                                ColumnIndexEnd = fna.ColumnIndexEnd,
                                FilePath = fna.FilePath,
                                IsBraced = fna.IsBraced,
                                Parent = fna.Parent
                            };
                            callStack.Push(new StackItem(extFn));
                            var res = eval(newFna, env);
                            callStack.Pop();
                            return res;
                        }
                        throw prog.RemarkList.VariableIsNotDeclared(extMemAcc.Ident);
                    }
                    fn = eval(fna.Fn, env);
                    noExtMethodApply:
                    var builtin = fn as Builtin;
                    var eArgs = fna.Arguments.Select(a => (Exp)eval(a, env)).ToList();
                    if (builtin != null)
                    {
                        callStack.Push(new StackItem(builtin, builtin.Name));
                        var res = builtin.Fn(new FnArguments(eArgs), fna);
                        callStack.Pop();
                        return res;
                    }
                    var fn2 = fn.AsFn(fna, prog);
                    var paramsEnv = Env<Value>.Create(prog, fn2.Env);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                    {
                        var eArg = (Value) eArgs[ix++];
                        paramsEnv.Declare(p.Ident, eArg, true);
                    }
                    var fnEnv = Env<Value>.Create(prog, paramsEnv);
                    callStack.Push(new StackItem(fn2));
                    if (fn2.Sequence.Count == 1)
                    {
                        var r = evalSequenceItem(fn2.Sequence[0], fnEnv);
                        if (ret == null)
                        {
                            callStack.Pop();
                            return r;
                        }
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
                        ColumnIndex = f.ColumnIndex,
                        LineIndexEnd = f.LineIndexEnd,
                        ColumnIndexEnd = f.ColumnIndexEnd,
                        FilePath = f.FilePath,
                        IsBraced = f.IsBraced
                    };
                    return fn3;
                case When w:
                    var test = eval(w.Test, env);
                    var testB = test.AsBool(w.Test, prog);
                    if (w.Otherwise == null)
                    {
                        if (testB.Value)
                            eval(w.Then, Env<Value>.Create(prog, env));
                        return Void.Instance;
                    }
                    else
                    {
                        if (testB.Value)
                            return eval(w.Then, Env<Value>.Create(prog, env));
                        return eval(w.Otherwise, Env<Value>.Create(prog, env));
                    }
                case Loop l:
                    var loopEnv = Env<Value>.Create(prog, env);
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
                    var objEnv = Env<Value>.Create(prog, env);
                    foreach (var v in n.Body)
                        eval(v, objEnv);
                    return new Obj(n.Body, objEnv);
                case Value ve:
                    return ve;
                case Sequence seq:
                    var scopeEnv = Env<Value>.Create(prog, env);
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
                    throw prog.RemarkList.ProgramException(exVal, ts, callStack.ToList());
                case Attempt att:
                    try
                    {
                        return eval(att.Body, env);
                    }
                    catch (EfektProgramException ex)
                    {
                        if (att.Grab != null)
                        {
                            var grabEnv = Env<Value>.Create(prog, env);
                            grabEnv.Declare(new Ident("exception", TokenType.Ident), ex.Value, true);
                            eval(att.Grab, grabEnv);
                        }
                        return Void.Instance;
                    }
                    finally
                    {
                        if (att.AtLast != null)
                            eval(att.AtLast, env);
                    }
                case Import imp:
                    isImportContext = true;
                    var modImpEl = eval(imp.QualifiedIdent, env);
                    isImportContext = false;
                    var modImp = modImpEl.AsObj(imp, prog);
                    env.AddImport(imp.QualifiedIdent, modImp.Env);
                    return Void.Instance;
                default:
                    throw new NotSupportedException();
            }
        }

        private void evalSequenceItemFull(Element bodyElement, Env<Value> env)
        {
            var bodyVal = evalSequenceItem(bodyElement, env);
            if (bodyVal != Void.Instance)
            {
                if (bodyElement is FnApply fna2)
                    prog.RemarkList.ValueReturnedFromFunctionNotUsed(fna2);
                else
                    prog.RemarkList.ValueIsNotAssigned(bodyElement);
            }
        }

        private Value evalSequenceItem(Element bodyElement, Env<Value> env)
        {
            C.ReturnsNn();

            var stackItem = callStack.Peek();
            stackItem.LineIndex = bodyElement.LineIndex;
            stackItem.ColumnIndex = bodyElement.ColumnIndex;
            var bodyVal = eval(bodyElement, env);
            return bodyVal;
        }
    }
}