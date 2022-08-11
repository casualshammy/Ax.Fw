#nullable enable
namespace Ax.Fw.MetroFramework.Data;

public class TrayPopupOptions
{
    public TrayPopupOptions(string? _title, string? _message, TrayPopupType _type)
    {
        Title = _title;
        Message = _message;
        Type = _type;
    }

    public string? Title { get; }
    public string? Message { get; }
    public Image? Image { get; init; }
    public Action? OnClick { get; init; }
    public Action? OnClose { get; init; }
    public TrayPopupType Type { get; }
    public bool Sound { get; init; }

}
