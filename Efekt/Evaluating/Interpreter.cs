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
        public int LineIndex { get; internal set; }
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
        public Stack<StackItem> CallStack { get; private set; }

        private static readonly StringWriter sw = new StringWriter();
        private static readonly PlainTextCodeWriter ctw = new PlainTextCodeWriter(sw);
        private static readonly Printer cw = new Printer(ctw);

        public Value Eval(Prog program)
        {
            prog = program;
            CallStack = new Stack<StackItem>();

            try
            {
                return eval(program.RootElement, Env.CreateRoot(program.Remark));
            }
            catch (EfektException ex)
            {
                cw.Write(ex.Element);
                var msg =
                    ex.Message + " in '" + sw.GetAndReset() + "'"
                    + Environment.NewLine
                    + string.Join(
                        Environment.NewLine,
                        CallStack
                            .Select(cs => "  " + Utils.GetFilePathRelativeToBase(cs.FilePath)
                                          + ":" + (cs.LineIndex + 1) + " " + cs.FnName));
                throw new EfektException(msg, ex, ex.Element);
            }
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
                    var val2 = eval(a.Exp, env);
                    if (a.To is Ident ident)
                    {
                        env.Set(ident, val2);
                        return Void.Instance;
                    }
                    else if (a.To is MemberAccess ma)
                    {
                        var obj = eval(ma.Exp, env);
                        var o2 = obj.AsObj(prog.Remark, ma);
                        o2.Env.Set(ma.Ident, val2);
                        return Void.Instance;
                    }
                    throw prog.Remark.Error.AssignTargetIsInvalid(a.To);
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
                        CallStack.Push(new StackItem(builtin, builtin.Name));
                        var res = builtin.Fn(prog.Remark, new FnArguments(eArgs), fna);
                        CallStack.Pop();
                        return res;
                    }
                    var fn2 = fn.AsFn(prog.Remark, fna);
                    var paramsEnv = Env.Create(prog.Remark, fn2.Env);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                    {
                        var eArg = eArgs[ix++];
                        paramsEnv.Declare(p, eArg);
                    }
                    var fnEnv = Env.Create(prog.Remark, paramsEnv);
                    CallStack.Push(new StackItem(fn2));
                    foreach (var bodyElement in fn2.Sequence)
                    {
                        evalSequenceItem(bodyElement, fnEnv);
                        if (ret != null)
                        {
                            var tmp = ret;
                            ret = null;
                            CallStack.Pop();
                            return tmp;
                        }
                    }
                    CallStack.Pop();
                    return Void.Instance;
                case Fn f:
                    var fn3 = new Fn(f.Parameters, f.Sequence, env);
                    fn3.Parent = f.Parent;
                    fn3.LineIndex = f.LineIndex;
                    fn3.FilePath = f.FilePath;
                    return fn3;
                case When w:
                    var test = eval(w.Test, env);
                    var testB = test.AsBool(prog.Remark, w.Test);
                    if (testB.Value)
                        return eval(w.Then, Env.Create(prog.Remark, env));
                    else if (w.Otherwise != null)
                        return eval(w.Otherwise, Env.Create(prog.Remark, env));
                    else
                        return Void.Instance;
                case Loop l:
                    var loopEnv = Env.Create(prog.Remark, env);
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
                    var o = exp.AsObj(prog.Remark, ma);
                    return o.Env.Get(ma.Ident);
                case New n:
                    var objEnv = Env.Create(prog.Remark, env);
                    foreach (var v in n.Body)
                        eval(v, objEnv);
                    return new Obj(n.Body, objEnv);
                case Value ve:
                    return ve;
                case Sequence seq:
                    var scopeEnv = Env.Create(prog.Remark, env);
                    foreach (var item in seq)
                    {
                        evalSequenceItem(item, scopeEnv);
                        if (ret != null)
                        {
                            return Void.Instance;
                            //var tmp = ret;
                            //ret = null;
                            //return tmp;
                        }
                    }
                    return Void.Instance;
                default:
                    throw new Exception();
            }
        }

        private void evalSequenceItem(Element bodyElement, Env env)
        {
            CallStack.Peek().LineIndex = bodyElement.LineIndex;
            var bodyVal = eval(bodyElement, env);
            if (bodyVal != Void.Instance)
            {
                if (bodyElement is FnApply fna2)
                    prog.Remark.Warn.ValueReturnedFromFunctionNotUsed(fna2);
                else
                    prog.Remark.Warn.ValueIsNotAssigned(bodyElement);
            }
        }
    }
}