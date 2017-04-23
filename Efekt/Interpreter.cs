using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Env
    {
        [NotNull] private readonly Dictionary<string, ValueElement> dict = new Dictionary<string, ValueElement>();

        public Env([CanBeNull] Env parent)
        {
            Parent = parent;
        }

        [CanBeNull]
        public Env Parent { get; }

        public ValueElement Get(string name)
        {
            if (dict.TryGetValue(name, out ValueElement value))
                return value;
            if (Parent != null)
                return Parent.Get(name);
            throw new Exception();
        }

        public void Declare(string name, ValueElement value)
        {
            if (dict.ContainsKey(name))
                throw new Exception();
            dict.Add(name, value);
        }

        public void Set(string name, ValueElement value)
        {
            if (!dict.ContainsKey(name))
                throw new Exception();
            dict[name] = value;
        }
    }


    public class Interpreter
    {
        [CanBeNull] private ValueElement ret;

        public ValueElement Eval(Element se)
        {
            /*   if (se is ElementList body)
               {
                   se = new FnApply(
                       new Fn(new IdentList(new List<Ident>()), body), 
                       new ElementList<ExpElement>(new List<ExpElement>()));
               }*/

            return Eval(se, new Env(null));
        }

        public ValueElement Eval(Element se, Env env)
        {
            switch (se)
            {
                case Var v:
                    var val = Eval(v.Exp, env);
                    env.Declare(v.Ident.Name, val);
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
                case ValueElement ve:
                    return ve;
                default:
                    throw new NotImplementedException();
                case null:
                    throw new Exception();
            }
        }
    }
}