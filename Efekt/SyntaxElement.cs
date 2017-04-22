using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public interface SyntaxElement
    {
    }


    public interface ExpElement : SyntaxElement
    {
    }


    public interface ValueElement : ExpElement
    {
    }


    public interface IElementList<out T> : SyntaxElement, IReadOnlyList<T> where T : SyntaxElement
    {
    }


    public abstract class ElementList<T> : IElementList<T> where T : SyntaxElement
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


    public sealed class StatementList : ElementList<SyntaxElement>
    {
        public StatementList([NotNull] params SyntaxElement[] items) : base(items)
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


    public sealed class Var : SyntaxElement
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
        public When([NotNull] ExpElement test, [NotNull] SyntaxElement then, [CanBeNull] SyntaxElement otherwise)
        {
            Test = test;
            Then = then;
            Otherwise = otherwise;
        }

        [NotNull]
        public ExpElement Test { get; }

        [NotNull]
        public SyntaxElement Then { get; }

        [CanBeNull]
        public SyntaxElement Otherwise { get; }
    }


    public sealed class Loop : SyntaxElement
    {
        public Loop([NotNull] StatementList body)
        {
            Body = body;
        }

        [NotNull]
        public StatementList Body { get; }
    }


    public sealed class Return : SyntaxElement
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
        public Fn([NotNull] IdentList parameters, [NotNull] StatementList body)
        {
            Parameters = parameters;
            Body = body;
        }

        [NotNull]
        public IdentList Parameters { get; }

        [NotNull]
        public StatementList Body { get; }

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