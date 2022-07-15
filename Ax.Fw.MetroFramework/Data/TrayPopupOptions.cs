namespace Ax.Fw.MetroFramework.Data;

public class TrayPopupOptions
{
    public TrayPopupOptions(string _title, string _message, Image? _image, Action? _onClick, Action? _onClose, TrayPopupType _type, bool _sound)
    {
        Title = _title;
        Message = _message;
        Image = _image;
        OnClick = _onClick;
        OnClose = _onClose;
        Type = _type;
        Sound = _sound;
    }

    public string Title { get; private set; }
    public string Message { get; private set; }
    public Image? Image { get; private set; }
    public Action? OnClick { get; private set; }
    public Action? OnClose { get; private set; }
    public TrayPopupType Type { get; private set; }
    public bool Sound { get; private set; }
}
