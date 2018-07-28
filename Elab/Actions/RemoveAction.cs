using System.Windows.Forms;
using Efekt;

namespace Elab.Actions
{
    public class RemoveAction : IElabAction
    {
        private readonly Element element;
        private readonly TreeNode node;
        public string Name => "Remove";


        public RemoveAction(Element element, TreeNode node)
        {
            this.element = element;
            this.node = node;
        }

        public bool CanIvoke()
        {
            return element.Parent is Sequence;
        }


        public void Invoke()
        {
            var p = (Sequence)element.Parent;
            var el = (SequenceItem)element;
            p.Remove(el);
            node.Remove();
        }
    }
}