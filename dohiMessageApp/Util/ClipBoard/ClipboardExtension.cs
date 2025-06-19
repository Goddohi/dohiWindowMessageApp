using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        /// <summary>
        /// 클립보드에서 안전하게 이미지를 한 번만 가져옵니다.
        /// 실패 시 null 반환 (예외 발생 안 함)
        /// </summary>
        public static BitmapSource GetImageSafeOnce()
        {
            try
            {
                BitmapSource image = null;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Clipboard.ContainsImage())
                    {
                        image = Clipboard.GetImage();
                    }
                });

                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
