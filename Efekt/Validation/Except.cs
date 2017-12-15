using System;
using System.Collections.Generic;
using System.Linq;
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


        private EfektException ex(Element subject, string message)
        {
            return prog.RemarkList.AddException(Remark.NewRemark(RemarkSeverity.Exception,
                message,
                subject,
                prog.Interpreter.CallStack));
        }


        private EfektException fatal(Element subject, string message)
        {
            return prog.RemarkList.AddFatal(Remark.NewRemark(RemarkSeverity.Fatal, message, subject));
        }


        [Pure]
        public EfektException ExpectedDifferentType(Element inExp, Element value, string expectedTypeName)
        {
            return ex(inExp,
                "Expected type '" + expectedTypeName
                                  + "' but the expression is of type '" + value.GetType().Name + "'");
        }

        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsNotDeclared(Ident ident)
        {
            return fatal(ident, "Variable '" + ident.Name + "' is not declared");
        }

        [Pure]
        // TODO move to structure validation eventually
        public EfektException MoreVariableCandidates(Dictionary<QualifiedIdent, Value> candidates, Ident ident)
        {
            return fatal(ident,
                "Variable '" + ident.Name + "' can be found multiple times: " +
                Environment.NewLine +
                String.Join(Environment.NewLine, candidates.Select(
                    c => "    " + c.Key.ToDebugString() + " : " + c.Value.GetType())));
        }

        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsAlreadyDeclared(Ident ident)
        {
            return fatal(ident, "Variable '" + ident.Name + "' is already declared");
        }

        [Pure]
        // TODO move to structure validation eventually
        public EfektException ExtensionFuncHasNoParameters(Fn extFn, MemberAccess ma)
        {
            return fatal(extFn, "Function must accept at least 1 parameter to be an extension function.");
        }
    }
}