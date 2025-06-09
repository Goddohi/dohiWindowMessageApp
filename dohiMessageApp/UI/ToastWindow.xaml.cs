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
using WalkieDohi.Entity;

namespace WalkieDohi.UI
{
    /// <summary>
    /// ToastWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ToastWindow : Window
    {
        // 자동 닫힘을 위한 타이머
        private readonly DispatcherTimer _timer;

        private GroupEntity Group = null;
        private string Sender = null;
        /// <summary>
        /// 토스트창 생성자
        /// </summary>
        /// <param name="title">알림 제목</param>
        /// <param name="message">알림 메시지</param>
        public ToastWindow(string sender, string message , GroupEntity group = null)
        {
            // 알림창이 포커스를 훔치지 않게 설정 (입력도중 방해 금지)
            this.ShowActivated = false;
            this.Topmost = true;
            this.Focusable = false;
            Sender = sender;
            Group = group;
            InitializeComponent();
            TitleText.Text = (group == null) ? $"📨 {sender}님이 보냄" :  $"📨 [ {Group.GroupName} ] {sender}님이 보냄";
            
            MessageText.Text = message;
            
            Loaded += ToastWindow_Loaded;

            // 타이머 초기화 (3초 후 닫기)
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timer.Tick += CloseWithFadeOut;
        }

        /// <summary>
        /// 창이 로드될 때 위치와 페이드인 처리
        /// </summary>
        private void ToastWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 화면 우측 하단에 위치
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;

            // 페이드 인 애니메이션
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(OpacityProperty, fadeIn);

            // 자동 닫힘 타이머 시작
            _timer.Start();
        }



        /// <summary>
        /// 타이머 만료 시 페이드 아웃 후 창 닫기
        /// </summary>
        private void CloseWithFadeOut(object sender, EventArgs e)
        {
            _timer.Stop();

            // 페이드 아웃 애니메이션
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s2, e2) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// 알림창 클릭 시 메인 창을 전면에 표시하고 알림창 닫기
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
                mainWindow.SelectChatTab(Sender,Group);
            }
            this.Close();
        }
    }
}
