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
        [NotNull] private readonly IReadOnlyList<T> items;

        protected ElementList([NotNull] IReadOnlyList<T> items)
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
        public ElementList([NotNull] params Element[] items) : base(items)
        {
        }
    }


    public sealed class Sequence : ElementList<Element>, Stm
    {
        public Sequence([NotNull] params Element[] items) : base(items)
        {
        }
    }


    public sealed class ClassBody : ElementList<Var>
    {
        public ClassBody([NotNull] params Var[] items) : base(items)
        {
        }
    }


    public sealed class FnArguments : ElementList<Exp>
    {
        public FnArguments([NotNull] params Exp[] items) : base(items)
        {
        }
    }


    public sealed class FnParameters : ElementList<Ident>
    {
        public FnParameters([NotNull] params Ident[] items) : base(items)
        {
        }
    }


    public sealed class Values : ElementList<Value>
    {
        public Values([NotNull] params Value[] items) : base(items)
        {
        }
    }


    public sealed class Builtin : Value
    {
        public Builtin([NotNull] string name, [NotNull] Func<FnArguments, Value> fn)
        {
            C.Assert(!string.IsNullOrWhiteSpace(name));
            C.Assert(name.Trim().Length == name.Length);
            C.Nn(fn);

            Name = name;
            Fn = fn;
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public Func<FnArguments, Value> Fn { get; }
    }


    public sealed class Ident : Exp
    {
        public Ident([NotNull] string name)
        {
            C.Assert(!string.IsNullOrWhiteSpace(name));
            C.Assert(name.Trim().Length == name.Length);

            Name = name;
        }

        [NotNull]
        public string Name { get; }
    }


    public sealed class Var : Stm
    {
        public Var([NotNull] Ident ident, [NotNull] Exp exp)
        {
            C.Nn(ident);
            C.Nn(exp);

            Ident = ident;
            Exp = exp;
        }

        [NotNull]
        public Ident Ident { get; }

        [NotNull]
        public Exp Exp { get; }
    }


    public sealed class Assign : Stm
    {
        public Assign([NotNull] Ident ident, [NotNull] Exp exp)
        {
            C.Nn(ident);
            C.Nn(exp);

            Ident = ident;
            Exp = exp;
        }

        [NotNull]
        public Ident Ident { get; }

        [NotNull]
        public Exp Exp { get; }
    }


    public sealed class When : Exp
    {
        public When([NotNull] Exp test, [NotNull] Element then, [CanBeNull] Element otherwise)
        {
            C.Nn(test);
            C.Nn(then);

            Test = test;
            Then = then;
            Otherwise = otherwise;
        }

        [NotNull]
        public Exp Test { get; }

        [NotNull]
        public Element Then { get; }

        [CanBeNull]
        public Element Otherwise { get; }
    }


    public sealed class Loop : Stm
    {
        public Loop([NotNull] Sequence body)
        {
            C.AllNotNull(body);
            Body = body;
        }

        [NotNull]
        public Sequence Body { get; }
    }


    public sealed class Return : Stm
    {
        public Return([NotNull] Exp exp)
        {
            C.Nn(exp);
            Exp = exp;
        }

        [NotNull]
        public Exp Exp { get; }
    }


    public sealed class Break : Stm
    {
        private Break()
        {
        }

        [NotNull]
        public static Break Instance { get; } = new Break();
    }


    public sealed class Fn : Value
    {
        public Fn([NotNull] FnParameters parameters, [NotNull] Sequence sequence)
        {
            C.AllNotNull(parameters);
            C.AllNotNull(sequence);

            Parameters = parameters;
            Sequence = sequence;
        }

        public Fn([NotNull] FnParameters parameters, [NotNull] Sequence sequence, [NotNull] Env env)
            : this(parameters, sequence)
        {
            C.Nn(env);

            Env = env;
        }

        [NotNull]
        public FnParameters Parameters { get; }

        [NotNull]
        public Sequence Sequence { get; }

        [NotNull]
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

        [NotNull]
        public static Bool True { get; } = new Bool(true);

        [NotNull]
        public static Bool False { get; } = new Bool(false);
    }


    public sealed class Void : Value
    {
        private Void()
        {
        }

        [NotNull]
        public static Void Instance { get; } = new Void();
    }


    public sealed class FnApply : Exp
    {
        public FnApply([NotNull] Exp fn, [NotNull] FnArguments arguments)
        {
            C.Nn(fn);
            C.AllNotNull(arguments);

            Fn = fn;
            Arguments = arguments;
        }

        [NotNull]
        public Exp Fn { get; }

        [NotNull]
        public FnArguments Arguments { get; }
    }


    public sealed class ArrConstructor : Exp
    {
        public ArrConstructor(FnArguments arguments)
        {
            C.AllNotNull(arguments);

            Arguments = arguments;
        }

        [NotNull]
        public FnArguments Arguments { get; }
    }


    public sealed class Arr : Value
    {
        public Arr(Values values)
        {
            C.AllNotNull(values);

            Values = values;
        }

        [NotNull]
        public Values Values { get; }
    }


    public sealed class New : Exp
    {
        public New([NotNull] ClassBody body)
        {
            C.AllNotNull(body);

            Body = body;
        }

        [NotNull]
        public ClassBody Body { get; }
    }


    public sealed class Obj : Value
    {
        public Obj([NotNull] ClassBody body, [NotNull] Env env)
        {
            C.AllNotNull(body);
            C.Nn(env);

            Body = body;
            Env = env;
        }

        [NotNull]
        public ClassBody Body { get; }

        [NotNull]
        public Env Env { get; }
    }


    public sealed class MemberAccess : Exp
    {
        public MemberAccess([NotNull] Exp exp, [NotNull] Ident ident)
        {
            C.Nn(exp);
            C.Nn(ident);

            Exp = exp;
            Ident = ident;
        }

        [NotNull]
        public Exp Exp { get; }

        [NotNull]
        public Ident Ident { get; }
    }
}