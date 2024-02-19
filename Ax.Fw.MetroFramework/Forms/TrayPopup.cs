using Ax.Fw.MetroFramework.Data;
using Ax.Fw.Windows.WinAPI;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace Ax.Fw.MetroFramework.Forms;

public partial class TrayPopup : BorderlessForm
{
  private readonly System.Timers.Timer p_fadeTimer;
  private DateTimeOffset p_loadTime;
  private const float p_fadeOutStep = 1f / 5000f * 33.3f;
  private readonly TrayPopupOptions p_options;

  static TrayPopup()
  {
    Observable
        .Interval(TimeSpan.FromMilliseconds(250))
        .Subscribe(_ => ArrangementTimer_Elapsed());
  }

  public TrayPopup(TrayPopupOptions _options)
  {
    InitializeComponent();
    p_options = _options;
    Title = _options.Title ?? "";
    Message = _options.Message ?? "";

    if (_options.Image == null)
    {
      if (_options.Type == TrayPopupType.Error)
        Icon = Resources.DialogError.Value;
      else if (_options.Type == TrayPopupType.Warning)
        Icon = Resources.DialogWarning.Value;
      else
        Icon = Resources.DialogInfo.Value;
    }
    else
    {
      Icon = _options.Image;
    }

    p_fadeTimer = new System.Timers.Timer(33.3); // 30fps
    p_fadeTimer.Elapsed += FadeTimer_OnTick;

    FormClosing += PopupNotification_FormClosing;

    Opacity = 0f;

    BeginInvoke((MethodInvoker)delegate
    {
      ArrangementTimer_Elapsed();
      Opacity = 1f;
      p_loadTime = DateTime.UtcNow;
      p_fadeTimer.Start();
      Timeout = Timeout == 0 ? 30 : Timeout;
      MouseClick += ALL_MouseClick;
      foreach (Control control in Controls)
      {
        control.MouseEnter += ALL_MouseEnter;
        control.MouseClick += ALL_MouseClick;
      }
    });
  }

  public string Title
  {
    get => metroLabel1.Text;
    set => metroLabel1.Text = value;
  }

  public string Message
  {
    get => metroLabel2.Text;
    set
    {
      metroLabel2.Text = WordWrap(value);
      Size = new Size(Width, Math.Max(68, metroLabel2.Location.Y + metroLabel2.Size.Height + 10)); // 68 - standard height
    }
  }

  public new Image? Icon
  {
    get => pictureBox1.Image;
    set => pictureBox1.Image = value;
  }

  public int Timeout { get; private set; }

  public bool IsClosed { get; private set; }

  public void Show(int timeout)
  {
    var prevForegroundWindow = NativeMethods.GetForegroundWindow();
    TopMost = true;
    Timeout = timeout;
    base.Show();
    NativeMethods.SetForegroundWindow(prevForegroundWindow);
    if (p_options.Sound)
    {
      if (p_options.Type == TrayPopupType.Error || p_options.Type == TrayPopupType.Warning)
        _ = Windows.Utilities.PlaySystemExclamationAsync();
      else
        _ = Windows.Utilities.PlaySystemNotificationAsync();
    }
  }

  public new void Show()
  {
    Show(7);
  }

  public void RefreshTimeout()
  {
    p_loadTime = DateTime.UtcNow;
    Opacity = 1.0d;
  }

  private void FadeTimer_OnTick(object? sender, ElapsedEventArgs e)
  {
    if (IsClosed)
    {
      p_fadeTimer.Stop();
      return;
    }

    if (Opacity > p_fadeOutStep)
    {
      if (DateTime.UtcNow - p_loadTime > TimeSpan.FromSeconds(Timeout))
        PostInvoke(() => { Opacity -= p_fadeOutStep; });
    }
    else
    {
      PostInvoke(Close);
      p_options.OnClose?.Invoke();
    }
  }

  private static void ArrangementTimer_Elapsed()
  {
    var appBarData = GetAppBarData();
    var popups = Windows.Utilities.FindForms<TrayPopup>()
        .Where(_x => !_x.IsClosed)
        .ToList();

    popups.Sort((_first, _second) =>
    {
      return appBarData.uEdge == ABE.Top ? _first.DesktopLocation.Y.CompareTo(_second.DesktopLocation.Y) : -_first.DesktopLocation.Y.CompareTo(_second.DesktopLocation.Y);
    });
    if (appBarData.uEdge == ABE.Top)
    {
      var y = 0;
      foreach (TrayPopup popup in popups)
      {
        var x = Screen.PrimaryScreen?.WorkingArea.Width - popup.Width;
        popup.PostInvoke(() =>
        {
          popup.SetDesktopLocation(x ?? 0, y);
          y += popup.Height + 10;
        });
      }
    }
    else
    {
      var y = Screen.PrimaryScreen?.WorkingArea.Height - (popups.FirstOrDefault() != null ? (popups.FirstOrDefault()?.Height ?? 0) : 0);
      foreach (TrayPopup popup in popups)
      {
        var x = Screen.PrimaryScreen?.WorkingArea.Width - popup.Width;
        popup.PostInvoke(() =>
        {
          popup.SetDesktopLocation(x ?? 0, y ?? 0);
          y -= popup.Height - 10;
        });
      }
    }
  }

  private void ALL_MouseEnter(object? sender, EventArgs e)
  {
    RefreshTimeout();
  }

  private void ALL_MouseClick(object? sender, MouseEventArgs e)
  {
    if (e.Button == MouseButtons.Right)
    {
      Task.Run(() => p_options?.OnClose?.Invoke());
      Close();
      ArrangementTimer_Elapsed();
    }
    else
    {
      if (p_options.OnClick != null)
      {
        Task.Run(() => p_options.OnClick.Invoke());
        Close();
        ArrangementTimer_Elapsed();
      }
    }
  }

  private void PopupNotification_FormClosing(object? sender, FormClosingEventArgs e)
  {
    IsClosed = true;
  }

  private string WordWrap(string _text)
  {
    using (var font = StyleManager.Current.GetLabelFont(metroLabel2.FontSize, metroLabel2.FontWeight))
    {
      var words = new Queue<string>(_text.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries));
      var result = string.Empty;
      var currentLine = string.Empty;
      var sizeOfSpace = TextRenderer.MeasureText(" ", font).Width;
      while (words.TryDequeue(out var word))
      {
        var currentLineNewSize = TextRenderer.MeasureText(currentLine + sizeOfSpace + word, font).Width;
        if (currentLineNewSize > metroLabel2.MaximumSize.Width)
        {
          currentLine = word;
          result += $"{Environment.NewLine}{currentLine}";
          continue;
        }

        if (currentLine == "")
        {
          currentLine += $"{word}";
          result += $"{word}";
        }
        else
        {
          currentLine += $" {word}";
          result += $" {word}";
        }
      }
      return result.TrimEnd('\n').TrimEnd('\r');
    }
  }

  private static APPBARDATA GetAppBarData()
  {
    var taskbarHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
    var data = new APPBARDATA
    {
      cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
      hWnd = taskbarHandle
    };
    var result = NativeMethods.SHAppBarMessage((uint)APPBARMESSAGE.GetTaskbarPos, ref data);
    if (result == IntPtr.Zero)
      throw new InvalidOperationException();

    return data;
  }

}
