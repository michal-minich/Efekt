using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    [CanBeNull]
    internal delegate Element ParseElement();

    [CanBeNull]
    internal delegate Element ParseOpElement(Exp prev);


    internal sealed class Parser
    {
        private readonly List<ParseOpElement> opOparsers;
        private readonly List<ParseElement> parsers;
        private TokenIterator ti;


        internal Parser()
        {
            parsers = new List<ParseElement>
            {
                ParseIdent,
                ParseInt,
                ParseBool,
                ParseVar,
                ParseFn,
                ParseWhen,
                ParseLoop,
                ParseBreak,
                ParseReturn,
                ParseCurly,
                ParseSingleBraced,
                ParseArr,
                ParseNew
            };

            opOparsers = new List<ParseOpElement>
            {
                ParseFnApply,
                ParseOpApply
            };
        }

        private string text => ti.Current.Text;
        private TokenType type => ti.Current.Type;


        internal Element Parse(IEnumerable<Token> tokens)
        {
            ti = new TokenIterator(tokens);
            var elements = new ElementListBuilder();
            ti.Next();
            while (ti.HasWork)
            {
                var e = ParseOne();
                elements.Add(e);
            }
            var seq = elements.GetSequenceAndReset();
            return seq.Count == 1 ? seq[0] : seq;
        }


        [CanBeNull]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private ElementList ParseBracedList(char? stopOnBrace = null, bool skipComa = false)
        {
            char end;
            if (text == "(")
                end = ')';
            else if (text == "{")
                end = '}';
            else if (text == "[")
                end = ']';
            else
                return null;
            if (stopOnBrace != end)
                throw Error.Fail();
            ti.Next();
            return ParseList(skipComa, end);
        }


        private ElementList ParseList(bool skipComa, char end)
        {
            var elements = new ElementListBuilder();
            while (ti.HasWork)
            {
                var e = ParseOne();
                elements.Add(e);
                if (text[0] == end)
                {
                    ti.Next();
                    break;
                }
                if (skipComa)
                {
                    if (text == ",")
                        ti.Next();
                    else
                        break;
                }
            }
            return elements.GetAndReset();
        }


        [NotNull]
        private Element ParseOne(bool withOps = true)
        {
            foreach (var p in parsers)
            {
                C.Nn(p);
                var e = p();
                if (e == null)
                    continue;
                if (withOps)
                    e = ParseWithOp(e);
                return e;
            }
            throw Error.Fail();
        }


        private Element ParseWithOp(Element e)
        {
            if (ti.Finished || text != "(" && type != TokenType.Op)
                return e;
            var prev = e as Exp;
            if (prev == null)
            {
                if (text == "(")
                    return e;
                throw Error.Fail();
            }
            foreach (var opar in opOparsers)
            {
                C.Nn(opar);
                e = opar(prev);
                if (e != null)
                    return ParseWithOp(e);
            }
            throw Error.Fail();
        }


        [CanBeNull]
        private FnApply ParseFnApply(Exp prev)
        {
            var args = ParseBracedList(')', true);
            if (args == null)
                return null;
            var argsExpList = args.Select(a => a as Exp).ToArray();
            if (argsExpList.Any(a => a == null))
                throw Error.Fail();
            return new FnApply(prev, new FnArguments(argsExpList));
        }


        [CanBeNull]
        private Element ParseOpApply(Exp prev)
        {
            if (type != TokenType.Op)
                return null;
            var opText = text;
            ti.Next();
            var second = ParseOne(false);
            var secondExp = second as Exp;
            if (secondExp == null)
                throw Error.Fail();
            if (opText == "=")
            {
                var i = prev as Ident;
                if (i == null)
                    throw Error.Fail();
                return new Assign(i, secondExp);
            }
            if (opText == ".")
            {
                if (secondExp is Ident i)
                    return new MemberAccess(prev, i);
                throw Error.Fail();
            }
            return new FnApply(new Ident(opText, TokenType.Op), new FnArguments(new[] {prev, secondExp}));
        }


        [CanBeNull]
        private ElementList ParseCurly()
        {
            return ParseBracedList('}');
        }


        [CanBeNull]
        private Element ParseSingleBraced()
        {
            var es = ParseBracedList(')');
            if (es == null)
                return null;
            if (es.Count == 1)
                return es.First();
            throw Error.Fail();
        }


        [CanBeNull]
        private ArrConstructor ParseArr()
        {
            if (text != "[")
                return null;
            var elements = ParseBracedList(']', true);
            if (elements == null)
                throw Error.Fail();
            return new ArrConstructor(new FnArguments(elements.Cast<Exp>().ToArray()));
        }


        [CanBeNull]
        private New ParseNew()
        {
            if (text != "new")
                return null;
            ti.Next();
            var e = ParseOne();
            if (e is ElementList el)
                return new New(new ClassBody(el.Cast<Var>().ToArray()));
            throw Error.Fail();
        }


        [CanBeNull]
        private Ident ParseIdent()
        {
            if (type != TokenType.Ident && type != TokenType.Op)
                return null;
            var i = new Ident(text, type);
            ti.Next();
            return i;
        }


        [CanBeNull]
        private Int ParseInt()
        {
            if (type != TokenType.Int)
                return null;
            var i = new Int(int.Parse(text.Replace("_", "")));
            ti.Next();
            return i;
        }


        [CanBeNull]
        private Bool ParseBool()
        {
            switch (text)
            {
                case "true":
                    ti.Next();
                    return Bool.True;
                case "false":
                    ti.Next();
                    return Bool.False;
                default:
                    return null;
            }
        }


        [CanBeNull]
        private Return ParseReturn()
        {
            if (text != "return")
                return null;
            var lineIndexOnReturn = ti.LineIndex;
            ti.Next();
            if (ti.Finished || lineIndexOnReturn != ti.LineIndex)
                return new Return(Void.Instance);
            var se = ParseOne();
            if (se is Exp exp)
                return new Return(exp);
            throw Error.Fail();
        }


        [CanBeNull]
        private Fn ParseFn()
        {
            if (text != "fn")
                return null;
            ti.Next();
            var @params = text == "{" 
                ? new FnParameters() 
                : new FnParameters(ParseList(false, '{').Cast<Ident>().ToArray());
            var se = ParseBracedList('}');
            if (se is ElementList sel)
                return new Fn(@params, new Sequence(sel.ToArray()));
            throw Error.Fail();
        }


        [CanBeNull]
        private When ParseWhen()
        {
            if (text != "if")
                return null;
            ti.Next();
            var test = ParseOne();
            var testExp = test as Exp;
            if (testExp == null)
                throw Error.Fail();
            if (text != "then")
                throw Error.Fail();
            ti.Next();
            var then = ParseOne();
            Element otherwise;
            if (text == "else")
            {
                ti.Next();
                otherwise = ParseOne();
            }
            else
            {
                otherwise = null;
            }
            return new When(testExp, then, otherwise);
        }


        [CanBeNull]
        private Loop ParseLoop()
        {
            if (text != "loop")
                return null;
            ti.Next();
            var body = ParseBracedList('}');
            if (body == null)
                throw Error.Fail();
            return new Loop(new Sequence(body.ToArray()));
        }


        [CanBeNull]
        private Break ParseBreak()
        {
            if (text != "break")
                return null;
            ti.Next();
            return Break.Instance;
        }


        [CanBeNull]
        private Var ParseVar()
        {
            if (text != "var")
                return null;
            ti.Next();
            var i = type == TokenType.Ident
                ? new Ident(text, TokenType.Ident)
                : throw Error.Fail();
            ti.NextAndMatch("=");
            var se = ParseOne();
            if (se is Exp exp)
                return new Var(i, exp);
            throw Error.Fail();
        }
    }
}