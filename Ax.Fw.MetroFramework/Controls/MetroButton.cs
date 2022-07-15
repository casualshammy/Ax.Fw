using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Designers;
using Ax.Fw.SharedTypes.Interfaces;
using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls;

[ToolboxBitmap(typeof(Button))]
[DefaultEvent("Click")]
[Designer(typeof(MetroButtonDesigner))]
public class MetroButton : Button
{
    private readonly ILifetime p_lifetime = new Lifetime();
    private bool p_highlight;
    private bool p_isHovered;
    private bool p_isPressed;
    private bool p_isFocused;

    public MetroButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
        StyleManager.Current.ColorsChanged
            .Subscribe(_ => Invalidate(), p_lifetime);
    }

    public bool Highlight
    {
        get
        {
            return p_highlight;
        }
        set
        {
            p_highlight = value;
        }
    }

    protected override void OnPaint(PaintEventArgs _e)
    {
        var backColor = StyleManager.Current.GetHoverColor(StyleManager.Current.BackColor, 1f);

        if (p_isHovered && !p_isPressed && Enabled)
            backColor = StyleManager.Current.GetHoverColor(StyleManager.Current.BackColor, 3f);
        else if (p_isHovered && p_isPressed && Enabled)
            backColor = StyleManager.Current.GetHoverColor(StyleManager.Current.BackColor, 5f);
        else if (!Enabled)
            backColor = StyleManager.Current.GetHoverColor(StyleManager.Current.BackColor, 3f);

        _e.Graphics.Clear(backColor);

        if (!p_isHovered && !p_isPressed && Enabled)
        {
            using (var pen = new Pen(StyleManager.Current.PrimaryColor, 3f))
            {
                var rect = new Rectangle(1, 1, Width - 2, Height - 2);
                _e.Graphics.DrawRectangle(pen, rect);
            }
        }

        var textColor = p_isPressed ? StyleManager.Current.GetHoverColor(StyleManager.Current.BackColor, 1f) : StyleManager.Current.PrimaryColor;
        TextRenderer.DrawText(_e.Graphics, Text, StyleManager.Current.DefaultFontBold(11f), ClientRectangle, textColor, backColor, StyleManager.Current.GetTextFormatFlags(TextAlign));
    }

    protected override void OnGotFocus(EventArgs _e)
    {
        p_isFocused = true;
        Invalidate();
        base.OnGotFocus(_e);
    }

    protected override void OnLostFocus(EventArgs _e)
    {
        p_isFocused = false;
        p_isHovered = false;
        p_isPressed = false;
        Invalidate();
        base.OnLostFocus(_e);
    }

    protected override void OnEnter(EventArgs _e)
    {
        p_isFocused = true;
        Invalidate();
        base.OnEnter(_e);
    }

    protected override void OnLeave(EventArgs _e)
    {
        p_isFocused = false;
        p_isHovered = false;
        p_isPressed = false;
        Invalidate();
        base.OnLeave(_e);
    }

    protected override void OnKeyDown(KeyEventArgs _e)
    {
        if (_e.KeyCode == Keys.Space)
        {
            p_isHovered = true;
            p_isPressed = true;
            Invalidate();
        }

        base.OnKeyDown(_e);
    }

    protected override void OnKeyUp(KeyEventArgs _e)
    {
        p_isHovered = false;
        p_isPressed = false;
        Invalidate();
        base.OnKeyUp(_e);
    }

    protected override void OnMouseEnter(EventArgs _e)
    {
        p_isHovered = true;
        Invalidate();
        base.OnMouseEnter(_e);
    }

    protected override void OnMouseDown(MouseEventArgs _e)
    {
        if (_e.Button == MouseButtons.Left)
        {
            p_isPressed = true;
            Invalidate();
        }

        base.OnMouseDown(_e);
    }

    protected override void OnMouseUp(MouseEventArgs _e)
    {
        p_isPressed = false;
        Invalidate();
        base.OnMouseUp(_e);
    }

    protected override void OnMouseLeave(EventArgs _e)
    {
        p_isHovered = false;
        Invalidate();
        base.OnMouseLeave(_e);
    }

    protected override void OnEnabledChanged(EventArgs _e)
    {
        base.OnEnabledChanged(_e);
        Invalidate();
    }

    protected override void Dispose(bool _disposing)
    {
        base.Dispose(_disposing);
        p_lifetime.Complete();
    }

}
