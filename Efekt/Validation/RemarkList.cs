using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public sealed class Remark
    {
        public readonly RemarkSeverity Severity;
        public readonly string Message;
        public readonly string FilePath;
        public readonly int LineIndexStart;
        [CanBeNull] public readonly Element Subject;
        [CanBeNull] public readonly Element InExp;
        [CanBeNull] public readonly IReadOnlyList<StackItem> CallStack;

        public Remark(
            RemarkSeverity severity,
            string message,
            string filePath,
            int lineIndexStart,
            Element subject = null,
            Element inExp = null,
            IReadOnlyList<StackItem> callStack = null)
        {
            Severity = severity;
            Message = message;
            FilePath = filePath;
            LineIndexStart = lineIndexStart;
            Subject = subject;
            InExp = inExp;
            CallStack = callStack;
        }


        public string GetString()
        {
            string msg;

            if (CallStack == null)
            {
                msg = Utils.GetFilePathRelativeToBase(FilePath) + ":" + (LineIndexStart + 1) + " "
                      + Severity + ": " + Message;
            }
            else
            {
                msg = Severity + ": " + Message + Environment.NewLine
                      + string.Join(
                          Environment.NewLine,
                          CallStack
                              .Select(cs =>
                              {
                                  var filePath = Utils.GetFilePathRelativeToBase(cs.FilePath);
                                  return "  " + filePath
                                         + ":" + (cs.LineIndex + 1) + " " + cs.FnName;
                              }));
            }

            if (InExp != null)
                msg += " in '" + InExp.ToDebugString() + "'";

            return msg;
        }
    }


    public enum RemarkSeverity
    {
        Warning,
        Error,
        Fatal,
        Exception
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

            remarks.Add(remark);
            prog.ErrorWriter.WriteLine(remark.GetString());
        }


        [Pure] // not pure but use return value 
        public EfektException AddFatal(Remark remark)
        {
            C.Nn(remark.Subject);
            if (remark.Severity != RemarkSeverity.Fatal)
                throw new InvalidOperationException();

            remarks.Add(remark);
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

            remarks.Add(remark);
            var msg = remark.GetString();
            prog.ErrorWriter.WriteLine(msg);
            return new EfektException(msg);
        }
    }
}