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
        private IEnumerable<Token> tokens;

        private void next()
        {
            if (te.MoveNext())
                tok = te.Current;
        }

        public SyntaxElement Parse(IEnumerable<Token> tokens)
        {
            this.tokens = tokens;
            te = this.tokens.GetEnumerator();

            var list = new List<SyntaxElement>();
            while (te.MoveNext())
            {
                tok = te.Current;
                var se = ParseFromCurrent();
                list.Add(se);
            }

            return new ElementList<SyntaxElement>(list);
        }


        [CanBeNull]
        private SyntaxElement ParseFromCurrent()
        {
            if (tok.Text == "}" /* || tok.Text == ")"*/)
            {
                next();
                return null;
            }

            if (tok.Type == TokenType.Key)
            {
                if (tok.Text == "var")
                {
                    next();
                    var i = tok.Type == TokenType.Ident
                        ? new Ident(tok.Text)
                        : throw new Exception();
                    CheckNextAndSkip("=");
                    var se = ParseFromCurrent();
                    if (se is ExpElement exp)
                        return new Var(i, exp);
                    throw new Exception();
                }
                if (tok.Text == "fn")
                {
                    next();
                    if (tok.Type != TokenType.Ident && tok.Text != "{")
                        throw new Exception();

                    ElementList<Ident> @params;
                    if (tok.Type == TokenType.Ident)
                    {
                        var pEs = ParseFromCurrent();
                        if (pEs is ElementList<Ident> ps)
                            @params = ps;
                        else
                            throw new Exception();
                    }
                    else
                        @params = new ElementList<Ident>(new List<Ident>());

                    var se = ParseFromCurrent();
                    if (se is ElementList<SyntaxElement> body)
                    return new Fn(@params, body);
                }
                if (tok.Text == "return")
                {
                    next();
                    var se = ParseFromCurrent();
                    if (se is null)
                        return new Return(Void.Instance);
                    if (se is ExpElement exp)
                        return new Return(exp);
                    throw new Exception();
                }
                throw new Exception();
            }

            if (tok.Type == TokenType.Num)
                return new Int(int.Parse(tok.Text.Replace("_", "")));

            if (tok.Type == TokenType.Markup)
                if (tok.Text == "{")
                {
                    var list = new List<SyntaxElement>();
                    while (true)
                    {
                        next();
                        var se = ParseFromCurrent();
                        if (se == null)
                            break;
                        list.Add(se);
                    }
                    if (list.All(e => e is Ident))
                        return new ElementList<Ident>(list.Cast<Ident>().ToList());
                    if (list.All(e => e is ExpElement))
                        return new ElementList<ExpElement>(list.Cast<ExpElement>().ToList());
                    return new ElementList<SyntaxElement>(list);
                }

            return Void.Instance;
            throw new Exception();
        }

        private void CheckNextAndSkip(string text)
        {
            next();
            if (tok.Text == text)
                next();
            else
                throw new Exception("Expected '" + text + "', found '" + tok.Text + "'.");
        }
    }
}