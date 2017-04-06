using System;
using System.Collections.Generic;

namespace Efekt
{
    public sealed class Tokenizer
    {
        public IEnumerable<Token> Tokenize(string code)
        {
            var ix = -1;
            char ch;
            var tokType = TokenType.Num;

            void next()
            {
                ++ix;
                if (ix >= code.Length)
                {
                    ch = '\0';
                    return;
                }
                ch = code[ix];
            }

            next();

            while (true)
            {
                var startIx = ix;

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    next();
                    continue;
                }

                if (ch >= '0' && ch <= '9')
                {
                    tokType = TokenType.Num;
                    while (ch >= '0' && ch <= '9' || ch == '_')
                        next();
                    goto final;
                }

                if (ch >= 'a' && ch <= 'z')
                {
                    tokType = TokenType.Ident;
                    while (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9' || ch == '_')
                        next();
                    goto final;
                }

                if (ch >= 'a' && ch <= 'Z')
                {
                    tokType = TokenType.TypeIdent;
                    while (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9' || ch == '_')
                        next();
                    goto final;
                }

                if (ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == ',' || ch == '>' || ch == '[' ||
                    ch == ']')
                {
                    tokType = TokenType.Markup;
                    next();
                    goto final;
                }

                const string opChars = "<>~`\\@#$%^&*+-=./:?!|";
                if (opChars.Contains(ch.ToString()))
                {
                    tokType = TokenType.Op;
                    while (opChars.Contains(ch.ToString()))
                        next();
                }

                final:

                if (startIx != ix)
                {
                    var t = new Token(tokType, code.Substring(startIx, ix - startIx));
                    yield return t;
                }

                if (ch == '\0')
                    break;

                if (startIx == ix)
                    throw new NotSupportedException("Character not supported '" + ch + "'.");
            }
        }
    }

    public class Token
    {
        public Token(TokenType type, string text)
        {
            Type = type;
            Text = text;
        }

        public string Text { get; set; }
        public TokenType Type { get; set; }
    }

    public enum TokenType
    {
        Ident,
        TypeIdent,
        Num,
        Markup,
        Op
    }
}