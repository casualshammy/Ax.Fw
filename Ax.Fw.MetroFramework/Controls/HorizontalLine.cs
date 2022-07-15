using Ax.Fw.Extensions;

namespace Ax.Fw.MetroFramework.Controls;

public class HorizontalLine : UserControl
{
    private readonly Lifetime p_lifetime = new();

    public HorizontalLine()
    {
        StyleManager.Current.ColorsChanged
            .Subscribe(_ => Invalidate(), p_lifetime);

        AutoScaleDimensions = new SizeF(6f, 13f);
        AutoScaleMode = AutoScaleMode.Font;
        Name = "HorizontalLine";
        base.MaximumSize = new Size(2000, 2);
        base.MinimumSize = new Size(2, 2);
        Width = 350;
    }

    protected override void OnPaint(PaintEventArgs _e)
    {
        base.OnPaint(_e);
        using (var brush = new SolidBrush(StyleManager.Current.PrimaryColor))
            _e.Graphics.FillRectangle(brush, -1, -1, Width + 1, Height + 1);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        p_lifetime.Complete();
    }

}
