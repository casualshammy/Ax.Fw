using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI.Forms
{
    public class MetroFormButton : Button, IMetroControl
    {
        private MetroColorStyle metroStyle = MetroColorStyle.Blue;

        private MetroThemeStyle metroTheme;

        private MetroStyleManager metroStyleManager;

        private bool isHovered;

        private bool isPressed;

        [Category("Metro Appearance")]
        public MetroColorStyle Style
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Style;
                }

                return metroStyle;
            }
            set
            {
                metroStyle = value;
            }
        }

        [Category("Metro Appearance")]
        public MetroThemeStyle Theme
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Theme;
                }

                return metroTheme;
            }
            set
            {
                metroTheme = value;
            }
        }

        [Browsable(false)]
        public MetroStyleManager StyleManager
        {
            get
            {
                return metroStyleManager;
            }
            set
            {
                metroStyleManager = value;
            }
        }

        public MetroFormButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color color = (base.Parent == null) ? MetroPaint.BackColor.Form(Theme) : ((base.Parent is IMetroForm) ? MetroPaint.BackColor.Form(Theme) : ((!(base.Parent is IMetroControl)) ? base.Parent.BackColor : MetroPaint.GetStyleColor(Style)));
            Color foreColor;
            if (isHovered && !isPressed && base.Enabled)
            {
                foreColor = MetroPaint.ForeColor.Button.Normal(Theme);
                color = MetroPaint.BackColor.Button.Normal(Theme);
            }
            else if (isHovered && isPressed && base.Enabled)
            {
                foreColor = MetroPaint.ForeColor.Button.Press(Theme);
                color = MetroPaint.GetStyleColor(Style);
            }
            else if (!base.Enabled)
            {
                foreColor = MetroPaint.ForeColor.Button.Disabled(Theme);
                color = MetroPaint.BackColor.Button.Disabled(Theme);
            }
            else
            {
                foreColor = MetroPaint.ForeColor.Button.Normal(Theme);
            }

            e.Graphics.Clear(color);
            TextRenderer.DrawText(e.Graphics, Text, Control.DefaultFont, base.ClientRectangle, foreColor, color, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }
    }

}
