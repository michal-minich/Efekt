using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Tokenizer
    {
        private static readonly string[] keywords =
        {
            "var", "let",
            "import",
            "fn", "return",
            "if", "else",
            "loop", "break", "continue",
            "label", "goto",
            "true", "false",
            "new",
            "and", "or",
            "throw", "try", "catch", "finally"
        };


        private readonly List<char> opChars = "<>~`\\@#$%^&*+-=./:?!|".ToList();

        // ReSharper disable once NotNullMemberIsNotInitialized
        private string code;

        private char ch;
        private int ix;
        private TokenType tokType;


        private void next()
        {
            C.Req(ix >= -1);
            
            ++ix;

            if (ix >= code.Length)
            {
                ch = '\0';
                ix = code.Length;
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
            C.Nn(codeText);

            var tokens = new List<Token>();

            if (codeText.Length == 0)
                return tokens;

            code = codeText;
            ix = -1;
            tokType = TokenType.Terminal;

            next();

            while (true)
            {
                var startIx = ix;

                if (ch == '\0')
                    break;

                if (markIf(ch == ' ' || ch == '\t', TokenType.White))
                {
                    while (ch == ' ' || ch == '\t')
                        next();
                    goto final;
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
                    while (ch != '\0' && (ch >= '0' && ch <= '9' || ch == '_'))
                        next();
                    goto final;
                }

                if (markIf(ch == '\'', TokenType.Char))
                {
                    while (ch != '\0' && ch != '\'')
                    {
                        if (ch == '\\')
                            next();
                        next();
                    }
                    next();
                    goto final;
                }

                if (markIf(ch == '\"', TokenType.Text))
                {
                    while (ch != '\0' && ch != '\"')
                    {
                        if (ch == '\\')
                            next();
                        next();
                    }
                    next();
                    goto final;
                }

                if (markIf(ch >= 'a' && ch <= 'z', TokenType.Ident))
                {
                    while (ch != '\0' && (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9' || ch == '_'))
                        next();
                    goto final;
                }

                if (markIf(ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == ','
                           || ch == ';' || ch == '[' || ch == ']', TokenType.Markup))
                    goto final;

                if (ch == '/' && code.Length >= startIx + 2)
                {
                    var text = code.Substring(startIx, 2);
                    if (markIf(text == "//", TokenType.LineCommentBegin))
                    {
                        next();
                        goto final;
                    }

                    if (markIf(text == "/*", TokenType.CommentBegin))
                    {
                        next();
                        goto final;
                    }
                }

                if (ch == '*' && code.Length >= startIx + 2)
                {
                    var text = code.Substring(startIx, 2);
                    if (markIf(text == "*/", TokenType.CommentEnd))
                    {
                        next();
                        goto final;
                    }
                }

                if (markIf(opChars.Contains(ch), TokenType.Op))
                {
                    while (ch != '\0' && opChars.Contains(ch))
                        next();
                }

                final:


                if (ch != '\0' && ix - startIx == 0)
                    mark(TokenType.Invalid);

                C.Assume(ix - startIx > 0);

                var text2 = code.Substring(startIx, ix - startIx);
                if (tokType == TokenType.Ident && keywords.Contains(text2))
                    tokType = TokenType.Key;
                if (tokType == TokenType.Key && text2 == "and" || text2 == "or")
                    tokType = TokenType.Op;
                tokens.AddValue(new Token(tokType, text2));
            }

            C.Assume(!tokens.Contains(Token.Terminal));
            C.Assume(String.Join("", tokens.Select(t => t.Text)) == code);

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
        None,
        Terminal,
        Invalid,

        White,
        NewLine,
        LineCommentBegin,
        CommentBegin,
        CommentEnd,

        Ident,
        Int,
        Markup,
        Op,
        Key,
        Char,
        Text
    }
}