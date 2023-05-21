using Ax.Fw.Extensions;
using Ax.Fw.MetroFramework.Data;
using Ax.Fw.MetroFramework.Designers;
using System.ComponentModel;

namespace Ax.Fw.MetroFramework.Controls
{
    [Designer(typeof(MetroTextBoxDesigner))]
    public class MetroTextBox : Control
    {
        private readonly Lifetime p_lifetime = new();
        private TextBox p_baseTextBox;
        private MetroTextBoxSize p_metroTextBoxSize;
        private MetroTextBoxWeight p_metroTextBoxWeight = MetroTextBoxWeight.Regular;

        public MetroTextBox()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, value: true);

            p_baseTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = StyleManager.Current.GetTextBoxFont(p_metroTextBoxSize, p_metroTextBoxWeight),
                Location = new Point(3, 3),
                Size = new Size(Width - 6, Height - 6)
            };
            Size = new Size(p_baseTextBox.Width + 6, p_baseTextBox.Height + 6);
            Controls.Add(p_baseTextBox);

            UpdateBaseTextBox();
            AddEventHandler();

            StyleManager.Current.ColorsChanged
                .Subscribe(_ =>
                {
                    try
                    {
                        BeginInvoke(() => Invalidate(true));
                    }
                    catch { }
                }, p_lifetime);
        }

        [Category("Metro Appearance")]
        public MetroTextBoxSize FontSize
        {
            get
            {
                return p_metroTextBoxSize;
            }
            set
            {
                p_metroTextBoxSize = value;
                UpdateBaseTextBox();
            }
        }

        [Category("Metro Appearance")]
        public MetroTextBoxWeight FontWeight
        {
            get
            {
                return p_metroTextBoxWeight;
            }
            set
            {
                p_metroTextBoxWeight = value;
                UpdateBaseTextBox();
            }
        }

        public HorizontalAlignment TextAlign
        {
            get
            {
                return p_baseTextBox.TextAlign;
            }
            set
            {
                p_baseTextBox.TextAlign = value;
            }
        }

        public bool ReadOnly
        {
            get
            {
                return p_baseTextBox.ReadOnly;
            }
            set
            {
                p_baseTextBox.ReadOnly = value;
            }
        }

        public bool Multiline
        {
            get
            {
                return p_baseTextBox.Multiline;
            }
            set
            {
                p_baseTextBox.Multiline = value;
            }
        }

        public override string Text
        {
            get
            {
                return p_baseTextBox.Text;
            }
            set
            {
                p_baseTextBox.Text = value;
            }
        }

        [Browsable(false)]
        public string SelectedText
        {
            get
            {
                return p_baseTextBox.SelectedText;
            }
            set
            {
                p_baseTextBox.Text = value;
            }
        }

        public event EventHandler? AcceptsTabChanged = null;

        private void BaseTextBoxAcceptsTabChanged(object? sender, EventArgs e)
        {
            if (AcceptsTabChanged != null)
            {
                AcceptsTabChanged(this, e);
            }
        }

        private void BaseTextBoxSizeChanged(object? sender, EventArgs e)
        {
            base.OnSizeChanged(e);
        }

        private void BaseTextBoxCursorChanged(object? sender, EventArgs e)
        {
            base.OnCursorChanged(e);
        }

        private void BaseTextBoxContextMenuStripChanged(object? sender, EventArgs e)
        {
            base.OnContextMenuStripChanged(e);
        }

        private void BaseTextBoxClientSizeChanged(object? sender, EventArgs e)
        {
            base.OnClientSizeChanged(e);
        }

        private void BaseTextBoxClick(object? sender, EventArgs e)
        {
            base.OnClick(e);
        }

        private void BaseTextBoxChangeUiCues(object? sender, UICuesEventArgs e)
        {
            base.OnChangeUICues(e);
        }

        private void BaseTextBoxCausesValidationChanged(object? sender, EventArgs e)
        {
            base.OnCausesValidationChanged(e);
        }

        private void BaseTextBoxKeyUp(object? sender, KeyEventArgs e)
        {
            base.OnKeyUp(e);
        }

        private void BaseTextBoxKeyPress(object? sender, KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
        }

        private void BaseTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        private void BaseTextBoxTextChanged(object? sender, EventArgs e)
        {
            base.OnTextChanged(e);
        }

        public void Select(int start, int length)
        {
            p_baseTextBox.Select(start, length);
        }

        public void SelectAll()
        {
            p_baseTextBox.SelectAll();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(StyleManager.Current.BackColor);
            p_baseTextBox.BackColor = StyleManager.Current.BackColor;
            p_baseTextBox.ForeColor = StyleManager.Current.PrimaryColor;

            using (var pen = new Pen(StyleManager.Current.PrimaryColor))
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
        }

        public override void Refresh()
        {
            base.Refresh();
            UpdateBaseTextBox();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateBaseTextBox();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            p_lifetime.End();
        }

        private void AddEventHandler()
        {
            p_baseTextBox.AcceptsTabChanged += BaseTextBoxAcceptsTabChanged;
            p_baseTextBox.CausesValidationChanged += BaseTextBoxCausesValidationChanged;
            p_baseTextBox.ChangeUICues += BaseTextBoxChangeUiCues;
            p_baseTextBox.Click += BaseTextBoxClick;
            p_baseTextBox.ClientSizeChanged += BaseTextBoxClientSizeChanged;
            p_baseTextBox.ContextMenuStripChanged += BaseTextBoxContextMenuStripChanged;
            p_baseTextBox.CursorChanged += BaseTextBoxCursorChanged;
            p_baseTextBox.KeyDown += BaseTextBoxKeyDown;
            p_baseTextBox.KeyPress += BaseTextBoxKeyPress;
            p_baseTextBox.KeyUp += BaseTextBoxKeyUp;
            p_baseTextBox.SizeChanged += BaseTextBoxSizeChanged;
            p_baseTextBox.TextChanged += BaseTextBoxTextChanged;
        }

        private void UpdateBaseTextBox()
        {
            if (p_baseTextBox != null)
            {
                p_baseTextBox.Font = StyleManager.Current.GetTextBoxFont(p_metroTextBoxSize, p_metroTextBoxWeight);
                p_baseTextBox.Location = new Point(3, 3);
                p_baseTextBox.Size = new Size(Width - 6, Height - 6);
            }
        }

    }
}
