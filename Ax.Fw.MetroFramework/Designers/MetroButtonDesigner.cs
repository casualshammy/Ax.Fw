using System.Collections;
using System.Windows.Forms.Design;

namespace Ax.Fw.MetroFramework.Designers;

internal class MetroButtonDesigner : ControlDesigner
{
    public override SelectionRules SelectionRules => base.SelectionRules;

    protected override void PreFilterProperties(IDictionary _properties)
    {
        _properties.Remove("ImeMode");
        _properties.Remove("Padding");
        _properties.Remove("FlatAppearance");
        _properties.Remove("FlatStyle");
        _properties.Remove("AutoEllipsis");
        _properties.Remove("UseCompatibleTextRendering");
        _properties.Remove("Image");
        _properties.Remove("ImageAlign");
        _properties.Remove("ImageIndex");
        _properties.Remove("ImageKey");
        _properties.Remove("ImageList");
        _properties.Remove("TextImageRelation");
        _properties.Remove("BackColor");
        _properties.Remove("BackgroundImage");
        _properties.Remove("BackgroundImageLayout");
        _properties.Remove("UseVisualStyleBackColor");
        _properties.Remove("Font");
        _properties.Remove("ForeColor");
        _properties.Remove("RightToLeft");
        base.PreFilterProperties(_properties);
    }
}
