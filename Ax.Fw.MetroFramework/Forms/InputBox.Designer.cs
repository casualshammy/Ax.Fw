using Ax.Fw.MetroFramework.Controls;
using Ax.Fw.MetroFramework.Data;

namespace Ax.Fw.MetroFramework.Forms;

public partial class InputBox
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
        this.button1 = new MetroButton();
        this.button2 = new MetroButton();
        this.textBox1 = new MetroTextBox();
        this.metroLabel1 = new MetroLabel();
        this.SuspendLayout();
        // 
        // button1
        // 
        this.button1.Highlight = true;
        this.button1.Location = new System.Drawing.Point(153, 63);
        this.button1.Name = "button1";
        this.button1.Size = new System.Drawing.Size(75, 23);
        this.button1.TabIndex = 0;
        this.button1.Text = "OK";
        // 
        // button2
        // 
        this.button2.Highlight = true;
        this.button2.Location = new System.Drawing.Point(234, 63);
        this.button2.Name = "button2";
        this.button2.Size = new System.Drawing.Size(75, 23);
        this.button2.TabIndex = 1;
        this.button2.Text = "Cancel";
        // 
        // textBox1
        // 
        this.textBox1.FontSize = MetroTextBoxSize.Small;
        this.textBox1.FontWeight = MetroTextBoxWeight.Regular;
        this.textBox1.Location = new System.Drawing.Point(12, 37);
        this.textBox1.Multiline = false;
        this.textBox1.Name = "textBox1";
        this.textBox1.SelectedText = "";
        this.textBox1.Size = new System.Drawing.Size(297, 20);
        this.textBox1.TabIndex = 3;
        // 
        // metroLabel1
        // 
        this.metroLabel1.AutoSize = true;
        this.metroLabel1.FontSize = MetroLabelSize.Medium;
        this.metroLabel1.FontWeight = MetroLabelWeight.Regular;
        this.metroLabel1.LabelMode = MetroLabelMode.Default;
        this.metroLabel1.Location = new System.Drawing.Point(12, 15);
        this.metroLabel1.Name = "metroLabel1";
        this.metroLabel1.Size = new System.Drawing.Size(34, 19);
        this.metroLabel1.TabIndex = 4;
        this.metroLabel1.Text = "Text";
        // 
        // InputBox
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(321, 94);
        this.Controls.Add(this.metroLabel1);
        this.Controls.Add(this.textBox1);
        this.Controls.Add(this.button2);
        this.Controls.Add(this.button1);
        this.Location = new System.Drawing.Point(0, 0);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "InputBox";
        this.Padding = new System.Windows.Forms.Padding(20, 30, 20, 20);
        this.Resizable = false;
        this.Text = "InputBox";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private MetroButton button1;
    private MetroButton button2;
    private MetroTextBox textBox1;
    private MetroLabel metroLabel1;
}
