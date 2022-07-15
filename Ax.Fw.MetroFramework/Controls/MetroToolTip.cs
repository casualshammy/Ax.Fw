using Ax.Fw.MetroFramework;
using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls;

[ToolboxBitmap(typeof(ToolTip))]
public class MetroToolTip : ToolTip
{
    public MetroToolTip()
    {
        OwnerDraw = true;
        ShowAlways = true;
        Draw += MetroToolTip_Draw;
        Popup += MetroToolTip_Popup;
    }

    [Browsable(false)]
    [DefaultValue(true)]
    public new bool ShowAlways
    {
        get
        {
            return base.ShowAlways;
        }
        set
        {
            base.ShowAlways = true;
        }
    }

    [DefaultValue(true)]
    [Browsable(false)]
    public new bool OwnerDraw
    {
        get
        {
            return base.OwnerDraw;
        }
        set
        {
            base.OwnerDraw = true;
        }
    }

    [Browsable(false)]
    public new bool IsBalloon
    {
        get
        {
            return base.IsBalloon;
        }
        set
        {
            base.IsBalloon = false;
        }
    }

    [Browsable(false)]
    public new Color BackColor
    {
        get
        {
            return base.BackColor;
        }
        set
        {
            base.BackColor = value;
        }
    }

    [Browsable(false)]
    public new Color ForeColor
    {
        get
        {
            return base.ForeColor;
        }
        set
        {
            base.ForeColor = value;
        }
    }

    [Browsable(false)]
    public new string ToolTipTitle
    {
        get
        {
            return base.ToolTipTitle;
        }
        set
        {
            base.ToolTipTitle = "";
        }
    }

    [Browsable(false)]
    public new ToolTipIcon ToolTipIcon
    {
        get
        {
            return base.ToolTipIcon;
        }
        set
        {
            base.ToolTipIcon = ToolTipIcon.None;
        }
    }

    public new void SetToolTip(Control control, string caption)
    {
        base.SetToolTip(control, caption);

        foreach (Control control2 in control.Controls)
        {
            SetToolTip(control2, caption);
        }
    }

    private void MetroToolTip_Popup(object? sender, PopupEventArgs e)
    {
        e.ToolTipSize = new Size(e.ToolTipSize.Width + 24, e.ToolTipSize.Height + 9);
    }

    private void MetroToolTip_Draw(object? sender, DrawToolTipEventArgs e)
    {
        using (SolidBrush brush = new SolidBrush(StyleManager.Current.BackColor))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        using (Pen pen = new Pen(StyleManager.Current.PrimaryColor))
        {
            e.Graphics.DrawRectangle(pen, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));
        }

        var font = StyleManager.Current.DefaultFont(13f);
        TextRenderer.DrawText(e.Graphics, e.ToolTipText, font, e.Bounds, StyleManager.Current.PrimaryColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

}
