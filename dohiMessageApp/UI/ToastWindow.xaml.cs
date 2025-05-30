using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WalkieDohi.UI
{
    /// <summary>
    /// ToastWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ToastWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public ToastWindow(string title, string message)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;
            
            Loaded += ToastWindow_Loaded;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timer.Tick += CloseWithFadeOut;
        }


        private void ToastWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(OpacityProperty, fadeIn);

            _timer.Start();
        } 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 오른쪽 하단 위치 설정
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;

            // Fade In
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(OpacityProperty, fadeIn);

            // 시작 타이머
            _timer.Start();
        }

        private void CloseWithFadeOut(object sender, EventArgs e)
        {
            _timer.Stop();

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s2, e2) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
            this.Close();
        }
    }
}
