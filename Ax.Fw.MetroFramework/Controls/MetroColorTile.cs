using Ax.Fw.Extensions;

namespace Ax.Fw.MetroFramework.Controls;

public class MetroColorTile : UserControl
{
    private readonly Lifetime p_lifetime = new();

    public MetroColorTile()
    {
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
        base.OnPaint(_e);
        _e.Graphics.Clear(StyleManager.Current.PrimaryColor);
    }

    protected override void Dispose(bool _disposing)
    {
        base.Dispose(_disposing);
        p_lifetime.Complete();
    }

}
