using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class TokenIterator
    {
        [CanBeNull]
        public static TokenIterator Instance { get; private set; }

        private readonly IEnumerator<Token> te;

        public Token Current { get; private set; }
        public int LineIndex { get; private set; }
        public string FilePath { get; }
        public bool Finished => Current.Type == TokenType.Terminal;
        public bool HasWork => !Finished;


        public TokenIterator(string filePath, IEnumerable<Token> tokens)
        {
            FilePath = filePath;
            te = tokens.GetEnumerator();
            Instance = this;
        }


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
                LineIndex = -1;
                Current = Token.Terminal;
                Instance = null;
            }
        }


        [DebuggerStepThrough]
        public void Next()
        {
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
            if (Current.Type == TokenType.NewLine)
            {
                MoveNext();
                goto again;
            }
            if (Current.Type == TokenType.LineCommentBegin
                || Current.Type == TokenType.CommentBegin)
                goto again;
        }
    }
}