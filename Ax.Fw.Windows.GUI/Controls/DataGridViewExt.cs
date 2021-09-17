using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI
{
    public sealed partial class DataGridViewExt : DataGridView
    {
        public DataGridViewExt()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }
    }
}