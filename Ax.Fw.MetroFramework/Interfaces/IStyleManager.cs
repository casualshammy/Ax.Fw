using Ax.Fw.MetroFramework.Data;
using System.Reactive;

namespace Ax.Fw.MetroFramework.Interfaces;

public interface IStyleManager
{
    Color BackColor { get; }
    IObservable<Unit> ColorsChanged { get; }
    Color PrimaryColor { get; }
    Color SecondaryColor { get; }

    Color GetDisabledColor(Color _color);
    Color GetHoverColor(Color _color, float _multi = 3);
    Font GetLabelFont(MetroLabelSize _size, MetroLabelWeight _weight);
    Font GetLinkFont(MetroLinkSize _linkSize, MetroLinkWeight _linkWeight);
    Font GetTextBoxFont(MetroTextBoxSize _linkSize, MetroTextBoxWeight _linkWeight);
    TextFormatFlags GetTextFormatFlags(ContentAlignment textAlign);
    void SetColors(Color _primaryColor, Color _secondaryColor, Color _backColor);
}
