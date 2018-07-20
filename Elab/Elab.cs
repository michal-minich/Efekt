using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Efekt;

namespace Elab
{
    public partial class MainForm : Form
    {
        private readonly Prog prog;

        public MainForm()
        {
            InitializeComponent();

            var args = @"..\..\..\Efekt\lib\ ..\..\..\Efekt\test.ef".Split(' ');
            var cw = new ConsoleWriter();
            prog = Prog.Load2(cw, cw, args, true);
            
            NodeFiller.TreeViewImages = new ImageList();

            addImage(@"..\..\Images\Empty.png");
            foreach (var f in Directory.GetFiles(@"..\..\Images\"))
            {
                if (f.EndsWith("Empty.png"))
                    continue;
                addImage(f);
            }

            MainTree.ImageList = NodeFiller.TreeViewImages;

            var rootNode = MainTree.Nodes.Add("Program");
            rootNode.Tag = prog.RootElement;

            NodeFiller.fill(rootNode, prog.RootElement);

            foreach (var n in rootNode.Flatten())
            {
                if (n.Text == "new")
                    break;
                n.Expand();
            }

            //rootNode.ExpandAll();
        }


        private static void addImage(string filePath)
        {
            var key = Path.GetFileNameWithoutExtension(filePath);
            NodeFiller.TreeViewImages.Images.Add(key, Image.FromFile(filePath));
        }


        private void MainTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var el = (Element) e.Node.Tag;
            
            Spec type;
            switch (el)
            {
                case Exp exp:
                    type = exp.Spec;
                    break;
                case Declr d:
                    type = d.Spec;
                    break;
                default:
                    type = null;
                    break;
            }

            if (type == null)
            {
                switch (el)
                {
                    case Exp exp:
                        TypePicture.Image = null;
                        TypeNameLabel.Text = "Expression";
                        break;
                    default:
                        TypePicture.Visible = false;
                        TypeNameLabel.Visible = false;
                        TypeLabel.Visible = false;
                        break;
                }
            }
            else
            {
                TypePicture.Image = MainTree.ImageList.Images[type.GetType().Name];
                TypeNameLabel.Text = type.ToCodeString();
                TypePicture.Visible = true;
                TypeNameLabel.Visible = true;
                TypeLabel.Visible = true;
            }

            var expName  = el.GetType().Name;
            ExpressionPicture.Image = MainTree.ImageList.Images[expName];
            ExpressionNameLabel.Text = ElementNamer.Name(el);

            CodeTextBox.Text = el.ToCodeString();
        }
    }
}
