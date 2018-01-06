using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public interface Element
    {
        int LineIndex { get; set; }
        int ColumnIndex { get; set; }
        int LineIndexEnd { get; set; }
        int ColumnIndexEnd { get; set; }
        string FilePath { get; set; }
        Element Parent { get; set; }
        bool IsBraced { get; set; }
        Spec Spec { get; set; }
    }

    public interface Declr : Stm
    {
        Ident Ident { get; }
        [CanBeNull] Exp Exp { get; }
        List<Ident> UsedBy { get; }
    }


    public abstract class AElement : Element
    {
        protected AElement()
        {
            LineIndex = -1;
            FilePath = "runtime.ef";
        }

        public int LineIndex { get; set; }
        public int ColumnIndex { get; set; }
        public int LineIndexEnd { get; set; }
        public int ColumnIndexEnd { get; set; }
        public string FilePath { get; set; }
        public Element Parent { get; set; }
        public bool IsBraced { get; set; }
        public Spec Spec { get; set; }

        public override string ToString()
        {
            return GetType().Name + ": " + this.ToDebugString();
        }
    }


    public interface Exp : SequenceItem
    {
    }


    public interface Stm : Element
    {
    }


    public interface Value : Exp
    {
    }


    public interface AssignTarget : Exp
    {
    }


    public interface QualifiedIdent : AssignTarget
    {
    }


    public interface ClassItem : Stm
    {
    }


    public interface LoopOnlyItem : Stm
    {
    }


    public interface SequenceItem : Element
    {
    }


    public interface IElementList<out T> : IReadOnlyList<T> where T : Element
    {
    }


    public abstract class ElementList<T> : IElementList<T> where T : Element
    {
        protected readonly List<T> items;

        [DebuggerStepThrough]
        protected ElementList(List<T> items)
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


    public sealed class Sequence : ElementList<SequenceItem>, SequenceItem
    {
        [DebuggerStepThrough]
        public Sequence(List<SequenceItem> items) : base(items)
        {
            LineIndex = -1;
            FilePath = "runtime.ef";
            foreach (var i in items)
                i.Parent = this;
        }

        public int LineIndex { get; set; }
        public int ColumnIndex { get; set; }
        public int LineIndexEnd { get; set; }
        public int ColumnIndexEnd { get; set; }
        public string FilePath { get; set; }
        public Element Parent { get; set; }
        public bool IsBraced { get; set; }
        public Spec Spec { get; set; }

        public void InsertImport(Import i)
        {
            items.Insert(0, i);
        }
    }


    public sealed class ClassBody : ElementList<ClassItem>
    {
        [DebuggerStepThrough]
        public ClassBody(List<ClassItem> items) : base(items)
        {
        }
    }


    public sealed class FnArguments : ElementList<Exp>
    {
        [DebuggerStepThrough]
        public FnArguments() : base(new List<Exp>())
        {
        }

        [DebuggerStepThrough]
        public FnArguments(List<Exp> items) : base(items)
        {
        }
    }


    public sealed class FnParameters : ElementList<Param>
    {
        [DebuggerStepThrough]
        public FnParameters() : base(new List<Param>())
        {
        }

        [DebuggerStepThrough]
        public FnParameters(List<Param> items) : base(items)
        {
        }
    }


    public sealed class Values : ElementList<Value>
    {
        [DebuggerStepThrough]
        public Values(List<Value> items) : base(items)
        {
        }
        [NotNull]
        public new Value this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }
    }


    public sealed class Builtin : AElement, Value
    {
        [DebuggerStepThrough]
        public Builtin(string name, List<Spec> signature, Func<FnArguments, FnApply, Value> fn)
        {
            C.Req(!string.IsNullOrWhiteSpace(name));
            C.Req(name.Trim().Length == name.Length);
            C.AllNotNull(signature);
            C.Nn(fn);

            Name = name;
            FnSpec = new FnSpec(signature);
            Fn = fn;
        }

        public string Name { get; }
        public Func<FnArguments, FnApply, Value> Fn { get; }
        public FnSpec FnSpec { get; }
    }

    public class Invalid : AElement
    {
        public Invalid(string text)
        {
            C.Nn(text);
            Text = text;
        }
        public string Text { get; }
    }


    public sealed class Ident : AElement, QualifiedIdent
    {
        [DebuggerStepThrough]
        public Ident(string name, TokenType tokenType)
        {
            C.Req(!string.IsNullOrWhiteSpace(name));
            C.Req(name.Trim().Length == name.Length);

            Name = name;
            TokenType = tokenType;
        }

        public string Name { get; }
        
        public TokenType TokenType { get; }
        public Declr DeclareBy { get; set; }
    }


    public sealed class Var : AElement, Declr, SequenceItem, ClassItem
    {
        [DebuggerStepThrough]
        public Var(Ident ident, Exp exp)
        {
            C.Nn(ident, exp);

            Ident = ident;
            Exp = exp;
            UsedBy = new List<Ident>();

            ident.Parent = this;
            exp.Parent = this;
        }

        public Ident Ident { get; }
        public Exp Exp { get; }
        public List<Ident> UsedBy { get; }
    }


    public sealed class Let : AElement, Declr, SequenceItem, ClassItem
    {
        [DebuggerStepThrough]
        public Let(Ident ident, Exp exp)
        {
            C.Nn(ident, exp);

            Ident = ident;
            Exp = exp;
            UsedBy = new List<Ident>();

            ident.Parent = this;
            exp.Parent = this;
        }

        public Ident Ident { get; }
        public Exp Exp { get; }
        public List<Ident> UsedBy { get; }
    }
    

    public sealed class Param : AElement, Declr
    {
        [DebuggerStepThrough]
        public Param(Ident ident)
        {
            C.Nn(ident);
            Ident = ident;
            ident.Parent = this;
            UsedBy = new List<Ident>();
        }

        public Ident Ident { get; }
        public Exp Exp => null;
        public List<Ident> UsedBy { get; }
    }


    public sealed class Assign : AElement, SequenceItem
    {
        [DebuggerStepThrough]
        public Assign(AssignTarget to, Exp exp)
        {
            C.Nn(to, exp);

            To = to;
            Exp = exp;

            to.Parent = this;
            exp.Parent = this;
        }

        public AssignTarget To { get; }
        public Exp Exp { get; }
    }


    public sealed class When : AElement, Exp
    {
        [DebuggerStepThrough]
        public When(Exp test, Element then, [CanBeNull] Element otherwise)
        {
            C.Nn(test, then);

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


    public sealed class Loop : AElement, SequenceItem, Stm
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


    public sealed class Return : AElement, SequenceItem, Stm
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


    public sealed class Break : AElement, SequenceItem, LoopOnlyItem
    {
        [DebuggerStepThrough]
        public Break()
        {
        }
    }


    public sealed class Continue : AElement, SequenceItem, LoopOnlyItem
    {
        [DebuggerStepThrough]
        public Continue()
        {
        }
    }


    public sealed class Fn : AElement, Value
    {
        [DebuggerStepThrough]
        public Fn(FnParameters parameters, Sequence sequence)
        {
            C.Nn(parameters, sequence);

            Parameters = parameters;
            Sequence = sequence;

            sequence.Parent = this;
        }

        [DebuggerStepThrough]
        public Fn(FnParameters parameters, Sequence sequence, Env<Value> env)
            : this(parameters, sequence)
        {
            C.Nn(env);
            Env = env;
        }

        public FnParameters Parameters { get; }
        public Sequence Sequence { get; }
        public Env<Value> Env { get; }
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


    public sealed class Char : AElement, Value
    {
        [DebuggerStepThrough]
        public Char(char value)
        {
            Value = value;
        }

        public char Value { get; }
    }


    public sealed class Text : Arr
    {
        [DebuggerStepThrough]
        public Text(string value)
            : base(new Values(value.Select(v => new Char(v) as Value).ToList()))
        {
            Value = value;
        }

        public string Value { get; }
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
            C.Nn(fn, arguments);

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


    public class Arr : AElement, Value
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
        public string FullStaticName { get; }
    }


    public sealed class Obj : AElement, Value
    {
        [DebuggerStepThrough]
        public Obj(ClassBody body, Env<Value> env)
        {
            C.Nn(body, env);

            Body = body;
            Env = env;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public ClassBody Body { get; }

        public Env<Value> Env { get; }
    }


    public sealed class MemberAccess : AElement, QualifiedIdent
    {
        [DebuggerStepThrough]
        public MemberAccess(Exp exp, Ident ident)
        {
            C.Nn(exp, ident);

            Exp = exp;
            Ident = ident;

            exp.Parent = this;
            ident.Parent = this;
        }

        public Exp Exp { get; }
        public Ident Ident { get; }
    }


    public sealed class Toss : AElement, SequenceItem, Stm
    {

        [DebuggerStepThrough]
        public Toss(Exp exception)
        {
            C.Nn(exception);
            Exception = exception;
            exception.Parent = this;
        }

        public Exp Exception { get; }
    }


    public sealed class Attempt : AElement, SequenceItem, Stm
    {

        [DebuggerStepThrough]
        public Attempt(Sequence body, [CanBeNull] Sequence grab, [CanBeNull] Sequence atLast)
        {
            C.Nn(body);

            Body = body;
            Grab = grab;
            AtLast = atLast;

            body.Parent = this;
            if (grab != null)
                grab.Parent = this;
            if (atLast != null)
                atLast.Parent = this;
        }


        public Sequence Body { get; }
        [CanBeNull] public Sequence Grab { get; }
        [CanBeNull] public Sequence AtLast { get; }
    }


    public sealed class Import : AElement, SequenceItem, ClassItem
    {
        [DebuggerStepThrough]
        public Import(QualifiedIdent qualifiedIdent)
        {
            C.Nn(qualifiedIdent);
            QualifiedIdent = qualifiedIdent;
        }

        public readonly QualifiedIdent QualifiedIdent;
    }


    public interface Spec : Element
    {
    }

    public abstract class ASpec : AElement, Spec
    {
        public override bool Equals(object obj)
        {
            return GetType().Name.Equals(obj);
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }
    }

    public sealed class AnySpec : ASpec
    {
        public static AnySpec Instance { get; } = new AnySpec();
    }


    public sealed class AnyOfSpec : ASpec
    {
        public List<Spec> Possible { get; }

        public AnyOfSpec(List<Spec> possible)
        {
            C.AllNotNull(possible);
            Possible = possible;
        }
    }


    public sealed class ArrSpec : ASpec
    {
    }


    public sealed class BoolSpec : ASpec
    {
        public static BoolSpec Instance { get; } = new BoolSpec();
    }


    public sealed class CharSpec : ASpec
    {
        public static CharSpec Instance { get; } = new CharSpec();
    }


    public sealed class TextSpec : ASpec
    {
        public static TextSpec Instance { get; } = new TextSpec();
    }


    public sealed class FnSpec : ASpec
    {
        public FnSpec(List<Spec> signature)
        {
            C.AllNotNull(signature);
            Signature = signature;
        }

        public List<Spec> Signature { get; }

        public List<Spec> ArgSpec
        {
            get { return Signature.SkipLast().ToList(); }
        }

        public Spec ReturnSpec
        {
            get { return Signature.Last(); }
        }
    }


    public sealed class IntSpec : ASpec
    {
        public static IntSpec Instance { get; } = new IntSpec();
    }


    public sealed class ObjSpec : ASpec
    {
        public ObjSpec()
        {
            Members = new List<ObjSpecMember>();
        }

        public ObjSpec(List<ObjSpecMember> members)
        {
            C.AllNotNull(members);
            Members = members;
        }

        public List<ObjSpecMember> Members { get; set; }
    }


    public sealed class ObjSpecMember
    {
        public ObjSpecMember(string name, Spec spec, bool isLet)
        {
            Name = name;
            Spec = spec;
            IsLet = isLet;
        }

        public string Name { get; set; }
        public Spec Spec { get; set; }
        public bool IsLet { get; set; }
    }


    public sealed class VoidSpec : ASpec
    {
        public static VoidSpec Instance { get; } = new VoidSpec();
    }
}