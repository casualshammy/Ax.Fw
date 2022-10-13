using Ax.Fw.Rnd;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Ax.Fw
{
    public static class Utilities
    {
        private static ImmutableDictionary<int, (Image Image, IDisposable DisposeFunc)> p_imagesFromBase64 =
            ImmutableDictionary<int, (Image Image, IDisposable DisposeFunc)>.Empty;

        public static Random Rnd => ThreadSafeRandomProvider.GetThreadRandom();

        public static string GetRandomString(int _size, bool _onlyLetters)
        {
            var rnd = Rnd;
            var builder = new StringBuilder(_size);
            var chars = _onlyLetters ? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" : "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int i = 0; i < _size; i++)
            {
                var c = chars[rnd.Next(0, chars.Length)];
                builder.Append(c);
            }
            return builder.ToString();
        }

        public static async Task<bool> IsInternetAvailable()
        {
            try
            {
                using var ping = new Ping();
                var pingReply = await ping.SendPingAsync("8.8.8.8", 2000);
                return pingReply != null && (pingReply.Status == IPStatus.Success);
            }
            catch
            {
                return false;
            }
        }

        public static bool FontIsInstalled(string _fontName)
        {
            using var fontsCollection = new InstalledFontCollection();
            return fontsCollection.Families.Any(_i => _i.Name == _fontName);
        }

        public static Image Base64ToImage(string _base64)
        {
            var byteBuffer = Convert.FromBase64String(_base64);
            using var memoryStream = new MemoryStream(byteBuffer);
            memoryStream.Position = 0;
            return Image.FromStream(memoryStream);
        }

        public static string WordWrap(string _text, int _chunkSize)
        {
            var words = _text.Split(' ').ToList();
            var result = new StringBuilder();
            while (words.Any())
            {
                var sb = new StringBuilder();
                while (words.Any() && sb.Length + 1 + words.First().Length <= _chunkSize)
                {
                    sb.Append(" " + words.First());
                    words.RemoveAt(0);
                }
                result.Append(sb.ToString() + "\r\n");
            }
            return result.ToString().TrimEnd('\n').TrimEnd('\r');
        }

        public static string SecureString(string _input)
        {
            var rnd = Rnd;
            var indexesToHide = new int[_input.Length / 2];
            for (int i = 0; i < indexesToHide.Length; i++)
            {
                var newValue = rnd.Next(0, _input.Length);
                while (indexesToHide.Contains(newValue))
                    newValue = rnd.Next(0, _input.Length);
                indexesToHide[i] = newValue;
            }
            var builder = new StringBuilder(_input.Length);
            var counter = 0;
            foreach (char c in _input)
            {
                builder.Append(indexesToHide.Contains(counter) ? '*' : c);
                counter++;
            }
            return builder.ToString();
        }

        public static IEnumerable<Type> GetTypesWithAttr<T>(bool _inherit) where T : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(_x => _x.GetTypes())
                .Where(_x => _x.IsDefined(typeof(T), _inherit));
        }

        public static T? GetAttribute<T>(Type _type) where T : Attribute
        {
            var attr = Attribute.GetCustomAttribute(_type, typeof(T)) as T;
            return attr;
        }

        public static IEnumerable<Type> GetTypesOf<T>()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(_x => _x.GetTypes())
                .Where(_x => typeof(T).IsAssignableFrom(_x));
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
            if (p_imagesFromBase64.TryGetValue(hash, out var tuple) && tuple.Image != null && tuple.DisposeFunc != null)
            {
                _image = tuple.Image;
                return tuple.DisposeFunc;
            }

            var ms = new MemoryStream(Convert.FromBase64String(_base64));
            var image = Image.FromStream(ms);
            var disposable = Disposable.Create(() =>
            {
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

            p_imagesFromBase64 = p_imagesFromBase64.SetItem(hash, (image, disposable));

            _image = image;
            return disposable;
        }


    }
}
