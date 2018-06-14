using System.Collections.Generic;
using System.Windows.Forms;

namespace Elab
{
    public static class UiUtils
    {
        public static IEnumerable<TreeNode> Flatten(this TreeNode root)
        {
            yield return root;

            foreach (TreeNode node in root.Nodes)
            foreach (var item in Flatten(node))
                yield return item;
        }
    }
}
