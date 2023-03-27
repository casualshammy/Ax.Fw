#nullable enable
using Ax.Fw.Windows.WinAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ax.Fw.Windows;

public static class Utilities
{
  record ImageCacheEntry(Image Image, MemoryStream MemoryStream, IDisposable Disposable);

  private static readonly ConcurrentDictionary<int, ImageCacheEntry> p_imagesFromBase64 = new();

  public static IEnumerable<T> FindForms<T>() where T : Form => Application.OpenForms.OfType<T>();

  public static T? GetWindow<T>() where T : Form => FindForms<T>().FirstOrDefault();

  public static async Task<T> WaitForm<T>(CancellationToken _token) where T : Form
  {
    T? form;
    while ((form = GetWindow<T>()) == default && !_token.IsCancellationRequested)
      await Task.Delay(100, _token);

    return form ?? throw new TaskCanceledException();
  }

  public static async Task PlaySystemNotificationAsync()
      => await Task.Run(() => NativeMethods.SndPlaySoundW("SystemNotification", Win32Consts.SND_ALIAS | Win32Consts.SND_NODEFAULT));

  public static async Task PlaySystemExclamationAsync()
      => await Task.Run(() => NativeMethods.SndPlaySoundW("SystemExclamation", Win32Consts.SND_ALIAS | Win32Consts.SND_NODEFAULT));

  public static Thread GetControlThread(Control _control)
  {
    if (_control is null)
      throw new ArgumentNullException(nameof(_control));

    Thread? thread = null;
    _control.Invoke(new Action(() =>
    {
      thread = Thread.CurrentThread;
    }));
    return thread!;
  }

  /// <summary>
  /// Creates <see cref="Image"/> from base64-encoded string
  /// PAY ATTENTION: do not dispose <see cref="Image"/> directly. Use the return value of this method instead
  /// </summary>
  /// <param name="_base64"></param>
  /// <returns><see cref="IDisposable"/> object that should be disposed when you don't need generated <see cref="Image"/> more</returns>
  public static IDisposable GetImageFromBase64(string _base64, out Image _image)
  {
    var hash = _base64.GetHashCode();
    if (p_imagesFromBase64.TryGetValue(hash, out var cacheEntry) && cacheEntry.Image != null && cacheEntry.Disposable != null)
    {
      _image = cacheEntry.Image;
      return cacheEntry.Disposable;
    }

    var ms = new MemoryStream(Convert.FromBase64String(_base64));
    var image = Image.FromStream(ms);
    var disposable = Disposable.Create(() =>
    {
      p_imagesFromBase64.TryRemove(hash, out _);
      try
      {
        image?.Dispose();
      }
      catch { }
      try
      {
        ms?.Dispose();
      }
      catch { }
    });

    p_imagesFromBase64[hash] = new ImageCacheEntry(image, ms, disposable);

    _image = image;
    return disposable;
  }

  public static bool FontIsInstalled(string _fontName)
  {
    using var fontsCollection = new InstalledFontCollection();
    return fontsCollection.Families.Any(_i => _i.Name == _fontName);
  }

}
