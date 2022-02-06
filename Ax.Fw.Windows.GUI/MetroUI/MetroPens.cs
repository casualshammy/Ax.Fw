using System.Collections.Generic;
using System.Drawing;

namespace Ax.Fw.Windows.GUI.Forms
{
    public sealed class MetroPens
    {
        private static Dictionary<string, Pen> metroPens;

        public static Pen Black => GetSavePen("Black", MetroColors.Black);

        public static Pen White => GetSavePen("White", MetroColors.White);

        public static Pen Silver => GetSavePen("Silver", MetroColors.Silver);

        public static Pen Blue => GetSavePen("Blue", MetroColors.Blue);

        public static Pen Green => GetSavePen("Green", MetroColors.Green);

        public static Pen Lime => GetSavePen("Lime", MetroColors.Lime);

        public static Pen Teal => GetSavePen("Teal", MetroColors.Teal);

        public static Pen Orange => GetSavePen("Orange", MetroColors.Orange);

        public static Pen Brown => GetSavePen("Brown", MetroColors.Brown);

        public static Pen Pink => GetSavePen("Pink", MetroColors.Pink);

        public static Pen Magenta => GetSavePen("Magenta", MetroColors.Magenta);

        public static Pen Purple => GetSavePen("Purple", MetroColors.Purple);

        public static Pen Red => GetSavePen("Red", MetroColors.Red);

        public static Pen Yellow => GetSavePen("Yellow", MetroColors.Yellow);

        private static Pen GetSavePen(string key, Color color)
        {
            if (metroPens == null)
            {
                metroPens = new Dictionary<string, Pen>();
            }

            if (!metroPens.ContainsKey(key))
            {
                metroPens.Add(key, new Pen(color, 1f));
            }

            return metroPens[key].Clone() as Pen;
        }
    }

}
