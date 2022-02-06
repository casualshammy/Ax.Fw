using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI.Forms
{
    public class MetroRealisticDropShadow : Form
    {
        private Form shadowTargetForm;

        private Point Offset = new Point(-15, -15);

        private bool isBringingToFront;

        private Bitmap getShadow;

        private Timer timer = new Timer();

        private long lastResizedOn;

        public MetroRealisticDropShadow(Form parentForm)
        {
            shadowTargetForm = parentForm;
            base.FormBorderStyle = FormBorderStyle.None;
            base.ShowInTaskbar = false;
            uint windowLong = WinApi.GetWindowLong(base.Handle, -20);
            windowLong = (windowLong | 0x80000 | 0x20);
            WinApi.SetWindowLong(base.Handle, -20, windowLong);
            base.StartPosition = parentForm.StartPosition;
            parentForm.Activated += shadowTargetForm_Activated;
            base.Deactivate += MetroRealisticDropShadow_Deactivated;
            parentForm.Move += shadowTargetForm_Move;
            parentForm.Resize += shadowTargetForm_Resize;
            parentForm.ResizeEnd += shadowTargetForm_ResizeEnd;
            parentForm.Owner = this;
            base.Bounds = GetBounds();
            base.Load += MetroRealisticDropShadow_Load;
        }

        private void MetroRealisticDropShadow_Load(object sendr, EventArgs e)
        {
            timer.Interval = 50;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sendr, EventArgs e)
        {
            timer.Tick -= timer_Tick;
            timer.Stop();
            getShadow = DrawBlurBorder();
            SetBitmap(getShadow, byte.MaxValue);
        }

        private Rectangle GetBounds()
        {
            return new Rectangle(shadowTargetForm.Location.X + Offset.X, shadowTargetForm.Location.Y + Offset.Y, shadowTargetForm.ClientRectangle.Width + Math.Abs(Offset.X * 2), shadowTargetForm.ClientRectangle.Height + Math.Abs(Offset.Y * 2));
        }

        private void shadowTargetForm_Activated(object o, EventArgs e)
        {
            base.Visible = (shadowTargetForm.WindowState == FormWindowState.Normal);
            if (base.Visible)
            {
                Show();
            }

            if (isBringingToFront)
            {
                isBringingToFront = false;
            }
            else
            {
                BringToFront();
            }
        }

        private void MetroRealisticDropShadow_Deactivated(object o, EventArgs e)
        {
            isBringingToFront = true;
        }

        private void shadowTargetForm_Move(object o, EventArgs e)
        {
            if (o is Form)
            {
                base.Bounds = GetBounds();
            }
        }

        private void shadowTargetForm_Resize(object o, EventArgs e)
        {
            if (o is Form)
            {
                base.Bounds = GetBounds();
            }

            base.Visible = false;
            if (DateTime.Now.Ticks - lastResizedOn > 100000)
            {
                lastResizedOn = DateTime.Now.Ticks;
                Invalidate();
            }
        }

        private void shadowTargetForm_ResizeEnd(object o, EventArgs e)
        {
            base.Visible = true;
            lastResizedOn = 0L;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            getShadow = DrawBlurBorder();
            SetBitmap(getShadow, byte.MaxValue);
        }

        private void SetBitmap(Bitmap bitmap, byte opacity)
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                throw new ApplicationException("The bitmap must be 32ppp with alpha-channel.");
            }

            IntPtr dC = WinApi.GetDC(IntPtr.Zero);
            IntPtr intPtr = WinApi.CreateCompatibleDC(dC);
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr hObject = IntPtr.Zero;
            try
            {
                intPtr2 = bitmap.GetHbitmap(Color.FromArgb(0));
                hObject = WinApi.SelectObject(intPtr, intPtr2);
                WinApi.Size psize = new WinApi.Size(bitmap.Width, bitmap.Height);
                WinApi.Point pprSrc = new WinApi.Point(0, 0);
                WinApi.Point pptDst = new WinApi.Point(base.Left, base.Top);
                WinApi.BLENDFUNCTION pblend = default(WinApi.BLENDFUNCTION);
                pblend.BlendOp = 0;
                pblend.BlendFlags = 0;
                pblend.SourceConstantAlpha = opacity;
                pblend.AlphaFormat = 1;
                WinApi.UpdateLayeredWindow(base.Handle, dC, ref pptDst, ref psize, intPtr, ref pprSrc, 0, ref pblend, 2);
            }
            finally
            {
                WinApi.ReleaseDC(IntPtr.Zero, dC);
                if (intPtr2 != IntPtr.Zero)
                {
                    WinApi.SelectObject(intPtr, hObject);
                    WinApi.DeleteObject(intPtr2);
                }

                WinApi.DeleteDC(intPtr);
            }
        }

        private Bitmap DrawBlurBorder()
        {
            return (Bitmap)DrawOutsetShadow(0, 0, 40, 1, Color.Black, new Rectangle(1, 1, base.ClientRectangle.Width, base.ClientRectangle.Height));
        }

        private Image DrawOutsetShadow(int hShadow, int vShadow, int blur, int spread, Color color, Rectangle shadowCanvasArea)
        {
            Rectangle rectangle = shadowCanvasArea;
            Rectangle rectangle2 = shadowCanvasArea;
            rectangle2.Offset(hShadow, vShadow);
            rectangle2.Inflate(-blur, -blur);
            rectangle.Inflate(spread, spread);
            rectangle.Offset(hShadow, vShadow);
            Rectangle rectangle3 = rectangle;
            Bitmap bitmap = new Bitmap(rectangle3.Width, rectangle3.Height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            int cornerRadius = 0;
            do
            {
                double num = (double)(rectangle.Height - rectangle2.Height) / (double)(blur * 2 + spread * 2);
                Color fillColor = Color.FromArgb((int)(200.0 * (num * num)), color);
                Rectangle bounds = rectangle2;
                bounds.Offset(-rectangle3.Left, -rectangle3.Top);
                DrawRoundedRectangle(graphics, bounds, cornerRadius, Pens.Transparent, fillColor);
                rectangle2.Inflate(1, 1);
                cornerRadius = (int)((double)blur * (1.0 - num * num));
            }
            while (rectangle.Contains(rectangle2));
            graphics.Flush();
            graphics.Dispose();
            return bitmap;
        }

        private void DrawRoundedRectangle(Graphics g, Rectangle bounds, int cornerRadius, Pen drawPen, Color fillColor)
        {
            int num = Convert.ToInt32(Math.Ceiling(drawPen.Width));
            bounds = Rectangle.Inflate(bounds, -num, -num);
            GraphicsPath graphicsPath = new GraphicsPath();
            if (cornerRadius > 0)
            {
                graphicsPath.AddArc(bounds.X, bounds.Y, cornerRadius, cornerRadius, 180f, 90f);
                graphicsPath.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y, cornerRadius, cornerRadius, 270f, 90f);
                graphicsPath.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 0f, 90f);
                graphicsPath.AddArc(bounds.X, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 90f, 90f);
            }
            else
            {
                graphicsPath.AddRectangle(bounds);
            }

            graphicsPath.CloseAllFigures();
            if (cornerRadius > 5)
            {
                using (SolidBrush brush = new SolidBrush(fillColor))
                {
                    g.FillPath(brush, graphicsPath);
                }
            }

            if (drawPen != Pens.Transparent)
            {
                using (Pen pen = new Pen(drawPen.Color))
                {
                    LineCap lineCap3 = pen.EndCap = (pen.StartCap = LineCap.Round);
                    g.DrawPath(pen, graphicsPath);
                }
            }
        }
    }

}
