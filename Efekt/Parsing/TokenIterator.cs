using System.Collections.Generic;

namespace Efekt
{
    public sealed class TokenIterator
    {
        private readonly RemarkList remarkList;
       
        private readonly IEnumerator<Token> te;


        public Token Current { get; private set; }
        public int LineIndex { get; private set; }
        public int ColumnIndex { get; private set; }
        public bool CrossedLine { get; private set; }
        public string FilePath { get; }
        public bool Finished => Current.Type == TokenType.Terminal;
        public bool HasWork => !Finished;


        public TokenIterator(string filePath, IEnumerable<Token> tokens, RemarkList remarkList)
        {
            C.Nn(tokens);

            this.remarkList = remarkList;
            FilePath = filePath;
            te = tokens.GetEnumerator();
            Current = new Token(TokenType.None, "");
        }

        
        private void MoveNext()
        {
            if (Current.Type == TokenType.NewLine)
            {
                ++LineIndex;
                CrossedLine = true;
                ColumnIndex = 0;
            }
            else
            {
                ColumnIndex += Current.Text.Length;
            }

            if (te.MoveNext())
            {
                Current = te.Current;
                if (Current.Type == TokenType.Invalid)
                {
                    throw remarkList.TokenIsInvalid(this);
                }
            }
            else
            {
                LineIndex = 0;
                ColumnIndex = 0;
                Current = Token.Terminal;
            }
        }

        

        public void Next()
        {
            CrossedLine = false;

            MoveNext();

            again:

            if (Current.Type == TokenType.LineCommentBegin)
            {
                while (HasWork && Current.Type != TokenType.NewLine)
                    MoveNext();
                MoveNext();
            }

            if (Current.Type == TokenType.CommentBegin)
            {
                while (HasWork && Current.Type != TokenType.CommentEnd)
                    MoveNext();
                MoveNext();
            }

            if (Current.Type == TokenType.NewLine || Current.Type == TokenType.White)
            {
                MoveNext();
                goto again;
            }

            if (Current.Type == TokenType.LineCommentBegin
                || Current.Type == TokenType.CommentBegin)
            {
                goto again;
            }
        }
    }
}