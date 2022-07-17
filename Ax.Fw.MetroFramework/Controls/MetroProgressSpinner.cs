using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Designers;
using Ax.Fw.SharedTypes.Interfaces;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Ax.Fw.MetroFramework.Controls;

[Designer(typeof(MetroProgressSpinnerDesigner))]
[ToolboxBitmap(typeof(ProgressBar))]
public class MetroProgressSpinner : Control
{
    private readonly ILifetime p_lifetime = new Lifetime();
    private readonly System.Windows.Forms.Timer p_timer = new();
    private float p_speed;
    private bool p_backwards;
    private float p_angle = 270f;
    private int p_progress;
    private int p_minimum;
    private int p_maximum = 100;

    public MetroProgressSpinner()
    {
        StyleManager.Current.ColorsChanged
            .Subscribe(_ =>
            {
                try
                {
                    BeginInvoke(() => Invalidate(true));
                }
                catch { }
            }, p_lifetime);

        p_timer.Interval = 20;
        p_timer.Tick += Timer_Tick;
        p_timer.Enabled = true;
        Width = 16;
        Height = 16;
        p_speed = 1f;
        DoubleBuffered = true;
    }

    [DefaultValue(true)]
    [Category("Metro Behaviour")]
    public bool Spinning
    {
        get
        {
            return p_timer.Enabled;
        }
        set
        {
            p_timer.Enabled = value;
        }
    }

    [DefaultValue(1f)]
    [Category("Metro Behaviour")]
    public float Speed
    {
        get
        {
            return p_speed;
        }
        set
        {
            if (value <= 0f || value > 10f)
                throw new ArgumentOutOfRangeException("Speed value must be > 0 and <= 10.", (Exception?)null);

            p_speed = value;
        }
    }

    [DefaultValue(false)]
    [Category("Metro Behaviour")]
    public bool Backwards
    {
        get
        {
            return p_backwards;
        }
        set
        {
            p_backwards = value;
            Refresh();
        }
    }

    [DefaultValue(0)]
    [Category("Metro Appearance")]
    public int Value
    {
        get
        {
            return p_progress;
        }
        set
        {
            if (value != -1 && (value < p_minimum || value > p_maximum))
            {
                throw new ArgumentOutOfRangeException("Progress value must be -1 or between Minimum and Maximum.", (Exception?)null);
            }

            p_progress = value;
            Refresh();
        }
    }

    [DefaultValue(0)]
    [Category("Metro Appearance")]
    public int Minimum
    {
        get
        {
            return p_minimum;
        }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("Minimum value must be >= 0.", (Exception?)null);
            }

            if (value >= p_maximum)
            {
                throw new ArgumentOutOfRangeException("Minimum value must be < Maximum.", (Exception?)null);
            }

            p_minimum = value;
            if (p_progress != -1 && p_progress < p_minimum)
            {
                p_progress = p_minimum;
            }

            Refresh();
        }
    }

    [Category("Metro Appearance")]
    [DefaultValue(0)]
    public int Maximum
    {
        get
        {
            return p_maximum;
        }
        set
        {
            if (value <= p_minimum)
            {
                throw new ArgumentOutOfRangeException("Maximum value must be > Minimum.", (Exception?)null);
            }

            p_maximum = value;
            if (p_progress > p_maximum)
            {
                p_progress = p_maximum;
            }

            Refresh();
        }
    }

    protected override void OnPaint(PaintEventArgs _e)
    {
        _e.Graphics.Clear(StyleManager.Current.BackColor);
        using (var pen = new Pen(StyleManager.Current.PrimaryColor, Width / 5f))
        {
            var num = (int)Math.Ceiling(Width / 10f);
            _e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            if (p_progress != -1)
            {
                var num2 = (p_progress - p_minimum) / (float)(p_maximum - p_minimum);
                var num3 = 30f + 300f * num2;
                if (p_backwards)
                    num3 = 0f - num3;

                _e.Graphics.DrawArc(pen, num, num, Width - 2 * num - 1, Height - 2 * num - 1, p_angle, num3);
                return;
            }

            for (int i = 0; i <= 180; i += 15)
            {
                var num4 = 290 - i * 290 / 180;
                if (num4 > 255)
                    num4 = 255;

                if (num4 < 0)
                    num4 = 0;

                Color color3 = Color.FromArgb(num4, pen.Color);
                using (var pen2 = new Pen(color3, pen.Width))
                {
                    var startAngle = p_angle + (i - 30) * (p_backwards ? 1 : -1);
                    var sweepAngle = 15 * (p_backwards ? 1 : -1);
                    _e.Graphics.DrawArc(pen2, num, num, Width - 2 * num - 1, Height - 2 * num - 1, startAngle, sweepAngle);
                }
            }
        }
    }

    protected override void Dispose(bool _disposing)
    {
        base.Dispose(_disposing);
        p_lifetime.Complete();
    }

    private void Timer_Tick(object? _sender, EventArgs _e)
    {
        if (!DesignMode)
        {
            p_angle += 6f * p_speed * (!p_backwards ? 1 : -1);
            Refresh();
        }
    }

}
