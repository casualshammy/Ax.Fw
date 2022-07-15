using Ax.Fw.MetroFramework.Data;
using Ax.Fw.MetroFramework.Designers;
using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls;

[Designer(typeof(MetroProgressBarDesigner))]
[ToolboxBitmap(typeof(ProgressBar))]
public class MetroProgressBar : ProgressBar
{
    private readonly System.Windows.Forms.Timer p_marqueeTimer = new();
    private MetroProgressBarSize p_metroLabelSize = MetroProgressBarSize.Medium;
    private MetroProgressBarWeight p_metroLabelWeight;
    private ContentAlignment p_textAlign = ContentAlignment.MiddleRight;
    private ProgressBarStyle p_progressBarStyle = ProgressBarStyle.Continuous;

    private bool p_hideProgressText = true;
    private int p_marqueeX;
    private bool p_marqueeTimerEnabled;

    public MetroProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
        p_marqueeTimer.Stop();
        p_marqueeTimer.Interval = 10;
        p_marqueeTimer.Tick += marqueeTimer_Tick;
    }

    [Category("Metro Appearance")]
    public MetroProgressBarSize FontSize
    {
        get
        {
            return p_metroLabelSize;
        }
        set
        {
            p_metroLabelSize = value;
        }
    }

    [Category("Metro Appearance")]
    public MetroProgressBarWeight FontWeight
    {
        get
        {
            return p_metroLabelWeight;
        }
        set
        {
            p_metroLabelWeight = value;
        }
    }

    [Category("Metro Appearance")]
    public ContentAlignment TextAlign
    {
        get
        {
            return p_textAlign;
        }
        set
        {
            p_textAlign = value;
        }
    }

    [Category("Metro Appearance")]
    public bool HideProgressText
    {
        get
        {
            return p_hideProgressText;
        }
        set
        {
            p_hideProgressText = value;
        }
    }

    [Category("Metro Appearance")]
    public ProgressBarStyle ProgressBarStyle
    {
        get
        {
            return p_progressBarStyle;
        }
        set
        {
            p_progressBarStyle = value;
        }
    }

    public new int Value
    {
        get
        {
            return base.Value;
        }
        set
        {
            if (value <= Maximum)
            {
                base.Value = value;
                Invalidate();
            }
        }
    }

    [Browsable(false)]
    public double ProgressTotalPercent => (1.0 - (Maximum - Value) / (double)Maximum) * 100.0;

    [Browsable(false)]
    public double ProgressTotalValue => 1.0 - (Maximum - Value) / (double)Maximum;

    [Browsable(false)]
    public string ProgressPercentText => $"{Math.Round(ProgressTotalPercent)}%";

    private double ProgressBarWidth => Value / (double)Maximum * ClientRectangle.Width;

    private int ProgressBarMarqueeWidth => ClientRectangle.Width / 3;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Color color = Enabled ? StyleManager.Current.PrimaryColor : StyleManager.Current.GetDisabledColor(StyleManager.Current.PrimaryColor);
        e.Graphics.Clear(color);
        if (p_progressBarStyle == ProgressBarStyle.Continuous)
        {
            if (!DesignMode)
            {
                StopTimer();
            }

            DrawProgressContinuous(e.Graphics);
        }
        else if (p_progressBarStyle == ProgressBarStyle.Blocks)
        {
            if (!DesignMode)
            {
                StopTimer();
            }

            DrawProgressContinuous(e.Graphics);
        }
        else if (p_progressBarStyle == ProgressBarStyle.Marquee)
        {
            if (!DesignMode && Enabled)
            {
                StartTimer();
            }

            if (!Enabled)
            {
                StopTimer();
            }

            if (Value == Maximum)
            {
                StopTimer();
                DrawProgressContinuous(e.Graphics);
            }
            else
            {
                DrawProgressMarquee(e.Graphics);
            }
        }

        DrawProgressText(e.Graphics);
        using (Pen pen = new Pen(StyleManager.Current.PrimaryColor))
        {
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
        }
    }

    private void DrawProgressContinuous(Graphics graphics)
    {
        using (var brush = new SolidBrush(StyleManager.Current.PrimaryColor))
            graphics.FillRectangle(brush, 0, 0, (int)ProgressBarWidth, ClientRectangle.Height);
    }

    private void DrawProgressMarquee(Graphics graphics)
    {
        using (var brush = new SolidBrush(StyleManager.Current.PrimaryColor))
            graphics.FillRectangle(brush, p_marqueeX, 0, ProgressBarMarqueeWidth, ClientRectangle.Height);
    }

    private void DrawProgressText(Graphics graphics)
    {
        if (!HideProgressText)
        {
            Color transparent = Color.Transparent;
            TextRenderer.DrawText(
                foreColor: Enabled ? StyleManager.Current.PrimaryColor : StyleManager.Current.GetDisabledColor(StyleManager.Current.PrimaryColor),
                dc: graphics,
                text: ProgressPercentText,
                font: StyleManager.Current.GetProgressBarFont(p_metroLabelSize, p_metroLabelWeight),
                bounds: ClientRectangle,
                backColor: transparent,
                flags: StyleManager.Current.GetTextFormatFlags(TextAlign));
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        base.GetPreferredSize(proposedSize);
        using (Graphics dc = CreateGraphics())
        {
            proposedSize = new Size(int.MaxValue, int.MaxValue);
            return TextRenderer.MeasureText(
                dc,
                ProgressPercentText,
                StyleManager.Current.GetProgressBarFont(p_metroLabelSize, p_metroLabelWeight),
                proposedSize,
                StyleManager.Current.GetTextFormatFlags(TextAlign));
        }
    }

    private void StartTimer()
    {
        if (!p_marqueeTimerEnabled)
        {
            p_marqueeX = -ProgressBarMarqueeWidth;
            p_marqueeTimer.Stop();
            p_marqueeTimer.Start();
            p_marqueeTimerEnabled = true;
            Invalidate();
        }
    }

    private void StopTimer()
    {
        if (p_marqueeTimer != null)
        {
            p_marqueeTimer.Stop();
            Invalidate();
        }
    }

    private void marqueeTimer_Tick(object? sender, EventArgs e)
    {
        p_marqueeX++;
        if (p_marqueeX > ClientRectangle.Width)
        {
            p_marqueeX = -ProgressBarMarqueeWidth;
        }

        Invalidate();
    }

}
