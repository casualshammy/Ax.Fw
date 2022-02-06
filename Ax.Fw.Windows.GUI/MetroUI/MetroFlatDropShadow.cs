using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI.Forms
{
    public class MetroFlatDropShadow : Form
    {
        private const int WS_EX_TRANSPARENT = 32;

        private const int WS_EX_NOACTIVATE = 134217728;

        private Form shadowTargetForm;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle = (createParams.ExStyle | 0x20 | 0x8000000);
                return createParams;
            }
        }

        public MetroFlatDropShadow(Form targetForm)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
            shadowTargetForm = targetForm;
            shadowTargetForm.Activated += shadowTargetForm_Activated;
            shadowTargetForm.ResizeBegin += shadowTargetForm_ResizeBegin;
            shadowTargetForm.ResizeEnd += shadowTargetForm_ResizeEnd;
            shadowTargetForm.VisibleChanged += shadowTargetForm_VisibleChanged;
            base.Opacity = 0.2;
            base.ShowInTaskbar = false;
            base.ShowIcon = false;
            base.FormBorderStyle = FormBorderStyle.None;
            base.StartPosition = shadowTargetForm.StartPosition;
            if (shadowTargetForm.Owner != null)
            {
                base.Owner = shadowTargetForm.Owner;
            }

            shadowTargetForm.Owner = this;
        }

        private void shadowTargetForm_VisibleChanged(object sender, EventArgs e)
        {
            base.Visible = shadowTargetForm.Visible;
        }

        private void shadowTargetForm_Activated(object sender, EventArgs e)
        {
            base.Bounds = new Rectangle(shadowTargetForm.Location.X - 5, shadowTargetForm.Location.Y - 5, shadowTargetForm.Width + 10, shadowTargetForm.Height + 10);
            base.Visible = (shadowTargetForm.WindowState == FormWindowState.Normal);
            if (base.Visible)
            {
                Show();
            }
        }

        private void shadowTargetForm_ResizeBegin(object sender, EventArgs e)
        {
            base.Visible = false;
            Hide();
        }

        private void shadowTargetForm_ResizeEnd(object sender, EventArgs e)
        {
            base.Bounds = new Rectangle(shadowTargetForm.Location.X - 5, shadowTargetForm.Location.Y - 5, shadowTargetForm.Width + 10, shadowTargetForm.Height + 10);
            base.Visible = (shadowTargetForm.WindowState == FormWindowState.Normal);
            if (base.Visible)
            {
                Show();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Gray);
            using (Brush brush = new SolidBrush(Color.Black))
            {
                e.Graphics.FillRectangle(brush, new Rectangle(4, 4, base.ClientRectangle.Width - 8, base.ClientRectangle.Height - 8));
            }
        }
    }

}
