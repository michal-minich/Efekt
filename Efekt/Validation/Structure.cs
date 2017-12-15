using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Structure
    {
        private readonly Prog prog;

        public Structure(Prog prog)
        {
            this.prog = prog;
        }

        private EfektException f(string message, [NotNull] Element target)
        {
            return prog.RemarkList.AddFatal(Remark.NewRemark(RemarkSeverity.Fatal,
                message,
                target));
        }

        [Pure]
        internal EfektException ExpectedDifferentElement(Element target, Type type)
        {
            return f("Expected element of type '"
                     + type.Name + "', but '"
                     + target.ToDebugString() + "' is of type '" + target.GetType().Name + "'.", target);
        }

        [Pure]
        public EfektException AssignTargetIsInvalid(Exp target)
        {
            return f("Only identifier or object member can be assigned a value", target);
        }

        [Pure]
        public EfektException OnlyIdentifierCanBeDeclared(Exp target)
        {
            return f("Only identifier can be used with 'var'", target);
        }

        [Pure]
        public EfektException SecondOperandMustBeExpression(Element target)
        {
            return f("Second operand must be expression", target);
        }

        [Pure]
        public EfektException FunctionArgumentMustBeExpression(Element target)
        {
            return f("function argument must be expression", target);
        }

        [Pure]
        public EfektException EndBraceDoesNotMatchesStart()
        {
            return f("End Brace Does Not Matches Start", Void.Instance);
        }

        [Pure]
        public EfektException CharShouldHaveOnlyOneChar()
        {
            return f("CharShouldHaveOnlyOneChar", Void.Instance);
        }

        [Pure]
        public EfektException BraceExpected()
        {
            return f("BraceExpected", Void.Instance);
        }

        [Pure]
        public EfektException ExpectedIdentifierAfterDot(Element second)
        {
            return f("ExpectedIdentifierAfterDot", second);
        }

        [Pure]
        public EfektException ExpectedExpression(Element element)
        {
            return f("Expected expression, found '" + element.GetType().Name + "'", element);
        }

        [Pure]
        public EfektException ExpectedOnlyOneExpressionInsideBraces(List<Element> elbItems)
        {
            return f("ExpectedExpressionAfterReturn", Void.Instance);
        }

        [Pure]
        public EfektException InvalidElementAfterVar(Element se)
        {
            return f("InvalidElementAfterVar", se);
        }

        [Pure]
        public EfektException MissingTestExpression()
        {
            return f("MissingTestExpression", Void.Instance);
        }

        [Pure]
        public EfektException ExpectedWordThen(Exp testExp)
        {
            return f("ExpectedWordThen", testExp);
        }

        [Pure]
        public Exception ExpectedQualifiedIdentAfterImport(Element notQi)
        {
            return f("Expected (qualified identifier) after import keyword. '"
                     + notQi.ToDebugString() + "' as found instead", notQi);
        }
    }
}