namespace Ax.Fw.MetroFramework.Controls;

public class ErrorProviderExt
{
    private static readonly Dictionary<Control, Color> p_controlColors = new();
    private static readonly MetroToolTip p_toolTip = new();

    public static void SetError(Control control, string text, Color color)
    {
        if (!p_controlColors.ContainsKey(control))
        {
            p_controlColors.Add(control, control.BackColor);
            control.LostFocus += ControlOnLostFocus;
            control.Disposed += ControlOnDisposed;
        }
        control.BackColor = color;
        p_toolTip.Show(text, control, control.Width, control.Height);
    }

    public static void ClearError(Control control)
    {
        if (p_controlColors.ContainsKey(control))
        {
            control.BackColor = p_controlColors[control];
            p_toolTip.Hide(control);
            p_controlColors.Remove(control);
            control.LostFocus -= ControlOnLostFocus;
            control.Disposed -= ControlOnDisposed;
        }
    }

    private static void ControlOnLostFocus(object? sender, EventArgs eventArgs)
    {
        if (sender is Control control)
        {
            p_toolTip.Hide(control);
        }
    }

    private static void ControlOnDisposed(object? sender, EventArgs eventArgs)
    {
        if (sender is Control control)
        {
            p_controlColors.Remove(control);
            control.LostFocus -= ControlOnLostFocus;
            control.Disposed -= ControlOnDisposed;
        }
    }
}
