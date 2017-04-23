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
        private IEnumerator<Token> te;
        private Token tok;


        private void nextDontSkipWNewLine() => tok = te.MoveNext() ? te.Current : new Token(TokenType.None, "\0");


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

        public ElementList ParseNew(IEnumerable<Token> tokens)
        {
            var parsers = new List<ParseElement>();
            parsers.Add(ParseIdent);

            te = tokens.GetEnumerator();
            var list = new List<Element>();
            next();
            var elements = new List<Element>();
            while (true)
            {
                if (tok.Type == TokenType.None)
                    break;
                Element e = null;
                foreach (var p in parsers)
                {
                    e = p();
                    if (e != null)
                        break;
                }
                if (e == null)
                    throw new Exception();
                elements.Add(e);
            }

            return new ElementList(elements.ToArray());
        }

        public ElementList ParseStatementList()
        {
            var list = new List<Element>();
            next();
            while (true)
            {
                if (tok.Type == TokenType.None)
                    break;
                var se = ParseFullWithPostOp();
                if (se == null)
                    throw new Exception();
                list.Add(se);
            }
            return new ElementList(list.ToArray());
        }


        public Element Parse(IEnumerable<Token> tokens)
        {
            te = tokens.GetEnumerator();
            var list = new List<Element>();
            next();
            while (true)
            {
                if (tok.Type == TokenType.None)
                    break;
                var se = ParseFullWithPostOp();
                if (se == null)
                    throw new Exception();
                list.Add(se);
            }
            return new ElementList(list.ToArray());
        }


        [CanBeNull]
        private List<Element> PaseList()
        {
            next();
            var list = new List<Element>();
            while (true)
            {
                var se = ParseFullWithPostOp();
                if (se == null)
                    return list;
                list.Add(se);
            }
        }
        

        [CanBeNull]
        private Element ParseFullWithPostOp()
        {
            var se = ParseFull();
            if (tok.Text == ",")
            {
                var list = PaseList();
                if (list == null || list.Count == 0)
                    throw new Exception();
                return new ElementList(list.ToArray());
            }
            if (tok.Text == "(")
            {
                if (se is ExpElement exp)
                {
                var list = PaseList();
                if (list == null)
                    throw new Exception();
                return new FnApply(exp, new ExpList(list.Cast<ExpElement>().ToArray()));
                }
                throw new Exception("cannot apply non exp");
            }
            return se;
        }

        [CanBeNull]
        private Element ParseFull()
        {
            if (tok.Type == TokenType.NewLine)
                next();

            if (tok.Text == "}" || tok.Text == ")")
            {
                next();
                return null;
            }

            if (tok.Type == TokenType.Ident)
            {
                return ParseIdent();
            }

            if (tok.Type == TokenType.Key)
            {
                if (tok.Text == "var")
                {
                    return ParseVar();
                }
                if (tok.Text == "fn")
                {
                    return ParseFn();
                }
                if (tok.Text == "return")
                {
                    return ParseReturn();
                }
                throw new Exception();
            }

            if (tok.Type == TokenType.Int)
            {
                return ParseInt();
            }

            if (tok.Type == TokenType.Markup)
            {
                if (tok.Text == "{")
                {
                    var list = PaseList();
                    if (list == null)
                        return null;
                    return new ElementList(list.ToArray());
                }
                
                if (tok.Text == "(")
                {
                    var list = PaseList();
                    if (list == null)
                        return null;
                    if (list.Count == 0)
                        return new ElementList();
                    if (list.Count == 1)
                        return list[0];
                    throw new Exception();
                }
            }

            if (tok.Type == TokenType.None)
            {
                return null;
            }

            throw new Exception();
        }

        private Element ParseIdent()
        {
            var i = new Ident(tok.Text);
            next();
            return i;
        }


        [CanBeNull]
        private Element ParseInt()
        {
            if (tok.Type != TokenType.Ident)
                return null;
            var i = new Int(int.Parse(tok.Text.Replace("_", "")));
            next();
            return i;
        }

        private Element ParseReturn()
        {
            nextDontSkipWNewLine();
            if (tok.Type == TokenType.NewLine)
            {
                next();
                return new Return(Void.Instance);
            }
            var se = ParseFullWithPostOp();
            if (se is null)
                return new Return(Void.Instance);
            if (se is ExpElement exp)
                return new Return(exp);
            throw new Exception();
        }

        private Element ParseFn()
        {
            next();
            if (!(tok.Type == TokenType.Ident || tok.Text == "{"))
                throw new Exception();
            var @params = new IdentList();
            var se = ParseFull();
            if (se is ElementList sel)
                return new Fn(@params, sel);
            throw new Exception();
        }


        private Element ParseVar()
        {
            next();
            var i = tok.Type == TokenType.Ident
                ? new Ident(tok.Text)
                : throw new Exception();
            CheckNextAndSkip("=");
            var se = ParseFullWithPostOp();
            if (se is ExpElement exp)
                return new Var(i, exp);
            throw new Exception();
        }
    }
}