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

        private EfektException f(string message, [CanBeNull] Element target)
        {
            return prog.RemarkList.AddFatal(new Remark(
                RemarkSeverity.Fatal,
                message,
                target == null ? "" : target.FilePath,
                target == null ? -1 : target.LineIndex,
                target,
                target == null ? null : target.Parent));
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
            return f("End Brace Does Not Matches Start", null);
        }

        [Pure]
        public EfektException CharShouldHaveOnlyOneChar()
        {
            return f("CharShouldHaveOnlyOneChar", null);
        }

        [Pure]
        public EfektException BraceExpected()
        {
            return f("BraceExpected", null);
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
            return f("ExpectedExpressionAfterReturn", null);
        }

        [Pure]
        public EfektException InvalidElementAfterVar(Element se)
        {
            return f("InvalidElementAfterVar", se);
        }

        [Pure]
        public EfektException MissingTestExpression()
        {
            return f("MissingTestExpression", null);
        }

        [Pure]
        public EfektException ExpectedWordThen(Exp testExp)
        {
            return f("ExpectedWordThen", testExp);
        }
    }
}