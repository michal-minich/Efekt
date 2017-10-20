using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Tokenizer
    {
        [NotNull] private static readonly string[] keywords =
            {"var", "fn", "if", "else", "return", "loop", "break", "continue", "label", "goto", "true", "false", "new"};


        private string code;
        private char ch;
        private int ix;
        private TokenType tokType;

        private void next()
        {
            ++ix;
            // ReSharper disable once PossibleNullReferenceException
            if (ix >= code.Length)
            {
                ch = '\0';
                return;
            }
            ch = code[ix];
        }


        private void mark(TokenType tokenType)
        {
            tokType = tokenType;
            next();
        }


        [NotNull]
        public IEnumerable<Token> Tokenize([NotNull] string codeText)
        {
            var tokens = new List<Token>();
            code = codeText;
            ix = -1;
            tokType = TokenType.Terminal;

            next();

            while (true)
            {
                var startIx = ix;

                if (ch == ' ' || ch == '\t')
                {
                    next();
                    continue;
                }

                if (ch == '\r')
                {
                    mark(TokenType.NewLine);
                    if (ch == '\n')
                        next();
                    goto final;
                }

                if (ch == '\n')
                {
                    mark(TokenType.NewLine);
                    goto final;
                }

                if (ch >= '0' && ch <= '9')
                {
                    mark(TokenType.Int);
                    while (ch >= '0' && ch <= '9' || ch == '_')
                        next();
                    goto final;
                }

                if (ch >= 'a' && ch <= 'z')
                {
                    mark(TokenType.Ident);
                    while (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9' || ch == '_')
                        next();
                    goto final;
                }

                if (ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == ',' || ch == ';' || ch == '[' ||
                    ch == ']')
                {
                    mark(TokenType.Markup);
                    goto final;
                }

                const string opChars = "<>~`\\@#$%^&*+-=./:?!|";
                if (opChars.Contains(ch.ToString()))
                {
                    mark(TokenType.Op);
                    while (true)
                    {
                        if (ix - startIx == 3)
                        {
                            var text = code.Substring(startIx, 3);
                            if (text == "---" || text == "--*" || text == "*--")
                                break;
                        }

                        if (opChars.Contains(ch.ToString()))
                            next();
                        else
                            break;
                    }
                }

                final:

                if (startIx != ix)
                {
                    var text = code.Substring(startIx, ix - startIx);
                    if (tokType == TokenType.Ident && keywords.Contains(text))
                        tokType = TokenType.Key;
                    if (tokType == TokenType.Op)
                    {
                        if (text == "---")
                            tokType = TokenType.LineCommentBegin;
                        else if (text == "--*")
                            tokType = TokenType.CommentBegin;
                        else if (text == "*--")
                            tokType = TokenType.CommentEnd;
                    }

                    var t = new Token(tokType, text);
                    tokens.Add(t);
                }

                if (ch == '\0')
                    break;

                if (startIx == ix)
                    throw new NotSupportedException("Character not supported '" + ch + "'.");
            }

            return tokens;
        }
    }

    public struct Token
    {
        public static Token Terminal = new Token(TokenType.Terminal, "\0");

        public Token(TokenType type, string text)
        {
            Type = type;
            Text = text;
        }

        public string Text { get; }
        public TokenType Type { get; }

        // for debug only
        public override string ToString()
        {
            return Type + ": \"" + Text + "\"";
        }
    }

    public enum TokenType
    {
        Terminal,
        Ident,
        Int,
        Markup,
        Op,
        Key,
        NewLine,
        LineCommentBegin,
        CommentBegin,
        CommentEnd
    }
}