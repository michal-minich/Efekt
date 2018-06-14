using System;
using System.Collections.Generic;
using System.Linq;

namespace Efekt
{
    public sealed class Printer
    {
        private readonly PlainTextCodeWriter w;
        private readonly bool asCode;

        public Printer(PlainTextCodeWriter codeWriter, bool asCode)
        {
            w = codeWriter;
            this.asCode = asCode;
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
                    if (fna.Fn is Ident ident && ident.TokenType == TokenType.Op)
                    {
                        w.Markup("(");
                        Write(fna.Arguments[0]).Space();
                        Write(fna.Fn).Space();
                        Write(fna.Arguments[1]);
                        w.Markup(")");
                        break;
                    }
                    Write(fna.Fn);
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
                case Continue _:
                    w.Key("continue");
                    break;
                case Break _:
                    w.Key("break");
                    break;
                case Let l:
                    w.Key("let").Space();
                    writeDeclAssing(l.Ident, l.Exp);
                    break;
                case Var v:
                    w.Key("var").Space();
                    writeDeclAssing(v.Ident, v.Exp);
                    break;
                case Param p:
                    Write(p.Ident);
                    break;
                case Assign a:
                    writeAssign(a.To, a.Exp);
                    break;
                case Char ch:
                    if (asCode)
                        w.Markup("\'");
                    w.Text(ch.Value.ToString());
                    if (asCode)
                        w.Markup("\'");
                    break;
                case Text tx:
                    if (asCode)
                        w.Markup("\"").Text(tx.Value);
                    w.Text(tx.Value);
                    if (asCode)
                        w.Markup("\"");
                    break;

                case ArrConstructor arrC:
                    printArr(arrC.Arguments);
                    break;

                case Arr arr:
                    printArr(arr.Values);
                    break;

                case MemberAccess ma:
                    Write(ma.Exp).Op(".");
                    Write(ma.Ident);
                    break;
                case New n:
                    w.Key("new").Space().Markup("{");
                    foreach (var se2 in n.Body)
                    {
                        C.Assert(se2 != null);
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
                case Toss ts:
                    w.Key("throw").Space();
                    Write(ts.Exception);
                    break;
                case Attempt att:
                    w.Key("try").Space().Markup("{");
                    Write(att.Body);
                    w.Markup("}");
                    if (att.Grab != null)
                    {
                        w.Space().Markup("catch").Space().Markup("{");
                        Write(att.Grab);
                        w.Markup("}");
                    }
                    if (att.AtLast != null)
                    {
                        w.Space().Markup("finally").Space().Markup("{");
                        Write(att.AtLast);
                        w.Markup("}");
                    }
                    break;
                case Import imp:
                    w.Key("import").Space();
                    Write(imp.QualifiedIdent);
                    break;
                case Invalid inv:
                    w.Markup(inv.Text);
                    break;
                case Spec spec:
                {
                    switch (spec)
                    {
                        case AnySpec @as:
                            w.Type("Any");
                            break;
                        case TextSpec ts:
                            w.Type("Text");
                            break;
                        case ArrSpec ars:
                            w.Type("Arr").Markup("(");
                            Write(ars.ItemSpec).Markup(")");
                            break;
                        case BoolSpec bs:
                            w.Type("Bool");
                            break;
                        case CharSpec chs:
                            w.Type("Char");
                            break;
                        case FnSpec fs:
                            w.Type("Fn").Markup("(");
                            var counter = 0;
                            foreach (var p in fs.ParameterSpec)
                            {
                                Write(p);
                                if (++counter != fs.ParameterSpec.Count)
                                    w.Markup(",").Space();
                            }

                            w.Markup(")").Space().Op("->").Space();
                            Write(fs.ReturnSpec);
                            break;
                        case IntSpec @is:
                            w.Type("Int");
                            break;
                        case ObjSpec os:
                            w.Type("Obj").Markup("(");
                            counter = 0;
                            foreach (var m in os.Env.Items)
                            {
                                w.Ident(m.Key.Ident.Name).Space().Op(":").Space();
                                Write(m.Value.Value);
                                if (++counter != os.Env.Items.Count)
                                    w.Markup(",").Space();
                            }

                            w.Markup(")");
                            break;
                        case NotSetSpec nss:
                            w.Type("NotSet");
                            break;
                        case VoidSpec vs:
                            w.Type("Void");
                            break;

                        default:
                            throw new Exception();
                        }

                    break;
                }
                default:
                    w.Markup("<" + se.GetType().Name + ">");
                    break;
            }

            return w;
        }


        private void printArr(IReadOnlyCollection<Exp> values)
        {
            w.Markup("[");
            var counter = 0;
            foreach (var i in values)
            {
                Write(i);
                if (++counter != values.Count)
                    w.Markup(",").Space();
            }

            w.Markup("]");
        }


        private void writeAssign(Exp to, Exp e)
        {
            Write(to).Space().Op("=").Space();
            Write(e);
        }


        private void writeDeclAssing(Exp to, Exp e)
        {
            Write(to);
            if (e != Void.Instance)
            {
                w.Space().Op("=").Space();
                Write(e);
            }
        }
    }
}