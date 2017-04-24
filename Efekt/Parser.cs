using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    internal interface IElementVisitor<out T>
    {
        T VisitElementList(ElementList el);
    }

    [CanBeNull]
    internal delegate Element ParseElement();


    internal sealed class ParserState
    {
        private IEnumerator<Token> te;
        private Token tok;
    }

    internal sealed class Parser
    {
        private readonly List<ParseElement> parsers;
        private IEnumerator<Token> te;
        private Token tok;

        public Parser()
        {
            parsers = new List<ParseElement>
            {
                ParseIdent, ParseInt, ParseVar, ParseFn, ParseReturn, ParseCurly
            };
        }


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


        private bool finished { get { return tok.Type == TokenType.None && tok.Text == "\0"; } }


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
            var elements = ParseUntilEnd();
            return new ElementList(elements.ToArray());
        }


        [NotNull]
        private List<Element> ParseUntilEnd(bool stopOnBrace = false )
        {
            var elements = new List<Element>();
            while (true)
            {
                if (finished)
                    break;
                if (stopOnBrace && "])}".ToList().Any(x => x == tok.Text[0]))
                {
                    next();
                    break;
                }
                var e = ParseOne();
                if (e == null && !finished)
                    throw new Exception();
                elements.Add(e);
            }
            return elements;
        }


        [CanBeNull]
        private Element ParseOne()
        {
            foreach (var p in parsers)
            {
                var e = p();
                if (e != null)
                {
                    if (tok.Text == "(")
                    {
                        next();
                        var args = ParseUntilEnd(true);
                        var exp = e as ExpElement;
                        if (exp == null)
                            throw new Exception();
                        var argsExpList = args.Select(a => a as ExpElement).ToArray();
                        if (argsExpList.Any(a => a == null))
                            throw new Exception();
                        return new FnApply(exp, new ExpList(argsExpList));
                    }
                    return e;
                }
            }
            if (!finished)
                throw new Exception();
            return null;
        }

        private ElementList ParseCurly()
        {
            if (tok.Text != "{")
                return null;

            next();

            var elements = ParseUntilEnd(true);

            return new ElementList(elements.ToArray());
        }

        private Ident ParseIdent()
        {
            if (tok.Type != TokenType.Ident)
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
            if (se is ExpElement exp)
                return new Return(exp);
            throw new Exception();
        }


        private Fn ParseFn()
        {
            if (tok.Text != "fn")
                return null;
            next();
            if (!(tok.Type == TokenType.Ident || tok.Text == "{"))
                throw new Exception();
            var @params = new IdentList();
            var se = ParseOne();
            if (se is ElementList sel)
                return new Fn(@params, sel);
            throw new Exception();
        }


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
            if (se is ExpElement exp)
                return new Var(i, exp);
            throw new Exception();
        }
    }
}