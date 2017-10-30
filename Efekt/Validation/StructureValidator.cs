using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class StructureValidator
    {
        private readonly Prog prog;

        public StructureValidator(Prog prog)
        {
            this.prog = prog;
        }

        [Pure]
        public Exception AssignTargetIsInvalid(Exp target)
        {
            return prog.RemarkList.AddException(new Remark(
                RemarkSerity.Fatal,
                "Only identifier or object member can be assigned a value",
                target.FilePath,
                target.LineIndex,
                target));

        }

        [Pure]
        public Exception OnlyIdentifierCanBeDeclared(Exp target)
        {
            return new EfektException("Only identifier can be used with 'var'", target.Parent);
        }

        [Pure]
        public Exception SecondOperandMustBeExpression(Element target)
        {
            return new EfektException("Second operand must be expression", target.Parent);
        }

        [Pure]
        public Exception FunctionArgumentMustBeExpression(Element target)
        {
            return new EfektException("function argument must be expression", target.Parent);
        }

        [Pure]
        public Exception EndBraceDoesNotMatchesStart(Element target)
        {
            return new EfektException("End Brace Does Not Matches Start", new Int(1));
        }

        [Pure]
        public Exception CharShouldHaveOnlyOneChar()
        {
            return new EfektException("CharShouldHaveOnlyOneChar", new Int(1));
        }

        [Pure]
        public Exception BraceExpected()
        {
            return new EfektException("BraceExpected", new Int(1));
        }

        [Pure]
        public Exception ExpectedIdentifierAfterDot(Element second)
        {
            return new EfektException("ExpectedIdntifierAfterDot", second.Parent);
        }

        [Pure]
        internal Exception ExpectedExpressionAfterReturn(Element element)
        {
            return new EfektException("ExpectedExpressionAfterReturn", element.Parent);
        }

        [Pure]
        public Exception ExpectedOnlyOneExpressionInsideBraces(List<Element> elbItems)
        {
            return new EfektException("ExpectedExpressionAfterReturn", new Int(1));
        }

        [Pure]
        public Exception InvalidElementAfterVar(Element se)
        {
            return new EfektException("InvalidElelentAfterVar", se.Parent);
        }

        [Pure]
        public Exception MissingTestExpression()
        {
            return new EfektException("MissingTestExpression", new Int(1));
        }

        [Pure]
        internal Exception ExpectedWordThen(Exp testExp)
        {
            return new EfektException("ExpectedWordThen", testExp);
        }
    }
}