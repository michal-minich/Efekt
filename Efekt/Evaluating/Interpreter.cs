using System;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public class Interpreter
    {
        [CanBeNull] private Value ret;
        private bool isBreak;


        public Value Eval(Element se)
        {
            if (se is Exp exp)
                se = new Sequence(new[] {new Return(exp)});

            if (se is Sequence body)
                se = new FnApply(
                    new Fn(new FnParameters(), body),
                    new FnArguments());

            return eval(se, Env.CreateRoot());
        }


        private Value eval(Element se, Env env)
        {
            switch (se)
            {
                case Var v:
                    var val = eval(v.Exp, env);
                    env.Declare(v.Ident.Name, val);
                    return Void.Instance;
                case Assign a:
                    var val2 = eval(a.Exp, env);
                    if (a.To is Ident ident)
                    {
                        env.Set(ident.Name, val2);
                        return Void.Instance;
                    }
                    throw Error.Fail();
                case Ident i:
                    return env.Get(i.Name);
                case Return r:
                    ret = eval(r.Exp, env);
                    return Void.Instance;
                case FnApply fna:
                    var fn = eval(fna.Fn, env);
                    var builtin = fn as Builtin;
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var eArgs = fna.Arguments.Select(a => eval(a, env)).ToArray();
                    if (builtin != null)
                        return builtin.Fn(new FnArguments(eArgs));
                    var fn2 = fn as Fn;
                    if (fn2 == null)
                        throw Error.Fail();
                    var paramsEnv = Env.Create(fn2.Env);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                    {
                        C.Nn(p);
                        var eArg = eArgs[ix++];
                        C.Nn(eArg);
                        paramsEnv.Declare(p.Name, eArg);
                    }
                    var fnEnv = Env.Create(paramsEnv);
                    foreach (var bodyElement in fn2.Sequence)
                    {
                        C.Nn(bodyElement);
                        // ReSharper disable once UnusedVariable
                        var bodyVal = eval(bodyElement, fnEnv);
                        //if (bodyVal != Void.Instance)
                        //if (bodyElement is Value)
                        //    throw new Exception("Unused value");
                        if (ret != null)
                        {
                            var tmp = ret;
                            ret = null;
                            return tmp;
                        }
                    }
                    return Void.Instance;
                case Fn f:
                    return new Fn(f.Parameters, f.Sequence, env);
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
                        return o.Env.Get(ma.Ident.Name);
                    throw Error.Fail();
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
    }
}