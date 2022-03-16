using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Drawing;
using MetroFramework.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI
{
    public partial class VerticalLine : UserControl, IMetroControl
    {
        public MetroColorStyle Style { get; set; }
        public MetroThemeStyle Theme { get; set; }
        public MetroStyleManager StyleManager { get; set; }
        private readonly SolidBrush p_brush = new(Color.Black);

        public VerticalLine()
        {
            InitializeComponent();
            Paint += LineSeparator_Paint;
            // ReSharper disable once RedundantBaseQualifier
            base.MaximumSize = new Size(2, 2000);
            // ReSharper disable once RedundantBaseQualifier
            base.MinimumSize = new Size(2, 2);
            Height = 350;
        }

        private void LineSeparator_Paint(object _sender, PaintEventArgs _e)
        {
            p_brush.Color = MetroPaint.GetStyleColor(Style);
            _e.Graphics.FillRectangle(p_brush, -1, -1, Width + 1, Height + 1);
        }
    }
}