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


    public interface IElementList<out T> : SyntaxElement where T : SyntaxElement
    {
        IReadOnlyList<T> Items { get; }
    }


    public sealed class ElementList<T> : IElementList<T> where T : SyntaxElement
    {
        public ElementList(IReadOnlyList<T> items)
        {
            Items = items;
        }

        [NotNull]
        public IReadOnlyList<T> Items { get; }
    }


    public sealed class Ident : ExpElement
    {
        public Ident(string name)
        {
            Name = name;
        }

        [NotNull]
        public string Name { get; }
    }


    public sealed class Var : SyntaxElement
    {
        public Var(Ident ident, ExpElement exp)
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
        public When(ExpElement test, SyntaxElement then, [CanBeNull] SyntaxElement otherwise)
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
        public Loop(IElementList<SyntaxElement> body)
        {
            Body = body;
        }

        [NotNull]
        public IElementList<SyntaxElement> Body { get; }
    }


    public sealed class Return : SyntaxElement
    {
        public Return(ExpElement exp)
        {
            Exp = exp;
        }

        [NotNull]
        public ExpElement Exp { get; }
    }


    public sealed class Fn : ValueElement
    {
        public Fn(IElementList<Ident> parameters, IElementList<SyntaxElement> body)
        {
            Parameters = parameters;
            Body = body;
        }

        [NotNull]
        public IElementList<Ident> Parameters { get; }

        [NotNull]
        public IElementList<SyntaxElement> Body { get; }

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

        public static Void Instance { get; } = new Void();
    }


    public sealed class FnApply : ExpElement
    {
        public FnApply(ExpElement fn, IElementList<ExpElement> arguments)
        {
            Fn = fn;
            Arguments = arguments;
        }

        [NotNull]
        public ExpElement Fn { get; }

        [NotNull]
        public IElementList<ExpElement> Arguments { get; }
    }
}