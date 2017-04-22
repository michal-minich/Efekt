using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
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


        public SyntaxElement Parse(IEnumerable<Token> tokens)
        {
            te = tokens.GetEnumerator();
            var list = new List<SyntaxElement>();
            next();
            while (true)
            {
                if (tok.Type == TokenType.None)
                    break;
                var se = ParseFull();
                if (se == null)
                    throw new Exception();
                list.Add(se);
            }
            return new ElementList<SyntaxElement>(list);
        }


        [CanBeNull]
        private List<SyntaxElement> PaseList()
        {
            next();
            var list = new List<SyntaxElement>();
            while (true)
            {
                var se = ParseFull();
                if (se == null)
                    return list;
                list.Add(se);
            }
        }
        

        [CanBeNull]
        private SyntaxElement ParseFull()
        {
            var se = ParseOne();
            if (tok.Text == ",")
            {
                var list = PaseList();
                if (list == null || list.Count == 0)
                    throw new Exception();
                return new ElementList<SyntaxElement>(list);
            }
            if (tok.Text == "(")
            {
                if (se is ExpElement exp)
                {
                var list = PaseList();
                if (list == null)
                    throw new Exception();
                return new FnApply(exp, new ElementList<ExpElement>(list.Cast<ExpElement>().ToList()));
                }
                throw new Exception("cannot apply non exp");
            }
            return se;
        }

        [CanBeNull]
        private SyntaxElement ParseOne()
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
                    return new ElementList<SyntaxElement>(list);
                }
                
                if (tok.Text == "(")
                {
                    var list = PaseList();
                    if (list == null)
                        return null;
                    if (list.Count == 0)
                        return new ElementList<SyntaxElement>(new List<SyntaxElement>());
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

        private SyntaxElement ParseIdent()
        {
            var i = new Ident(tok.Text);
            next();
            return i;
        }


        private SyntaxElement ParseInt()
        {
            var i = new Int(int.Parse(tok.Text.Replace("_", "")));
            next();
            return i;
        }

        private SyntaxElement ParseReturn()
        {
            nextDontSkipWNewLine();
            if (tok.Type == TokenType.NewLine)
            {
                next();
                return new Return(Void.Instance);
            }
            var se = ParseFull();
            if (se is null)
                return new Return(Void.Instance);
            if (se is ExpElement exp)
                return new Return(exp);
            throw new Exception();
        }

        private SyntaxElement ParseFn()
        {
            next();
            if (!(tok.Type == TokenType.Ident || tok.Text == "{"))
                throw new Exception();
            IElementList<Ident> @params = new ElementList<Ident>(new List<Ident>());
            var se = ParseOne();
            if (se is IElementList<SyntaxElement> sel)
                return new Fn(@params, sel);
            throw new Exception();
        }


        private SyntaxElement ParseVar()
        {
            next();
            var i = tok.Type == TokenType.Ident
                ? new Ident(tok.Text)
                : throw new Exception();
            CheckNextAndSkip("=");
            var se = ParseFull();
            if (se is ExpElement exp)
                return new Var(i, exp);
            throw new Exception();
        }
    }
}