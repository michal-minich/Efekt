using System.Text;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class StringWriter : TextWriter
    {
        [NotNull]
        private readonly StringBuilder sb = new StringBuilder();

        public void Write(string value)
        {
            sb.Append(value);
        }

        public string GetAndReset()
        {
            var s = sb.ToString();
            sb.Clear();
            return s;
        }
    }
}