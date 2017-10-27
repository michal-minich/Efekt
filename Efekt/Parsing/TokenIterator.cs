using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Efekt
{
    internal sealed class TokenIterator
    {
        [CanBeNull] internal static TokenIterator Instance { get; private set; }

        private readonly IEnumerator<Token> te;

        internal Token Current { get; private set; }
        internal int LineIndex { get; private set; }
        internal string FilePath { get; }
        internal bool Finished => Current.Type == TokenType.Terminal;
        internal bool HasWork => !Finished;


        internal TokenIterator(string filePath, IEnumerable<Token> tokens)
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