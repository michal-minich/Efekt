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
        }
    }
}
