using System.Text;

namespace Efekt
{
    public sealed class StringWriter : TextWriter
    {
        private readonly StringBuilder sb = new StringBuilder();

        public void Write(string value)
        {
            sb.Append(value);
        }

        public void WriteLine(string value)
        {
            sb.AppendLine(value);
        }

        public string GetAndReset()
        {
            C.ReturnsNn();

            var s = sb.ToString();
            sb.Clear();
            return s;
        }
    }
}