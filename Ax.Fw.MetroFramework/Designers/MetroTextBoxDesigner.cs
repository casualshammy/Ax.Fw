using System.Collections;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace Ax.Fw.MetroFramework.Designers;

internal class MetroTextBoxDesigner : ControlDesigner
{
    public override SelectionRules SelectionRules
    {
        get
        {
            var propertyDescriptor = TypeDescriptor.GetProperties(Component)["Multiline"];
            if (propertyDescriptor != null)
            {
                if ((propertyDescriptor.GetValue(Component) as bool?) == true)
                {
                    return SelectionRules.Moveable | SelectionRules.Visible | SelectionRules.TopSizeable | SelectionRules.BottomSizeable | SelectionRules.LeftSizeable | SelectionRules.RightSizeable;
                }

                return SelectionRules.Moveable | SelectionRules.Visible | SelectionRules.LeftSizeable | SelectionRules.RightSizeable;
            }

            return base.SelectionRules;
        }
    }

    protected override void PreFilterProperties(IDictionary properties)
    {
        properties.Remove("BackgroundImage");
        properties.Remove("ImeMode");
        properties.Remove("Padding");
        properties.Remove("BackgroundImageLayout");
        properties.Remove("Font");
        base.PreFilterProperties(properties);
    }
}
