namespace Ax.Fw.MetroFramework.Sandbox
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.metroProgressBar1 = new Ax.Fw.MetroFramework.Controls.MetroProgressBar();
            this.buttonInputBox = new Ax.Fw.MetroFramework.Controls.MetroButton();
            this.metroButton1 = new Ax.Fw.MetroFramework.Controls.MetroButton();
            this.metroTextBox1 = new Ax.Fw.MetroFramework.Controls.MetroTextBox();
            this.metroLabel1 = new Ax.Fw.MetroFramework.Controls.MetroLabel();
            this.SuspendLayout();
            // 
            // metroProgressBar1
            // 
            this.metroProgressBar1.FontSize = Ax.Fw.MetroFramework.Data.MetroProgressBarSize.Medium;
            this.metroProgressBar1.FontWeight = Ax.Fw.MetroFramework.Data.MetroProgressBarWeight.Light;
            this.metroProgressBar1.HideProgressText = true;
            this.metroProgressBar1.Location = new System.Drawing.Point(23, 63);
            this.metroProgressBar1.Name = "metroProgressBar1";
            this.metroProgressBar1.ProgressBarStyle = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.metroProgressBar1.Size = new System.Drawing.Size(265, 28);
            this.metroProgressBar1.TabIndex = 0;
            this.metroProgressBar1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // buttonInputBox
            // 
            this.buttonInputBox.Highlight = false;
            this.buttonInputBox.Location = new System.Drawing.Point(23, 213);
            this.buttonInputBox.Name = "buttonInputBox";
            this.buttonInputBox.Size = new System.Drawing.Size(101, 30);
            this.buttonInputBox.TabIndex = 1;
            this.buttonInputBox.Text = "Input Box";
            this.buttonInputBox.Click += new System.EventHandler(this.buttonInputBox_Click);
            // 
            // metroButton1
            // 
            this.metroButton1.Highlight = false;
            this.metroButton1.Location = new System.Drawing.Point(23, 249);
            this.metroButton1.Name = "metroButton1";
            this.metroButton1.Size = new System.Drawing.Size(101, 30);
            this.metroButton1.TabIndex = 2;
            this.metroButton1.Text = "Tray Popup";
            this.metroButton1.Click += new System.EventHandler(this.metroButton1_Click);
            // 
            // metroTextBox1
            // 
            this.metroTextBox1.FontSize = Ax.Fw.MetroFramework.Data.MetroTextBoxSize.Small;
            this.metroTextBox1.FontWeight = Ax.Fw.MetroFramework.Data.MetroTextBoxWeight.Regular;
            this.metroTextBox1.Location = new System.Drawing.Point(369, 205);
            this.metroTextBox1.Multiline = false;
            this.metroTextBox1.Name = "metroTextBox1";
            this.metroTextBox1.ReadOnly = false;
            this.metroTextBox1.SelectedText = "";
            this.metroTextBox1.Size = new System.Drawing.Size(285, 25);
            this.metroTextBox1.TabIndex = 3;
            this.metroTextBox1.Text = "metroTextBox1";
            this.metroTextBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            // 
            // metroLabel1
            // 
            this.metroLabel1.AutoSize = true;
            this.metroLabel1.FontSize = Ax.Fw.MetroFramework.Data.MetroLabelSize.Medium;
            this.metroLabel1.FontWeight = Ax.Fw.MetroFramework.Data.MetroLabelWeight.Light;
            this.metroLabel1.LabelMode = Ax.Fw.MetroFramework.Data.MetroLabelMode.Default;
            this.metroLabel1.Location = new System.Drawing.Point(346, 308);
            this.metroLabel1.Name = "metroLabel1";
            this.metroLabel1.Size = new System.Drawing.Size(81, 19);
            this.metroLabel1.TabIndex = 4;
            this.metroLabel1.Text = "metroLabel1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.metroLabel1);
            this.Controls.Add(this.metroTextBox1);
            this.Controls.Add(this.metroButton1);
            this.Controls.Add(this.buttonInputBox);
            this.Controls.Add(this.metroProgressBar1);
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Controls.MetroProgressBar metroProgressBar1;
        private Controls.MetroButton buttonInputBox;
        private Controls.MetroButton metroButton1;
        private Controls.MetroTextBox metroTextBox1;
        private Controls.MetroLabel metroLabel1;
    }
}