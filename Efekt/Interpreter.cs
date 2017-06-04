using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Env
    {
        [NotNull] private readonly Dictionary<string, Value> dict = new Dictionary<string, Value>();

        public Env([CanBeNull] Env parent)
        {
            Parent = parent;
        }

        [CanBeNull]
        public Env Parent { get; }

        public Value Get(string name)
        {
            if (dict.TryGetValue(name, out Value value))
                return value;
            if (Parent != null)
                return Parent.Get(name);
            throw new Exception();
        }

        public void Declare(string name, Value value)
        {
            if (dict.ContainsKey(name))
                throw new Exception();
            dict.Add(name, value);
        }

        public void Set(string name, Value value)
        {
            var e = this;
            do
            {
                if (e.dict.ContainsKey(name))
                {
                    e.dict[name] = value;
                    return;
                }
                e = e.Parent;
            } while (e != null);
            throw new Exception();
        }
    }


    public class Interpreter
    {
        [CanBeNull] private Value ret;
        private bool isBreak;

        public Value Eval(Element se)
        {
            if (se is Exp exp)
                se = new ElementList(new Return(exp));

            if (se is ElementList body)
                se = new FnApply(
                    new Fn(new IdentList(), body),
                    new ExpList());

            return Eval(se, new Env(null));
        }

        public Value Eval(Element se, Env env)
        {
            switch (se)
            {
                case Var v:
                    var val = Eval(v.Exp, env);
                    env.Declare(v.Ident.Name, val);
                    return Void.Instance;
                case Assign a:
                    var val2 = Eval(a.Exp, env);
                    env.Set(a.Ident.Name, val2);
                    return Void.Instance;
                case Ident i:
                    return env.Get(i.Name);
                case Return r:
                    ret = Eval(r.Exp, env);
                    return Void.Instance;
                case FnApply fna:
                    var fn = Eval(fna.Fn, env);
                    var fn2 = fn as Fn;
                    if (fn2 == null)
                        throw new Exception();
                    var eArgs = fna.Arguments.Select(a => Eval(a, env)).ToList();
                    var paramsEnv = new Env(fn2.LexicalEnv);
                    var ix = 0;
                    foreach (var p in fn2.Parameters)
                        paramsEnv.Declare(p.Name, eArgs[ix++]);
                    var fnEnv = new Env(paramsEnv);
                    foreach (var bodyElement in fn2.Body)
                    {
                        var bodyVal = Eval(bodyElement, fnEnv);
                        if (bodyVal != Void.Instance)
                            throw new Exception("Unused value");
                        if (ret != null)
                        {
                            var tmp = ret;
                            ret = null;
                            return tmp;
                        }
                    }
                    return Void.Instance;
                case Fn f:
                    if (f.LexicalEnv == null)
                        f.LexicalEnv = env;
                    return f;
                case When w:
                    if (Eval(w.Test, env) == Bool.True)
                        return Eval(w.Then, env);
                    else if (w.Otherwise != null)
                        return Eval(w.Otherwise, env);
                    else
                        return Void.Instance;
                case Loop l:
                    while (true)
                    {
                        foreach (var e in l.Body)
                        {
                            if (isBreak)
                            {
                                isBreak = false;
                                return Void.Instance;
                            }
                            Eval(e, env);
                        }
                    }
                case Break b:
                    isBreak = true;
                    return Void.Instance;
                case Value ve:
                    return ve;
                case ElementList el:
                    var newEnv = new Env(env);
                    foreach (var listElement in el)
                    {
                        var bodyVal = Eval(listElement, newEnv);
                        if (bodyVal != Void.Instance)
                            throw new Exception("Unused value");
                        if (ret != null)
                            return Void.Instance;
                    }
                    return Void.Instance;
                default:
                    throw new NotImplementedException();
                case null:
                    throw new Exception();
            }
        }
    }
}