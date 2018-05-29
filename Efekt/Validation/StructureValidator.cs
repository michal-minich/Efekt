using System;
using System.Collections.Generic;

namespace Efekt
{

    public sealed class StructureValidator
    {
        private class LoopInfo
        {
            public bool HasContinue;
            public bool HasBreak;
            public bool HasReturn;
        }

        private readonly Prog prog;
        private readonly Stack<LoopInfo> LoopStack = new Stack<LoopInfo>();

        public StructureValidator(Prog program)
        {
            C.Nn(program);
            prog = program;
        }


        public void Validate()
        {
            validate(prog.RootElement);
        }


        private void validate(Element el)
        {
            C.Nn(el);

            switch (el)
            {
                case Declr d:
                    validate(d.Ident);
                    if (d.Exp != null)
                        validate(d.Exp);
                    break;
                case Assign a:
                    validate(a.Exp);
                    validate(a.To);
                    break;
                case Ident i:
                    break;
                case Return r:
                    if (LoopStack.Count != 0)
                        LoopStack.Peek().HasReturn = true;
                    break;
                case FnApply fna:
                    validate(fna.Fn);
                    foreach (var fnaa in fna.Arguments)
                    {
                        validate(fnaa);
                    }
                    break;
                case Fn f:
                    foreach (var fp in f.Parameters)
                    {
                        validate(fp);
                    }
                    validate(f.Sequence);
                    break;
                case When w:
                    validate(w.Test);
                    validate(w.Then);
                    if (w.Otherwise != null)
                        validate(w.Otherwise);
                    break;
                case Loop l:
                    var loopInfo = new LoopInfo();
                    LoopStack.Push(loopInfo);
                    validate(l.Body);
                    if (!(loopInfo.HasBreak || loopInfo.HasReturn))
                        throw prog.RemarkList.ContinueOrReturnRequiredInLoop(l);
                    LoopStack.Pop();
                    break;
                case Continue cont:
                    if (LoopStack.Count != 0)
                        LoopStack.Peek().HasContinue = true;
                    else
                        throw prog.RemarkList.ContinueOutsideLoop(cont);
                    break;
                case Break brk:
                    if (LoopStack.Count != 0)
                        LoopStack.Peek().HasBreak = true;
                    else
                        throw prog.RemarkList.BreakOutsideLoop(brk);
                    break;
                case ArrConstructor ae:
                    foreach (var aa in ae.Arguments)
                    {
                        validate(aa);
                    }
                    break;
                case MemberAccess ma:
                    validate(ma.Exp);
                    validate(ma.Ident);
                    break;
                case New n:
                    foreach (var nb in n.Body)
                    {
                        validate(nb);
                    }
                    break;
                case Value ve:
                    break;
                case Sequence seq:
                    foreach (var item in seq)
                    {
                        validate(item);
                    }
                    break;
                case Toss ts:
                    validate(ts.Exception);
                    break;
                case Attempt att:
                    validate(att.Body);
                    if (att.Grab == null && att.AtLast == null)
                        throw prog.RemarkList.AttemptMustHaveGrabOrAtLastOrBoth(att);
                    if (att.Grab != null)
                        validate(att.Grab);
                    if (att.AtLast != null)
                        validate(att.AtLast);
                    break;
                case Import imp:
                    validate(imp.QualifiedIdent);
                    break;

                case Spec spec:
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}