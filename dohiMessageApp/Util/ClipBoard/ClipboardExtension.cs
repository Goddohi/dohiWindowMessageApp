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
    public static class ClipboardExtension
    {
        public static void CopyTextSafe(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Clipboard.SetText(text ?? "");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("복사 실패: " + ex.Message);
                }
            });
        }

        public static BitmapSource GetImageSafeOnce()
        {
         
            try
            {
                BitmapSource image = null;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dataObj = Clipboard.GetDataObject();
                    var formats = dataObj?.GetFormats() ?? new string[0];

                    bool hasMimeImageFormat = formats.Any(f =>
                        f.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

                    // 1) 텔레그램 등: MIME 이미지가 있으면 우선 시도
                    if (hasMimeImageFormat)
                    {
                        if (formats.Contains("image/png"))
                            image = TryDecodeFromClipboardFormat("image/png");
                        if (image == null && formats.Contains("image/jpeg"))
                            image = TryDecodeFromClipboardFormat("image/jpeg");
                        // 필요시 더 시도: image/bmp → TryDecodeFromClipboardFormat("image/bmp")
                        // (webp/jxl은 WPF 기본 코덱 미지원)
                    }

                    // 2) 일반 비트맵 (카톡 등)
                    if (image == null && Clipboard.ContainsImage())
                    {
                        image = Clipboard.GetImage();
                    }

                    // 3) 최후의 보루: 원시 "PNG" 포맷 강제 시도
                    if (image == null)
                    {
                        image = TryDecodeFromClipboardFormat("PNG");
                    }

                    // 최종 Freeze (덮어쓴 뒤에도 적용되도록 마지막에 한 번만)
                    if (image != null && image.CanFreeze)
                        image.Freeze();
                });

                return image;
            }
            catch
            {
                return null;
            }
        }
        

        private static BitmapSource TryDecodeFromClipboardFormat(string format)
        {
            try
            {
                var data = Clipboard.GetData(format);
                if (data == null) return null;

                Stream s = null;
                if (data is MemoryStream ms) s = ms;
                else if (data is byte[] bytes) s = new MemoryStream(bytes);
                else if (data is Stream any) s = any;

                if (s == null) return null;

                using (var copy = new MemoryStream())
                {
                    s.Position = 0;
                    s.CopyTo(copy);
                    copy.Position = 0;

                    BitmapDecoder decoder;
                    var f = format.ToLowerInvariant();
                    if (f.Contains("png"))
                        decoder = new PngBitmapDecoder(copy, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    else if (f.Contains("jpeg") || f.Contains("jpg"))
                        decoder = new JpegBitmapDecoder(copy, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    else
                        decoder = BitmapDecoder.Create(copy, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                    var frame = decoder.Frames.FirstOrDefault();
                    if (frame != null && frame.CanFreeze)
                        frame.Freeze();
                    return frame;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
