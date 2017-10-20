using System;
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
        [NotNull] private readonly List<ParseOpElement> opOparsers;
        [NotNull] private readonly List<ParseElement> parsers;
        private IEnumerator<Token> te;
        private Token tok;
        private int lineIndex;

        public Parser()
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
                ParseArr,
                ParseNew
            };

            opOparsers = new List<ParseOpElement>
            {
                ParseFnApply,
                ParseOpApply
            };
        }


        private bool finished => tok.Type == TokenType.Terminal;


        private bool hasWork => !finished;


        private void nextTok()
        {
            if (te.MoveNext())
            {
                tok = te.Current;
                if (tok.Type == TokenType.NewLine)
                    ++lineIndex;
            }
            else
            {
                tok = Token.Terminal;
            }
        }


        private void nextWithoutComment()
        {
            nextTok();
            if (finished)
                return;
            if (tok.Type == TokenType.LineCommentBegin)
            {
                while (hasWork && tok.Type != TokenType.NewLine)
                    nextTok();
                nextTok();
            }
            if (tok.Type == TokenType.CommentBegin)
            {
                while (hasWork && tok.Type != TokenType.CommentEnd)
                    nextTok();
                nextTok();
            }
        }


        private void next()
        {
            do
            {
                nextWithoutComment();
            } while (hasWork && tok.Type == TokenType.NewLine);
        }


        private void CheckNextAndSkip(string text)
        {
            next();
            if (tok.Text == text)
                next();
            else
                throw new Exception("Expected '" + text + "', found '" + tok.Text + "'.");
        }


        [NotNull]
        public Element Parse([NotNull] IEnumerable<Token> tokens)
        {
            te = tokens.GetEnumerator();
            next();
            var elements = ParseAll();
            var e = elements[0];
            C.Nn(e);
            return elements.Count == 1 ? e : new Sequence(elements.ToArray());
        }


        [NotNull]
        private List<Element> ParseAll()
        {
            var elements = new List<Element>();
            while (hasWork)
            {
                var e = ParseOne();
                if (e == null && hasWork)
                    throw new Exception();
                elements.Add(e);
            }
            return elements;
        }


        [CanBeNull]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private List<Element> ParseList(char? stopOnBrace = null, bool skipComa = false)
        {
            char end;
            if (tok.Text == "(")
                end = ')';
            else if (tok.Text == "{")
                end = '}';
            else if (tok.Text == "[")
                end = ']';
            else
                return null;
            if (stopOnBrace != end)
                throw new Exception();
            next();
            var elements = new List<Element>();
            while (hasWork)
            {
                if (tok.Text[0] == end)
                {
                    next();
                    break;
                }
                if (skipComa && tok.Text == ",")
                    next();
                var e = ParseOne();
                if (e == null && hasWork)
                    throw new Exception();
                elements.Add(e);
            }
            return elements;
        }


        [NotNull]
        private FnParameters ParseFnParameters()
        {
            var elements = new List<Ident>();
            while (hasWork)
            {
                if (tok.Text == ",")
                    next();
                if (finished || tok.Text == "{" || tok.Type != TokenType.Ident)
                    break;
                var e = ParseOne();
                if (e == null && hasWork)
                    throw new Exception();
                var i = e as Ident;
                if (i == null)
                    throw new Exception();
                elements.Add(i);
            }
            return new FnParameters(elements.ToArray());
        }


        [CanBeNull]
        private Element ParseOne(bool withOps = true)
        {
            if (tok.Text == "(")
            {
                var es = ParseList(')');
                C.Nn(es);
                if (es.Count != 1)
                    throw new Exception();
                var e = es.First();
                if (withOps)
                    e = ParseWithOp(e);
                return e;
            }
            return ParseOne2(withOps);
        }


        [CanBeNull]
        private Element ParseOne2(bool withOps = true)
        {
            foreach (var p in parsers)
            {
                var e = p();
                if (e == null)
                    continue;
                if (withOps)
                    e = ParseWithOp(e);
                return e;
            }
            if (hasWork)
                throw new Exception();
            return null;
        }


        private Element ParseWithOp(Element e)
        {
            if (finished || tok.Text != "(" && tok.Type != TokenType.Op)
                return e;
            var prev = e as Exp;
            if (prev == null)
            {
                if (tok.Text == "(")
                    return e;
                throw new Exception();
            }
            foreach (var opar in opOparsers)
            {
                // ReSharper disable once PossibleNullReferenceException
                e = opar(prev);
                if (e != null)
                    return ParseWithOp(e);
            }
            throw new Exception();
        }


        [CanBeNull]
        private FnApply ParseFnApply(Exp prev)
        {
            var args = ParseList(')', true);
            if (args == null)
                return null;
            var argsExpList = args.Select(a => a as Exp).ToArray();
            if (argsExpList.Any(a => a == null))
                throw new Exception();
            return new FnApply(prev, new FnArguments(argsExpList));
        }


        [CanBeNull]
        private Element ParseOpApply(Exp prev)
        {
            if (tok.Type != TokenType.Op)
                return null;
            var opText = tok.Text;
            next();
            var second = ParseOne(false);
            if (second == null)
                throw new Exception();
            var secondExp = second as Exp;
            if (secondExp == null)
                throw new Exception();
            if (opText == "=")
            {
                var i = prev as Ident;
                if (i == null)
                    throw new Exception();
                return new Assign(i, secondExp);
            }
            if (opText == ".")
            {
                if (secondExp is Ident i)
                    return new MemberAccess(prev, i);
                throw new Exception();
            }
            return new FnApply(new Ident(opText), new FnArguments(prev, secondExp));
        }


        [CanBeNull]
        private ElementList ParseCurly()
        {
            var elements = ParseList('}');
            if (elements == null)
                return null;
            return new ElementList(elements.ToArray());
        }


        [CanBeNull]
        private ArrConstructor ParseArr()
        {
            if (tok.Text != "[")
                return null;
            var elements = ParseList(']', true);
            if (elements == null)
                throw new Exception();
            return new ArrConstructor(new FnArguments(elements.Cast<Exp>().ToArray()));
        }


        [CanBeNull]
        private New ParseNew()
        {
            if (tok.Text != "new")
                return null;
            next();
            var e = ParseOne();
            if (e is ElementList el)
                return new New(new ClassBody(el.Cast<Var>().ToArray()));
            throw new Exception();
        }


        [CanBeNull]
        private Ident ParseIdent()
        {
            if (tok.Type != TokenType.Ident && tok.Type != TokenType.Op)
                return null;
            var i = new Ident(tok.Text);
            next();
            return i;
        }


        [CanBeNull]
        private Int ParseInt()
        {
            if (tok.Type != TokenType.Int)
                return null;
            var i = new Int(int.Parse(tok.Text.Replace("_", "")));
            next();
            return i;
        }


        [CanBeNull]
        private Bool ParseBool()
        {
            switch (tok.Text)
            {
                case "true":
                    next();
                    return Bool.True;
                case "false":
                    next();
                    return Bool.False;
                default:
                    return null;
            }
        }


        [CanBeNull]
        private Return ParseReturn()
        {
            if (tok.Text != "return")
                return null;
            var linexIndexOnReturn = lineIndex;
            next();
            if (linexIndexOnReturn != lineIndex)
                return new Return(Void.Instance);
            var se = ParseOne();
            if (se == null && finished)
                return new Return(Void.Instance);
            if (se is Exp exp)
                return new Return(exp);
            throw new Exception();
        }


        [CanBeNull]
        private Fn ParseFn()
        {
            if (tok.Text != "fn")
                return null;
            next();
            if (!(tok.Type == TokenType.Ident || tok.Text == "{"))
                throw new Exception();
            var @params = ParseFnParameters();
            var se = ParseOne(false);
            if (se is ElementList sel)
                return new Fn(@params, new Sequence(sel.ToArray()));
            if (se is Exp e)
                return new Fn(@params, new Sequence(e));
            throw new Exception();
        }


        [CanBeNull]
        private When ParseWhen()
        {
            if (tok.Text != "if")
                return null;
            next();
            var test = ParseOne();
            var testExp = test as Exp;
            if (testExp == null)
                throw new Exception();
            if (tok.Text != "then")
                throw new Exception();
            next();
            var then = ParseOne();
            if (then == null)
                throw new Exception();
            Element otherwise;
            if (tok.Text == "else")
            {
                next();
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
            if (tok.Text != "loop")
                return null;
            next();
            var body = ParseList('}');
            if (body == null)
                throw new Exception();
            return new Loop(new Sequence(body.ToArray()));
        }


        [CanBeNull]
        private Break ParseBreak()
        {
            if (tok.Text != "break")
                return null;
            next();
            return Break.Instance;
        }


        [CanBeNull]
        private Var ParseVar()
        {
            if (tok.Text != "var")
                return null;
            next();
            var i = tok.Type == TokenType.Ident
                ? new Ident(tok.Text)
                : throw new Exception();
            CheckNextAndSkip("=");
            var se = ParseOne();
            if (se is Exp exp)
                return new Var(i, exp);
            throw new Exception();
        }
    }
}