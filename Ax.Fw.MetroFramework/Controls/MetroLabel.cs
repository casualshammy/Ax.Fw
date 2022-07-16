using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Data;
using Ax.Fw.MetroFramework.Designers;
using Ax.Fw.Windows.WinAPI;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Ax.Fw.MetroFramework.Controls
{
    [ToolboxBitmap(typeof(Label))]
    [Designer(typeof(MetroLabelDesigner))]
    public class MetroLabel : Label
    {
        private class DoubleBufferedTextBox : TextBox
        {
            public DoubleBufferedTextBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
            }
        }

        private readonly DoubleBufferedTextBox p_baseTextBox;
        private readonly Lifetime p_lifetime = new();
        private MetroLabelSize p_metroLabelSize = MetroLabelSize.Medium;
        private MetroLabelWeight p_metroLabelWeight;
        private MetroLabelMode p_labelMode;

        public MetroLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
            p_baseTextBox = new DoubleBufferedTextBox
            {
                Visible = false
            };
            Controls.Add(p_baseTextBox);
            StyleManager.Current.ColorsChanged
                .Subscribe(_ => BeginInvoke(() => Invalidate()), p_lifetime);
        }

        [Category("Metro Appearance")]
        public MetroLabelSize FontSize
        {
            get
            {
                return p_metroLabelSize;
            }
            set
            {
                p_metroLabelSize = value;
                Refresh();
            }
        }

        [Category("Metro Appearance")]
        public MetroLabelWeight FontWeight
        {
            get
            {
                return p_metroLabelWeight;
            }
            set
            {
                p_metroLabelWeight = value;
                Refresh();
            }
        }

        [Category("Metro Appearance")]
        public MetroLabelMode LabelMode
        {
            get
            {
                return p_labelMode;
            }
            set
            {
                p_labelMode = value;
                Refresh();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(StyleManager.Current.BackColor);
            if (LabelMode == MetroLabelMode.Selectable)
            {
                CreateBaseTextBox();
                UpdateBaseTextBox();
            }
            else
            {
                DestroyBaseTextbox();
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    StyleManager.Current.GetLabelFont(p_metroLabelSize, p_metroLabelWeight),
                    ClientRectangle,
                    StyleManager.Current.PrimaryColor,
                    StyleManager.Current.BackColor,
                    StyleManager.Current.GetTextFormatFlags(TextAlign));
            }
        }

        public override void Refresh()
        {
            if (LabelMode == MetroLabelMode.Selectable)
            {
                UpdateBaseTextBox();
            }

            base.Refresh();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            base.GetPreferredSize(proposedSize);
            using (Graphics dc = CreateGraphics())
            {
                proposedSize = new Size(int.MaxValue, int.MaxValue);
                return TextRenderer.MeasureText(
                    dc, 
                    Text, 
                    StyleManager.Current.GetLabelFont(p_metroLabelSize, p_metroLabelWeight), 
                    proposedSize, 
                    StyleManager.Current.GetTextFormatFlags(TextAlign));
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            if (LabelMode == MetroLabelMode.Selectable)
            {
                HideBaseTextBox();
            }

            base.OnResize(e);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (LabelMode == MetroLabelMode.Selectable)
            {
                ShowBaseTextBox();
            }
        }

        protected override void Dispose(bool _disposing)
        {
            base.Dispose(_disposing);
            p_lifetime.Complete();
        }

        private void CreateBaseTextBox()
        {
            if (!p_baseTextBox.Visible)
            {
                p_baseTextBox.Visible = true;
                p_baseTextBox.BorderStyle = BorderStyle.None;
                p_baseTextBox.Font = StyleManager.Current.GetLabelFont(p_metroLabelSize, p_metroLabelWeight);
                p_baseTextBox.Location = new Point(1, 0);
                p_baseTextBox.Text = Text;
                p_baseTextBox.ReadOnly = true;
                p_baseTextBox.Size = GetPreferredSize(Size.Empty);
                p_baseTextBox.Multiline = true;
                p_baseTextBox.DoubleClick += BaseTextBoxOnDoubleClick;
                p_baseTextBox.Click += BaseTextBoxOnClick;
                Controls.Add(p_baseTextBox);
            }
        }

        private void DestroyBaseTextbox()
        {
            if (p_baseTextBox.Visible)
            {
                p_baseTextBox.DoubleClick -= BaseTextBoxOnDoubleClick;
                p_baseTextBox.Click -= BaseTextBoxOnClick;
                p_baseTextBox.Visible = false;
            }
        }

        private void UpdateBaseTextBox()
        {
            if (!p_baseTextBox.Visible)
                return;

            p_baseTextBox.SuspendLayout();
            p_baseTextBox.BackColor = StyleManager.Current.BackColor;
            p_baseTextBox.ForeColor = StyleManager.Current.PrimaryColor;

            p_baseTextBox.Font = StyleManager.Current.GetLabelFont(p_metroLabelSize, p_metroLabelWeight);
            p_baseTextBox.Text = Text;
            p_baseTextBox.BorderStyle = BorderStyle.None;
            Size = GetPreferredSize(Size.Empty);
            p_baseTextBox.ResumeLayout();
        }

        private void HideBaseTextBox()
        {
            p_baseTextBox.Visible = false;
        }

        private void ShowBaseTextBox()
        {
            p_baseTextBox.Visible = true;
        }

        private void BaseTextBoxOnClick(object? sender, EventArgs eventArgs)
        {
            NativeMethods.HideCaret(p_baseTextBox.Handle);
        }

        private void BaseTextBoxOnDoubleClick(object? sender, EventArgs eventArgs)
        {
            p_baseTextBox.SelectAll();
            NativeMethods.HideCaret(p_baseTextBox.Handle);
        }

    }
}
