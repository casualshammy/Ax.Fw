using System.Collections.Generic;
using System.Drawing;

namespace Ax.Fw.Windows.GUI.Forms
{
    public sealed class MetroBrushes
    {
        private static Dictionary<string, SolidBrush> metroBrushes;

        public static SolidBrush Black => GetSaveBrush("Black", MetroColors.Black);

        public static SolidBrush White => GetSaveBrush("White", MetroColors.White);

        public static SolidBrush Silver => GetSaveBrush("Silver", MetroColors.Silver);

        public static SolidBrush Blue => GetSaveBrush("Blue", MetroColors.Blue);

        public static SolidBrush Green => GetSaveBrush("Green", MetroColors.Green);

        public static SolidBrush Lime => GetSaveBrush("Lime", MetroColors.Lime);

        public static SolidBrush Teal => GetSaveBrush("Teal", MetroColors.Teal);

        public static SolidBrush Orange => GetSaveBrush("Orange", MetroColors.Orange);

        public static SolidBrush Brown => GetSaveBrush("Brown", MetroColors.Brown);

        public static SolidBrush Pink => GetSaveBrush("Pink", MetroColors.Pink);

        public static SolidBrush Magenta => GetSaveBrush("Magenta", MetroColors.Magenta);

        public static SolidBrush Purple => GetSaveBrush("Purple", MetroColors.Purple);

        public static SolidBrush Red => GetSaveBrush("Red", MetroColors.Red);

        public static SolidBrush Yellow => GetSaveBrush("Yellow", MetroColors.Yellow);

        private static SolidBrush GetSaveBrush(string key, Color color)
        {
            if (metroBrushes == null)
            {
                metroBrushes = new Dictionary<string, SolidBrush>();
            }

            if (!metroBrushes.ContainsKey(key))
            {
                metroBrushes.Add(key, new SolidBrush(color));
            }

            return metroBrushes[key].Clone() as SolidBrush;
        }
    }

}
