using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Efekt
{
    public interface Element
    {
        int LineIndex { get; set; }
        string FilePath { get; set; }
        Element Parent { get; set; }
        bool IsBraced { get; set; }
    }


    public abstract class AElement : Element
    {
        protected AElement()
        {
            LineIndex = -1;
            FilePath = "runtime.ef";
        }

        public int LineIndex { get; set; }
        public string FilePath { get; set; }
        public Element Parent { get; set; }
        public bool IsBraced { get; set; }
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


    public interface IElementList<out T> : IReadOnlyList<T> where T : class, Element
    {
    }


    public abstract class ElementList<T> : IElementList<T> where T : class, Element
    {
        private readonly IReadOnlyList<T> items;

        [DebuggerStepThrough]
        protected ElementList(IReadOnlyList<T> items)
        {
            C.AllNotNull(items);
            this.items = items;
        }
        
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        public int Count => items.Count;
        [NotNull]
        public T this[int index] => items[index];
    }


    public sealed class ElementListBuilder
    {
        public List<Element> Items { get; } = new List<Element>();

        [DebuggerStepThrough]
        public void Add(Element e)
        {
            C.Nn(e);
            Items.Add(e);
        }
    }


    public sealed class Sequence : ElementList<Element>, Stm
    {
        [DebuggerStepThrough]
        public Sequence(IReadOnlyList<Element> items) : base(items)
        {
            if (TokenIterator.Instance != null)
            {
                //LineIndex = TokenIterator.Instance.LineIndex;
                //FilePath = TokenIterator.Instance.FilePath;
            }
            LineIndex = -1;
            FilePath = "runtime.ef";
            foreach (var i in items)
                i.Parent = this;
        }

        public int LineIndex { get; set; }
        public string FilePath { get; set; }
        public Element Parent { get; set; }
        public bool IsBraced { get; set; }
    }


    public sealed class ClassBody : ElementList<Var>
    {
        [DebuggerStepThrough]
        public ClassBody(IReadOnlyList<Var> items) : base(items)
        {
        }
    }


    public sealed class FnArguments : ElementList<Exp>
    {
        [DebuggerStepThrough]
        public FnArguments() : base(new Exp[0])
        {
        }

        [DebuggerStepThrough]
        public FnArguments(IReadOnlyList<Exp> items) : base(items)
        {
        }
    }


    public sealed class FnParameters : ElementList<Ident>
    {
        [DebuggerStepThrough]
        public FnParameters() : base(new Ident[0])
        {
        }

        [DebuggerStepThrough]
        public FnParameters(IReadOnlyList<Ident> items) : base(items)
        {
        }
    }


    public sealed class Values : ElementList<Value>
    {
        [DebuggerStepThrough]
        public Values(IReadOnlyList<Value> items) : base(items)
        {
        }
    }


    public sealed class Builtin : AElement, Value
    {
        [DebuggerStepThrough]
        public Builtin(string name, Func<Remark, FnArguments, Exp, Value> fn)
        {
            C.Assert(!string.IsNullOrWhiteSpace(name));
            C.Assert(name.Trim().Length == name.Length);
            C.Nn(fn);

            Name = name;
            Fn = fn;
        }

        public string Name { get; }
        public Func<Remark, FnArguments, Exp, Value> Fn { get; }
    }


    public sealed class Ident : AElement, Exp
    {
        [DebuggerStepThrough]
        public Ident(string name, TokenType tokenType)
        {
            C.Assert(!string.IsNullOrWhiteSpace(name));
            C.Assert(name.Trim().Length == name.Length);

            Name = name;
            TokenType = tokenType;
        }
        
        public string Name { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public TokenType TokenType { get; }
    }


    public sealed class Var : AElement, Stm
    {
        [DebuggerStepThrough]
        public Var(Ident ident, Exp exp)
        {
            C.Nn(ident);
            C.Nn(exp);

            Ident = ident;
            Exp = exp;

            ident.Parent = this;
            exp.Parent = this;
        }
        
        public Ident Ident { get; }
        public Exp Exp { get; }
    }


    public sealed class Assign : AElement, Stm
    {
        [DebuggerStepThrough]
        public Assign(Exp to, Exp exp)
        {
            C.Nn(to);
            C.Nn(exp);

            To = to;
            Exp = exp;

            to.Parent = this;
            exp.Parent = this;
        }
        
        public Exp To { get; }
        public Exp Exp { get; }
    }


    public sealed class When : AElement, Exp
    {
        [DebuggerStepThrough]
        public When(Exp test, Element then, [CanBeNull] Element otherwise)
        {
            C.Nn(test);
            C.Nn(then);

            Test = test;
            Then = then;
            Otherwise = otherwise;

            test.Parent = this;
            then.Parent = this;
            if (otherwise != null)
                otherwise.Parent = this;
        }


        public Exp Test { get; }
        public Element Then { get; }
        [CanBeNull]
        public Element Otherwise { get; }
    }


    public sealed class Loop : AElement, Stm
    {
        [DebuggerStepThrough]
        public Loop(Sequence body)
        {
            C.Nn(body);
            Body = body;
            body.Parent = this;
        }
        
        public Sequence Body { get; }
    }


    public sealed class Return : AElement, Stm
    {
        [DebuggerStepThrough]
        public Return(Exp exp)
        {
            C.Nn(exp);
            Exp = exp;
            exp.Parent = this;
        }

        public Exp Exp { get; }
    }


    public sealed class Break : AElement, Stm
    {
        [DebuggerStepThrough]
        public Break()
        {
        }
    }


    public sealed class Fn : AElement, Value
    {
        [DebuggerStepThrough]
        public Fn(FnParameters parameters, Sequence sequence)
        {
            C.Nn(parameters);
            C.Nn(sequence);

            Parameters = parameters;
            Sequence = sequence;

            sequence.Parent = this;
        }

        [DebuggerStepThrough]
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


    public sealed class Int : AElement, Value
    {
        [DebuggerStepThrough]
        public Int(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }


    public sealed class Bool : AElement, Value
    {
        [DebuggerStepThrough]
        public Bool(bool value)
        {
            Value = value;
        }

        public bool Value { get; }
    }


    public sealed class Void : AElement, Value
    {
        [DebuggerStepThrough]
        private Void()
        {
        }
        
        public static Void Instance { get; } = new Void();
    }


    public sealed class FnApply : AElement, Exp
    {
        [DebuggerStepThrough]
        public FnApply(Exp fn, FnArguments arguments)
        {
            C.Nn(fn);
            C.Nn(arguments);

            Fn = fn;
            Arguments = arguments;

            fn.Parent = this;
        }

        public Exp Fn { get; }
        public FnArguments Arguments { get; set; }
    }


    public sealed class ArrConstructor : AElement, Exp
    {
        [DebuggerStepThrough]
        public ArrConstructor(FnArguments arguments)
        {
            C.Nn(arguments);
            Arguments = arguments;
        }

        public FnArguments Arguments { get; }
    }


    public sealed class Arr : AElement, Value
    {
        [DebuggerStepThrough]
        public Arr(Values values)
        {
            C.Nn(values);
            Values = values;
        }

        public Values Values { get; }
    }


    public sealed class New : AElement, Exp
    {
        public New(ClassBody body)
        {
            C.Nn(body);
            Body = body;
        }

        public ClassBody Body { get; }
    }


    public sealed class Obj : AElement, Value
    {
        [DebuggerStepThrough]
        public Obj(ClassBody body, Env env)
        {
            C.Nn(body);
            C.Nn(env);

            Body = body;
            Env = env;
        }
        
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public ClassBody Body { get; }
        public Env Env { get; }
    }


    public sealed class MemberAccess : AElement, Exp
    {
        [DebuggerStepThrough]
        public MemberAccess(Exp exp, Ident ident)
        {
            C.Nn(exp);
            C.Nn(ident);

            Exp = exp;
            Ident = ident;

            exp.Parent = this;
            ident.Parent = this;
        }
        
        public Exp Exp { get; }
        public Ident Ident { get; }
    }
}