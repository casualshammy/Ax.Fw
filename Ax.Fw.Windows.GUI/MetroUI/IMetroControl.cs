namespace Ax.Fw.Windows.GUI.Forms
{
    public interface IMetroControl
    {
        MetroColorStyle Style
        {
            get;
            set;
        }

        MetroThemeStyle Theme
        {
            get;
            set;
        }

        MetroStyleManager StyleManager
        {
            get;
            set;
        }
    }

}
