using System;

namespace Efekt
{
    public sealed class CodeWriter
    {
        private readonly CodeTextWriter ctw;

        public CodeWriter(CodeTextWriter codeTextWriter)
        {
            ctw = codeTextWriter;
        }

        public void Write(Element se)
        {
            switch (se)
            {
                case When w:
                    ctw.WriteKey("if").WriteSpace();
                    Write(w.Test);
                    ctw.WriteSpace().WriteMarkup("{");
                    Write(w.Then);
                    ctw.WriteMarkup("}");
                    if (w.Otherwise != null)
                    {
                        ctw.WriteSpace().WriteMarkup("else").WriteSpace().WriteMarkup("{");
                        Write(w.Otherwise);
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
                    Write(fna.Fn);
                    ctw.WriteSpace();
                    var c = 0;
                    ctw.WriteMarkup("(");
                    foreach (var a in fna.Arguments)
                    {
                        Write(a);
                        if (fna.Arguments.Count != ++c)
                            ctw.WriteMarkup(", ");
                    }
                    ctw.WriteMarkup(")");
                    return;
                case Fn f:
                    ctw.WriteKey("fn").WriteSpace();
                    c = 0;
                    foreach (var p in f.Parameters)
                    {
                        Write(p);
                        if (f.Parameters.Count != ++c)
                            ctw.WriteMarkup(", ");
                    }
                    ctw.WriteMarkup("{");
                    c = 0;
                    foreach (var p in f.Body)
                    {
                        Write(p);
                        if (f.Body.Count != ++c)
                            ctw.WriteLine();
                    }
                    ctw.WriteMarkup("}");
                    break;
                case Return r:
                    ctw.WriteKey("return");
                    if (r.Exp != Void.Instance)
                    {
                        ctw.WriteSpace();
                        Write(r.Exp);
                    }
                    break;
                case Loop l:
                    ctw.WriteKey("loop").WriteSpace();
                    ctw.WriteMarkup("{");
                    Write(l.Body);
                    ctw.WriteMarkup("}");
                    break;
                case Var v:
                    ctw.WriteKey("var").WriteSpace();
                    Write(v.Ident);
                    ctw.WriteSpace().WriteOp("=").WriteSpace();
                    Write(v.Exp);
                    break;
                case ElementList sel:
                    foreach (var se2 in sel)
                    {
                        Write(se2);
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