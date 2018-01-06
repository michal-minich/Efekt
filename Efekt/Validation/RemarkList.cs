using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Remark
    {
        public static Remark NewGeneralRemark(RemarkSeverity severity, string message)
        {
            return new Remark(severity, message, null, 0, 0, 0, 0, null, null);
        }

        public static Remark NewFileRemark(RemarkSeverity severity, string message, string filePath)
        {
            return new Remark(severity, message, filePath, 0, 0, 0, 0, null, null);
        }


        public static Remark NewTokenRemark(
            RemarkSeverity severity, string message, string filePath, int lineIndexStart,
            int columnIndexStart, Token token)
        {
            return new Remark(
                severity, message, filePath, lineIndexStart, columnIndexStart,
                lineIndexStart, columnIndexStart + token.Text.Length,
                null, null);
        }

        public static Remark NewStructureRemark(
            RemarkSeverity severity,
            string message,
            Element subject)
        {
            var filePath = Utils.GetFilePathRelativeToBase(subject.FilePath);
            return new Remark(
                severity, message, filePath,
                subject.LineIndex, subject.ColumnIndex, subject.LineIndexEnd, subject.ColumnIndexEnd,
                subject, null);
        }

        public static Remark NewExceptionRemark(
            string message,
            Element subject,
            IReadOnlyList<StackItem> callStack,
            RemarkSeverity severity = RemarkSeverity.RuntimeError)
        {
            var filePath = Utils.GetFilePathRelativeToBase(subject.FilePath);
            return new Remark(
                severity, message, filePath,
                subject.LineIndex, subject.ColumnIndex, subject.LineIndexEnd, subject.ColumnIndexEnd,
                subject, callStack);
        }

        public readonly RemarkSeverity Severity;
        public readonly string Message;
        [CanBeNull] public readonly string FilePath;
        public readonly int LineIndexStart;
        public readonly int ColumnIndexStart;
        public readonly int LineIndexEnd;
        public readonly int ColumnIndexEnd;
        [CanBeNull] public readonly Element Subject;
        [CanBeNull] public readonly IReadOnlyList<StackItem> CallStack;

        private Remark(
            RemarkSeverity severity,
            string message,
            [CanBeNull] string filePath,
            int lineIndexStart,
            int columnIndexStart,
            int lineIndexEnd,
            int columnIndexEnd,
            Element subject,
            IReadOnlyList<StackItem> callStack)
        {
            Severity = severity;
            Message = message;
            FilePath = filePath;
            LineIndexStart = lineIndexStart;
            ColumnIndexStart = columnIndexStart;
            LineIndexEnd = lineIndexEnd;
            ColumnIndexEnd = columnIndexEnd;
            Subject = subject;
            CallStack = callStack;
        }


        public string GetString()
        {
            C.ReturnsNn();

            string msg;

            if (CallStack == null)
            {
                msg = getPathLineCol() + Severity + ": " + Message;
            }
            else
            {
                var stack = CallStack;
                msg = getPathLineCol() + Severity + ": " + Message + Environment.NewLine
                      + string.Join(
                          Environment.NewLine,
                          stack
                              .Where(cs => cs.FilePath != "runtime.ef")
                              .Select(cs =>
                              {
                                  var filePath = Utils.GetFilePathRelativeToBase(cs.FilePath);
                                  return "  " + filePath
                                              + ":" + (cs.LineIndex + 1) + "," + (cs.ColumnIndex + 1) + " " + cs.FnName;
                              }));
            }

            return msg;
        }

        private string getPathLineCol()
        {
            if (FilePath == null)
                return "";
            return Utils.GetFilePathRelativeToBase(FilePath)
                   + ":" + (LineIndexStart + 1) + "," + (ColumnIndexStart + 1)
                   + "," + (LineIndexEnd + 1) + "," + (ColumnIndexEnd + 1) + " ";
        }
    }


    public enum RemarkSeverity
    {
        Info,
        Suggestion,
        Warning,
        Error,
        Fatal,
        RuntimeError,
        ProgramException
    }


    public sealed class RemarkList : IReadOnlyList<Remark>
    {
        private readonly Prog prog;
        private readonly List<Remark> remarks = new List<Remark>();


        public RemarkList(Prog prog)
        {
            this.prog = prog;
        }

        public IEnumerator<Remark> GetEnumerator() => remarks.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => remarks.Count;

        [NotNull] public Remark this[int index] => remarks[index];


        private string Add(Remark remark)
        {
            remarks.AddValue(remark);
            var msg = remark.GetString();
            prog.ErrorWriter.WriteLine(msg);
            return msg;
        }


        [Pure] // not pure but use return value 
        private EfektException Fail(Remark remark)
        {
            var msg = Add(remark);
            return new EfektException(msg);
        }


        [Pure]
        private EfektException fail(string message)
        {
            return Fail(Remark.NewGeneralRemark(RemarkSeverity.Fatal, message));
        }


        [Pure]
        private EfektException fail(string message, string filePath)
        {
            return Fail(Remark.NewFileRemark(RemarkSeverity.Fatal, message, filePath));
        }


        [Pure]
        private EfektException fail(Token token, string message, int lineIndex, int columnIndex, string filePath)
        {
            return Fail(Remark.NewTokenRemark(RemarkSeverity.Fatal, message, filePath, lineIndex, columnIndex, token));
        }


        private EfektException fail(TokenIterator ti, string message)
        {
            return Fail(Remark.NewTokenRemark(RemarkSeverity.Fatal, message,
                ti.FilePath, ti.LineIndex, ti.ColumnIndex, ti.Current));
        }


        [Pure]
        private EfektException fail(Element subject, string message)
        {
            return Fail(Remark.NewStructureRemark(RemarkSeverity.Fatal, message, subject));
        }


        [Pure]
        private EfektException ex(Element subject, string message)
        {
            return Fail(Remark.NewExceptionRemark(message, subject, prog.Interpreter.CallStack));
        }


        private void w(Element subject, string message)
        {
            Add(Remark.NewStructureRemark(RemarkSeverity.Warning, message, subject));
        }


        [Pure]
        public EfektException ExpectedDifferentElement(Element subject, Type type)
        {
            return fail(subject, "Expected element of type "
                                 + type.Name + ", but '"
                                 + subject.ToDebugString() + "' is of type " + subject.GetType().Name + ".");
        }


        [Pure]
        public EfektException ExpectedClassElement(Element subject)
        {
            return fail(subject, "Object / file can contain only 'var', 'let' or 'import'");
        }


        [Pure]
        public EfektException ExpectedSequenceElement(Element subject)
        {
            return fail(subject, "Sequence / function cannot contain element of type " + subject.GetType().Name);
        }


        [Pure]
        public EfektException AssignTargetIsInvalid(Exp subject)
        {
            return fail(subject, "Only identifier or object member can be assigned a value");
        }


        [Pure]
        public EfektException OnlyIdentifierCanBeDeclared(Exp subject)
        {
            return fail(subject, "Only identifier can be used with 'var'");
        }


        [Pure]
        public EfektException SecondOperandMustBeExpression(Element subject)
        {
            return fail(subject, "Second operand must be expression");
        }


        [Pure]
        public EfektException OnlyExpressionCanBeAssigned(Element subject)
        {
            return fail(subject, "Only expression can be assigned to variable");
        }


        [Pure]
        public EfektException FunctionArgumentMustBeExpression(Element subject)
        {
            return fail(subject, "function argument must be expression");
        }


        [Pure]
        public EfektException CharShouldHaveOnlyOneChar(TokenIterator ti)
        {
            return fail(ti, "Character literal should contain exactly 1 character inside quotes");
        }


        [Pure]
        public EfektException OpeningBraceIsExpected(TokenIterator ti, char expectedStartBrace)
        {
            return fail(ti, "Opening brace '" + expectedStartBrace + "' is expected");
        }

        [Pure]
        public EfektException ClosingBraceDoesNotMatchesOpening(TokenIterator ti, char actualStartBrace,
            string expectedEndBrace)
        {
            return fail(ti,
                "Closing brace '" + expectedEndBrace + "' does not match opening brace '" + actualStartBrace + "'");
        }


        [Pure]
        public EfektException ExpectedIdentifierAfterDot(Element second)
        {
            return fail(second, "Expected identifier after dot (.)");
        }


        [Pure]
        public EfektException ExpectedExpression(Element element)
        {
            return fail(element, "Expected expression, found '" + element.GetType().Name + "'");
        }


        [Pure]
        public EfektException ExpectedOnlyOneExpressionInsideBraces(List<Element> items)
        {
            C.Req(items.Count >= 2);

            return fail(items[2], "Only one expression should be enclosed in braces");
        }


        [Pure]
        public EfektException InvalidElementAfterVar(Element se)
        {
            return fail(se, "Only identifier can be after 'var' keyword");
        }


        [Pure]
        public EfektException ExpectedTestExpression(TokenIterator ti)
        {
            return fail(ti, "Expected test expression after 'if' keyword");
        }


        [Pure]
        public EfektException ExpectedWordThen(Exp testExp)
        {
            return fail(testExp, "Expected word 'then'");
        }


        [Pure]
        public Exception TokenIsInvalid(TokenIterator ti)
        {
            return fail(ti, "Invalid token '" + ti.Current.Text + "'");
        }


        [Pure]
        public Exception ExpectedQualifiedIdentAfterImport(Element notQi)
        {
            return fail(notQi, "Expected (qualified) identifier after 'import' keyword");
        }


        [Pure]
        public EfektException ExpectedDifferentType(Element subject, Element value, string expectedTypeName)
        {
            return ex(subject,
                "Expected type " + expectedTypeName + " but the expression is of type " + value.GetType().Name);
        }


        public void ExpectedDifferentType(Element subject, Spec expected, Spec actual)
        {
            w(subject,
                "Expected type " + expected.GetType().Name + " but the expression is of type " + actual.GetType().Name);
        }


        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsNotDeclared(Ident ident)
        {
            return fail(ident, "Variable '" + ident.Name + "' is not declared");
        }


        [Pure]
        // TODO move to structure validation eventually
        public EfektException MoreVariableCandidates<T>(Dictionary<QualifiedIdent, T> candidates, Ident ident)
        {
            return fail(ident, "Variable '" + ident.Name + "' can be found multiple times: " +
                               Environment.NewLine +
                               String.Join(Environment.NewLine, candidates.Select(
                                   c => "    " + c.Key.ToDebugString() + " : " + c.Value.GetType())));
        }


        [Pure]
        // TODO move to structure validation eventually
        public EfektException VariableIsAlreadyDeclared(Ident ident)
        {
            return fail(ident, "Variable '" + ident.Name + "' is already declared");
        }


        [Pure]
        // TODO move to structure validation eventually
        public EfektException ExtensionFuncHasNoParameters(Fn extFn, MemberAccess ma)
        {
            return fail(extFn, "Function must accept at least 1 parameter to be an extension function.");
        }


        // TODO move to structure validation eventually
        public void ValueReturnedFromFunctionNotUsed(FnApply fna)
        {
            w(fna, "Value returned from function '" + fna.Fn.ToDebugString()
                                                    + "' is not used. In '" + fna.ToDebugString() + "'");
        }


        // TODO move to structure validation eventually
        public void ValueIsNotAssigned(Element unusedValue)
        {
            w(unusedValue, "Value of expression is not used");
        }


        // TODO move to type validation eventually
        public void AssigningDifferentType<T>(Ident ident, T old, T @new)
        {
            w(ident, "Variable '" + ident.Name + "' of type '"
                     + old.GetType().Name + "' is being assigned value of type '"
                     + @new.GetType().Name + "'");
        }


        public void ReasigingLet(Ident ident)
        {
            w(ident, "Reasinging value in let variable '" + ident.Name + "'");
        }


        [Pure]
        public EfektProgramException ProgramException(Value tossed, Toss ts, List<StackItem> callStack)
        {
            var msg = Add(Remark.NewExceptionRemark(
                tossed.ToDebugString(), ts, callStack, RemarkSeverity.ProgramException));
            return new EfektProgramException(msg, tossed);
        }


        [Pure]
        public EfektException OlnyEfFilesAreSupported(string filePath)
        {
            return fail("Only .ef files are supported " + filePath, filePath);
        }


        [Pure]
        public EfektException CouldNotLocateFileOrFolder(string filePath)
        {
            return fail("Could not locate file or folder " + filePath, filePath);
        }


        [Pure]
        public EfektException CannotFindStartFunction()
        {
            return fail("Function named 'start' is needed. It can be in any file");
        }


        [Pure]
        public EfektException VariableIsNotYetInitializied(Ident ident)
        {
            return fail(ident, "Variable is not yet initialized and cannot be read");
        }


        [Pure]
        public EfektException VariableMightNotYetBeInitialized(Ident ident)
        {
            return fail(ident, "Variable might net yet be initialized and reading it might be unvanted");
        }
    }
}