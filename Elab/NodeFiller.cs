using System.Windows.Forms;
using Efekt;

namespace Elab
{
    internal static class NodeFiller
    {
        public static void fill(TreeNode node, Element el)
        {
            switch (el)
            {

                case ArrConstructor arrC:
                    var nArrC = node.Nodes.Add("Array");
                    foreach (var a in arrC.Arguments)
                    {
                        fill(nArrC, a);
                    }

                    break;

                case Assign assign:
                    var nAssign = node.Nodes.Add("=");
                    fill(nAssign, assign.To);
                    fill(nAssign, assign.Exp);
                    break;

                case Attempt att:
                    var nAtt = node.Nodes.Add("Try");
                    if (att.Grab != null)
                    {
                        var nAttGrab = nAtt.Nodes.Add("Catch");
                        foreach (var g in att.Grab)
                        {
                            fill(nAttGrab, g);
                        }
                    }

                    if (att.AtLast != null)
                    {
                        var nAttAtLast = nAtt.Nodes.Add("Finally");
                        foreach (var al in att.AtLast)
                        {
                            fill(nAttAtLast, al);
                        }
                    }

                    break;

                case Bool b:
                    node.Nodes.Add(b.Value ? "True" : "False");
                    break;

                case Break _:
                    node.Nodes.Add("Break");
                    break;

                case Continue _:
                    node.Nodes.Add("Continue");
                    break;

                case Builtin builtin:
                    node.Nodes.Add(builtin.Name);
                    break;

                case Fn fn:
                    var nFn = node.Nodes.Add("Function");
                    var nFnParams = nFn.Nodes.Add("Parameters");
                    foreach (var p in fn.Parameters)
                    {
                        fill(nFnParams, p);
                    }

                    var nFnSeq = nFn.Nodes.Add("Body");
                    fill(nFnSeq, fn.Sequence);
                    break;

                case FnApply fna:
                    var nFna = node.Nodes.Add("Apply");
                    var nFnaFn = nFna.Nodes.Add("Function");
                    fill(nFnaFn, fna.Fn);
                    var nFnaArgs = nFna.Nodes.Add("Arguments");
                    foreach (var fnaArg in fna.Arguments)
                    {
                        fill(nFnaArgs, fnaArg);
                    }

                    break;

                case Char c:
                    node.Nodes.Add("'" + c.Value + "'");
                    break;

                case Ident ident:
                    node.Nodes.Add(ident.Name);
                    break;

                case Import import:
                    var nImport = node.Nodes.Add("Import");
                    fill(nImport, import.QualifiedIdent);
                    break;

                case Int i:
                    node.Nodes.Add(i.Value.ToString());
                    break;

                case Let let:
                    var nLet = node.Nodes.Add("Let");
                    fill(nLet, let.Ident);
                    if (let.Exp != null)
                        fill(nLet, let.Exp);
                    break;

                case Var var:
                    var nVar = node.Nodes.Add("Let");
                    fill(nVar, var.Ident);
                    if (var.Exp != null)
                        fill(nVar, var.Exp);
                    break;

                case Param param:
                    node.Nodes.Add(param.Ident.Name);
                    break;

                case Loop loop:
                    var nLoop = node.Nodes.Add("Loop");
                    foreach (var loopItem in loop.Body)
                    {
                        fill(nLoop, loopItem);
                    }

                    break;

                case MemberAccess ma:
                    var nMa = node.Nodes.Add("Member Access");
                    fill(nMa, ma.Exp);
                    fill(nMa, ma.Ident);
                    break;

                case New @new:
                    var nNew = node.Nodes.Add("New");
                    foreach (var bodyItem in @new.Body)
                    {
                        fill(nNew, bodyItem);
                    }

                    break;

                case Return ret:
                    var nRet = node.Nodes.Add("Return");
                    fill(nRet, ret.Exp);
                    break;

                case Sequence seq:
                    var nSeq = node.Nodes.Add("Sequence");
                    foreach (var si in seq)
                    {
                        fill(nSeq, si);
                    }

                    break;

                case Text text:
                    node.Nodes.Add("\"" + text.Value + "\"");
                    break;

                case Toss toss:
                    var nToss = node.Nodes.Add("Throw");
                    fill(nToss, toss.Exception);
                    break;

                case Void _:
                    node.Nodes.Add("Void");
                    break;

                case When @when:
                    var nWhen = node.Nodes.Add("If");
                    var nWhenTest = nWhen.Nodes.Add("Test");
                    fill(nWhenTest, when.Test);
                    var nWhenThen = nWhen.Nodes.Add("Then");
                    fill(nWhenThen, when.Then);
                    if (when.Otherwise != null)
                    {
                        var nWhenOth = nWhen.Nodes.Add("Else");
                        fill(nWhenOth, when.Otherwise);
                    }

                    break;

                default:
                    node.Nodes.Add(el.ToDebugString());
                    break;
            }
        }
    }
}