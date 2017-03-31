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


    public class ElementList<T> : SyntaxElement where T : SyntaxElement
    {
        public ElementList(List<T> items)
        {
            Items = items;
        }


        [NotNull]
        public List<T> Items { get; }
    }


    public class Ident : ExpElement
    {
        public Ident(string name)
        {
            Name = name;
        }

        [NotNull]
        public string Name { get; }
    }


    public class Var : SyntaxElement
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


    public class When : ExpElement
    {
        public When(SyntaxElement test, SyntaxElement then, [CanBeNull] SyntaxElement otherwise)
        {
            Test = test;
            Then = then;
            Otherwise = otherwise;
        }

        [NotNull]
        public SyntaxElement Test { get; }

        [NotNull]
        public SyntaxElement Then { get; }

        [CanBeNull]
        public SyntaxElement Otherwise { get; }
    }


    public class Loop : SyntaxElement
    {
        public Loop(ElementList<SyntaxElement> body)
        {
            Body = body;
        }

        [NotNull]
        public ElementList<SyntaxElement> Body { get; }
    }


    public class Return : SyntaxElement
    {
        public Return(ExpElement exp)
        {
            Exp = exp;
        }

        [NotNull]
        public ExpElement Exp { get; }
    }


    public class Fn : ValueElement
    {
        public Fn(ElementList<Ident> parameters, ElementList<SyntaxElement> body)
        {
            Parameters = parameters;
            Body = body;
        }

        [NotNull]
        public ElementList<Ident> Parameters { get; }

        [NotNull]
        public ElementList<SyntaxElement> Body { get; }

        public Env LexicalEnv { get; set; }
    }


    public class Int : ValueElement
    {
        public Int(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }


    public class Void : ValueElement
    {
        private Void()
        {
        }

        public static Void Instance { get; } = new Void();
    }

    public class FnApply : ExpElement
    {
        public FnApply(ExpElement fn, ElementList<ExpElement> arguments)
        {
            Fn = fn;
            Arguments = arguments;
        }

        [NotNull]
        public ExpElement Fn { get; }

        [NotNull]
        public ElementList<ExpElement> Arguments { get; }
    }
}