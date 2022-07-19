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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buttonInputBox);
            this.Controls.Add(this.metroProgressBar1);
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private Controls.MetroProgressBar metroProgressBar1;
        private Controls.MetroButton buttonInputBox;
    }
}