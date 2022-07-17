using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.MetroFramework.Controls;

internal class FormButton : Button
{
    private readonly ILifetime p_lifetime = new Lifetime();
    private bool p_isHovered;
    private bool p_isPressed;
    private Color p_hoveredColor;

    public FormButton()
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

    protected override void OnPaint(PaintEventArgs _e)
    {
        var backColor = StyleManager.Current.BackColor;
        var foreColor = StyleManager.Current.PrimaryColor;
        if (p_isHovered && !p_isPressed && Enabled)
        {
            backColor = p_hoveredColor;
        }
        else if (p_isHovered && p_isPressed && Enabled)
        {
            backColor = StyleManager.Current.PrimaryColor;
            foreColor = StyleManager.Current.BackColor;
        }

        _e.Graphics.Clear(backColor);
        TextRenderer.DrawText(
            _e.Graphics,
            Text,
            DefaultFont,
            ClientRectangle,
            foreColor,
            backColor,
            TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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

    protected override void Dispose(bool _disposing)
    {
        base.Dispose(_disposing);
        p_lifetime.Complete();
    }

    private void RecalculateColors() => p_hoveredColor = StyleManager.Current.GetHoverColor(StyleManager.Current.BackColor, 1f);

}
