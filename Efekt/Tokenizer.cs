using System.Collections.Generic;

namespace Efekt
{
    public class Tokenizer
    {
        public IReadOnlyList<Token> Tokenize(string code)
        {
            var ch = code[0];
            var ix = 0;
            while (true)
            {
                if (ch >= 'a' && ch <= 'z')
                {
                }
            }
        }
    }

    public class Token
    {
        public string Text { get; set; }
        public TokenType Type { get; set; }
    }

    public enum TokenType
    {
        Key,
        Ident,
        Num,
        Markup,
        Op
    }
}