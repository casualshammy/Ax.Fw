using Ax.Fw.MetroFramework.Data;
using Ax.Fw.MetroFramework.Interfaces;
using System.Reactive;
using System.Reactive.Subjects;

namespace Ax.Fw.MetroFramework;

public class StyleManager : IStyleManager
{
    private readonly ReplaySubject<Unit> p_changesFlow = new(1);

    public static readonly StyleManager Current = new(Color.Black, Color.Blue, Color.WhiteSmoke);

    internal StyleManager(
        Color _primaryColor,
        Color _secondaryColor,
        Color _backColor)
    {
        SetColors(_primaryColor, _secondaryColor, _backColor);
    }

    public Color PrimaryColor { get; private set; }
    public Color SecondaryColor { get; private set; }
    public Color BackColor { get; private set; }
    public IObservable<Unit> ColorsChanged => p_changesFlow;

    public void SetColors(Color _primaryColor, Color _secondaryColor, Color _backColor)
    {
        PrimaryColor = _primaryColor;
        SecondaryColor = _secondaryColor;
        BackColor = _backColor;
        p_changesFlow.OnNext(Unit.Default);
    }

    public Color GetHoverColor(Color _color, float _multi = 3f)
    {
        var r = (int)(_color.R > 255 / 2f ? _color.R - 25 * _multi : _color.R + 25 * _multi);
        var g = (int)(_color.G > 255 / 2f ? _color.G - 25 * _multi : _color.G + 25 * _multi);
        var b = (int)(_color.B > 255 / 2f ? _color.B - 25 * _multi : _color.B + 25 * _multi);

        return Color.FromArgb(_color.A, Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    public Color GetOppositeColor(Color _color) => Color.FromArgb(255 - _color.R, 255 - _color.G, 255 - _color.B);

    public Color GetDisabledColor(Color _color)
    {
        var s = _color.R + _color.G + _color.B;
        if (s > 255 * 3 / 2)
            return Color.FromArgb(255, Color.DarkSlateGray);
        else
            return Color.FromArgb(255, Color.DarkSlateGray);
    }

    public TextFormatFlags GetTextFormatFlags(ContentAlignment textAlign)
    {
        TextFormatFlags textFormatFlags = TextFormatFlags.EndEllipsis;
        switch (textAlign)
        {
            case ContentAlignment.TopLeft:

                break;
            case ContentAlignment.TopCenter:
                textFormatFlags |= TextFormatFlags.HorizontalCenter;
                break;
            case ContentAlignment.TopRight:
                textFormatFlags |= TextFormatFlags.Right;
                break;
            case ContentAlignment.MiddleLeft:
                textFormatFlags |= TextFormatFlags.VerticalCenter;
                break;
            case ContentAlignment.MiddleCenter:
                textFormatFlags |= TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                break;
            case ContentAlignment.MiddleRight:
                textFormatFlags |= TextFormatFlags.Right | TextFormatFlags.VerticalCenter;
                break;
            case ContentAlignment.BottomLeft:
                textFormatFlags |= TextFormatFlags.Bottom;
                break;
            case ContentAlignment.BottomCenter:
                textFormatFlags |= TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter;
                break;
            case ContentAlignment.BottomRight:
                textFormatFlags |= TextFormatFlags.Bottom | TextFormatFlags.Right;
                break;
        }

        return textFormatFlags;
    }

    public Font GetLabelFont(MetroLabelSize _size, MetroLabelWeight _weight)
    {
        switch (_size)
        {
            case MetroLabelSize.Small:
                switch (_weight)
                {
                    case MetroLabelWeight.Light:
                        return DefaultFontLight(12f);
                    case MetroLabelWeight.Regular:
                        return DefaultFont(12f);
                    case MetroLabelWeight.Bold:
                        return DefaultFontBold(12f);
                }

                break;
            case MetroLabelSize.Medium:
                switch (_weight)
                {
                    case MetroLabelWeight.Light:
                        return DefaultFontLight(14f);
                    case MetroLabelWeight.Regular:
                        return DefaultFont(14f);
                    case MetroLabelWeight.Bold:
                        return DefaultFontBold(14f);
                }

                break;
            case MetroLabelSize.Tall:
                switch (_weight)
                {
                    case MetroLabelWeight.Light:
                        return DefaultFontLight(18f);
                    case MetroLabelWeight.Regular:
                        return DefaultFont(18f);
                    case MetroLabelWeight.Bold:
                        return DefaultFontBold(18f);
                }

                break;
        }

        return DefaultFontLight(14f);
    }

    public Font GetLinkFont(MetroLinkSize _linkSize, MetroLinkWeight _linkWeight)
    {
        switch (_linkSize)
        {
            case MetroLinkSize.Small:
                switch (_linkWeight)
                {
                    case MetroLinkWeight.Light:
                        return DefaultFontLight(12f);
                    case MetroLinkWeight.Regular:
                        return DefaultFont(12f);
                    case MetroLinkWeight.Bold:
                        return DefaultFontBold(12f);
                }

                break;
            case MetroLinkSize.Medium:
                switch (_linkWeight)
                {
                    case MetroLinkWeight.Light:
                        return DefaultFontLight(14f);
                    case MetroLinkWeight.Regular:
                        return DefaultFont(14f);
                    case MetroLinkWeight.Bold:
                        return DefaultFontBold(14f);
                }

                break;
            case MetroLinkSize.Tall:
                switch (_linkWeight)
                {
                    case MetroLinkWeight.Light:
                        return DefaultFontLight(18f);
                    case MetroLinkWeight.Regular:
                        return DefaultFont(18f);
                    case MetroLinkWeight.Bold:
                        return DefaultFontBold(18f);
                }

                break;
        }

        return DefaultFont(12f);
    }

    public Font GetTextBoxFont(MetroTextBoxSize _linkSize, MetroTextBoxWeight _linkWeight)
    {
        switch (_linkSize)
        {
            case MetroTextBoxSize.Small:
                switch (_linkWeight)
                {
                    case MetroTextBoxWeight.Light:
                        return DefaultFontLight(12f);
                    case MetroTextBoxWeight.Regular:
                        return DefaultFont(12f);
                    case MetroTextBoxWeight.Bold:
                        return DefaultFontBold(12f);
                }

                break;
            case MetroTextBoxSize.Medium:
                switch (_linkWeight)
                {
                    case MetroTextBoxWeight.Light:
                        return DefaultFontLight(14f);
                    case MetroTextBoxWeight.Regular:
                        return DefaultFont(14f);
                    case MetroTextBoxWeight.Bold:
                        return DefaultFontBold(14f);
                }

                break;
            case MetroTextBoxSize.Tall:
                switch (_linkWeight)
                {
                    case MetroTextBoxWeight.Light:
                        return DefaultFontLight(18f);
                    case MetroTextBoxWeight.Regular:
                        return DefaultFont(18f);
                    case MetroTextBoxWeight.Bold:
                        return DefaultFontBold(18f);
                }

                break;
        }

        return DefaultFont(12f);
    }

    public Font GetProgressBarFont(MetroProgressBarSize labelSize, MetroProgressBarWeight labelWeight)
    {
        switch (labelSize)
        {
            case MetroProgressBarSize.Small:
                switch (labelWeight)
                {
                    case MetroProgressBarWeight.Light:
                        return DefaultFontLight(12f);
                    case MetroProgressBarWeight.Regular:
                        return DefaultFont(12f);
                    case MetroProgressBarWeight.Bold:
                        return DefaultFontBold(12f);
                }

                break;
            case MetroProgressBarSize.Medium:
                switch (labelWeight)
                {
                    case MetroProgressBarWeight.Light:
                        return DefaultFontLight(14f);
                    case MetroProgressBarWeight.Regular:
                        return DefaultFont(14f);
                    case MetroProgressBarWeight.Bold:
                        return DefaultFontBold(14f);
                }

                break;
            case MetroProgressBarSize.Tall:
                switch (labelWeight)
                {
                    case MetroProgressBarWeight.Light:
                        return DefaultFontLight(18f);
                    case MetroProgressBarWeight.Regular:
                        return DefaultFont(18f);
                    case MetroProgressBarWeight.Bold:
                        return DefaultFontBold(18f);
                }

                break;
        }

        return DefaultFontLight(14f);
    }

    private static Font DefaultFontLight(float size)
    {
        return GetSaveFont("Segoe UI Light", FontStyle.Regular, size);
    }

    internal Font DefaultFont(float size)
    {
        return GetSaveFont("Segoe UI", FontStyle.Regular, size);
    }

    internal Font DefaultFontBold(float size)
    {
        return GetSaveFont("Segoe UI", FontStyle.Bold, size);
    }

    private static Font GetSaveFont(string key, FontStyle style, float size)
    {
        return new Font(key, size, style, GraphicsUnit.Pixel);
    }

}
