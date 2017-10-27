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
        public Exception AssignTargetIsInvalid(Exp target)
        {
            return new EfektException("Only identifier or object member can be assigned a value", target.Parent);
        }
        
        [Pure]
        public Exception VariableIsNotDeclared(Ident ident)
        {
            return new EfektException("Variable '" + ident.Name + "' is not declared", ident.Parent);
        }

        [Pure]
        public Exception VariableIsAlreadyDeclared(Ident ident)
        {
            return new EfektException("Variable '" + ident.Name + "' is already declared", ident.Parent);
        }

        [Pure]
        public Exception DifferentTypeExpected(Exp value, string expectedTypeName, Exp inExp)
        {
            return new EfektException("Expected type '" + expectedTypeName
                                      + "' but the expression is of type '" + value.GetType().Name + "'", inExp);
        }
    }
}
