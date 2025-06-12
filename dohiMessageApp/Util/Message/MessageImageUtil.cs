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
    public static class MessageImageUtil
    {

        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        public static bool isImagecheck(string FileName)
        {
            if (FileName == null) return false;
            string ext = Path.GetExtension(FileName).ToLower();

            if (SupportedImageExtensions.Contains(ext))
            {
                return true;
            }
            return false;
        }

        public static string GetRandomClipboadImgName()
        {
            return "clipboard_image_" + StringUtil.GenerateRandomString(10) + ".png";
        }

        /// <summary>
        /// 클립보드의 이미지를 Base64String으로 반환
        /// </summary>
        public static string ClipboardPasteImageIfExistsReturnBase64String()
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
            throw new InvalidOperationException("클립보드에 이미지 없음");
        }

        public static BitmapImage LoadImageFromBase64(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String)) return null;
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
