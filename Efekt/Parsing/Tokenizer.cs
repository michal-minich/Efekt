using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Tokenizer
    {
        private static readonly string[] keywords =
            {"var", "fn", "if", "else", "return", "loop", "break", "continue", "label", "goto", "true", "false", "new"};


        private readonly List<char> opChars = "<>~`\\@#$%^&*+-=./:?!|".ToList();

        // ReSharper disable once NotNullMemberIsNotInitialized
        [NotNull] private string code;
        private char ch;
        private int ix;
        private TokenType tokType;

        private void next()
        {
            ++ix;
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


        private bool markIf(bool condition, TokenType tokenType)
        {
            if (condition)
                mark(tokenType);
            return condition;
        }


        public IEnumerable<Token> Tokenize(string codeText)
        {
            var tokens = new List<Token>();
            code = codeText;
            ix = -1;
            tokType = TokenType.Terminal;

            next();

            while (true)
            {
                var startIx = ix;

                if (ch == '\0')
                    break;

                if (ch == ' ' || ch == '\t')
                {
                    next();
                    continue;
                }

                if (markIf(ch == '\r', TokenType.NewLine))
                {
                    if (ch == '\n')
                        next();
                    goto final;
                }

                if (markIf(ch == '\n', TokenType.NewLine))
                    goto final;

                if (markIf(ch >= '0' && ch <= '9', TokenType.Int))
                {
                    while (ch >= '0' && ch <= '9' || ch == '_')
                        next();
                    goto final;
                }

                if (markIf(ch == '\'', TokenType.Char))
                {
                    while (ch != '\'')
                        next();
                    next();
                    goto final;
                }

                if (markIf(ch == '\"', TokenType.Text))
                {
                    while (ch != '\"')
                        next();
                    next();
                    goto final;
                }

                if (markIf(ch >= 'a' && ch <= 'z', TokenType.Ident))
                {
                    while (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9' || ch == '_')
                        next();
                    goto final;
                }

                if (markIf(ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == ','
                           || ch == ';' || ch == '[' || ch == ']', TokenType.Markup))
                    goto final;

                if (ch == '-')
                {
                    var text = code.Substring(startIx, 3);
                    if (markIf(text == "---", TokenType.LineCommentBegin))
                    {
                        next();
                        next();
                        goto final;
                    }
                    if (markIf(text == "--*", TokenType.CommentBegin))
                    {
                        next();
                        next();
                        goto final;
                    }
                }

                if (ch == '*')
                {
                    var text = code.Substring(startIx, 3);
                    if (markIf(text == "*--", TokenType.CommentEnd))
                    {
                        next();
                        next();
                        goto final;
                    }
                }

                if (markIf(opChars.Contains(ch), TokenType.Op))
                {
                    while (opChars.Contains(ch))
                        next();
                }

                final:

                if (startIx == ix)
                    throw new NotSupportedException("Character not supported '" + ch + "'.");

                var text2 = code.Substring(startIx, ix - startIx);
                if (tokType == TokenType.Ident && keywords.Contains(text2))
                    tokType = TokenType.Key;
                tokens.Add(new Token(tokType, text2));
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

        [NotNull]
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
        CommentEnd,
        Char,
        Text
    }
}