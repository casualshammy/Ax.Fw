using Ax.Fw.MetroFramework.Controls;
using Ax.Fw.MetroFramework.Data;

namespace Ax.Fw.MetroFramework.Forms;

public partial class WaitingOverlay : Form
{
    private readonly Form parentForm;
    private WaitingOverlaySub panel;
    private readonly int waitInMs;

    /// <summary>
    ///
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="label"></param>
    /// <param name="waitInMs"></param>
    public WaitingOverlay(Form parent, string label, int waitInMs = 0)
    {
        InitializeComponent();
        parentForm = parent;
        this.waitInMs = waitInMs;
        Load += delegate
        {
            Size = parentForm.Size;
            Location = parentForm.Location;
            panel = new WaitingOverlaySub(this);
            panel.Show(this);
            Label = label;
        };
    }

    public string Label
    {
        get => panel.metroLabel1.Text;
        set
        {
            panel.metroLabel1.Text = value;
            var width = Math.Max(190, panel.metroLabel1.Location.X + 10 + TextRenderer.MeasureText(value, StyleManager.Current.GetLabelFont(panel.metroLabel1.FontSize, panel.metroLabel1.FontWeight)).Width);
            panel.Size = new Size(width, 50); // 10 is neccessary
            panel.Location = new Point(Location.X + Size.Width / 2 - panel.Size.Width / 2, Location.Y + Size.Height / 2 - panel.Size.Height / 2);
            panel.Invalidate();
        }
    }

    public new WaitingOverlay Show()
    {
        Show(parentForm);
        if (waitInMs != 0)
        {
            Task.Factory.StartNew(() => Thread.Sleep(waitInMs)).ContinueWith(l => Close(), TaskScheduler.FromCurrentSynchronizationContext());
        }
        return this;
    }

    private void WaitingOverlay_FormClosing(object sender, FormClosingEventArgs e)
    {
        panel.Close();
    }

    private class WaitingOverlaySub : Form
    {
        private readonly Form parentForm;

        internal WaitingOverlaySub(Form form)
        {
            InitializeComponent();
            parentForm = form;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var styleBrush = new SolidBrush(StyleManager.Current.PrimaryColor))
            {
                e.Graphics.FillRectangles(styleBrush, new[]
                {
                    new Rectangle(0, 0, Width, 2),
                    new Rectangle(Width - 2, 0, 2, Height),
                    new Rectangle(0, 0, 2, Height),
                    new Rectangle(0, Height - 2, Width, 2)
                });
            }
        }

        private void WaitingOverlaySub_Load(object sender, EventArgs e)
        {
            if (parentForm != null)
            {
                Location = new Point(parentForm.Location.X + parentForm.Size.Width / 2 - Size.Width / 2, parentForm.Location.Y + parentForm.Size.Height / 2 - Size.Height / 2);
            }
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private readonly System.ComponentModel.IContainer? components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.metroLabel1 = new MetroLabel();
            this.metroProgressSpinner1 = new MetroProgressSpinner();
            this.SuspendLayout();
            //
            // metroLabel1
            //
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.FontSize = MetroLabelSize.Tall;
            this.metroLabel1.FontWeight = MetroLabelWeight.Bold;
            this.metroLabel1.LabelMode = MetroLabelMode.Default;
            this.metroLabel1.Location = new System.Drawing.Point(51, 13);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new Size(110, 25);
            this.metroLabel1.TabIndex = 0;
            this.metroLabel1.Text = "Please wait...";
            //
            // metroProgressSpinner1
            //
            this.metroProgressSpinner1.Location = new System.Drawing.Point(5, 5);
            this.metroProgressSpinner1.Maximum = 100;
            this.metroProgressSpinner1.Name = "metroProgressSpinner1";
            this.metroProgressSpinner1.Size = new System.Drawing.Size(40, 40);
            this.metroProgressSpinner1.Speed = 2F;
            this.metroProgressSpinner1.TabIndex = 1;
            //
            // WaitingOverlaySub
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(190, 50);
            this.Controls.Add(this.metroProgressSpinner1);
            this.Controls.Add(this.metroLabel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "WaitingOverlaySub";
            this.Padding = new System.Windows.Forms.Padding(20, 30, 20, 20);
            this.ShowInTaskbar = false;
            this.Text = "WaitingOverlaySub";
            this.Load += new System.EventHandler(this.WaitingOverlaySub_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion Windows Form Designer generated code

        internal MetroLabel metroLabel1;
        internal MetroProgressSpinner metroProgressSpinner1;
    }
}
