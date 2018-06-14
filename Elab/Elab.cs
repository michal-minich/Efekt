using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Efekt;

namespace Elab
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            var args = @"..\..\..\Efekt\lib\ ..\..\..\Efekt\test.ef".Split(' ');
            var cw = new ConsoleWriter();
            var prog = Prog.Load2(cw, cw, args, true);

            //TreeViewImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TreeViewImages.ImageStream")));

            //TreeViewImages.TransparentColor = System.Drawing.Color.Transparent;
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
    }
}
