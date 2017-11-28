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


        private EfektException ex(Element subject, [CanBeNull] Element inExp, string message)
        {
            return prog.RemarkList.AddException(new Remark(
                RemarkSeverity.Exception,
                message,
                subject.FilePath,
                subject.LineIndex,
                subject,
                inExp,
                prog.Interpreter.CallStack));
        }


        [Pure]
        public EfektException DifferentTypeExpected(Element value, string expectedTypeName, Element inExp)
        {
            return ex(value, inExp, "Expected type '" + expectedTypeName
                                    + "' but the expression is of type '" + value.GetType().Name + "'");
        }

        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsNotDeclared(Ident ident)
        {
            return ex(ident, null, "Variable '" + ident.Name + "' is not declared");
        }

        [Pure]
        // TODO move to structure validation eventually
        public EfektException MoreVariableCandidates(Dictionary<QualifiedIdent, Value> candidates, Ident ident)
        {
            return ex(ident, null,
                "Variable '" + ident.Name + "' can be found multiple times: " +
                Environment.NewLine +
                String.Join(Environment.NewLine, candidates.Select(
                    c => "    " + c.Key.ToDebugString() + " : " + c.Value.GetType())));
        }

        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsAlreadyDeclared(Ident ident)
        {
            return ex(ident, null, "Variable '" + ident.Name + "' is already declared");
        }

        [Pure]
        public EfektException ExtensionFuncHasNoParameters(Fn extFn, MemberAccess ma)
        {
            return ex(extFn, ma, "Function must accept at least 1 parameter to be an extension function.");
        }
    }
}