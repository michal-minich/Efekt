namespace Efekt
{
    public sealed class Remark
    {
        private readonly TextWriter writer;
        public Warn Warn { get; }
        public Error Error { get; }

        public Remark(TextWriter writer)
        {
            this.writer = writer;
            Warn = new Warn(writer);
            Error = new Error();
        }
    }
}
