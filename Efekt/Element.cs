using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public interface Element
    {
    }


    public interface Exp : Element
    {
    }


    public interface Stm : Element
    {
    }


    public interface Value : Exp
    {
    }


    public interface IElementList<out T> : IReadOnlyList<T> where T : Element
    {
    }


    public abstract class ElementList<T> : IElementList<T> where T : Element
    {
        private readonly IReadOnlyList<T> items;

        protected ElementList(IReadOnlyList<T> items)
        {
            C.AllNotNull(items);
            this.items = items;
        }

        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        public int Count => items.Count;
        public T this[int index] => items[index];
    }


    public sealed class ElementList : ElementList<Element>, Element
    {
        public ElementList(params Element[] items) : base(items)
        {
        }
    }


    public sealed class ElementListBuilder
    {
        private readonly List<Element> items = new List<Element>();

        public void Add(Element e)
        {
            C.Nn(e);
            items.Add(e);
        }

        public ElementList GetAndReset()
        {
            var res = new ElementList(items.ToArray());
            items.Clear();
            return res;
        }

        public Sequence GetSequenceAndReset()
        {
            var res = new Sequence(items.ToArray());
            items.Clear();
            return res;
        }
    }


    public sealed class Sequence : ElementList<Element>, Stm
    {
        public Sequence(params Element[] items) : base(items)
        {
        }
    }


    public sealed class ClassBody : ElementList<Var>
    {
        public ClassBody() : base(new Var[0])
        {
        }

        public ClassBody(IReadOnlyList<Var> items) : base(items)
        {
        }
    }


    public sealed class FnArguments : ElementList<Exp>
    {
        public FnArguments() : base(new Exp[0])
        {
        }

        public FnArguments(IReadOnlyList<Exp> items) : base(items)
        {
        }
    }


    public sealed class FnParameters : ElementList<Ident>
    {
        public FnParameters() : base(new Ident[0])
        {
        }

        public FnParameters(IReadOnlyList<Ident> items) : base(items)
        {
        }
    }


    public sealed class Values : ElementList<Value>
    {
        public Values() : base(new Value[0])
        {
        }

        public Values(IReadOnlyList<Value> items) : base(items)
        {
        }
    }


    public sealed class Builtin : Value
    {
        public Builtin(string name, Func<FnArguments, Value> fn)
        {
            C.Assert(!string.IsNullOrWhiteSpace(name));
            C.Assert(name.Trim().Length == name.Length);
            C.Nn(fn);

            Name = name;
            Fn = fn;
        }

        public string Name { get; }
        public Func<FnArguments, Value> Fn { get; }
    }


    public sealed class Ident : Exp
    {
        public Ident(string name, TokenType tokenType)
        {
            C.Assert(!string.IsNullOrWhiteSpace(name));
            C.Assert(name.Trim().Length == name.Length);

            Name = name;
            TokenType = tokenType;
        }
        
        public string Name { get; }
        public TokenType TokenType { get; }
    }


    public sealed class Var : Stm
    {
        public Var(Ident ident, Exp exp)
        {
            C.Nn(ident);
            C.Nn(exp);

            Ident = ident;
            Exp = exp;
        }
        
        public Ident Ident { get; }
        public Exp Exp { get; }
    }


    public sealed class Assign : Stm
    {
        public Assign(Ident ident, Exp exp)
        {
            C.Nn(ident);
            C.Nn(exp);

            Ident = ident;
            Exp = exp;
        }
        
        public Ident Ident { get; }
        public Exp Exp { get; }
    }


    public sealed class When : Exp
    {
        public When(Exp test, Element then, [CanBeNull] Element otherwise)
        {
            C.Nn(test);
            C.Nn(then);

            Test = test;
            Then = then;
            Otherwise = otherwise;
        }


        public Exp Test { get; }
        public Element Then { get; }
        [CanBeNull]
        public Element Otherwise { get; }
    }


    public sealed class Loop : Stm
    {
        public Loop(Sequence body)
        {
            C.AllNotNull(body);
            Body = body;
        }
        
        public Sequence Body { get; }
    }


    public sealed class Return : Stm
    {
        public Return(Exp exp)
        {
            C.Nn(exp);
            Exp = exp;
        }

        public Exp Exp { get; }
    }


    public sealed class Break : Stm
    {
        private Break()
        {
        }

        public static Break Instance { get; } = new Break();
    }


    public sealed class Fn : Value
    {
        public Fn(FnParameters parameters, Sequence sequence)
        {
            C.AllNotNull(parameters);
            C.AllNotNull(sequence);

            Parameters = parameters;
            Sequence = sequence;
        }

        public Fn(FnParameters parameters, Sequence sequence, Env env)
            : this(parameters, sequence)
        {
            C.Nn(env);

            Env = env;
        }

        public FnParameters Parameters { get; }
        public Sequence Sequence { get; }
        public Env Env { get; }
    }


    public sealed class Int : Value
    {
        public Int(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }


    public sealed class Bool : Value
    {
        private Bool(bool value)
        {
            Value = value;
        }

        public bool Value { get; }
        public static Bool True { get; } = new Bool(true);
        public static Bool False { get; } = new Bool(false);
    }


    public sealed class Void : Value
    {
        private Void()
        {
        }
        
        public static Void Instance { get; } = new Void();
    }


    public sealed class FnApply : Exp
    {
        public FnApply(Exp fn, FnArguments arguments)
        {
            C.Nn(fn);
            C.AllNotNull(arguments);

            Fn = fn;
            Arguments = arguments;
        }

        public Exp Fn { get; }
        public FnArguments Arguments { get; }
    }


    public sealed class ArrConstructor : Exp
    {
        public ArrConstructor(FnArguments arguments)
        {
            C.AllNotNull(arguments);

            Arguments = arguments;
        }

        public FnArguments Arguments { get; }
    }


    public sealed class Arr : Value
    {
        public Arr(Values values)
        {
            C.AllNotNull(values);

            Values = values;
        }

        public Values Values { get; }
    }


    public sealed class New : Exp
    {
        public New(ClassBody body)
        {
            C.AllNotNull(body);

            Body = body;
        }

        public ClassBody Body { get; }
    }


    public sealed class Obj : Value
    {
        public Obj(ClassBody body, Env env)
        {
            C.AllNotNull(body);
            C.Nn(env);

            Body = body;
            Env = env;
        }
        
        public ClassBody Body { get; }
        public Env Env { get; }
    }


    public sealed class MemberAccess : Exp
    {
        public MemberAccess(Exp exp, Ident ident)
        {
            C.Nn(exp);
            C.Nn(ident);

            Exp = exp;
            Ident = ident;
        }
        
        public Exp Exp { get; }
        public Ident Ident { get; }
    }
}