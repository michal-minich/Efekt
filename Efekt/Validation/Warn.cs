namespace Efekt
{
    public sealed class Warn
    {
        private readonly Prog prog;

        public Warn(Prog prog)
        {
            this.prog = prog;
        }

        private void w(string message, Element subject)
        {
            var filePath = Utils.GetFilePathRelativeToBase(subject.FilePath);
            prog.RemarkList.Add(new Remark(RemarkSeverity.Warning, message, filePath, subject.LineIndex, subject));
        }

        // TODO move to structure validation eventually
        public void ValueReturnedFromFunctionNotUsed(FnApply fna)
        {
            w("Value returned from function is not used", fna);
        }

        // TODO move to structure validation eventually
        public void ValueIsNotAssigned(Element unusedValue)
        {
            w("Value of expression is not used", unusedValue);
        }

        // TODO move to type validation eventually
        public void AssigningDifferentType(Ident ident, Value old, Value @new)
        {
            w("Variable '" + ident.Name + "' of type '"
              + old.GetType().Name + "' is being assigned value of type '"
              + @new.GetType().Name + "'", ident);
        }

        public void ReasigingLet(Ident ident)
        {
            w("Reasinging value in let variable '" + ident.Name + "'", ident);
        }
    }
}