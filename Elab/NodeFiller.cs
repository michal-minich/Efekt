using System.Windows.Forms;
using Efekt;

namespace Elab
{
    internal static class NodeFiller
    {
        internal static ImageList TreeViewImages { get; set; }

        public static void fill(TreeNode node, Element el)
        {
            switch (el)
            {

                case ArrConstructor arrC:
                    var nArrC = node.createNode("Array", nameof(ArrConstructor));
                    foreach (var a in arrC.Arguments)
                    {
                        fill(nArrC, a);
                    }

                    break;

                case Assign assign:
                    var assignText = assign.IsSimple() ? assign.ToCodeString() : assign.To.ToCodeString();
                    var nAssign = node.createNode(assignText, nameof(Assign));
                    fill(nAssign, assign.To);
                    fill(nAssign, assign.Exp);
                    break;

                case Attempt att:
                    var nAtt = node.createNode("try");
                    if (att.Grab != null)
                    {
                        var nAttGrab = nAtt.createNode("catch");
                        foreach (var g in att.Grab)
                        {
                            fill(nAttGrab, g);
                        }
                    }

                    if (att.AtLast != null)
                    {
                        var nAttAtLast = nAtt.createNode("finally");
                        foreach (var al in att.AtLast)
                        {
                            fill(nAttAtLast, al);
                        }
                    }

                    break;

                case Bool b:
                    var val = b.Value ? "true" : "false";
                    node.createNode(val, val);
                    break;

                case Break _:
                    node.createNode("break", nameof(Break));
                    break;

                case Continue _:
                    node.createNode("continue", nameof(Continue));
                    break;

                case Builtin builtin:
                    node.createNode(builtin.Name);
                    break;

                case Fn fn:
                    var nFn = node.createNode("fn", nameof(Fn));
                    if (fn.Parameters.Count != 0)
                    {
                        var nFnParams = nFn;//nFn.createNode("Parameters");
                        foreach (var p in fn.Parameters)
                        {
                            fill(nFnParams, p);
                        }
                    }
                    //var nFnSeq = nFn.createNode("Body");
                    fill(nFn, fn.Sequence);
                    break;

                case FnApply fna:
                    var fnaText = fna.IsSimple() ? fna.ToCodeString() : "Apply";
                    var nFna = node.createNode(fnaText, nameof(FnApply));
                    //var nFnaFn = fna.Fn.IsSimple() ? nFna : nFna.createNode("Function");
                    fill(nFna, fna.Fn);
                    if (fna.Arguments.Count != 0)
                    {
                        //var nFnaArgs = nFna.createNode("Arguments");
                        foreach (var fnaArg in fna.Arguments)
                        {
                            fill(nFna, fnaArg);
                        }
                    }
                    break;

                case Char c:
                    node.createNode("'" + c.Value + "'", nameof(Char));
                    break;

                case Ident ident:
                    node.createNode(ident.Name, nameof(Ident));
                    break;
                    
                case Import import:
                    var nImport = node.createNode(import.ToCodeString(), nameof(Import));
                    fill(nImport, import.QualifiedIdent);
                    break;

                case Int i:
                    node.createNode(i.Value.ToString(), nameof(Int));
                    break;

                case Text text:
                    node.createNode("\"" + text.Value + "\"", nameof(Text));
                    break;

                case Void _:
                    node.createNode("void", nameof(Void));
                    break;

                case Declr d:
                    var dText = d.IsSimple() ? d.ToCodeString() : d.Ident.Name;
                    var nLet = node.createNode(dText, d.GetType().Name);
                    if (d.Exp != null)
                        fill(nLet, d.Ident);
                    if (d.Exp != null)
                        fill(nLet, d.Exp);
                    break;


                case Loop loop:
                    var nLoop = node.createNode("loop");
                    foreach (var loopItem in loop.Body)
                    {
                        fill(nLoop, loopItem);
                    }

                    break;

                case MemberAccess ma:
                    var maText = ma.IsSimple() ? ma.ToCodeString() : "Member Access";
                    var nMa = node.createNode(maText, nameof(MemberAccess));
                    fill(nMa, ma.Exp);
                    fill(nMa, ma.Ident);
                    break;

                case New @new:
                    var nNew = @new.IsSimple() ? node : node.createNode("new", nameof(New));
                    foreach (var bodyItem in @new.Body)
                    {
                        fill(nNew, bodyItem);
                    }

                    break;

                case Return ret:
                    var retText = ret.Exp.IsSimple() ? ret.ToCodeString() : "return ...";
                    var nRet = node.createNode(retText, nameof(Return));
                    fill(nRet, ret.Exp);
                    break;

                case Sequence seq:
                    var nSeq = seq.Count == 1 ? node : node.createNode("Sequence", nameof(Sequence));
                    foreach (var si in seq)
                    {
                        fill(nSeq, si);
                    }

                    break;

                case Toss toss:
                    var nToss = node.createNode("throw", nameof(Toss));
                    fill(nToss, toss.Exception);
                    break;

                case When @when:
                    var whenText = when.IsSimple() ? when.ToCodeString() : (when.Test.IsSimple() ? "if " + when.Test.ToCodeString() + " ..." : "if");
                    var nWhen = node.createNode(whenText, nameof(When));
                    var nWhenTest = when.Test.IsSimple() ? nWhen : nWhen.createNode("Test");
                    fill(nWhenTest, when.Test);
                    var nWhenThen = when.Then.IsSimple() ? nWhen : nWhen.createNode("then");
                    fill(nWhenThen, when.Then);
                    if (when.Otherwise != null)
                    {
                        var nWhenOth = when.Otherwise.IsSimple() ? nWhen : nWhen.createNode("else");
                        fill(nWhenOth, when.Otherwise);
                    }

                    break;

                default:
                    node.createNode(el.ToCodeString());
                    break;
            }
        }


        private static TreeNode createNode(this TreeNode node, string text)
        {
            return node.createNode(text, "Empty");
        }

        
        private static TreeNode createNode(this TreeNode node, string text, string imageName)
        {
            var imageIndex = TreeViewImages.Images.IndexOfKey(imageName);
            var n = new TreeNode(text, imageIndex, imageIndex);
            node.Nodes.Add(n);
            return n;
        }
    }
}