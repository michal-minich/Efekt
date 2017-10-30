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
        private Prog prog;
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
                case Var v:
                    var val = eval(v.Exp, env);
                    env.Declare(v.Ident, val);
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
                    return env.Get(i);
                case Return r:
                    ret = eval(r.Exp, env);
                    return Void.Instance;
                case FnApply fna:
                    var fn = eval(fna.Fn, env);
                    var builtin = fn as Builtin;
                    var eArgs = fna.Arguments.Select(a => eval(a, env)).ToArray();
                    if (builtin != null)
                    {
                        callStack.Push(new StackItem(builtin, builtin.Name));
                        var res = builtin.Fn(new FnArguments(eArgs), fna);
                        callStack.Pop();
                        return res;
                    }
                    var fn2 = fn.AsFn(fna, prog);
                    var paramsEnv = Env.Create(prog, fn2.Env);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                    {
                        var eArg = eArgs[ix++];
                        paramsEnv.Declare(p, eArg);
                    }
                    var fnEnv = Env.Create(prog, paramsEnv);
                    callStack.Push(new StackItem(fn2));
                    foreach (var bodyElement in fn2.Sequence)
                    {
                        evalSequenceItem(bodyElement, fnEnv);
                        if (ret != null)
                        {
                            var tmp = ret;
                            ret = null;
                            callStack.Pop();
                            return tmp;
                        }
                    }
                    callStack.Pop();
                    return Void.Instance;
                case Fn f:
                    var fn3 = new Fn(f.Parameters, f.Sequence, env);
                    fn3.Parent = f.Parent;
                    fn3.LineIndex = f.LineIndex;
                    fn3.FilePath = f.FilePath;
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
                        foreach (var e in l.Body)
                        {
                            evalSequenceItem(e, loopEnv);
                            if (isBreak)
                            {
                                isBreak = false;
                                return Void.Instance;
                            }
                            if (ret != null)
                                return Void.Instance;
                        }
                case Break _:
                    isBreak = true;
                    return Void.Instance;
                case ArrConstructor ae:
                    return new Arr(new Values(ae.Arguments.Select(e => eval(e, env)).ToArray()));
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
                    foreach (var item in seq)
                    {
                        evalSequenceItem(item, scopeEnv);
                        if (ret != null)
                            return Void.Instance;
                    }
                    return Void.Instance;
                default:
                    throw new NotSupportedException();
            }
        }

        private void evalSequenceItem(Element bodyElement, Env env)
        {
            callStack.Peek().LineIndex = bodyElement.LineIndex;
            var bodyVal = eval(bodyElement, env);
            if (bodyVal != Void.Instance)
            {
                if (bodyElement is FnApply fna2)
                    prog.RemarkList.Warn.ValueReturnedFromFunctionNotUsed(fna2);
                else
                    prog.RemarkList.Warn.ValueIsNotAssigned(bodyElement);
            }
        }
    }
}