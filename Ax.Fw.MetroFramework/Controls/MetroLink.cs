using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Data;
using Ax.Fw.MetroFramework.Designers;
using Ax.Fw.SharedTypes.Interfaces;
using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls;

[DefaultEvent("Click")]
[ToolboxBitmap(typeof(LinkLabel))]
[Designer(typeof(MetroLinkDesigner))]
public class MetroLink : Button
{
  private readonly ILifetime p_lifetime = new Lifetime();
  private MetroLinkSize p_metroLinkSize = MetroLinkSize.Medium;
  private MetroLinkWeight p_metroLinkWeight = MetroLinkWeight.Bold;
  private Color? p_overridePrimaryColor;
  private bool p_isHovered;
  private bool p_isPressed;
  private bool p_isFocused;

  public MetroLink()
  {
    SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
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

  public bool IsFocused => p_isFocused;

  [Category("Metro Appearance")]
  public MetroLinkSize FontSize
  {
    get
    {
      return p_metroLinkSize;
    }
    set
    {
      p_metroLinkSize = value;
      Invalidate();
    }
  }

  [Category("Metro Appearance")]
  public MetroLinkWeight FontWeight
  {
    get
    {
      return p_metroLinkWeight;
    }
    set
    {
      p_metroLinkWeight = value;
      Invalidate();
    }
  }

  [Category("Metro Appearance")]
  public Color? OverridePrimaryColor
  {
    get
    {
      return p_overridePrimaryColor;
    }
    set
    {
      p_overridePrimaryColor = value;
      Invalidate(true);
    }
  }

  protected override void OnPaint(PaintEventArgs _e)
  {
    var foreColor = p_overridePrimaryColor ?? StyleManager.Current.PrimaryColor;
    if (p_isHovered && !p_isPressed && Enabled)
      foreColor = StyleManager.Current.GetHoverColor(foreColor);
    else if (p_isHovered && p_isPressed && Enabled)
      foreColor = StyleManager.Current.GetHoverColor(foreColor);
    else if (!Enabled)
      foreColor = StyleManager.Current.GetDisabledColor(foreColor);

    _e.Graphics.Clear(StyleManager.Current.BackColor);
    TextRenderer.DrawText(
        _e.Graphics,
        Text,
        StyleManager.Current.GetLinkFont(p_metroLinkSize, p_metroLinkWeight),
        ClientRectangle,
        foreColor,
        StyleManager.Current.BackColor,
        StyleManager.Current.GetTextFormatFlags(TextAlign));
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
