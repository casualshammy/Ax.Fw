﻿using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Drawing;
using MetroFramework.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI
{
    public partial class Line : UserControl, IMetroControl
    {
        public MetroColorStyle Style { get; set; }
        public MetroThemeStyle Theme { get; set; }
        public MetroStyleManager StyleManager { get; set; }
        private readonly Pen pen = new Pen(Color.Black, 2f);

        public Line()
        {
            InitializeComponent();
            Paint += LineSeparator_Paint;
            // ReSharper disable once RedundantBaseQualifier
            base.MaximumSize = new Size(2000, 2);
            // ReSharper disable once RedundantBaseQualifier
            base.MinimumSize = new Size(0, 2);
            Width = 350;
        }

        private void LineSeparator_Paint(object sender, PaintEventArgs e)
        {
            pen.Color = MetroPaint.GetStyleColor(Style);
            e.Graphics.DrawLine(pen, new Point(-1, 0), new Point(Width, 1));
        }
    }
}