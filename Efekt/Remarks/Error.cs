using System;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Error
    {
        [Pure]
        internal Exception Fail()
        {
            return new Exception();
        }

        [Pure]
        public Exception OnlyObjectsHaveMembers(Value nonObject)
        {
            return new EfektException("Only objects can have members", nonObject);
        }

        [Pure]
        public Exception AssignTargetIsInvalid(Exp target)
        {
            return new EfektException("Only identifier or object member can be assigned a value", target);
        }

        [Pure]
        internal Exception OnlyFunctionsCanBeApplied(Value nonFunction)
        {
            return new EfektException("Only functions can be applied to arguments", nonFunction);
        }

        [Pure]
        public Exception VariableIsNotDeclared(Ident ident)
        {
            return new EfektException("Variable '" + ident.Name + "' is not declared", ident);
        }

        [Pure]
        public Exception VariableIsAlreadyDeclared(Ident ident)
        {
            return new EfektException("Variable '" + ident.Name + "' is already declared", ident);
        }

        [Pure]
        public Exception DifferentTypeExpected(Exp exp, string expectedTypeName)
        {
            return new EfektException("Expected type '" + expectedTypeName
                                      + "' but the expression is of type '" + exp.GetType().Name + "'", exp);
        }
    }
}
