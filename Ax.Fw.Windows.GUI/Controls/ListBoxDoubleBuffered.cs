using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI
{
    public sealed class ListViewDoubleBuffered : ListView
    {
        public ListViewDoubleBuffered()
        {
            DoubleBuffered = true;
        }
    }
}