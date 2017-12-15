using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Remark
    {
        public static Remark NewRemark(RemarkSeverity severity, string message, string filePath, int lineIndexStart, int columnIndexStart, Token token)
        {
            return new Remark(
                severity, message, filePath, lineIndexStart, columnIndexStart,
                lineIndexStart, columnIndexStart + token.Text.Length,
                null, null);
        }

        public static Remark NewRemark(RemarkSeverity severity, string message, Element subject, IReadOnlyList<StackItem> callStack = null)
        {
            var filePath = Utils.GetFilePathRelativeToBase(subject.FilePath);
            return new Remark(
                severity, message, filePath,
                subject.LineIndex, subject.ColumnIndex, subject.LineIndexEnd, subject.ColumnIndexEnd,
                subject, callStack);
        }

        public readonly RemarkSeverity Severity;
        public readonly string Message;
        public readonly string FilePath;
        public readonly int LineIndexStart;
        public readonly int ColumnIndexStart;
        public readonly int LineIndexEnd;
        public readonly int ColumnIndexEnd;
        [CanBeNull] public readonly Element Subject;
        [CanBeNull] public readonly IReadOnlyList<StackItem> CallStack;

        private Remark(
            RemarkSeverity severity,
            string message,
            string filePath,
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
                msg = getPathLineCol() + " " + Severity + ": " + Message;
            }
            else
            {
                var stack = CallStack;
                msg = getPathLineCol() + " " + Severity + ": " + Message + Environment.NewLine
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
            return Utils.GetFilePathRelativeToBase(FilePath)
                   + ":" + (LineIndexStart + 1) + "," + (ColumnIndexStart + 1)
                   + "," + (LineIndexEnd + 1) + "," + (ColumnIndexEnd + 1);
        }
    }


    public enum RemarkSeverity
    {
        Warning,
        Error,
        Fatal,
        Exception,
        InterpretedException
    }


    public sealed class RemarkList : IReadOnlyList<Remark>
    {
        private readonly Prog prog;
        private readonly List<Remark> remarks = new List<Remark>();
        public readonly Structure Structure;
        public readonly Warn Warn;
        public readonly Except Except;


        public RemarkList(Prog prog)
        {
            this.prog = prog;
            Warn = new Warn(prog);
            Except = new Except(prog);
            Structure = new Structure(prog);
        }

        public IEnumerator<Remark> GetEnumerator() => remarks.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => remarks.Count;

        [NotNull]
        public Remark this[int index] => remarks[index];


        public void Add(Remark remark)
        {
            if (remark.Severity == RemarkSeverity.Fatal || remark.Severity == RemarkSeverity.Exception)
                throw new InvalidOperationException();

            remarks.AddValue(remark);
            prog.ErrorWriter.WriteLine(remark.GetString());
        }


        [Pure] // not pure but use return value 
        public EfektException AddFatal(Remark remark)
        {
            if (remark.Severity != RemarkSeverity.Fatal)
                throw new InvalidOperationException();

            remarks.AddValue(remark);
            var msg = remark.GetString();
            prog.ErrorWriter.WriteLine(msg);
            return new EfektException(msg);
        }


        [Pure] // not pure but use return value 
        public EfektException AddException(Remark remark)
        {
            C.Nn(remark.Subject);
            if (remark.Severity != RemarkSeverity.Exception)
                throw new InvalidOperationException();

            remarks.AddValue(remark);
            var msg = remark.GetString();
            prog.ErrorWriter.WriteLine(msg);
            return new EfektException(msg);
        }


        [Pure] // not pure but use return value 
        public EfektInterpretedException AddInterpretedException(Remark remark)
        {
            C.Nn(remark.Subject);
            if (remark.Severity != RemarkSeverity.InterpretedException)
                throw new InvalidOperationException();

            remarks.AddValue(remark);
            var msg = remark.GetString();
            prog.ErrorWriter.WriteLine(msg);
            return new EfektInterpretedException((Value)remark.Subject);
        }
    }
}