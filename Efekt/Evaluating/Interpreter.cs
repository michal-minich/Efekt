using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class StackItem
    {
        public Element e;
        private string fnName;
        public int LineIndex { get; internal set; }

        public string FnName
        {
            get
            {
                if (fnName == null)
                    fnName = getVarName((Fn)e) ?? "(anonymous)";
                return fnName;
            }
        }

        public string FilePath { get; }

        public StackItem(Element fna)
        {
            LineIndex = fna.LineIndex;
            FilePath = fna.FilePath;
            this.e = fna;
        }

        public StackItem(Element fna, string fnName)
        {
            LineIndex = fna.LineIndex;
            FilePath = fna.FilePath;
            this.e = fna;
            this.fnName = fnName;
        }



        [CanBeNull]
        private static Fn getParentFunction([CanBeNull] Element e)
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
        public  static string getVarName([CanBeNull] Fn fn)
        {
            return fn == null ? null : (fn.Parent is Var v ? v.Ident.Name : null);
        }
    }

    public sealed class Interpreter
    {
        [CanBeNull] private Value ret;
        private bool isBreak;
        private Prog prog;
        public Stack<StackItem> CallStack { get; private set; }
        
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
                var msg =
                    ex.Message
                    + Environment.NewLine
                    + string.Join(
                        Environment.NewLine,
                        /*new[] {new StackItem(ex.Element, getVarName(getParentFunction(ex.Element)) ?? "(runtime)")}
                            .Concat(*/CallStack.Select(cs => cs)/*.DistinctBy(cs => cs.LineIndex)*//*)*/
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
                        if (obj is Obj o2)
                        {
                            o2.Env.Set(ma.Ident, val2);
                            return Void.Instance;
                        }
                        throw prog.Remark.Error.OnlyObjectsHaveMembers(obj);
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
                        CallStack.Push(new StackItem(fna, builtin.Name));
                        var res = builtin.Fn(prog.Remark, new FnArguments(eArgs));
                        CallStack.Pop();
                        return res;
                    }
                    var fn2 = fn as Fn;
                    if (fn2 == null)
                        throw prog.Remark.Error.OnlyFunctionsCanBeApplied(fn);
                    var paramsEnv = Env.Create(prog.Remark, fn2.Env);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                    {
                        var eArg = eArgs[ix++];
                        paramsEnv.Declare(p, eArg);
                    }
                    var fnEnv = Env.Create(prog.Remark, paramsEnv);
                    CallStack.Push(new StackItem(fn2, StackItem.getVarName(fn2)));
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
                    return fn3;
                case When w:
                    var test = eval(w.Test, env);
                    var testB = test as Bool;
                    if (testB == null)
                        throw prog.Remark.Error.Fail();
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
                    if (exp is Obj o)
                        return o.Env.Get(ma.Ident);
                    throw prog.Remark.Error.OnlyObjectsHaveMembers(exp);
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