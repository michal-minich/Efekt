using System;

namespace Efekt
{
    public sealed class Namer
    {
        private readonly Prog prog;
        private bool isImportContext;

        public Namer(Prog prog)
        {
            this.prog = prog;
        }


        public void Name()
        {
            name(prog.RootElement, Env<Declr>.CreateDeclrRoot(prog));
        }


        private void name(Element se, Env<Declr> env)
        {
            C.Nn(se, env);

            switch (se)
            {
                case Declr d:
                    env.Declare(d.Ident, d, !(d is Var));
                    if (d.Exp != null)
                        name(d.Exp, env);
                    break;
                case Assign a:
                    name(a.To, env);
                    name(a.Exp, env);
                    break;
                case Ident i:
                    var dBy = env.GetOrNull(i); // TODO use Get
                    if (dBy == null)
                        return;
                    dBy.UsedBy.Add(i);
                    i.DeclareBy = dBy;
                    break;
                case Return r:
                    name(r.Exp, env);
                    break;
                case FnApply fna:
                    name(fna.Fn, env);
                    foreach (var a in fna.Arguments)
                        name(a, env);
                    break;
                case Fn f:
                    var fnParamsEnv = Env<Declr>.Create(prog, env);
                    foreach (var p in f.Parameters)
                        name(p, fnParamsEnv);
                    var fnBodyEnv = Env<Declr>.Create(prog, fnParamsEnv);
                    name(f.Sequence, fnBodyEnv);
                    break;
                case When w:
                    name(w.Test, env);
                    name(w.Then, env);
                    if (w.Otherwise != null)
                        name(w.Otherwise, env);
                    break;
                case Loop l:
                    name(l.Body, env);
                    break;
                case Break _:
                    break;
                case Continue _:
                    break;
                case ArrConstructor ae:
                    foreach (var a in ae.Arguments)
                        name(a, env);
                    break;
                case MemberAccess ma:
                    name(ma.Exp, env);
                    //name(ma.Ident, env);
                    // TODO
                    break;
                case New n:
                    var objEnv = Env<Declr>.Create(prog, env);
                    foreach (var v in n.Body)
                        name(v, objEnv);
                    break;
                case Value _:
                    break;
                case Sequence seq:
                    var scopeEnv = Env<Declr>.Create(prog, env);
                    foreach (var si in seq)
                        name(si, scopeEnv);
                    break;
                case Toss ts:
                    name(ts.Exception, env);
                    break;
                case Attempt att:
                    name(att.Body, env);
                    if (att.Grab != null)
                        name(att.Grab, env);
                    if (att.AtLast != null)
                        name(att.AtLast, env);
                    break;
                case Import imp:
                    // TODO
                    isImportContext = true;
                    /*var modImpEl = *///name(imp.QualifiedIdent, env);
                    isImportContext = false;
                    //var modImp = modImpEl.AsObj(imp, prog);
                    //env.AddImport(imp.QualifiedIdent, modImp);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}