using System;
using JetBrains.Annotations;

namespace Efekt
{
    internal static class Error
    {
        [Pure]
        internal static Exception Fail()
        {
            return new Exception();
        }

        [Pure]
        public static Exception OnlyObjectsHaveMembers(Value nonObject)
        {
            return new EfektException("Only objects can have members", nonObject);
        }

        [Pure]
        public static Exception AssignTargetIsInvalid(Exp target)
        {
            return new EfektException("Only identifier or object member can be assigned a value", target);
        }

        [Pure]
        internal static Exception OnlyFunctionsCanBeApplied(Value nonFunction)
        {
            return new EfektException("Only functions can be applied to arguments", nonFunction);
        }

        [Pure]
        public static Exception VariableIsNotDeclared(Ident ident)
        {
            return new EfektException("Variable '" + ident.Name + "' is not declared", ident);
        }

        [Pure]
        public static Exception VariableIsAlreadyDeclared(Ident ident)
        {
            return new EfektException("Variable '" + ident.Name + "' is already declared", ident);
        }
    }
}
