using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WalkieDohi.UI
{
    public partial class ImagePreviewWindow : Window
    {
        private double _scale = 1.0;
        private string imagePath;
        private string base64Image;
        

        // 파일 경로용 생성자
        public ImagePreviewWindow(string imagePath)
        {
            InitializeComponent();
            this.imagePath = imagePath;

            if (File.Exists(imagePath))
            {
                ZoomBox.Source = new BitmapImage(new Uri(imagePath));
                btnPathOpen.Visibility = Visibility.Visible;
            }

            InitZoomAndDrag();
        }

        // Base64용 생성자
        public ImagePreviewWindow(byte[] imageBytes)
        {
            InitializeComponent();

            ZoomBox.Source = LoadImageFromBytes(imageBytes);
            btnPathOpen.Visibility = Visibility.Collapsed;
            InitZoomAndDrag();
        }

        public ImagePreviewWindow(BitmapImage image)
        {
            InitializeComponent();
            if (image != null)
            {
                ZoomBox.Source = image;
            }

            InitZoomAndDrag();
        }

        public ImagePreviewWindow(string base64Image, bool isBase64)
        {
            InitializeComponent();
            this.base64Image = base64Image;

            if (isBase64)
            {
                byte[] bytes = Convert.FromBase64String(base64Image);
                ZoomBox.Source = LoadImageFromBytes(bytes);
            }

            InitZoomAndDrag();
        }

        private void InitZoomAndDrag()
        {
            ZoomBox.LayoutTransform = new ScaleTransform(_scale, _scale);
        }

        private BitmapImage LoadImageFromBytes(byte[] imageData)
        {
            using (var stream = new MemoryStream(imageData))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _scale += 0.1;
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _scale = Math.Max(0.1, _scale - 0.1);
            ApplyZoom();
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _scale += e.Delta > 0 ? 0.1 : -0.1;
            _scale = Math.Max(0.1, _scale);
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            ZoomBox.LayoutTransform = new ScaleTransform(_scale, _scale);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void PathOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{imagePath}\"");
            }
        }

        private Point _dragStartPoint;
        private bool _isDragging = false;

        private void ScrollViewerHost_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(ScrollViewerHost);
            _isDragging = true;
            ScrollViewerHost.CaptureMouse();
        }

        private void ScrollViewerHost_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(ScrollViewerHost);
                var diff = _dragStartPoint - currentPoint;

                ScrollViewerHost.ScrollToHorizontalOffset(ScrollViewerHost.HorizontalOffset + diff.X);
                ScrollViewerHost.ScrollToVerticalOffset(ScrollViewerHost.VerticalOffset + diff.Y);

                _dragStartPoint = currentPoint;
            }
        }

        private void ScrollViewerHost_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ScrollViewerHost.ReleaseMouseCapture();
        }
    }
}
