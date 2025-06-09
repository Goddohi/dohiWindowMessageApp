using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WalkieDohi.Util
{
    public static class ClipboadPasteUtil
    {
        private static string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string GetRandomClipboadImgName()
        {
            return "clipboard_image_" + GenerateRandomString(10) + ".png";
        }
        public static string PasteImageIfExists()
        {
            if (Clipboard.ContainsImage())
            {
                BitmapSource image = Clipboard.GetImage();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    byte[] imageBytes = stream.ToArray();
                    string base64 = Convert.ToBase64String(imageBytes);

                    return base64;
                }
            }
            return "";
        }

        public static BitmapImage LoadImageFromBase64(string base64String)
        {
            byte[] binaryData = Convert.FromBase64String(base64String);
            using (var stream = new MemoryStream(binaryData))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze(); // UI 스레드에서 안전하게 사용
                return image;
            }
        }


    }
}
