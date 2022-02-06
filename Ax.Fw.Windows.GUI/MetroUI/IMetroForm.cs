namespace Ax.Fw.Windows.GUI.Forms
{
    public interface IMetroForm
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
