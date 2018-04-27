using System;

namespace Efekt
{
    public sealed class Importer
    {
        private readonly Prog prog;

        public Importer(Prog program)
        {
            C.Nn(program);
            prog = program;
        }


        public void ResolveImports()
        {
            import(prog.RootElement);
        }


        private void import(Element el)
        {
            C.Nn(el);

            switch (el)
            {
                case Declr d:
                    import(d.Ident);
                    import(d.Exp);
                    break;
                case Assign a:
                    import(a.Exp);
                    import(a.To);
                    break;
                case Ident i:
                    break;
                case Return r:
                    break;
                case FnApply fna:
                    import(fna.Fn);
                    foreach (var fnaa in fna.Arguments)
                    {
                        import(fnaa);
                    }
                    break;
                case Fn f:
                    foreach (var fp in f.Parameters)
                    {
                        import(fp);
                    }
                    import(f.Sequence);
                    break;
                case When w:
                    import(w.Test);
                    import(w.Then);
                    if (w.Otherwise != null)
                        import(w.Otherwise);
                    break;
                case Loop l:
                    import(l.Body);
                    break;
                case Continue _:
                    break;
                case Break _:
                    break;
                case ArrConstructor ae:
                    foreach (var aa in ae.Arguments)
                    {
                        import(aa);
                    }
                    break;
                case MemberAccess ma:
                    import(ma.Exp);
                    import(ma.Ident);
                    break;
                case New n:
                    foreach (var nb in n.Body)
                    {
                        import(nb);
                    }
                    break;
                case Value ve:
                    break;
                case Sequence seq:
                    foreach (var item in seq)
                    {
                        import(item);
                    }
                    break;
                case Toss ts:
                    import(ts.Exception);
                    break;
                case Attempt att:
                    import(att.Body);
                    if (att.Grab == null && att.AtLast == null)
                        throw prog.RemarkList.AttemptMustHaveGrabOrAtLastOrBoth(att);
                    if (att.Grab != null)
                        import(att.AtLast);
                    if (att.Grab != null)
                        import(att.AtLast);
                    break;
                case Import imp:
                    //var modImpEl = import(imp.QualifiedIdent, env);
                    //var modImp = modImpEl.AsObj(imp, prog);
                    //env.AddImport(imp.QualifiedIdent, modImp.Env);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
