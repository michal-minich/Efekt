﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Efekt
{
    public sealed class Tokenizer
    {
        private string[] keywords = {"var", "fn", "if", "else", "return", "loop", "break", "continue", "label", "goto" };

        public IEnumerable<Token> Tokenize(string code)
        {
            var ix = -1;
            char ch;
            var tokType = TokenType.Int;

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

                if (ch == '\r')
                {
                    next();
                    if (ch == '\n')
                        next();
                    tokType = TokenType.NewLine;
                    goto final;
                }

                if (ch == '\n')
                {
                    next();
                    tokType = TokenType.NewLine;
                    goto final;
                }

                if (ch == ' ' || ch == '\t')
                {
                    next();
                    continue;
                }

                if (ch >= '0' && ch <= '9')
                {
                    tokType = TokenType.Int;
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

                if (ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == ',' || ch == ';' || ch == '[' ||
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
                    var text = code.Substring(startIx, ix - startIx);
                    if (tokType == TokenType.Ident && keywords.Contains(text))
                        tokType = TokenType.Key;
                    var t = new Token(tokType, text);
                    yield return t;
                }

                if (ch == '\0')
                    break;

                if (startIx == ix)
                    throw new NotSupportedException("Character not supported '" + ch + "'.");
            }
        }
    }

    public struct Token
    {
        public Token(TokenType type, string text)
        {
            Type = type;
            Text = text;
        }

        public string Text { get; }
        public TokenType Type { get; }
    }

    public enum TokenType
    {
        None,
        Ident,
        TypeIdent,
        Int,
        Markup,
        Op,
        Key,
        NewLine
    }
}