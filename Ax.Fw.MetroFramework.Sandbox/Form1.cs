using Ax.Fw.MetroFramework.Data;
using Ax.Fw.MetroFramework.Forms;
using System.Reactive.Linq;

namespace Ax.Fw.MetroFramework.Sandbox
{
    public partial class Form1 : BorderlessForm
    {
        public Form1()
        {
            InitializeComponent();
            Observable
                .Interval(TimeSpan.FromMilliseconds(100))
                .Subscribe(_ => PostInvoke(() =>
                {
                    metroProgressBar1.ProgressBarStyle = ProgressBarStyle.Marquee;
                    metroProgressBar1.Maximum = 100;
                    metroProgressBar1.Value = (metroProgressBar1.Value + 1) % 100;
                }));

            StyleManager.Current.SetColors(Color.Black, Color.DarkViolet, Color.White);
        }

        private void buttonInputBox_Click(object sender, EventArgs e)
        {
            var inputBox = InputBox.Input("Input something!");
            MessageBox.Show($"'{inputBox}'");
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            var settings = new TrayPopupOptions(
                "Test Title",
                $"This is your personal, private workspace to play around in. Only you can see the collections and APIs you create here - unless you share them with your team.{Environment.NewLine}Add a brief summary{Environment.NewLine}about this workspace",
                TrayPopupType.Warning);

            new TrayPopup(settings).Show();
        }
    }
}