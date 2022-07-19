namespace Ax.Fw.MetroFramework.Forms;

public partial class InputBox : BorderlessForm
{
    internal InputBox()
    {
        InitializeComponent();
        button1.Click += Button1Click;
        button2.Click += Button2Click;
        BeginInvoke((MethodInvoker)delegate
        {
            Location = MousePosition;
            OnActivated(EventArgs.Empty);
        });
    }

    private string? temp;

    /// <summary>
    /// Shows InputBox and returns user-provided string
    /// <para></para>
    /// Returns <b>null</b> if user has clicked "Cancel", empty string if user has just clicked "OK", user-provided string otherwise
    /// </summary>
    /// <param name="_prompt">Prompt text</param>
    /// <param name="metroColorStyle">MetroColorStyle of inputbox</param>
    /// <returns></returns>
    public static string? Input(string _prompt)
    {
        var bform = new InputBox() { metroLabel1 = { Text = _prompt }, temp = null };
        bform.ShowDialog();
        return bform.temp;
    }

    private void Button1Click(object? sender, EventArgs e)
    {
        temp = textBox1.Text ?? "";
        Close();
    }

    private void Button2Click(object? sender, EventArgs e)
    {
        Close();
    }

}
