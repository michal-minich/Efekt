using System;

namespace Efekt
{
    public sealed class CodeTextWriter
    {
        private readonly TextWriter writer;

        public int IndentLevel { get; private set; }

        public CodeTextWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public CodeTextWriter WriteIdent(string value)
        {
            writer.Write(value);
            return this;
        }

        public CodeTextWriter WriteOp(string value)
        {
            writer.Write(value);
            return this;
        }

        public CodeTextWriter WriteKey(string value)
        {
            writer.Write(value);
            return this;
        }

        public CodeTextWriter WriteMarkup(string value)
        {
            writer.Write(value);
            return this;
        }

        public CodeTextWriter WriteSpace()
        {
            writer.Write(" ");
            return this;
        }

        public CodeTextWriter Indent()
        {
            ++IndentLevel;
            writer.Write(Utils.RepeatString("    ", IndentLevel));
            return this;
        }

        public CodeTextWriter Unindent()
        {
            --IndentLevel;
            writer.Write(Utils.RepeatString("    ", IndentLevel));
            return this;
        }

        public CodeTextWriter WriteLine()
        {
            writer.Write(Environment.NewLine);
            return this;
        }

        public CodeTextWriter WriteNum(string value)
        {
            writer.Write(value);
            return this;
        }
    }
}