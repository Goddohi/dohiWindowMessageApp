using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WalkieDohi.ChattingRooms.UserControls
{
    /// <summary>
    /// DoodlePadControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DoodlePadControl : UserControl
    {
        public event EventHandler<string> DoodleCompleted; // 완료시 base64로 제공

        public DoodlePadControl()
        {
            InitializeComponent();
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;

            inkSig.Strokes.Clear();

            Storyboard sb = this.Resources["ShowStoryboard"] as Storyboard;
            if (sb != null)
            {
                sb.Begin();
            }
        }

        public void Hide()
        {
            Storyboard sb = this.Resources["HideStoryboard"] as Storyboard;
            if (sb != null)
            {
                sb.Completed -= HideStoryboard_Completed;
                sb.Completed += HideStoryboard_Completed;
                sb.Begin();
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }

        private void HideStoryboard_Completed(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void BtnSignClear_Click(object sender, RoutedEventArgs e)
        {
            inkSig.Strokes.Clear();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (inkSig.Strokes == null || inkSig.Strokes.Count == 0)
            {
                Hide();
                return;
            }

            byte[] bytes = RenderInkToPngBytes(inkSig);

            string base64 = Convert.ToBase64String(bytes);

            var handler = DoodleCompleted;
            if (handler != null)
            {
                handler(this, base64);
            }

            Hide();
        }

        // 이건 크기를 결정하고 사용하는 함수
        private byte[] RenderInkToPngBytes(InkCanvas ink, int width, int height)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                width, height, 96d, 96d, PixelFormats.Pbgra32);

            // InkCanvas만 렌더
            rtb.Render(ink);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                bytes = ms.ToArray();
            }

            return bytes;
        }


        private byte[] RenderInkToPngBytes(InkCanvas ink)
        {
            // 1) 실제 렌더링된 크기 기준
            int width = (int)Math.Max(1, ink.ActualWidth);
            int height = (int)Math.Max(1, ink.ActualHeight);

            // 혹시 ActualWidth가 0이면 Width/Height 속성 사용 (안전빵)
            if (width <= 1 && ink.Width > 0)
                width = (int)ink.Width;
            if (height <= 1 && ink.Height > 0)
                height = (int)ink.Height;

            var rtb = new RenderTargetBitmap(
                width, height, 96d, 96d, PixelFormats.Pbgra32);

            rtb.Render(ink);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
    }
}
