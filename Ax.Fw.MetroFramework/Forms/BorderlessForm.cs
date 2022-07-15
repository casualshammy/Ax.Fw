using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Controls;
using Ax.Fw.MetroFramework.Data;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Windows.WinAPI;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

namespace Ax.Fw.MetroFramework.Forms;

public class BorderlessForm : Form
{
    private readonly Dictionary<WindowButtons, FormButton> p_windowButtons = new();
    private readonly ILifetime p_lifetime = new Lifetime();
    private bool p_isResizable;
    private volatile bool p_isInitialized;

    public BorderlessForm()
    {
        InitializeComponent();
        RemoveCloseButton();

        Padding = new Padding(20, 60, 20, 20);
        StartPosition = FormStartPosition.CenterScreen;

        StyleManager.Current.ColorsChanged
            .Subscribe(_ => Invalidate(true), p_lifetime);
    }

    [Category("Metro Appearance")]
    public bool Resizable
    {
        get
        {
            return p_isResizable;
        }
        set
        {
            p_isResizable = value;
        }
    }

    // todo
    public void PostInvoke(Action _action)
    {
        BeginInvoke(new MethodInvoker(_action.Invoke));
    }

    // todo
    public IntPtr SafeHandle
    {
        get
        {
            var handle = IntPtr.Zero;
            var res = BeginInvoke(() => handle = Handle);
            if (res.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1)))
                return handle;

