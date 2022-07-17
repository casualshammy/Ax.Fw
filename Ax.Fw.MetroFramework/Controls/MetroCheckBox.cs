using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Data;
using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls;

[ToolboxBitmap(typeof(CheckBox))]
public class MetroCheckBox : CheckBox
{
    private readonly Lifetime p_lifetime = new();

    private MetroLinkSize metroLinkSize;

    private MetroLinkWeight metroLinkWeight = MetroLinkWeight.Regular;

    private bool isHovered;

    private bool isPressed;

    private bool isFocused;

    public MetroCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
        StyleManager.Current.ColorsChanged
            .Subscribe(_ =>
            {
                try
                {
                    BeginInvoke(() => Invalidate(true));
                }
                catch { }
            }, p_lifetime);
    }

    [Category("Metro Appearance")]
    public MetroLinkSize FontSize
    {
        get
        {
            return metroLinkSize;
        }
        set
        {
            metroLinkSize = value;
        }
    }

    [Category("Metro Appearance")]
    public MetroLinkWeight FontWeight
    {
        get
        {
            return metroLinkWeight;
        }
        set
        {
            metroLinkWeight = value;
        }
    }

    [Browsable(false)]
    public override Font Font
    {
        get
        {
            return base.Font;
        }
        set
        {
            base.Font = value;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var foreColor = StyleManager.Current.PrimaryColor;
        var backColor = StyleManager.Current.BackColor;

        if (isHovered && !isPressed && Enabled)
            foreColor = StyleManager.Current.GetHoverColor(foreColor);
        else if (isHovered && isPressed && Enabled)
            foreColor = StyleManager.Current.GetHoverColor(foreColor, 1f);
        else if (!Enabled)
            foreColor = StyleManager.Current.GetDisabledColor(foreColor);

        e.Graphics.Clear(backColor);
        using (var pen = new Pen(foreColor))
        {
            var rect = new Rectangle(0, Height / 2 - 6, 12, 12);
            e.Graphics.DrawRectangle(pen, rect);
        }

        if (Checked)
            using (var brush = new SolidBrush(StyleManager.Current.PrimaryColor))
            {
                Rectangle rect2 = new Rectangle(2, Height / 2 - 4, 9, 9);
                e.Graphics.FillRectangle(brush, rect2);
            }

        TextRenderer.DrawText(
            bounds: new Rectangle(16, 0, Width - 16, Height),
            dc: e.Graphics,
            text: Text,
            font: StyleManager.Current.GetLinkFont(metroLinkSize, metroLinkWeight),
            foreColor: foreColor,
            backColor: backColor,
            flags: StyleManager.Current.GetTextFormatFlags(TextAlign));
    }

    protected override void OnGotFocus(EventArgs e)
    {
        isFocused = true;
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        isFocused = false;
        isHovered = false;
        isPressed = false;
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnEnter(EventArgs e)
    {
        isFocused = true;
        Invalidate();
        base.OnEnter(e);
    }

    protected override void OnLeave(EventArgs e)
    {
        isFocused = false;
        isHovered = false;
        isPressed = false;
        Invalidate();
        base.OnLeave(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            isHovered = true;
            isPressed = true;
            Invalidate();
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        isHovered = false;
        isPressed = false;
        Invalidate();
        base.OnKeyUp(e);
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

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        base.GetPreferredSize(proposedSize);
        using (Graphics dc = CreateGraphics())
        {
            proposedSize = new Size(int.MaxValue, int.MaxValue);
            Size result = TextRenderer.MeasureText(dc, Text, StyleManager.Current.GetLinkFont(metroLinkSize, metroLinkWeight), proposedSize, StyleManager.Current.GetTextFormatFlags(TextAlign));
            result.Width += 16;
            return result;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        p_lifetime?.Complete();
    }

}
