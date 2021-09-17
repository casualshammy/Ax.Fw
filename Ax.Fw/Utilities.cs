using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Ax.Fw
{
    public static class Utilities
    {
        public static readonly Random Rnd = new();

        public static string GetRandomString(int _size, bool _onlyLetters)
        {
            var builder = new StringBuilder(_size);
            var chars = _onlyLetters ? "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" : "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int i = 0; i < _size; i++)
            {
                var c = chars[Rnd.Next(0, chars.Length)];
                builder.Append(c);
            }
            return builder.ToString();
        }

        public static async Task<bool> IsInternetAvailable()
        {
            try
            {
                using var ping = new Ping();
                PingReply pingReply = await ping.SendPingAsync("8.8.8.8", 2000);
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
            return fontsCollection.Families.Any(i => i.Name == _fontName);
        }

        public static Image Base64ToImage(string _base64)
        {
            byte[] byteBuffer = Convert.FromBase64String(_base64);
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
            int[] indexesToHide = new int[_input.Length / 2];
            for (int i = 0; i < indexesToHide.Length; i++)
            {
                var newValue = Rnd.Next(0, _input.Length);
                while (indexesToHide.Contains(newValue))
                    newValue = Rnd.Next(0, _input.Length);
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

    }
}
