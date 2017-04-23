using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public interface Element
    {
    }


    public interface ExpElement : Element
    {
    }


    public interface ValueElement : ExpElement
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

    public sealed class ExpList : ElementList<ExpElement>
    {
        public ExpList([NotNull] params ExpElement[] items) : base(items)
        {
        }
    }

    public sealed class IdentList : ElementList<Ident>
    {
        public IdentList([NotNull] params Ident[] items) : base(items)
        {
        }
    }

    public sealed class Ident : ExpElement
    {
        public Ident([NotNull] string name)
        {
            Name = name;
        }

        [NotNull]
        public string Name { get; }
    }


    public sealed class Var : Element
    {
        public Var([NotNull] Ident ident, [NotNull] ExpElement exp)
        {
            Ident = ident;
            Exp = exp;
        }

        [NotNull]
        public Ident Ident { get; }

        [NotNull]
        public ExpElement Exp { get; }
    }


    public sealed class When : ExpElement
    {
        public When([NotNull] ExpElement test, [NotNull] Element then, [CanBeNull] Element otherwise)
        {
            Test = test;
            Then = then;
            Otherwise = otherwise;
        }

        [NotNull]
        public ExpElement Test { get; }

        [NotNull]
        public Element Then { get; }

        [CanBeNull]
        public Element Otherwise { get; }
    }


    public sealed class Loop : Element
    {
        public Loop([NotNull] ElementList body)
        {
            Body = body;
        }

        [NotNull]
        public ElementList Body { get; }
    }


    public sealed class Return : Element
    {
        public Return([NotNull] ExpElement exp)
        {
            Exp = exp;
        }

        [NotNull]
        public ExpElement Exp { get; }
    }


    public sealed class Fn : ValueElement
    {
        public Fn([NotNull] IdentList parameters, [NotNull] ElementList body)
        {
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


    public sealed class Int : ValueElement
    {
        public Int(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }


    public sealed class Void : ValueElement
    {
        private Void()
        {
        }

        [NotNull]
        public static Void Instance { get; } = new Void();
    }


    public sealed class FnApply : ExpElement
    {
        public FnApply([NotNull] ExpElement fn, [NotNull] ExpList arguments)
        {
            Fn = fn;
            Arguments = arguments;
        }

        [NotNull]
        public ExpElement Fn { get; }

        [NotNull]
        public ExpList Arguments { get; }
    }
}