using System;
using System.Linq;

namespace Efekt
{
    public static class Simpler
    {
        public static bool IsSimple(Element se)
        {
            switch (se)
            {
                case When wh:
                    return wh.Test.IsSimple() && wh.Then.IsSimple() && wh.Otherwise != null && wh.Otherwise.IsSimple();

                case Ident i:
                    return true;

                case Int ii:
                    return true;

                case FnApply fna:
                    return fna.Fn.IsSimple() && fna.Arguments.All(IsSimple);

                case Fn f:
                    return f.Sequence.IsSimple();

                case Return r:
                    return r.Exp.IsSimple();

                case Loop l:
                    return false;

                case Bool b:
                    return true;

                case Continue _:
                    return true;

                case Break _:
                    return true;

                case Declr l:
                    return l.Exp == null || l.Exp.IsSimple();
                    
                case Assign a:
                    return a.To.IsSimple() && a.Exp.IsSimple();

                case Char ch:
                    return true;

                case Text tx:
                    return true;

                case ArrConstructor arrC:
                    return arrC.Arguments.All(IsSimple);

                case MemberAccess ma:
                    return ma.Exp.IsSimple();

                case New n:
                    return n.Body.Count == 0;

                case Sequence seq:
                    return seq.Count == 0 || seq.Count == 1 && seq.First().IsSimple();

                case Toss ts:
                    return ts.Exception.IsSimple();

                case Attempt att:
                    return false;

                case Import imp:
                    return true;

                case Void _:
                    return true;

                case Spec spec:
                {
                    switch (spec)
                    {
                        case AnySpec @as:
                            return true;

                        case TextSpec ts:
                            return true;

                       case ArrSpec ars:
                            return true;

                        case BoolSpec bs:
                            return true;

                        case CharSpec chs:
                            return true;

                        case FnSpec fs:
                            return true;

                        case IntSpec @is:
                            return true;

                        case ObjSpec os:
                            return os.Env.Items.Count == 0;

                        case NotSetSpec nss:
                            return true;

                        case VoidSpec vs:
                            return true;

                        default:
                            throw new Exception();
                    }
                }

                default:
                    throw new Exception();
            }
        }
    }
}