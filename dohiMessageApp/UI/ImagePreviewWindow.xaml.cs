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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WalkieDohi.UI
{
    /// <summary>
    /// ImagePreviewWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImagePreviewWindow : Window
    {
        private double _scale = 1.0;
        private string imagePath;

        public ImagePreviewWindow(string imagePath)
        {
            InitializeComponent();
            if (File.Exists(imagePath))
            {
                this.imagePath = imagePath;
                ZoomBox.Source = new BitmapImage(new Uri(imagePath));
            }

            ZoomBox.LayoutTransform = new ScaleTransform(_scale, _scale);
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
            if (File.Exists(imagePath))
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
