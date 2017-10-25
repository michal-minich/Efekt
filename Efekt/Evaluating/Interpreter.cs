using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class StackItem
    {
        public readonly int LineIndex;
        public readonly string FnName;

        public StackItem(int lineIndex, string fnName)
        {
            LineIndex = lineIndex;
            FnName = fnName;
        }
    }

    public sealed class Interpreter
    {
        [CanBeNull] private Value ret;
        private bool isBreak;
        public Stack<StackItem> CallStack { get; private set; }
        
        public Value Eval(Prog prog)
        {
            CallStack = new Stack<StackItem>();

            try
            {
                return eval(prog.RootElement, Env.CreateRoot());
            }
            catch (EfektException ex)
            {
                var f = Utils.GetFilePathRelativeToBase(prog.FilePath);
                var msg = ex.Message
                          + Environment.NewLine
                          + string.Join(
                              Environment.NewLine,
                              new[] {new StackItem(ex.Element.LineIndex, getVarName(getParentFunction(ex.Element)) ?? "(runtime)") }
                                  .Concat(CallStack.Select(cs => cs).DistinctBy(cs => cs.LineIndex))
                                  .Select(cs => "  " + f + ":" + (cs.LineIndex + 1) + " " + cs.FnName));
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
                        if (obj is Obj o2)
                        {
                            o2.Env.Set(ma.Ident, val2);
                            return Void.Instance;
                        }
                        throw Error.OnlyObjectsHaveMembers(obj);
                    }
                    throw Error.AssignTargetIsInvalid(a.To);
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
                        CallStack.Push(new StackItem(fna.LineIndex, builtin.Name));
                        var res = builtin.Fn(new FnArguments(eArgs));
                        CallStack.Pop();
                        return res;
                    }
                    var fn2 = fn as Fn;
                    if (fn2 == null)
                        throw Error.OnlyFunctionsCanBeApplied(fn);
                    CallStack.Push(new StackItem(fna.LineIndex, getVarName(getParentFunction(fna)) ?? "(anonymous)"));
                    var paramsEnv = Env.Create(fn2.Env);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                    {
                        C.Nn(p);
                        var eArg = eArgs[ix++];
                        C.Nn(eArg);
                        paramsEnv.Declare(p, eArg);
                    }
                    var fnEnv = Env.Create(paramsEnv);
                    foreach (var bodyElement in fn2.Sequence)
                    {
                        C.Nn(bodyElement);
                        var bodyVal = eval(bodyElement, fnEnv);
                        if (bodyVal != Void.Instance)
                        {
                            if (bodyElement is FnApply fna2)
                                Warn.ValueReturnedFromFunctionNotUsed(fna2);
                            else
                                throw Error.ValueIsNotAssigned(bodyElement);
                        }
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
                    return fn3;
                case When w:
                    if (eval(w.Test, env) == Bool.True)
                        return eval(w.Then, Env.Create(env));
                    else if (w.Otherwise != null)
                        return eval(w.Otherwise, Env.Create(env));
                    else
                        return Void.Instance;
                case Loop l:
                    var loopEnv = Env.Create(env);
                    while (true)
                        foreach (var e in l.Body)
                        {
                            C.Nn(e);
                            if (isBreak)
                            {
                                isBreak = false;
                                return Void.Instance;
                            }
                            eval(e, loopEnv);
                        }
                // ReSharper disable once UnusedVariable
                case Break b:
                    isBreak = true;
                    return Void.Instance;
                case ArrConstructor ae:
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return new Arr(new Values(ae.Arguments.Select(e => eval(e, env)).ToArray()));
                case MemberAccess ma:
                    var exp = eval(ma.Exp, env);
                    if (exp is Obj o)
                        return o.Env.Get(ma.Ident);
                    throw Error.OnlyObjectsHaveMembers(exp);
                case New n:
                    var objEnv = Env.Create(env);
                    foreach (var v in n.Body)
                        eval(v, objEnv);
                    return new Obj(n.Body, objEnv);
                case Value ve:
                    return ve;
                case Sequence seq:
                    var scopeEnv = Env.Create(env);
                    foreach (var item in seq)
                    {
                        var bodyVal = eval(item, scopeEnv);
                        if (bodyVal != Void.Instance)
                            throw new Exception("Unused value");
                        if (ret != null)
                            return Void.Instance;
                    }
                    return Void.Instance;
                default:
                    throw Error.Fail();
            }
        }


        [CanBeNull]
        private Fn getParentFunction([CanBeNull] Element e)
        {
            while (true)
            {
                if (e == null)
                    return null;
                if (e.Parent is Fn fn)
                    return fn;
                e = e.Parent;
            }
        }


        [CanBeNull]
        private string getVarName([CanBeNull] Fn fn)
        {
            return fn == null ? null : (fn.Parent is Var v ? v.Ident.Name : null);
        }
    }
}