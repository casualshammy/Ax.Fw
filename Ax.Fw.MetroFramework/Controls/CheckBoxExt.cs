using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls;

public partial class CheckBoxExt : CheckBox
{
    public CheckBoxExt()
    {
        InitializeComponent();
    }

    public CheckBoxExt(IContainer container)
    {
        container.Add(this);
        InitializeComponent();
    }

    private bool HandlingRightClick { get; set; }

    public delegate void MouseClickExt(object? sender, MouseEventArgs e);

    public event MouseClickExt? MouseClickExtended;

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Right && !HandlingRightClick)
        {
            HandlingRightClick = true;
            MouseClickExtended?.Invoke(null, mevent);
        }
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        HandlingRightClick = false;
        base.OnMouseUp(mevent);
    }

    protected override void OnMouseLeave(EventArgs eventargs)
    {
        HandlingRightClick = false;
        base.OnMouseLeave(eventargs);
    }
}
