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
        }

    }
}