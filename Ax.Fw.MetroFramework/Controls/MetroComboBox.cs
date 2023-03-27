using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Data;
using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls;

[ToolboxBitmap(typeof(ComboBox))]
public class MetroComboBox : ComboBox
{
  private readonly Lifetime p_lifetime = new();
  private MetroLinkSize p_metroLinkSize = MetroLinkSize.Medium;
  private MetroLinkWeight p_metroLinkWeight = MetroLinkWeight.Regular;
  private string p_overlayText = "";
  private bool p_isHovered;
  private bool p_isPressed;
  private bool p_isFocused;

  public MetroComboBox()
  {
    SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
    base.DrawMode = DrawMode.OwnerDrawFixed;
    base.DropDownStyle = ComboBoxStyle.DropDownList;
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
  public bool IsPressed => p_isPressed;
  public bool IsHovered => p_isHovered;

  [Browsable(false)]
  public new DrawMode DrawMode
  {
    get
    {
      return DrawMode.OwnerDrawFixed;
    }
    set
    {
      base.DrawMode = DrawMode.OwnerDrawFixed;
    }
  }

  [Browsable(false)]
  public new ComboBoxStyle DropDownStyle
  {
    get
    {
      return ComboBoxStyle.DropDownList;
    }
    set
    {
      base.DropDownStyle = ComboBoxStyle.DropDownList;
    }
  }

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
    }
  }

  [Browsable(false)]
  public override Color BackColor => StyleManager.Current.BackColor;

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

  [Browsable(false)]
  public override Color ForeColor
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

  public string OverlayText
  {
    get
    {
      return p_overlayText;
    }
    set
    {
      p_overlayText = value;
      Invalidate();
    }
  }

  protected override void OnPaint(PaintEventArgs e)
  {
    ItemHeight = GetPreferredSize(Size.Empty).Height;

    e.Graphics.Clear(StyleManager.Current.BackColor);
    using (var pen = new Pen(StyleManager.Current.PrimaryColor))
    {
      var rect = new Rectangle(0, 0, Width - 1, Height - 1);
      e.Graphics.DrawRectangle(pen, rect);
    }

    using (var brush = new SolidBrush(StyleManager.Current.PrimaryColor))
    {
      e.Graphics.FillPolygon(brush, new Point[3]
      {
                new Point(Width - 20, Height / 2 - 2),
                new Point(Width - 9, Height / 2 - 2),
                new Point(Width - 15, Height / 2 + 4)
      });
    }

    TextRenderer.DrawText(
        bounds: new Rectangle(2, 2, Width - 20, Height - 4),
        dc: e.Graphics,
        text: Text,
        font: StyleManager.Current.GetLinkFont(p_metroLinkSize, p_metroLinkWeight),
        foreColor: StyleManager.Current.PrimaryColor,
        backColor: StyleManager.Current.BackColor,
        flags: TextFormatFlags.VerticalCenter);

    if (!string.IsNullOrWhiteSpace(p_overlayText) && SelectedIndex == -1 && !DroppedDown)
    {
      var font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 10f, FontStyle.Italic);
      var size = TextRenderer.MeasureText(p_overlayText, font);
      var pt = new Point(2, Size.Height / 2 - size.Height / 2);
      using (var solidBrush = new SolidBrush(StyleManager.Current.PrimaryColor))
        TextRenderer.DrawText(e.Graphics, p_overlayText, font, pt, solidBrush.Color);
    }
  }

  protected override void OnDrawItem(DrawItemEventArgs e)
  {
    if (e.Index >= 0)
    {
      Color foreColor;
      Color backColor;
      if (e.State == (DrawItemState.NoAccelerator | DrawItemState.NoFocusRect) || e.State == DrawItemState.None)
      {
        foreColor = StyleManager.Current.PrimaryColor;
        backColor = StyleManager.Current.BackColor;
      }
      else
      {
        foreColor = StyleManager.Current.BackColor;
        backColor = StyleManager.Current.PrimaryColor;
      }

      using (var brush = new SolidBrush(backColor))
        e.Graphics.FillRectangle(brush, new Rectangle(e.Bounds.Left, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height));

      var bounds = new Rectangle(0, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);
      TextRenderer.DrawText(
          e.Graphics,
          Items[e.Index].ToString(),
          StyleManager.Current.GetLinkFont(p_metroLinkSize, p_metroLinkWeight),
          bounds,
          foreColor,
          backColor,
          TextFormatFlags.VerticalCenter);
    }
    else
    {
      base.OnDrawItem(e);
    }
  }

  protected override void OnGotFocus(EventArgs e)
  {
    p_isFocused = true;
    Invalidate();
    base.OnGotFocus(e);
  }

  protected override void OnLostFocus(EventArgs e)
  {
    p_isFocused = false;
    p_isHovered = false;
    p_isPressed = false;
    Invalidate();
    base.OnLostFocus(e);
  }

  protected override void OnEnter(EventArgs e)
  {
    p_isFocused = true;
    Invalidate();
    base.OnEnter(e);
  }

  protected override void OnLeave(EventArgs e)
  {
    p_isFocused = false;
    p_isHovered = false;
    p_isPressed = false;
    Invalidate();
    base.OnLeave(e);
  }

  protected override void OnKeyDown(KeyEventArgs e)
  {
    if (e.KeyCode == Keys.Space)
    {
      p_isHovered = true;
      p_isPressed = true;
      Invalidate();
    }

    base.OnKeyDown(e);
  }

  protected override void OnKeyUp(KeyEventArgs e)
  {
    p_isHovered = false;
    p_isPressed = false;
    Invalidate();
    base.OnKeyUp(e);
  }

  protected override void OnMouseEnter(EventArgs e)
  {
    p_isHovered = true;
    Invalidate();
    base.OnMouseEnter(e);
  }

  protected override void OnMouseDown(MouseEventArgs e)
  {
    if (e.Button == MouseButtons.Left)
    {
      p_isPressed = true;
      Invalidate();
    }

    base.OnMouseDown(e);
  }

  protected override void OnMouseUp(MouseEventArgs e)
  {
    p_isPressed = false;
    Invalidate();
    base.OnMouseUp(e);
  }

  protected override void OnMouseLeave(EventArgs e)
  {
    p_isHovered = false;
    Invalidate();
    base.OnMouseLeave(e);
  }

  public override Size GetPreferredSize(Size proposedSize)
  {
    base.GetPreferredSize(proposedSize);
    using (Graphics dc = CreateGraphics())
    {
      string text = Text.Length > 0 ? Text : "MeasureText";
      proposedSize = new Size(int.MaxValue, int.MaxValue);
      Size result = TextRenderer.MeasureText(
          dc,
          text,
          StyleManager.Current.GetLinkFont(p_metroLinkSize, p_metroLinkWeight),
          proposedSize,
          TextFormatFlags.VerticalCenter | TextFormatFlags.LeftAndRightPadding);
      result.Height += 4;
      return result;
    }
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    p_lifetime.Complete();
  }

}
