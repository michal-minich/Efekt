using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Except
    {
        private readonly Prog prog;

        public Except(Prog prog)
        {
            this.prog = prog;
        }


        [Pure]
        public EfektException DifferentTypeExpected(Exp value, string expectedTypeName, Exp inExp)
        {
            return prog.RemarkList.AddException(new Remark(
                RemarkSerity.Exception,
                "Expected type '" + expectedTypeName + "' but the expression is of type '"
                + value.GetType().Name + "'",
                value.FilePath,
                value.LineIndex,
                value,
                inExp,
                prog.Interpreter.CallStack));
        }


        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsNotDeclared(Ident ident)
        {
            return prog.RemarkList.AddException(new Remark(
                RemarkSerity.Exception,
                "Variable '" + ident.Name + "' is not declared",
                ident.FilePath,
                ident.LineIndex,
                ident,
                null,
                prog.Interpreter.CallStack));
        }


        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsAlreadyDeclared(Ident ident)
        {
            return prog.RemarkList.AddException(new Remark(
                RemarkSerity.Exception,
                "Variable '" + ident.Name + "' is already declared",
                ident.FilePath,
                ident.LineIndex,
                ident,
                null,
                prog.Interpreter.CallStack));
        }
    }
}