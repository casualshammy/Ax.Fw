using System.Drawing;

namespace Ax.Fw.Windows.GUI.Forms
{
    public sealed class MetroFonts
    {
        public static Font Title => DefaultLight(24f);

        public static Font Subtitle => Default(14f);

        public static Font Button => DefaultBold(11f);

        public static Font Tile => Default(14f);

        public static Font TileCount => Default(44f);

        private static Font GetSaveFont(string key, FontStyle style, float size)
        {
            return new Font(key, size, style, GraphicsUnit.Pixel);
        }

        public static Font DefaultLight(float size)
        {
            return GetSaveFont("Segoe UI Light", FontStyle.Regular, size);
        }

        public static Font Default(float size)
        {
            return GetSaveFont("Segoe UI", FontStyle.Regular, size);
        }

        public static Font DefaultBold(float size)
        {
            return GetSaveFont("Segoe UI", FontStyle.Bold, size);
        }

        public static Font Link(MetroLinkSize linkSize, MetroLinkWeight linkWeight)
        {
            switch (linkSize)
            {
                case MetroLinkSize.Small:
                    switch (linkWeight)
                    {
                        case MetroLinkWeight.Light:
                            return DefaultLight(12f);
                        case MetroLinkWeight.Regular:
                            return Default(12f);
                        case MetroLinkWeight.Bold:
                            return DefaultBold(12f);
                    }

                    break;
                case MetroLinkSize.Medium:
                    switch (linkWeight)
                    {
                        case MetroLinkWeight.Light:
                            return DefaultLight(14f);
                        case MetroLinkWeight.Regular:
                            return Default(14f);
                        case MetroLinkWeight.Bold:
                            return DefaultBold(14f);
                    }

                    break;
                case MetroLinkSize.Tall:
                    switch (linkWeight)
                    {
                        case MetroLinkWeight.Light:
                            return DefaultLight(18f);
                        case MetroLinkWeight.Regular:
                            return Default(18f);
                        case MetroLinkWeight.Bold:
                            return DefaultBold(18f);
                    }

                    break;
            }

            return Default(12f);
        }

        public static Font Label(MetroLabelSize labelSize, MetroLabelWeight labelWeight)
        {
            switch (labelSize)
            {
                case MetroLabelSize.Small:
                    switch (labelWeight)
                    {
                        case MetroLabelWeight.Light:
                            return DefaultLight(12f);
                        case MetroLabelWeight.Regular:
                            return Default(12f);
                        case MetroLabelWeight.Bold:
                            return DefaultBold(12f);
                    }

                    break;
                case MetroLabelSize.Medium:
                    switch (labelWeight)
                    {
                        case MetroLabelWeight.Light:
                            return DefaultLight(14f);
                        case MetroLabelWeight.Regular:
                            return Default(14f);
                        case MetroLabelWeight.Bold:
                            return DefaultBold(14f);
                    }

                    break;
                case MetroLabelSize.Tall:
                    switch (labelWeight)
                    {
                        case MetroLabelWeight.Light:
                            return DefaultLight(18f);
                        case MetroLabelWeight.Regular:
                            return Default(18f);
                        case MetroLabelWeight.Bold:
                            return DefaultBold(18f);
                    }

                    break;
            }

            return DefaultLight(14f);
        }

        public static Font TextBox(MetroTextBoxSize linkSize, MetroTextBoxWeight linkWeight)
        {
            switch (linkSize)
            {
                case MetroTextBoxSize.Small:
                    switch (linkWeight)
                    {
                        case MetroTextBoxWeight.Light:
                            return DefaultLight(12f);
                        case MetroTextBoxWeight.Regular:
                            return Default(12f);
                        case MetroTextBoxWeight.Bold:
                            return DefaultBold(12f);
                    }

                    break;
                case MetroTextBoxSize.Medium:
                    switch (linkWeight)
                    {
                        case MetroTextBoxWeight.Light:
                            return DefaultLight(14f);
                        case MetroTextBoxWeight.Regular:
                            return Default(14f);
                        case MetroTextBoxWeight.Bold:
                            return DefaultBold(14f);
                    }

                    break;
                case MetroTextBoxSize.Tall:
                    switch (linkWeight)
                    {
                        case MetroTextBoxWeight.Light:
                            return DefaultLight(18f);
                        case MetroTextBoxWeight.Regular:
                            return Default(18f);
                        case MetroTextBoxWeight.Bold:
                            return DefaultBold(18f);
                    }

                    break;
            }

            return Default(12f);
        }

        public static Font ProgressBar(MetroProgressBarSize labelSize, MetroProgressBarWeight labelWeight)
        {
            switch (labelSize)
            {
                case MetroProgressBarSize.Small:
                    switch (labelWeight)
                    {
                        case MetroProgressBarWeight.Light:
                            return DefaultLight(12f);
                        case MetroProgressBarWeight.Regular:
                            return Default(12f);
                        case MetroProgressBarWeight.Bold:
                            return DefaultBold(12f);
                    }

                    break;
                case MetroProgressBarSize.Medium:
                    switch (labelWeight)
                    {
                        case MetroProgressBarWeight.Light:
                            return DefaultLight(14f);
                        case MetroProgressBarWeight.Regular:
                            return Default(14f);
                        case MetroProgressBarWeight.Bold:
                            return DefaultBold(14f);
                    }

                    break;
                case MetroProgressBarSize.Tall:
                    switch (labelWeight)
                    {
                        case MetroProgressBarWeight.Light:
                            return DefaultLight(18f);
                        case MetroProgressBarWeight.Regular:
                            return Default(18f);
                        case MetroProgressBarWeight.Bold:
                            return DefaultBold(18f);
                    }

                    break;
            }

            return DefaultLight(14f);
        }

        public static Font TabControl(MetroTabControlSize labelSize, MetroTabControlWeight labelWeight)
        {
            switch (labelSize)
            {
                case MetroTabControlSize.Small:
                    switch (labelWeight)
                    {
                        case MetroTabControlWeight.Light:
                            return DefaultLight(12f);
                        case MetroTabControlWeight.Regular:
                            return Default(12f);
                        case MetroTabControlWeight.Bold:
                            return DefaultBold(12f);
                    }

                    break;
                case MetroTabControlSize.Medium:
                    switch (labelWeight)
                    {
                        case MetroTabControlWeight.Light:
                            return DefaultLight(14f);
                        case MetroTabControlWeight.Regular:
                            return Default(14f);
                        case MetroTabControlWeight.Bold:
                            return DefaultBold(14f);
                    }

                    break;
                case MetroTabControlSize.Tall:
                    switch (labelWeight)
                    {
                        case MetroTabControlWeight.Light:
                            return DefaultLight(18f);
                        case MetroTabControlWeight.Regular:
                            return Default(18f);
                        case MetroTabControlWeight.Bold:
                            return DefaultBold(18f);
                    }

                    break;
            }

            return DefaultLight(14f);
        }
    }

}
