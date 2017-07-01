using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public abstract class Element
    {
        private static readonly StringWriter sw = new StringWriter();
        private static readonly CodeTextWriter ctw = new CodeTextWriter(sw);
        private static readonly CodeWriter cw = new CodeWriter(ctw);

        public override string ToString()
        {
            cw.Write(this);
            return GetType().Name + ": " + sw.GetAndReset();
        }
    }


    public abstract class Exp : Element
    {
    }


    public abstract class Value : Exp
    {
    }


    /*public interface IElementList<out T> : Element, IReadOnlyList<T> where T : Element
    {
    }*/


    public abstract class ElementList<T> : Element, IReadOnlyList<T> where T : Element
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


    public sealed class ElementList : ElementList<Element>
    {
        public ElementList([NotNull] params Element[] items) : base(items)
        {
        }
    }


    public sealed class ExpList : ElementList<Exp>
    {
        public ExpList([NotNull] params Exp[] items) : base(items)
        {
        }
    }


    public sealed class IdentList : ElementList<Ident>
    {
        public IdentList([NotNull] params Ident[] items) : base(items)
        {
        }
    }


    public sealed class Builtin : Value
    {
        public Builtin([NotNull] string name, [NotNull] Func<ExpList, Value> fn)
        {
            Name = name;
            Fn = fn;
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public Func<ExpList, Value> Fn { get; }
    }


    public sealed class Ident : Exp
    {
        public Ident([NotNull] string name)
        {
            C.Req(!string.IsNullOrWhiteSpace(name));
            C.Req(name.Trim().Length == name.Length);

            Name = name;
        }

        [NotNull]
        public string Name { get; }
    }


    public sealed class Var : Element
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
    

    public sealed class Assign : Exp // TODO: make it implement Element
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


    public sealed class Loop : Element
    {
        public Loop([NotNull] ElementList body)
        {
            C.Nn(body);
            Body = body;
        }

        [NotNull]
        public ElementList Body { get; }
    }


    public sealed class Return : Element
    {
        public Return([NotNull] Exp exp)
        {
            C.Nn(exp);
            Exp = exp;
        }

        [NotNull]
        public Exp Exp { get; }
    }


    public sealed class Break : Element
    {
        private Break()
        {
        }

        [NotNull]
        public static Break Instance { get; } = new Break();
    }


    public sealed class Fn : Value
    {
        public Fn([NotNull] IdentList parameters, [NotNull] ElementList body)
        {
            C.Nn(parameters);
            C.Nn(body);

            Parameters = parameters;
            Body = body;
        }

        [NotNull]
        public IdentList Parameters { get; }

        [NotNull]
        public ElementList Body { get; }

        [NotNull]
        public Env LexicalEnv { get; set; }
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
        public FnApply([NotNull] Exp fn, [NotNull] ExpList arguments)
        {
            C.Nn(fn);
            C.Nn(arguments);

            Fn = fn;
            Arguments = arguments;
        }

        [NotNull]
        public Exp Fn { get; }

        [NotNull]
        public ExpList Arguments { get; }
    }
}