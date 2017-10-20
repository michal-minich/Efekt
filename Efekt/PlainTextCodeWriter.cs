using System;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class PlainTextCodeWriter
    {
        [NotNull] private readonly TextWriter writer;

        public PlainTextCodeWriter([NotNull] TextWriter writer)
        {
            this.writer = writer;
        }

        public int IndentLevel { get; private set; }

        [NotNull]
        public PlainTextCodeWriter Indent()
        {
            ++IndentLevel;
            writer.Write(Utils.RepeatString("    ", IndentLevel));
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Unindent()
        {
            --IndentLevel;
            writer.Write(Utils.RepeatString("    ", IndentLevel));
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Ident(string value)
        {
            writer.Write(value);
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Op(string value)
        {
            writer.Write(value);
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Key(string value)
        {
            writer.Write(value);
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Markup(string value)
        {
            writer.Write(value);
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Space()
        {
            writer.Write(" ");
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Line()
        {
            writer.Write(Environment.NewLine);
            return this;
        }

        [NotNull]
        public PlainTextCodeWriter Num(string value)
        {
            writer.Write(value);
            return this;
        }
    }
}