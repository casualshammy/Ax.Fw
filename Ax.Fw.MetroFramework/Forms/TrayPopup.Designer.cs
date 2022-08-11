using Ax.Fw.MetroFramework.Controls;
using Ax.Fw.MetroFramework.Data;

namespace Ax.Fw.MetroFramework.Forms;

partial class TrayPopup
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

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
            this.metroLabel1 = new Ax.Fw.MetroFramework.Controls.MetroLabel();
            this.metroLabel2 = new Ax.Fw.MetroFramework.Controls.MetroLabel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // metroLabel1
            // 
            this.metroLabel1.FontSize = Ax.Fw.MetroFramework.Data.MetroLabelSize.Medium;
            this.metroLabel1.FontWeight = Ax.Fw.MetroFramework.Data.MetroLabelWeight.Bold;
            this.metroLabel1.LabelMode = Ax.Fw.MetroFramework.Data.MetroLabelMode.Default;
            this.metroLabel1.Location = new System.Drawing.Point(63, 15);
            this.metroLabel1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(309, 27);
            this.metroLabel1.TabIndex = 1;
            this.metroLabel1.Text = "Hello";
            // 
            // metroLabel2
            // 
            this.metroLabel2.AutoSize = true;
            this.metroLabel2.FontSize = Ax.Fw.MetroFramework.Data.MetroLabelSize.Small;
            this.metroLabel2.FontWeight = Ax.Fw.MetroFramework.Data.MetroLabelWeight.Regular;
            this.metroLabel2.LabelMode = Ax.Fw.MetroFramework.Data.MetroLabelMode.Default;
            this.metroLabel2.Location = new System.Drawing.Point(64, 39);
            this.metroLabel2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.metroLabel2.MaximumSize = new System.Drawing.Size(330, 0);
            this.metroLabel2.Name = "metroLabel2";
            this.metroLabel2.Size = new System.Drawing.Size(79, 15);
            this.metroLabel2.TabIndex = 2;
            this.metroLabel2.Text = "How are you?";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(19, 20);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(37, 37);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // TrayPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(421, 78);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.metroLabel2);
            this.Controls.Add(this.metroLabel1);
            this.Location = new System.Drawing.Point(0, 0);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TrayPopup";
            this.Padding = new System.Windows.Forms.Padding(23, 35, 23, 23);
            this.ShowInTaskbar = false;
            this.Text = "TrayPopup";
            this.MouseEnter += new System.EventHandler(this.ALL_MouseEnter);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private MetroLabel metroLabel1;
    private MetroLabel metroLabel2;
    private System.Windows.Forms.PictureBox pictureBox1;
}
