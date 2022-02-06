using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Ax.Fw.Windows.GUI.Forms
{
    [Designer(typeof(ScrollableControlDesigner), typeof(ParentControlDesigner))]
    internal class MetroScrollBarDesigner : ControlDesigner
    {
        public override SelectionRules SelectionRules
        {
            get
            {
                //IL_003c: Unknown result type (might be due to invalid IL or missing references)
                PropertyDescriptor propertyDescriptor = TypeDescriptor.GetProperties(((ComponentDesigner)this).get_Component())["Orientation"];
                if (propertyDescriptor != null)
                {
                    MetroScrollOrientation metroScrollOrientation = (MetroScrollOrientation)propertyDescriptor.GetValue(((ComponentDesigner)this).get_Component());
                    if (metroScrollOrientation != MetroScrollOrientation.Vertical)
                    {
                        return (SelectionRules)1342177292;
                    }

                    return (SelectionRules)1342177283;
                }

                return ((ControlDesigner)this).get_SelectionRules();
            }
        }

        protected override void PreFilterProperties(IDictionary properties)
        {
            properties.Remove("Text");
            properties.Remove("BackgroundImage");
            properties.Remove("ForeColor");
            properties.Remove("ImeMode");
            properties.Remove("Padding");
            properties.Remove("BackgroundImageLayout");
            properties.Remove("BackColor");
            properties.Remove("Font");
            properties.Remove("RightToLeft");
            ((ControlDesigner)this).PreFilterProperties(properties);
        }

        public MetroScrollBarDesigner()
            : this()
        {
        }
    }

    [DefaultEvent("Scroll")]
    [DefaultProperty("Value")]
    [Designer(typeof(MetroScrollBarDesigner))]
    public class MetroScrollBar : Control, IMetroControl
    {
        private MetroColorStyle metroStyle = MetroColorStyle.Blue;

        private MetroThemeStyle metroTheme;

        private MetroStyleManager metroStyleManager;

        private bool isFirstScrollEventVertical = true;

        private bool isFirstScrollEventHorizontal = true;

        private bool inUpdate;

        private Rectangle clickedBarRectangle;

        private Rectangle thumbRectangle;

        private bool topBarClicked;

        private bool bottomBarClicked;

        private bool thumbClicked;

        private int thumbWidth = 6;

        private int thumbHeight;

        private int thumbBottomLimitBottom;

        private int thumbBottomLimitTop;

        private int thumbTopLimit;

        private int thumbPosition;

        private int trackPosition;

        private readonly Timer progressTimer = new Timer();

        private int mouseWheelBarPartitions = 10;

        private bool isHovered;

        private bool isPressed;

        private bool useBarColor;

        private bool highlightOnWheel;

        private MetroScrollOrientation metroOrientation = MetroScrollOrientation.Vertical;

        private ScrollOrientation scrollOrientation = ScrollOrientation.VerticalScroll;

        private int minimum;

        private int maximum = 100;

        private int smallChange = 1;

        private int largeChange = 10;

        private int curValue;

        private bool dontUpdateColor;

        private Timer autoHoverTimer;

        [Category("Metro Appearance")]
        public MetroColorStyle Style
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Style;
                }

                return metroStyle;
            }
            set
            {
                metroStyle = value;
            }
        }

        [Category("Metro Appearance")]
        public MetroThemeStyle Theme
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Theme;
                }

                return metroTheme;
            }
            set
            {
                metroTheme = value;
            }
        }

        [Browsable(false)]
        public MetroStyleManager StyleManager
        {
            get
            {
                return metroStyleManager;
            }
            set
            {
                metroStyleManager = value;
            }
        }

        public int MouseWheelBarPartitions
        {
            get
            {
                return mouseWheelBarPartitions;
            }
            set
            {
                if (value > 0)
                {
                    mouseWheelBarPartitions = value;
                    return;
                }

                throw new ArgumentOutOfRangeException("value", "MouseWheelBarPartitions has to be greather than zero");
            }
        }

        [Category("Metro Appearance")]
        public bool UseBarColor
        {
            get
            {
                return useBarColor;
            }
            set
            {
                useBarColor = value;
            }
        }

        [Category("Metro Appearance")]
        public int ScrollbarSize
        {
            get
            {
                if (Orientation != MetroScrollOrientation.Vertical)
                {
                    return base.Height;
                }

                return base.Width;
            }
            set
            {
                if (Orientation == MetroScrollOrientation.Vertical)
                {
                    base.Width = value;
                }
                else
                {
                    base.Height = value;
                }
            }
        }

        [DefaultValue(false)]
        [Category("Metro Appearance")]
        public bool HighlightOnWheel
        {
            get
            {
                return highlightOnWheel;
            }
            set
            {
                highlightOnWheel = value;
            }
        }

        public MetroScrollOrientation Orientation
        {
            get
            {
                return metroOrientation;
            }
            set
            {
                if (value != metroOrientation)
                {
                    metroOrientation = value;
                    if (value == MetroScrollOrientation.Vertical)
                    {
                        scrollOrientation = ScrollOrientation.VerticalScroll;
                    }
                    else
                    {
                        scrollOrientation = ScrollOrientation.HorizontalScroll;
                    }

                    base.Size = new Size(base.Height, base.Width);
                    SetupScrollBar();
                }
            }
        }

        public int Minimum
        {
            get
            {
                return minimum;
            }
            set
            {
                if (minimum != value && value >= 0 && value < maximum)
                {
                    minimum = value;
                    if (curValue < value)
                    {
                        curValue = value;
                    }

                    if (largeChange > maximum - minimum)
                    {
                        largeChange = maximum - minimum;
                    }

                    SetupScrollBar();
                    if (curValue < value)
                    {
                        dontUpdateColor = true;
                        Value = value;
                    }
                    else
                    {
                        ChangeThumbPosition(GetThumbPosition());
                        Refresh();
                    }
                }
            }
        }

        public int Maximum
        {
            get
            {
                return maximum;
            }
            set
            {
                if (value != maximum && value >= 1 && value > minimum)
                {
                    maximum = value;
                    if (largeChange > maximum - minimum)
                    {
                        largeChange = maximum - minimum;
                    }

                    SetupScrollBar();
                    if (curValue > value)
                    {
                        dontUpdateColor = true;
                        Value = maximum;
                    }
                    else
                    {
                        ChangeThumbPosition(GetThumbPosition());
                        Refresh();
                    }
                }
            }
        }

        [DefaultValue(1)]
        public int SmallChange
        {
            get
            {
                return smallChange;
            }
            set
            {
                if (value != smallChange && value >= 1 && value < largeChange)
                {
                    smallChange = value;
                    SetupScrollBar();
                }
            }
        }

        [DefaultValue(5)]
        public int LargeChange
        {
            get
            {
                return largeChange;
            }
            set
            {
                if (value != largeChange && value >= smallChange && value >= 2)
                {
                    if (value > maximum - minimum)
                    {
                        largeChange = maximum - minimum;
                    }
                    else
                    {
                        largeChange = value;
                    }

                    SetupScrollBar();
                }
            }
        }

        [DefaultValue(0)]
        [Browsable(false)]
        public int Value
        {
            get
            {
                return curValue;
            }
            set
            {
                if (curValue == value || value < minimum || value > maximum)
                {
                    return;
                }

                curValue = value;
                ChangeThumbPosition(GetThumbPosition());
                OnScroll(ScrollEventType.ThumbPosition, -1, value, scrollOrientation);
                if (!dontUpdateColor && highlightOnWheel)
                {
                    if (!isHovered)
                    {
                        isHovered = true;
                    }

                    if (autoHoverTimer == null)
                    {
                        autoHoverTimer = new Timer();
                        autoHoverTimer.Interval = 1000;
                        autoHoverTimer.Tick += autoHoverTimer_Tick;
                        autoHoverTimer.Start();
                    }
                    else
                    {
                        autoHoverTimer.Stop();
                        autoHoverTimer.Start();
                    }
                }
                else
                {
                    dontUpdateColor = false;
                }

                Refresh();
            }
        }

        public event ScrollEventHandler Scroll;

        private void OnScroll(ScrollEventType type, int oldValue, int newValue, ScrollOrientation orientation)
        {
            if (this.Scroll == null)
            {
                return;
            }

            if (orientation == ScrollOrientation.HorizontalScroll)
            {
                if (type != ScrollEventType.EndScroll && isFirstScrollEventHorizontal)
                {
                    type = ScrollEventType.First;
                }
                else if (!isFirstScrollEventHorizontal && type == ScrollEventType.EndScroll)
                {
                    isFirstScrollEventHorizontal = true;
                }
            }
            else if (type != ScrollEventType.EndScroll && isFirstScrollEventVertical)
            {
                type = ScrollEventType.First;
            }
            else if (!isFirstScrollEventHorizontal && type == ScrollEventType.EndScroll)
            {
                isFirstScrollEventVertical = true;
            }

            this.Scroll(this, new ScrollEventArgs(type, oldValue, newValue, orientation));
        }

        private void autoHoverTimer_Tick(object sender, EventArgs e)
        {
            isHovered = false;
            Invalidate();
            autoHoverTimer.Stop();
        }

        public MetroScrollBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
            base.Width = 10;
            base.Height = 200;
            SetupScrollBar();
            progressTimer.Interval = 20;
            progressTimer.Tick += ProgressTimerTick;
        }

        public MetroScrollBar(MetroScrollOrientation orientation)
            : this()
        {
            Orientation = orientation;
        }

        public MetroScrollBar(MetroScrollOrientation orientation, int width)
            : this(orientation)
        {
            base.Width = width;
        }

        public bool HitTest(Point point)
        {
            return thumbRectangle.Contains(point);
        }

        public void BeginUpdate()
        {
            WinApi.SendMessage(base.Handle, 11, param: false, 0);
            inUpdate = true;
        }

        public void EndUpdate()
        {
            WinApi.SendMessage(base.Handle, 11, param: true, 0);
            inUpdate = false;
            SetupScrollBar();
            Refresh();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color color = (base.Parent == null) ? MetroPaint.BackColor.Form(Theme) : ((!(base.Parent is IMetroControl)) ? base.Parent.BackColor : MetroPaint.BackColor.Form(Theme));
            Color thumbColor;
            Color barColor;
            if (isHovered && !isPressed && base.Enabled)
            {
                thumbColor = MetroPaint.BackColor.ScrollBar.Thumb.Hover(Theme);
                barColor = MetroPaint.BackColor.ScrollBar.Bar.Hover(Theme);
            }
            else if (isHovered && isPressed && base.Enabled)
            {
                thumbColor = MetroPaint.BackColor.ScrollBar.Thumb.Press(Theme);
                barColor = MetroPaint.BackColor.ScrollBar.Bar.Press(Theme);
            }
            else if (!base.Enabled)
            {
                thumbColor = MetroPaint.BackColor.ScrollBar.Thumb.Disabled(Theme);
                barColor = MetroPaint.BackColor.ScrollBar.Bar.Disabled(Theme);
            }
            else
            {
                thumbColor = MetroPaint.BackColor.ScrollBar.Thumb.Normal(Theme);
                barColor = MetroPaint.BackColor.ScrollBar.Bar.Normal(Theme);
            }

            e.Graphics.Clear(color);
            DrawScrollBar(e.Graphics, color, thumbColor, barColor);
        }

        private void DrawScrollBar(Graphics g, Color backColor, Color thumbColor, Color barColor)
        {
            if (useBarColor)
            {
                using (SolidBrush brush = new SolidBrush(barColor))
                {
                    g.FillRectangle(brush, base.ClientRectangle);
                }
            }

            using (SolidBrush brush2 = new SolidBrush(backColor))
            {
                Rectangle rect = new Rectangle(thumbRectangle.X - 1, thumbRectangle.Y - 1, thumbRectangle.Width + 2, thumbRectangle.Height + 2);
                g.FillRectangle(brush2, rect);
            }

            using (SolidBrush brush3 = new SolidBrush(thumbColor))
            {
                g.FillRectangle(brush3, thumbRectangle);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            isHovered = false;
            isPressed = false;
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnEnter(EventArgs e)
        {
            Invalidate();
            base.OnEnter(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            isHovered = false;
            isPressed = false;
            Invalidate();
            base.OnLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int num = e.Delta / 120 * (maximum - minimum) / mouseWheelBarPartitions;
            if (Orientation == MetroScrollOrientation.Vertical)
            {
                Value -= num;
            }
            else
            {
                Value += num;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                Point location = e.Location;
                if (thumbRectangle.Contains(location))
                {
                    thumbClicked = true;
                    thumbPosition = ((metroOrientation == MetroScrollOrientation.Vertical) ? (location.Y - thumbRectangle.Y) : (location.X - thumbRectangle.X));
                    Invalidate(thumbRectangle);
                    return;
                }

                trackPosition = ((metroOrientation == MetroScrollOrientation.Vertical) ? location.Y : location.X);
                if (trackPosition < ((metroOrientation == MetroScrollOrientation.Vertical) ? thumbRectangle.Y : thumbRectangle.X))
                {
                    topBarClicked = true;
                }
                else
                {
                    bottomBarClicked = true;
                }

                ProgressThumb(enableTimer: true);
            }
            else if (e.Button == MouseButtons.Right)
            {
                trackPosition = ((metroOrientation == MetroScrollOrientation.Vertical) ? e.Y : e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isPressed = false;
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                if (thumbClicked)
                {
                    thumbClicked = false;
                    OnScroll(ScrollEventType.EndScroll, -1, curValue, scrollOrientation);
                }
                else if (topBarClicked)
                {
                    topBarClicked = false;
                    StopTimer();
                }
                else if (bottomBarClicked)
                {
                    bottomBarClicked = false;
                    StopTimer();
                }

                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
            ResetScrollStatus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                if (!thumbClicked)
                {
                    return;
                }

                int num = curValue;
                int num2 = (metroOrientation == MetroScrollOrientation.Vertical) ? e.Location.Y : e.Location.X;
                int num3 = (metroOrientation == MetroScrollOrientation.Vertical) ? (num2 / base.Height / thumbHeight) : (num2 / base.Width / thumbWidth);
                if (num2 <= thumbTopLimit + thumbPosition)
                {
                    ChangeThumbPosition(thumbTopLimit);
                    curValue = minimum;
                    Invalidate();
                }
                else if (num2 >= thumbBottomLimitTop + thumbPosition)
                {
                    ChangeThumbPosition(thumbBottomLimitTop);
                    curValue = maximum;
                    Invalidate();
                }
                else
                {
                    ChangeThumbPosition(num2 - thumbPosition);
                    int num4;
                    int num5;
                    if (Orientation == MetroScrollOrientation.Vertical)
                    {
                        num4 = base.Height - num3;
                        num5 = thumbRectangle.Y;
                    }
                    else
                    {
                        num4 = base.Width - num3;
                        num5 = thumbRectangle.X;
                    }

                    float num6 = 0f;
                    if (num4 != 0)
                    {
                        num6 = (float)num5 / (float)num4;
                    }

                    curValue = Convert.ToInt32(num6 * (float)(maximum - minimum) + (float)minimum);
                }

                if (num != curValue)
                {
                    OnScroll(ScrollEventType.ThumbTrack, num, curValue, scrollOrientation);
                    Refresh();
                }
            }
            else if (!base.ClientRectangle.Contains(e.Location))
            {
                ResetScrollStatus();
            }
            else if (e.Button == MouseButtons.None)
            {
                if (thumbRectangle.Contains(e.Location))
                {
                    Invalidate(thumbRectangle);
                }
                else if (base.ClientRectangle.Contains(e.Location))
                {
                    Invalidate();
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            isHovered = true;
            isPressed = true;
            Invalidate();
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            isHovered = false;
            isPressed = false;
            Invalidate();
            base.OnKeyUp(e);
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, height, specified);
            if (base.DesignMode)
            {
                SetupScrollBar();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            SetupScrollBar();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            Keys keys = Keys.Up;
            Keys keys2 = Keys.Down;
            if (Orientation == MetroScrollOrientation.Horizontal)
            {
                keys = Keys.Left;
                keys2 = Keys.Right;
            }

            if (keyData == keys)
            {
                Value -= smallChange;
                return true;
            }

            if (keyData == keys2)
            {
                Value += smallChange;
                return true;
            }

            switch (keyData)
            {
                case Keys.Prior:
                    Value = GetValue(smallIncrement: false, up: true);
                    return true;
                case Keys.Next:
                    if (curValue + largeChange > maximum)
                    {
                        Value = maximum;
                    }
                    else
                    {
                        Value += largeChange;
                    }

                    return true;
                case Keys.Home:
                    Value = minimum;
                    return true;
                case Keys.End:
                    Value = maximum;
                    return true;
                default:
                    return base.ProcessDialogKey(keyData);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        private void SetupScrollBar()
        {
            if (!inUpdate)
            {
                if (Orientation == MetroScrollOrientation.Vertical)
                {
                    thumbWidth = ((base.Width > 0) ? base.Width : 10);
                    thumbHeight = GetThumbSize();
                    clickedBarRectangle = base.ClientRectangle;
                    clickedBarRectangle.Inflate(-1, -1);
                    thumbRectangle = new Rectangle(base.ClientRectangle.X, base.ClientRectangle.Y, thumbWidth, thumbHeight);
                    thumbPosition = thumbRectangle.Height / 2;
                    thumbBottomLimitBottom = base.ClientRectangle.Bottom;
                    thumbBottomLimitTop = thumbBottomLimitBottom - thumbRectangle.Height;
                    thumbTopLimit = base.ClientRectangle.Y;
                }
                else
                {
                    thumbHeight = ((base.Height > 0) ? base.Height : 10);
                    thumbWidth = GetThumbSize();
                    clickedBarRectangle = base.ClientRectangle;
                    clickedBarRectangle.Inflate(-1, -1);
                    thumbRectangle = new Rectangle(base.ClientRectangle.X, base.ClientRectangle.Y, thumbWidth, thumbHeight);
                    thumbPosition = thumbRectangle.Width / 2;
                    thumbBottomLimitBottom = base.ClientRectangle.Right;
                    thumbBottomLimitTop = thumbBottomLimitBottom - thumbRectangle.Width;
                    thumbTopLimit = base.ClientRectangle.X;
                }

                ChangeThumbPosition(GetThumbPosition());
                Refresh();
            }
        }

        private void ResetScrollStatus()
        {
            bottomBarClicked = (topBarClicked = false);
            StopTimer();
            Refresh();
        }

        private void ProgressTimerTick(object sender, EventArgs e)
        {
            ProgressThumb(enableTimer: true);
        }

        private int GetValue(bool smallIncrement, bool up)
        {
            int num;
            if (up)
            {
                num = curValue - (smallIncrement ? smallChange : largeChange);
                if (num < minimum)
                {
                    num = minimum;
                }
            }
            else
            {
                num = curValue + (smallIncrement ? smallChange : largeChange);
                if (num > maximum)
                {
                    num = maximum;
                }
            }

            return num;
        }

        private int GetThumbPosition()
        {
            if (thumbHeight == 0 || thumbWidth == 0)
            {
                return 0;
            }

            int num = (metroOrientation == MetroScrollOrientation.Vertical) ? (thumbPosition / base.Height / thumbHeight) : (thumbPosition / base.Width / thumbWidth);
            int num2 = (Orientation != MetroScrollOrientation.Vertical) ? (base.Width - num) : (base.Height - num);
            int num3 = maximum - minimum;
            float num4 = 0f;
            if (num3 != 0)
            {
                num4 = ((float)curValue - (float)minimum) / (float)num3;
            }

            return Math.Max(thumbTopLimit, Math.Min(thumbBottomLimitTop, Convert.ToInt32(num4 * (float)num2)));
        }

        private int GetThumbSize()
        {
            int num = (metroOrientation == MetroScrollOrientation.Vertical) ? base.Height : base.Width;
            if (maximum == 0 || largeChange == 0)
            {
                return num;
            }

            float val = (float)largeChange * (float)num / (float)maximum;
            return Convert.ToInt32(Math.Min(num, Math.Max(val, 10f)));
        }

        private void EnableTimer()
        {
            if (!progressTimer.Enabled)
            {
                progressTimer.Interval = 600;
                progressTimer.Start();
            }
            else
            {
                progressTimer.Interval = 10;
            }
        }

        private void StopTimer()
        {
            progressTimer.Stop();
        }

        private void ChangeThumbPosition(int position)
        {
            if (Orientation == MetroScrollOrientation.Vertical)
            {
                thumbRectangle.Y = position;
            }
            else
            {
                thumbRectangle.X = position;
            }
        }

        private void ProgressThumb(bool enableTimer)
        {
            int num = curValue;
            ScrollEventType type = ScrollEventType.First;
            int num2;
            int num3;
            if (Orientation == MetroScrollOrientation.Vertical)
            {
                num2 = thumbRectangle.Y;
                num3 = thumbRectangle.Height;
            }
            else
            {
                num2 = thumbRectangle.X;
                num3 = thumbRectangle.Width;
            }

            if (bottomBarClicked && num2 + num3 < trackPosition)
            {
                type = ScrollEventType.LargeIncrement;
                curValue = GetValue(smallIncrement: false, up: false);
                if (curValue == maximum)
                {
                    ChangeThumbPosition(thumbBottomLimitTop);
                    type = ScrollEventType.Last;
                }
                else
                {
                    ChangeThumbPosition(Math.Min(thumbBottomLimitTop, GetThumbPosition()));
                }
            }
            else if (topBarClicked && num2 > trackPosition)
            {
                type = ScrollEventType.LargeDecrement;
                curValue = GetValue(smallIncrement: false, up: true);
                if (curValue == minimum)
                {
                    ChangeThumbPosition(thumbTopLimit);
                    type = ScrollEventType.First;
                }
                else
                {
                    ChangeThumbPosition(Math.Max(thumbTopLimit, GetThumbPosition()));
                }
            }

            if (num != curValue)
            {
                OnScroll(type, num, curValue, scrollOrientation);
                Invalidate();
                if (enableTimer)
                {
                    EnableTimer();
                }
            }
        }
    }


    [ToolboxBitmap(typeof(Panel))]
    public class MetroPanel : Panel, IMetroControl
    {
        private MetroColorStyle metroStyle = MetroColorStyle.Blue;

        private MetroThemeStyle metroTheme;

        private MetroStyleManager metroStyleManager;

        private MetroScrollBar verticalScrollbar = new MetroScrollBar(MetroScrollOrientation.Vertical);

        private MetroScrollBar horizontalScrollbar = new MetroScrollBar(MetroScrollOrientation.Horizontal);

        [Category("Metro Appearance")]
        private bool showHorizontalScrollbar;

        [Category("Metro Appearance")]
        private bool showVerticalScrollbar;

        private bool useCustomBackground;

        [Category("Metro Appearance")]
        public MetroColorStyle Style
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Style;
                }

                return metroStyle;
            }
            set
            {
                metroStyle = value;
            }
        }

        [Category("Metro Appearance")]
        public MetroThemeStyle Theme
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Theme;
                }

                return metroTheme;
            }
            set
            {
                metroTheme = value;
            }
        }

        [Browsable(false)]
        public MetroStyleManager StyleManager
        {
            get
            {
                return metroStyleManager;
            }
            set
            {
                metroStyleManager = value;
            }
        }

        public bool HorizontalScrollbar
        {
            get
            {
                return showHorizontalScrollbar;
            }
            set
            {
                showHorizontalScrollbar = value;
            }
        }

        [Category("Metro Appearance")]
        public int HorizontalScrollbarSize
        {
            get
            {
                return horizontalScrollbar.ScrollbarSize;
            }
            set
            {
                horizontalScrollbar.ScrollbarSize = value;
            }
        }

        [Category("Metro Appearance")]
        public bool HorizontalScrollbarBarColor
        {
            get
            {
                return horizontalScrollbar.UseBarColor;
            }
            set
            {
                horizontalScrollbar.UseBarColor = value;
            }
        }

        [Category("Metro Appearance")]
        public bool HorizontalScrollbarHighlightOnWheel
        {
            get
            {
                return horizontalScrollbar.HighlightOnWheel;
            }
            set
            {
                horizontalScrollbar.HighlightOnWheel = value;
            }
        }

        public bool VerticalScrollbar
        {
            get
            {
                return showVerticalScrollbar;
            }
            set
            {
                showVerticalScrollbar = value;
            }
        }

        [Category("Metro Appearance")]
        public int VerticalScrollbarSize
        {
            get
            {
                return verticalScrollbar.ScrollbarSize;
            }
            set
            {
                verticalScrollbar.ScrollbarSize = value;
            }
        }

        [Category("Metro Appearance")]
        public bool VerticalScrollbarBarColor
        {
            get
            {
                return verticalScrollbar.UseBarColor;
            }
            set
            {
                verticalScrollbar.UseBarColor = value;
            }
        }

        [Category("Metro Appearance")]
        public bool VerticalScrollbarHighlightOnWheel
        {
            get
            {
                return verticalScrollbar.HighlightOnWheel;
            }
            set
            {
                verticalScrollbar.HighlightOnWheel = value;
            }
        }

        [Category("Metro Appearance")]
        public new bool AutoScroll
        {
            get
            {
                return base.AutoScroll;
            }
            set
            {
                if (value)
                {
                    showHorizontalScrollbar = true;
                    showVerticalScrollbar = true;
                }

                base.AutoScroll = value;
            }
        }

        [Category("Metro Appearance")]
        public bool CustomBackground
        {
            get
            {
                return useCustomBackground;
            }
            set
            {
                useCustomBackground = value;
            }
        }

        public MetroPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
            base.Controls.Add(verticalScrollbar);
            base.Controls.Add(horizontalScrollbar);
            verticalScrollbar.UseBarColor = true;
            horizontalScrollbar.UseBarColor = true;
            verticalScrollbar.Scroll += VerticalScrollbarScroll;
            horizontalScrollbar.Scroll += HorizontalScrollbarScroll;
        }

        private void HorizontalScrollbarScroll(object sender, ScrollEventArgs e)
        {
            base.AutoScrollPosition = new Point(e.NewValue, verticalScrollbar.Value);
            UpdateScrollBarPositions();
        }

        private void VerticalScrollbarScroll(object sender, ScrollEventArgs e)
        {
            base.AutoScrollPosition = new Point(horizontalScrollbar.Value, e.NewValue);
            UpdateScrollBarPositions();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color color = MetroPaint.BackColor.Form(Theme);
            if (useCustomBackground)
            {
                color = BackColor;
            }

            e.Graphics.Clear(color);
            if (base.DesignMode)
            {
                horizontalScrollbar.Visible = false;
                verticalScrollbar.Visible = false;
                return;
            }

            UpdateScrollBarPositions();
            if (HorizontalScrollbar)
            {
                horizontalScrollbar.Visible = base.HorizontalScroll.Visible;
            }

            if (base.HorizontalScroll.Visible)
            {
                horizontalScrollbar.Minimum = base.HorizontalScroll.Minimum;
                horizontalScrollbar.Maximum = base.HorizontalScroll.Maximum;
                horizontalScrollbar.SmallChange = base.HorizontalScroll.SmallChange;
                horizontalScrollbar.LargeChange = base.HorizontalScroll.LargeChange;
            }

            if (VerticalScrollbar)
            {
                verticalScrollbar.Visible = base.VerticalScroll.Visible;
            }

            if (base.VerticalScroll.Visible)
            {
                verticalScrollbar.Minimum = base.VerticalScroll.Minimum;
                verticalScrollbar.Maximum = base.VerticalScroll.Maximum;
                verticalScrollbar.SmallChange = base.VerticalScroll.SmallChange;
                verticalScrollbar.LargeChange = base.VerticalScroll.LargeChange;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            verticalScrollbar.Value = base.VerticalScroll.Value;
            horizontalScrollbar.Value = base.HorizontalScroll.Value;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (!base.DesignMode)
            {
                WinApi.ShowScrollBar(base.Handle, 3, 0);
            }
        }

        private void UpdateScrollBarPositions()
        {
            if (base.DesignMode)
            {
                return;
            }

            if (!AutoScroll)
            {
                verticalScrollbar.Visible = false;
                horizontalScrollbar.Visible = false;
                return;
            }

            if (VerticalScrollbar)
            {
                if (base.VerticalScroll.Visible)
                {
                    verticalScrollbar.Location = new Point(base.ClientRectangle.Width - verticalScrollbar.Width, base.ClientRectangle.Y);
                    verticalScrollbar.Height = base.ClientRectangle.Height;
                }
            }
            else
            {
                verticalScrollbar.Visible = false;
            }

            if (HorizontalScrollbar)
            {
                if (base.HorizontalScroll.Visible)
                {
                    horizontalScrollbar.Location = new Point(base.ClientRectangle.X, base.ClientRectangle.Height - horizontalScrollbar.Height);
                    horizontalScrollbar.Width = base.ClientRectangle.Width;
                }
            }
            else
            {
                horizontalScrollbar.Visible = false;
            }
        }
    }



    public sealed class MetroTaskWindow : MetroForm
    {
        private static MetroTaskWindow singletonWindow;

        private bool cancelTimer;

        private readonly int closeTime;

        private int elapsedTime;

        private int progressWidth;

        private DelayedCall timer;

        private readonly MetroPanel controlContainer;

        private bool isInitialized;

        public bool CancelTimer
        {
            get
            {
                return cancelTimer;
            }
            set
            {
                cancelTimer = value;
            }
        }

        public static void ShowTaskWindow(IWin32Window parent, string title, Control userControl, int secToClose)
        {
            if (singletonWindow != null)
            {
                singletonWindow.Close();
                singletonWindow.Dispose();
                singletonWindow = null;
            }

            singletonWindow = new MetroTaskWindow(secToClose, userControl);
            singletonWindow.Text = title;
            singletonWindow.Resizable = false;
            singletonWindow.StartPosition = FormStartPosition.Manual;
            if (parent != null && parent is IMetroForm)
            {
                singletonWindow.Theme = ((IMetroForm)parent).Theme;
                singletonWindow.Style = ((IMetroForm)parent).Style;
                singletonWindow.StyleManager = (((IMetroForm)parent).StyleManager.Clone() as MetroStyleManager);
                if (singletonWindow.StyleManager != null)
                {
                    singletonWindow.StyleManager.OwnerForm = singletonWindow;
                }
            }

            singletonWindow.Show(parent);
        }

        public static bool IsVisible()
        {
            if (singletonWindow != null)
            {
                return singletonWindow.Visible;
            }

            return false;
        }

        public static void ShowTaskWindow(IWin32Window parent, string text, Control userControl)
        {
            ShowTaskWindow(parent, text, userControl, 0);
        }

        public static void ShowTaskWindow(string text, Control userControl, int secToClose)
        {
            ShowTaskWindow(null, text, userControl, secToClose);
        }

        public static void ShowTaskWindow(string text, Control userControl)
        {
            ShowTaskWindow(null, text, userControl);
        }

        public static void CancelAutoClose()
        {
            if (singletonWindow != null)
            {
                singletonWindow.CancelTimer = true;
            }
        }

        public static void ForceClose()
        {
            if (singletonWindow != null)
            {
                CancelAutoClose();
                singletonWindow.Close();
                singletonWindow.Dispose();
                singletonWindow = null;
            }
        }

        public MetroTaskWindow()
        {
            controlContainer = new MetroPanel();
            base.Controls.Add(controlContainer);
        }

        public MetroTaskWindow(int duration, Control userControl)
            : this()
        {
            controlContainer.Controls.Add(userControl);
            userControl.Dock = DockStyle.Fill;
            closeTime = duration * 500;
            if (closeTime > 0)
            {
                timer = DelayedCall.Start(UpdateProgress, 5);
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            if (!isInitialized)
            {
                controlContainer.Theme = base.Theme;
                controlContainer.Style = base.Style;
                controlContainer.StyleManager = base.StyleManager;
                base.MaximizeBox = false;
                base.MinimizeBox = false;
                base.Movable = false;
                base.TopMost = true;
                base.FormBorderStyle = FormBorderStyle.FixedDialog;
                base.Size = new Size(400, 200);
                Taskbar taskbar = new Taskbar();
                switch (taskbar.Position)
                {
                    case TaskbarPosition.Left:
                        base.Location = new Point(taskbar.Bounds.Width + 5, taskbar.Bounds.Height - base.Height - 5);
                        break;
                    case TaskbarPosition.Top:
                        base.Location = new Point(taskbar.Bounds.Width - base.Width - 5, taskbar.Bounds.Height + 5);
                        break;
                    case TaskbarPosition.Right:
                        base.Location = new Point(taskbar.Bounds.X - base.Width - 5, taskbar.Bounds.Height - base.Height - 5);
                        break;
                    case TaskbarPosition.Bottom:
                        base.Location = new Point(taskbar.Bounds.Width - base.Width - 5, taskbar.Bounds.Y - base.Height - 5);
                        break;
                    default:
                        base.Location = new Point(Screen.PrimaryScreen.Bounds.Width - base.Width - 5, Screen.PrimaryScreen.Bounds.Height - base.Height - 5);
                        break;
                }

                controlContainer.Location = new Point(0, 60);
                controlContainer.Size = new Size(base.Width - 40, base.Height - 80);
                controlContainer.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
                base.StyleManager.UpdateOwnerForm();
                isInitialized = true;
                MoveAnimation moveAnimation = new MoveAnimation();
                moveAnimation.Start(controlContainer, new Point(20, 60), TransitionType.EaseInOutCubic, 15);
            }

            base.OnActivated(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (SolidBrush brush = new SolidBrush(MetroPaint.BackColor.Form(base.Theme)))
            {
                e.Graphics.FillRectangle(brush, new Rectangle(base.Width - progressWidth, 0, progressWidth, 5));
            }
        }

        private void UpdateProgress()
        {
            if (elapsedTime == closeTime)
            {
                timer.Dispose();
                timer = null;
                Close();
                return;
            }

            elapsedTime += 5;
            if (cancelTimer)
            {
                elapsedTime = 0;
            }

            double num = (double)elapsedTime / ((double)closeTime / 100.0);
            progressWidth = (int)((double)base.Width * (num / 100.0));
            Invalidate(new Rectangle(0, 0, base.Width, 5));
            if (!cancelTimer)
            {
                timer.Reset();
            }
        }
    }



    public class MetroForm : Form, IMetroForm, IDisposable
    {
        private MetroColorStyle metroStyle = MetroColorStyle.Blue;

        private MetroThemeStyle metroTheme;

        private MetroStyleManager metroStyleManager;

        protected MetroFlatDropShadow metroFlatShadowForm;

        protected MetroRealisticDropShadow metroRealisticShadowForm;

        private bool isInitialized;

        private TextAlign textAlign;

        private bool isMovable = true;

        private bool displayHeader = true;

        private bool isResizable = true;

        private ShadowType shadowType = ShadowType.Flat;

        private int borderWidth = 5;

        private Dictionary<WindowButtons, MetroFormButton> windowButtonList;

        [Category("Metro Appearance")]
        public MetroColorStyle Style
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Style;
                }

                return metroStyle;
            }
            set
            {
                metroStyle = value;
            }
        }

        [Category("Metro Appearance")]
        public MetroThemeStyle Theme
        {
            get
            {
                if (StyleManager != null)
                {
                    return StyleManager.Theme;
                }

                return metroTheme;
            }
            set
            {
                metroTheme = value;
            }
        }

        [Browsable(false)]
        public MetroStyleManager StyleManager
        {
            get
            {
                return metroStyleManager;
            }
            set
            {
                metroStyleManager = value;
            }
        }

        [Browsable(true)]
        [Category("Metro Appearance")]
        public TextAlign TextAlign
        {
            get
            {
                return textAlign;
            }
            set
            {
                textAlign = value;
            }
        }

        [Browsable(false)]
        public override Color BackColor => MetroPaint.BackColor.Form(Theme);

        [DefaultValue(FormBorderStyle.None)]
        [Browsable(false)]
        public new FormBorderStyle FormBorderStyle
        {
            get
            {
                return FormBorderStyle.None;
            }
            set
            {
                base.FormBorderStyle = FormBorderStyle.None;
            }
        }

        [Category("Metro Appearance")]
        public bool Movable
        {
            get
            {
                return isMovable;
            }
            set
            {
                isMovable = value;
            }
        }

        public new Padding Padding
        {
            get
            {
                return base.Padding;
            }
            set
            {
                if (!DisplayHeader)
                {
                    if (value.Top < 30)
                    {
                        value.Top = 30;
                    }
                }
                else if (value.Top < 60)
                {
                    value.Top = 60;
                }

                base.Padding = value;
            }
        }

        [Category("Metro Appearance")]
        public bool DisplayHeader
        {
            get
            {
                return displayHeader;
            }
            set
            {
                displayHeader = value;
                if (displayHeader)
                {
                    Padding = new Padding(20, 60, 20, 20);
                }
                else
                {
                    Padding = new Padding(20, 30, 20, 20);
                }

                Invalidate();
            }
        }

        [Category("Metro Appearance")]
        public bool Resizable
        {
            get
            {
                return isResizable;
            }
            set
            {
                isResizable = value;
            }
        }

        [Category("Metro Appearance")]
        public ShadowType ShadowType
        {
            get
            {
                return shadowType;
            }
            set
            {
                shadowType = value;
            }
        }

        public MetroForm()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
            base.Name = "MetroForm";
            Padding = new Padding(20, 60, 20, 20);
            base.StartPosition = FormStartPosition.CenterScreen;
            RemoveCloseButton();
            FormBorderStyle = FormBorderStyle.None;
        }

        protected override void Dispose(bool disposing)
        {
            if (metroFlatShadowForm != null && !metroFlatShadowForm.IsDisposed)
            {
                metroFlatShadowForm.Owner = null;
                metroFlatShadowForm.Dispose();
                metroFlatShadowForm = null;
            }

            if (metroRealisticShadowForm != null && !metroRealisticShadowForm.IsDisposed)
            {
                metroRealisticShadowForm.Owner = null;
                metroRealisticShadowForm.Dispose();
                metroRealisticShadowForm = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color color = MetroPaint.BackColor.Form(Theme);
            Color foreColor = MetroPaint.ForeColor.Title(Theme);
            e.Graphics.Clear(color);
            using (SolidBrush brush = MetroPaint.GetStyleBrush(Style))
            {
                Rectangle rect = new Rectangle(0, 0, base.Width, borderWidth);
                e.Graphics.FillRectangle(brush, rect);
            }

            if (displayHeader)
            {
                switch (TextAlign)
                {
                    case TextAlign.Left:
                        TextRenderer.DrawText(e.Graphics, Text, MetroFonts.Title, new Point(20, 20), foreColor);
                        break;
                    case TextAlign.Center:
                        TextRenderer.DrawText(e.Graphics, Text, MetroFonts.Title, new Point(base.ClientRectangle.Width, 20), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter);
                        break;
                    case TextAlign.Right:
                        {
                            Rectangle rectangle = MeasureText(e.Graphics, base.ClientRectangle, MetroFonts.Title, Text, TextFormatFlags.RightToLeft);
                            TextRenderer.DrawText(e.Graphics, Text, MetroFonts.Title, new Point(base.ClientRectangle.Width - rectangle.Width, 20), foreColor, TextFormatFlags.RightToLeft);
                            break;
                        }
                }
            }

            if (Resizable && (base.SizeGripStyle == SizeGripStyle.Auto || base.SizeGripStyle == SizeGripStyle.Show))
            {
                using (SolidBrush brush2 = new SolidBrush(MetroPaint.ForeColor.Button.Disabled(Theme)))
                {
                    Size size = new Size(2, 2);
                    e.Graphics.FillRectangles(brush2, new Rectangle[6]
                    {
                        new Rectangle(new Point(base.ClientRectangle.Width - 6, base.ClientRectangle.Height - 6), size),
                        new Rectangle(new Point(base.ClientRectangle.Width - 10, base.ClientRectangle.Height - 10), size),
                        new Rectangle(new Point(base.ClientRectangle.Width - 10, base.ClientRectangle.Height - 6), size),
                        new Rectangle(new Point(base.ClientRectangle.Width - 6, base.ClientRectangle.Height - 10), size),
                        new Rectangle(new Point(base.ClientRectangle.Width - 14, base.ClientRectangle.Height - 6), size),
                        new Rectangle(new Point(base.ClientRectangle.Width - 6, base.ClientRectangle.Height - 14), size)
                    });
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!(this is MetroTaskWindow))
            {
                MetroTaskWindow.ForceClose();
            }

            if (metroFlatShadowForm != null)
            {
                metroFlatShadowForm.Visible = false;
                metroFlatShadowForm.Owner = null;
                metroFlatShadowForm = null;
            }

            if (metroRealisticShadowForm != null)
            {
                metroRealisticShadowForm.Visible = false;
                metroRealisticShadowForm.Owner = null;
                metroRealisticShadowForm = null;
            }

            base.OnClosing(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (isInitialized)
            {
                Refresh();
            }
        }

        public bool FocusMe()
        {
            return WinApi.SetForegroundWindow(base.Handle);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (metroFlatShadowForm == null && !base.DesignMode && shadowType == ShadowType.Flat)
            {
                metroFlatShadowForm = new MetroFlatDropShadow(this);
            }

            if (metroRealisticShadowForm == null && !base.DesignMode && shadowType == ShadowType.DropShadow)
            {
                metroRealisticShadowForm = new MetroRealisticDropShadow(this);
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (!isInitialized)
            {
                if (base.ControlBox)
                {
                    AddWindowButton(WindowButtons.Close);
                    if (base.MaximizeBox)
                    {
                        AddWindowButton(WindowButtons.Maximize);
                    }

                    if (base.MinimizeBox)
                    {
                        AddWindowButton(WindowButtons.Minimize);
                    }

                    UpdateWindowButtonPosition();
                }

                if (base.StartPosition == FormStartPosition.CenterScreen)
                {
                    base.Location = new Point
                    {
                        X = (Screen.PrimaryScreen.WorkingArea.Width - (base.ClientRectangle.Width + 5)) / 2,
                        Y = (Screen.PrimaryScreen.WorkingArea.Height - (base.ClientRectangle.Height + 5)) / 2
                    };
                    base.OnActivated(e);
                }

                isInitialized = true;
            }

            if (!base.DesignMode)
            {
                Refresh();
            }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            UpdateWindowButtonPosition();
        }

        protected override void WndProc(ref Message m)
        {
            if (base.MaximizeBox)
            {
                if (!WndProc_Movable(m))
                {
                    return;
                }

                base.WndProc(ref m);
            }

            if (!base.DesignMode)
            {
                if ((!base.MaximizeBox && m.Msg == 515) || !WndProc_Movable(m))
                {
                    return;
                }

                if (m.Msg == 132)
                {
                    m.Result = HitTestNCA(m.HWnd, m.WParam, m.LParam);
                }
            }

            if (!base.MaximizeBox)
            {
                base.WndProc(ref m);
            }
        }

        private bool WndProc_Movable(Message m)
        {
            if (!Movable && m.Msg == 274)
            {
                return (m.WParam.ToInt32() & 0xFFF0) != 61456;
            }

            return true;
        }

        private IntPtr HitTestNCA(IntPtr hwnd, IntPtr wparam, IntPtr lparam)
        {
            Point pt = new Point((short)(int)lparam, (short)((int)lparam >> 16));
            int num = Math.Max(Padding.Right, Padding.Bottom);
            if (Resizable && RectangleToScreen(new Rectangle(base.ClientRectangle.Width - num, base.ClientRectangle.Height - num, num, num)).Contains(pt))
            {
                return (IntPtr)17L;
            }

            if (RectangleToScreen(new Rectangle(5, 5, base.ClientRectangle.Width - 10, 50)).Contains(pt))
            {
                return (IntPtr)2L;
            }

            return (IntPtr)1L;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Movable && base.WindowState != FormWindowState.Maximized && base.Width - borderWidth > e.Location.X && e.Location.X > borderWidth && e.Location.Y > borderWidth)
            {
                MoveControl();
            }
        }

        private void MoveControl()
        {
            WinApi.ReleaseCapture();
            WinApi.SendMessage(base.Handle, 161, 2, 0);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        private void AddWindowButton(WindowButtons button)
        {
            if (windowButtonList == null)
            {
                windowButtonList = new Dictionary<WindowButtons, MetroFormButton>();
            }

            if (windowButtonList.ContainsKey(button))
            {
                return;
            }

            MetroFormButton metroFormButton = new MetroFormButton();
            switch (button)
            {
                case WindowButtons.Close:
                    metroFormButton.Text = "✕";
                    break;
                case WindowButtons.Minimize:
                    metroFormButton.Text = "\ud83d\uddd5";
                    break;
                case WindowButtons.Maximize:
                    if (base.WindowState == FormWindowState.Normal)
                    {
                        metroFormButton.Text = "\ud83d\uddd6";
                    }
                    else
                    {
                        metroFormButton.Text = "\ud83d\uddd6";
                    }

                    break;
            }

            metroFormButton.Tag = button;
            metroFormButton.Size = new Size(25, 20);
            metroFormButton.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            metroFormButton.Click += WindowButton_Click;
            base.Controls.Add(metroFormButton);
            windowButtonList.Add(button, metroFormButton);
        }

        private void WindowButton_Click(object sender, EventArgs e)
        {
            MetroFormButton metroFormButton = sender as MetroFormButton;
            if (metroFormButton == null)
            {
                return;
            }

            switch ((WindowButtons)metroFormButton.Tag)
            {
                case WindowButtons.Close:
                    Close();
                    break;
                case WindowButtons.Minimize:
                    base.WindowState = FormWindowState.Minimized;
                    break;
                case WindowButtons.Maximize:
                    if (base.WindowState == FormWindowState.Normal)
                    {
                        base.WindowState = FormWindowState.Maximized;
                        metroFormButton.Text = "\ud83d\uddd6";
                    }
                    else
                    {
                        base.WindowState = FormWindowState.Normal;
                        metroFormButton.Text = "\ud83d\uddd6";
                    }

                    break;
            }
        }

        private void UpdateWindowButtonPosition()
        {
            if (!base.ControlBox)
            {
                return;
            }

            Dictionary<int, WindowButtons> dictionary = new Dictionary<int, WindowButtons>(3)
            {
                {
                    0,
                    WindowButtons.Close
                },
                {
                    1,
                    WindowButtons.Maximize
                },
                {
                    2,
                    WindowButtons.Minimize
                }
            };
            Point location = new Point(base.ClientRectangle.Width - 40, borderWidth);
            int num = location.X - 25;
            MetroFormButton metroFormButton = null;
            if (windowButtonList.Count == 1)
            {
                foreach (KeyValuePair<WindowButtons, MetroFormButton> windowButton in windowButtonList)
                {
                    windowButton.Value.Location = location;
                }
            }
            else
            {
                foreach (KeyValuePair<int, WindowButtons> item in dictionary)
                {
                    bool flag = windowButtonList.ContainsKey(item.Value);
                    if (metroFormButton == null && flag)
                    {
                        metroFormButton = windowButtonList[item.Value];
                        metroFormButton.Location = location;
                    }
                    else if (metroFormButton != null && flag)
                    {
                        windowButtonList[item.Value].Location = new Point(num, borderWidth);
                        num -= 25;
                    }
                }
            }

            Refresh();
        }

        public void RemoveCloseButton()
        {
            IntPtr systemMenu = WinApi.GetSystemMenu(base.Handle, bRevert: false);
            if (!(systemMenu == IntPtr.Zero))
            {
                int menuItemCount = WinApi.GetMenuItemCount(systemMenu);
                if (menuItemCount > 0)
                {
                    WinApi.RemoveMenu(systemMenu, (uint)(menuItemCount - 1), 5120u);
                    WinApi.RemoveMenu(systemMenu, (uint)(menuItemCount - 2), 5120u);
                    WinApi.DrawMenuBar(base.Handle);
                }
            }
        }

        private Rectangle MeasureText(Graphics g, Rectangle clientRectangle, Font font, string text, TextFormatFlags flags)
        {
            Size proposedSize = new Size(int.MaxValue, int.MinValue);
            Size size = TextRenderer.MeasureText(g, text, font, proposedSize, flags);
            return new Rectangle(clientRectangle.X, clientRectangle.Y, size.Width, size.Height);
        }
    }

}
