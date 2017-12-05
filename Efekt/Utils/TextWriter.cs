using System.Diagnostics.Contracts;

namespace Efekt
{
    [ContractClass(typeof(TextWriterContracts))]
    public interface TextWriter
    {
        void Write(string value);
        void WriteLine(string value);
    }


    [ContractClassFor(typeof(TextWriter))]
    public abstract class TextWriterContracts : TextWriter
    {
        public void Write(string value)
        {
            C.Nn(value);
        }

        public void WriteLine(string value)
        {
            C.Nn(value);
        }
    }
}