            return IntPtr.Zero;
        }
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // BorderlessForm
        // 
        ClientSize = new Size(303, 261);
        FormBorderStyle = FormBorderStyle.None;
        Name = "BorderlessForm";
        ResumeLayout(false);

    }

    public void RemoveCloseButton()
    {
        IntPtr systemMenu = NativeMethods.GetSystemMenu(Handle, bRevert: false);
        if (!(systemMenu == IntPtr.Zero))
        {
            int menuItemCount = NativeMethods.GetMenuItemCount(systemMenu);
            if (menuItemCount > 0)
            {
                NativeMethods.RemoveMenu(systemMenu, (uint)(menuItemCount - 1), 5120u);
                NativeMethods.RemoveMenu(systemMenu, (uint)(menuItemCount - 2), 5120u);
                NativeMethods.DrawMenuBar(Handle);
            }
        }
    }

    private void AddWindowButton(WindowButtons _button)
    {
        if (p_windowButtons.ContainsKey(_button))
            return;

        var formButton = new FormButton();
        switch (_button)
        {
            case WindowButtons.Close:
                formButton.Text = "✕";
                break;
            case WindowButtons.Minimize:
                formButton.Text = "\ud83d\uddd5";
                break;
            case WindowButtons.Maximize:
                if (WindowState == FormWindowState.Normal)
                    formButton.Text = "\ud83d\uddd6";
                else
                    formButton.Text = "\ud83d\uddd6";

                break;
        }

        formButton.Tag = _button;
        formButton.Size = new Size(25, 20);
        formButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        formButton.Click += WindowButton_Click;
        Controls.Add(formButton);
        p_windowButtons.Add(_button, formButton);
    }

    private void UpdateWindowButtonPosition()
    {
        if (!ControlBox)
            return;

        var dictionary = new Dictionary<int, WindowButtons>(3)
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
        var location = new Point(ClientRectangle.Width - 40, 5);
        int num = location.X - 25;
        FormButton? formButton = null;
        if (p_windowButtons.Count == 1)
        {
            p_windowButtons.First().Value.Location = location;
        }
        else
        {
            foreach (var item in dictionary)
            {
                bool flag = p_windowButtons.ContainsKey(item.Value);
                if (formButton == null && flag)
                {
                    formButton = p_windowButtons[item.Value];
                    formButton.Location = location;
                }
                else if (formButton != null && flag)
                {
                    p_windowButtons[item.Value].Location = new Point(num, 5);
                    num -= 25;
                }
            }
        }

        Refresh();
    }

    private void WindowButton_Click(object? _sender, EventArgs _e)
    {
        if (_sender is not FormButton formButton)
            return;

        switch ((WindowButtons)formButton.Tag)
        {
            case WindowButtons.Close:
                Close();
                break;
            case WindowButtons.Minimize:
                WindowState = FormWindowState.Minimized;
                break;
            case WindowButtons.Maximize:
                if (WindowState == FormWindowState.Normal)
                {
                    WindowState = FormWindowState.Maximized;
                    formButton.Text = "\ud83d\uddd6";
                }
                else
                {
                    WindowState = FormWindowState.Normal;
                    formButton.Text = "\ud83d\uddd6";
                }

                break;
        }
    }

    protected override void WndProc(ref Message _m)
    {
        if (MaximizeBox)
            base.WndProc(ref _m);

        if (!DesignMode)
        {
            if (!MaximizeBox && _m.Msg == 515)
                return;

            if (_m.Msg == 132)
                _m.Result = HitTestNCA(_m.HWnd, _m.WParam, _m.LParam);
        }

        if (!MaximizeBox)
            base.WndProc(ref _m);
    }

    protected override void OnActivated(EventArgs _e)
    {
        base.OnActivated(_e);
        if (!p_isInitialized)
        {
            if (ControlBox)
            {
                AddWindowButton(WindowButtons.Close);
                if (MaximizeBox)
                {
                    AddWindowButton(WindowButtons.Maximize);
                }

                if (MinimizeBox)
                {
                    AddWindowButton(WindowButtons.Minimize);
                }

                UpdateWindowButtonPosition();
            }

            if (StartPosition == FormStartPosition.CenterScreen)
            {
                Location = new Point
                {
                    X = (Screen.PrimaryScreen.WorkingArea.Width - (ClientRectangle.Width + 5)) / 2,
                    Y = (Screen.PrimaryScreen.WorkingArea.Height - (ClientRectangle.Height + 5)) / 2
                };
                base.OnActivated(_e);
            }

            p_isInitialized = true;
        }

        if (!DesignMode)
        {
            Refresh();
        }
    }

    protected override void OnPaint(PaintEventArgs _e)
    {
        Color color = StyleManager.Current.BackColor;
        Color foreColor = StyleManager.Current.PrimaryColor;
        _e.Graphics.Clear(color);

        var edgeRectangles = new Rectangle[4]
        {
            new Rectangle(0, 0, Width, 5),
            new Rectangle(Width - 3, 0, 3, Height),
            new Rectangle(0, 0,3, Height),
            new Rectangle(0, Height - 3, Width, 3)
        };
        foreach (var rectangle in edgeRectangles)
        {
            var angle = 0f;
            if (rectangle.Width > rectangle.Height)
            {
                if (rectangle.Y == 0)
                    angle = 0f;
                else
                    angle = 180f;
            }
            else
            {
                if (rectangle.X == 0)
                    angle = 90f;
                else
                    angle = 270f;
            }
            using (var brush = new LinearGradientBrush(rectangle, foreColor, StyleManager.Current.SecondaryColor, angle))
                _e.Graphics.FillRectangle(brush, rectangle);
        }

        //TextRenderer.DrawText(e.Graphics, Text, FontTitle, new Point(20, 20), foreColor);

        if (Resizable && (SizeGripStyle == SizeGripStyle.Auto || SizeGripStyle == SizeGripStyle.Show))
        {
            using (var brush = new SolidBrush(foreColor))
            {
                var size = new Size(2, 2);
                _e.Graphics.FillRectangles(brush, new Rectangle[6]
                {
                    new Rectangle(new Point(ClientRectangle.Width - 6, ClientRectangle.Height - 6), size),
                    new Rectangle(new Point(ClientRectangle.Width - 10, ClientRectangle.Height - 10), size),
                    new Rectangle(new Point(ClientRectangle.Width - 10, ClientRectangle.Height - 6), size),
                    new Rectangle(new Point(ClientRectangle.Width - 6, ClientRectangle.Height - 10), size),
                    new Rectangle(new Point(ClientRectangle.Width - 14, ClientRectangle.Height - 6), size),
                    new Rectangle(new Point(ClientRectangle.Width - 6, ClientRectangle.Height - 14), size)
                });
            }
        }


    }

    protected override void OnMouseDown(MouseEventArgs _e)
    {
        base.OnMouseDown(_e);

        if (_e.Button == MouseButtons.Left && WindowState != FormWindowState.Maximized && Width - 5 > _e.Location.X && _e.Location.X > 5 && _e.Location.Y > 5)
            MoveControl();
    }

    protected override void OnResize(EventArgs _e)
    {
        base.OnResize(_e);
        Invalidate();
        UpdateWindowButtonPosition();
    }

    protected override void Dispose(bool _disposing)
    {
        base.Dispose(_disposing);
        p_lifetime?.Complete();
    }

    private void MoveControl()
    {
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, 161, new IntPtr(2), IntPtr.Zero);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    private IntPtr HitTestNCA(IntPtr _hwnd, IntPtr _wparam, IntPtr _lparam)
    {
        var pt = new Point((short)(int)_lparam, (short)((int)_lparam >> 16));
        var num = Math.Max(Padding.Right, Padding.Bottom);
        if (Resizable && RectangleToScreen(new Rectangle(ClientRectangle.Width - num, ClientRectangle.Height - num, num, num)).Contains(pt))
            return (IntPtr)17L;

        if (RectangleToScreen(new Rectangle(5, 5, ClientRectangle.Width - 10, 50)).Contains(pt))
            return (IntPtr)2L;

        return (IntPtr)1L;
    }

    

}
