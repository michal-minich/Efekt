using System.Collections.Generic;
using System.Diagnostics;

namespace Efekt
{
    internal sealed class TokenIterator
    {
        private readonly IEnumerator<Token> te;
        internal Token Current { get; private set; }
        internal int LineIndex { get; private set; }


        internal bool Finished => Current.Type == TokenType.Terminal;
        internal bool HasWork => !Finished;


        internal TokenIterator(IEnumerable<Token> tokens) => te = tokens.GetEnumerator();


        private void MoveNext()
        {
            if (te.MoveNext())
            {
                Current = te.Current;
                if (Current.Type == TokenType.NewLine)
                    ++LineIndex;
            }
            else
            {
                Current = Token.Terminal;
            }
        }


        [DebuggerStepThrough]
        internal void Next()
        {
            again:
            MoveNext();
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
            if (Current.Type == TokenType.NewLine
                || Current.Type == TokenType.LineCommentBegin
                || Current.Type == TokenType.CommentBegin)
                goto again;
        }
    }
}