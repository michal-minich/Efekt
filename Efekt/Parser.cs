using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    [CanBeNull]
    internal delegate Element ParseElement();

    [CanBeNull]
    internal delegate Exp ParseOpElement(Exp prev);

    internal sealed class Parser
    {
        private readonly List<ParseOpElement> opOparsers;
        private readonly List<ParseElement> parsers;
        private IEnumerator<Token> te;
        private Token tok;

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
                ParseCurly
            };

            opOparsers = new List<ParseOpElement>
            {
                ParseFnApply,
                ParseOpApply
            };
        }


        private bool finished => tok.Type == TokenType.None && tok.Text == "\0";


        private void nextDontSkipWNewLine()
        {
            tok = te.MoveNext() ? te.Current : new Token(TokenType.None, "\0");
        }


        private void next()
        {
            again:
            nextDontSkipWNewLine();
            if (tok.Type == TokenType.None)
                return;
            if (tok.Type == TokenType.NewLine)
                goto again;
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
        public Element Parse(IEnumerable<Token> tokens)
        {
            te = tokens.GetEnumerator();
            next();
            var elements = ParseAll();
            return elements.Count == 1 ? elements[0] : new ElementList(elements.ToArray());
        }


        [NotNull]
        private List<Element> ParseAll()
        {
            var elements = new List<Element>();
            while (!finished)
            {
                var e = ParseOne();
                if (e == null && !finished)
                    throw new Exception();
                elements.Add(e);
            }
            return elements;
        }


        [CanBeNull]
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
            while (!finished)
            {
                if (tok.Text[0] == end)
                {
                    next();
                    break;
                }
                if (skipComa && tok.Text == ",")
                    next();
                var e = ParseOne();
                if (e == null && !finished)
                    throw new Exception();
                elements.Add(e);
            }
            return elements;
        }


        [NotNull]
        private IdentList ParseComaIdentList()
        {
            var elements = new List<Ident>();
            while (!finished)
            {
                if (tok.Text == ",")
                    next();
                if (finished || tok.Text == "{" || tok.Type != TokenType.Ident)
                    break;
                var e = ParseOne();
                if (e == null && !finished)
                    throw new Exception();
                var i = e as Ident;
                if (i == null)
                    throw new Exception();
                elements.Add(i);
            }
            return new IdentList(elements.ToArray());
        }


        [CanBeNull]
        private Element ParseOne(bool withOps = true)
        {
            if (tok.Text == "(")
            {
                var es = ParseList(')');
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
            Element e;
            foreach (var p in parsers)
            {
                e = p();
                if (e == null)
                    continue;
                if (withOps)
                    e = ParseWithOp(e);
                return e;
            }
            if (!finished)
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
                e = opar(prev);
                if (e != null)
                    return ParseWithOp(e);
            }
            throw new Exception();
        }


        [CanBeNull]
        private FnApply ParseFnApply(Exp prev)
        {
            if (tok.Text != "(")
                return null;
            var args = ParseList(')', true);
            var argsExpList = args.Select(a => a as Exp).ToArray();
            if (argsExpList.Any(a => a == null))
                throw new Exception();
            return new FnApply(prev, new ExpList(argsExpList));
        }


        [CanBeNull]
        private Exp ParseOpApply(Exp prev)
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
            return new FnApply(new Ident(opText), new ExpList(prev, secondExp));
        }


        [CanBeNull]
        private ElementList ParseCurly()
        {
            if (tok.Text != "{")
                return null;
            var elements = ParseList('}');
            return new ElementList(elements.ToArray());
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
            nextDontSkipWNewLine();
            if (tok.Type == TokenType.NewLine)
            {
                next();
                return new Return(Void.Instance);
            }
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
            var @params = ParseComaIdentList();
            var se = ParseOne(false);
            if (se is ElementList sel)
                return new Fn(@params, sel);
            if (se is Exp e)
                return new Fn(@params, new ElementList(e));
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
            if (tok.Text != "{")
                throw new Exception();
            var body = ParseList('}');
            return new Loop(new ElementList(body.ToArray()));
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