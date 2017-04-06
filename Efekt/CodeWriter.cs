using System;

namespace Efekt
{
    public static class CodeWriter { 

        public static void Write(SyntaxElement se, CodeTextWriter ctw)
        {
            switch (se)
            {
                case When w:
                    ctw.WriteKey("if").WriteSpace();
                    Write(w.Test, ctw);
                    ctw.WriteSpace().WriteMarkup("{");
                    Write(w.Then, ctw);
                    ctw.WriteMarkup("}");
                    if (w.Otherwise != null)
                    {
                        ctw.WriteSpace().WriteMarkup("else").WriteSpace().WriteMarkup("{");
                        Write(w.Otherwise, ctw);
                        ctw.WriteMarkup("}");
                    }
                    break;
                case Ident i:
                    ctw.WriteIdent(i.Name);
                    return;
                case Int ii:
                    ctw.WriteNum(ii.Value.ToString());
                    return;
                case FnApply fna:
                    Write(fna.Fn, ctw);
                    ctw.WriteSpace();
                    var c = 0;
                    ctw.WriteMarkup("(");
                    foreach (var a in fna.Arguments.Items)
                    {
                        Write(a, ctw);
                        if (fna.Arguments.Items.Count != ++c)
                            ctw.WriteMarkup(", ");
                    }
                    ctw.WriteMarkup(")");
                    return;
                case Fn f:
                    ctw.WriteKey("fn").WriteSpace();
                    c = 0;
                    foreach (var p in f.Parameters.Items)
                    {
                        Write(p, ctw);
                        if (f.Parameters.Items.Count != ++c)
                            ctw.WriteMarkup(", ");
                    }
                    ctw.WriteMarkup("{");
                    c = 0;
                    foreach (var p in f.Body.Items)
                    {
                        Write(p, ctw);
                        if (f.Body.Items.Count != ++c)
                            ctw.WriteLine();
                    }
                    ctw.WriteMarkup("}");
                    break;
                case Return r:
                    ctw.WriteKey("return");
                    if (r.Exp != Void.Instance)
                    {
                        ctw.WriteSpace();
                        Write(r.Exp, ctw);
                    }
                    break;
                case Loop l:
                    ctw.WriteKey("loop").WriteSpace();
                    ctw.WriteMarkup("{");
                    Write(l.Body, ctw);
                    ctw.WriteMarkup("}");
                    break;
                case Var v:
                    ctw.WriteKey("var").WriteSpace();
                    Write(v.Ident, ctw);
                    ctw.WriteSpace().WriteOp("=").WriteSpace();
                    Write(v.Exp, ctw);
                    break;
                case ElementList<SyntaxElement> sel:
                    foreach (var se2 in sel.Items)
                    {
                        Write(se2, ctw);
                        ctw.WriteLine();
                    }
                    break;
                default:
                    ctw.WriteMarkup("<" + se.GetType().Name + ">");
                    break;
                case null:
                    throw new Exception();
            }
        }
    }
}