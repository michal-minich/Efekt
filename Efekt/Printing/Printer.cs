namespace Efekt
{
    public sealed class Printer
    {
        private readonly PlainTextCodeWriter w;

        public Printer(PlainTextCodeWriter codeWriter)
        {
            w = codeWriter;
        }


        public PlainTextCodeWriter Write(Element se)
        {
            switch (se)
            {
                case When wh:
                    w.Key("if").Space();
                    Write(wh.Test).Space().Markup("{");
                    Write(wh.Then);
                    w.Markup("}");
                    if (wh.Otherwise != null)
                    {
                        w.Space().Markup("else").Space().Markup("{");
                        Write(wh.Otherwise);
                        w.Markup("}");
                    }
                    break;
                case Ident i:
                    w.Ident(i.Name);
                    break;
                case Int ii:
                    w.Num(ii.Value.ToString());
                    break;
                case FnApply fna:
                    Write(fna.Fn);
                    w.Space();
                    var c = 0;
                    w.Markup("(");
                    foreach (var a in fna.Arguments)
                    {
                        Write(a);
                        if (fna.Arguments.Count != ++c)
                            w.Markup(", ");
                    }
                    w.Markup(")");
                    break;
                case Fn f:
                    w.Key("fn").Space();
                    c = 0;
                    foreach (var p in f.Parameters)
                    {
                        Write(p);
                        if (f.Parameters.Count != ++c)
                            w.Markup(",");
                        w.Space();
                    }
                    w.Markup("{").Space();
                    c = 0;
                    foreach (var p in f.Sequence)
                    {
                        Write(p);
                        if (f.Sequence.Count != ++c)
                            w.Line();
                    }
                    w.Space().Markup("}");
                    break;
                case Return r:
                    w.Key("return");
                    if (r.Exp != Void.Instance)
                    {
                        w.Space();
                        Write(r.Exp);
                    }
                    break;
                case Loop l:
                    w.Key("loop").Space().Markup("{");
                    Write(l.Body).Markup("}");
                    break;
                case Bool b:
                    w.Key(b.Value ? "true" : "false");
                    break;
                case Break _:
                    w.Key("break");
                    break;
                case Var v:
                    w.Key("var").Space();
                    writeAssign(v.Ident, v.Exp);
                    break;
                case Assign a:
                    writeAssign(a.Ident, a.Exp);
                    break;
                case Arr arr:
                    w.Markup("[");
                    var counter = 0;
                    foreach (var i in arr.Values)
                    {
                        Write(i);
                        if (++counter != arr.Values.Count)
                            w.Markup(",").Space();
                    }
                    w.Markup("]");
                    break;
                case MemberAccess ma:
                    Write(ma.Exp).Op(".");
                    Write(ma.Ident);
                    break;
                case New n:
                    w.Key("new").Space().Markup("{");
                    foreach (var se2 in n.Body)
                    {
                        C.Nn(se2);
                        Write(se2);
                        w.Line();
                    }
                    w.Markup("}");
                    break;
                case Sequence seq:
                    foreach (var item in seq)
                    {
                        Write(item);
                        w.Line();
                    }
                    break;
                default:
                    w.Markup("<" + se.GetType().Name + ">");
                    break;
            }

            return w;
        }

        private void writeAssign(Ident i, Exp e)
        {
            Write(i).Space().Op("=").Space();
            Write(e);
        }
    }
}