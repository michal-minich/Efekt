using System;

namespace Efekt
{
    public sealed class PlainTextCodeWriter
    {
        private readonly TextWriter writer;

        public PlainTextCodeWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public int IndentLevel { get; private set; }

        public PlainTextCodeWriter Indent()
        {
            ++IndentLevel;
            writer.Write(Utils.RepeatString("    ", IndentLevel));
            return this;
        }

        public PlainTextCodeWriter Unindent()
        {
            --IndentLevel;
            writer.Write(Utils.RepeatString("    ", IndentLevel));
            return this;
        }

        public PlainTextCodeWriter Ident(string value)
        {
            writer.Write(value);
            return this;
        }

        public PlainTextCodeWriter Op(string value)
        {
            writer.Write(value);
            return this;
        }

        public PlainTextCodeWriter Key(string value)
        {
            writer.Write(value);
            return this;
        }

        public PlainTextCodeWriter Markup(string value)
        {
            writer.Write(value);
            return this;
        }

        public PlainTextCodeWriter Space()
        {
            writer.Write(" ");
            return this;
        }

        public PlainTextCodeWriter Line()
        {
            writer.Write(Environment.NewLine);
            return this;
        }

        public PlainTextCodeWriter Num(string value)
        {
            writer.Write(value);
            return this;
        }
    }
}