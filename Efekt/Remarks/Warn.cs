namespace Efekt
{
    public sealed class Warn
    {
        private readonly TextWriter writer;

        public Warn(TextWriter writer)
        {
            this.writer = writer;
        }

        private void w(string message, Element e)
        {
            var filePath = Utils.GetFilePathRelativeToBase(e.FilePath);
            writer.WriteLine(filePath + ":" + (e.LineIndex + 1) + " Warning: " + message);
        }

        internal void ValueReturnedFromFunctionNotUsed(FnApply fna)
        {
            w("Value returned from function is not used", fna);
        }
        
        public void ValueIsNotAssigned(Element unusedValue)
        {
            w("Value of expression is not used", unusedValue);
        }

        public void AssigningDifferentType(Ident ident, Value old, Value @new)
        {
            w("Variable '" + ident.Name + "' of type '"
              + old.GetType().Name + "' is being assigned value of type '"
              + @new.GetType().Name + "'", ident);
        }
    }
}
