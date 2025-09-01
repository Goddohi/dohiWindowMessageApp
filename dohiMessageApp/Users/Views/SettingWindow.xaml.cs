using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using WalkieDohi.Core.app;
using WalkieDohi.Entity;
using WalkieDohi.Users.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.IO;
using WalkieDohi.Util.Provider;

namespace WalkieDohi.Users.Views
{
    /// <summary>
    /// SettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingWindow : Window
    {
        private UserFileProvider userFileProvider = new UserJsonFileHandler();
        public SettingWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PortTextBox.Text  = MainData.currentUser.Preferences.Port.ToString(); 
            SortOptionComboBox.SelectedIndex = MainData.currentUser.Preferences.FriendSortOrder == FriendSortType.ByIp ? 1 : 0;

            var currentAutoStart = AutoStartManager.IsEnabled();
            _autoStartInitial = currentAutoStart;
            _autoStartPending = currentAutoStart;   // 현재 상태를 의도에도 복사
            AutoStartCheckBox.IsChecked = currentAutoStart;

        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (PortTextBox.Text.Equals("GAME"))
            {
                var gameWindow = new Games.Views.MiniGameWindow();
                gameWindow.Owner = this;
                gameWindow.ShowDialog();
                return;
            }

            if (!int.TryParse(PortTextBox.Text, out int newPort) || newPort < 1 || newPort > 65535)
            {
                MessageBox.Show("유효한 포트 번호를 입력해주세요. (1 ~ 65535)", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int currentPort = MainData.currentUser.Preferences.Port;
            if (newPort != currentPort && IsPortInUse(newPort))
            {
                MessageBox.Show($"포트 {newPort}는 이미 사용 중입니다. 다른 포트를 입력해주세요.", "포트 충돌", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MainData.currentUser.Preferences.Port = newPort;
            var selectedSort = (SortOptionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            MainData.currentUser.Preferences.FriendSortOrder = selectedSort == "IP순" ? FriendSortType.ByIp : FriendSortType.ByName;
            
            
            // 자동 시작 변경 적용
            bool autoStartChanged = _autoStartPending.HasValue && _autoStartPending.Value != _autoStartInitial;
            if (autoStartChanged)
            {
                bool ok = _autoStartPending.Value
                    ? AutoStartManager.SetEnabled(true, "--minimized")
                    : AutoStartManager.SetEnabled(false, null);

                if (!ok)
                {
                    MessageBox.Show("자동 시작 설정 저장에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 적용 성공했으니 초기값 갱신
                _autoStartInitial = _autoStartPending.Value;
            }

            if (userFileProvider.SaveUser(MainData.currentUser))
            {
                string msg = "설정이 저장되었습니다.";
                if (newPort != currentPort)
                    msg += "\n - WalkieDohi를 재시작해야 변경한 포트가 적용됩니다 - ";
                if (autoStartChanged)
                    msg += $"\n  자동 시작 : {(_autoStartPending.Value ? "사용" : "해제")} ";

                MessageBox.Show(msg);
            }

            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private int clickCount = 0;
        private void HiddenTrigger_MouseDown(object sender, MouseButtonEventArgs e)
        {
            clickCount++;
            if (clickCount >= 5)
            {
                var gameWindow = new Games.Views.MiniGameWindow();
                gameWindow.Owner = this;
                gameWindow.ShowDialog();
                clickCount = 0;
            }
        }
        private bool IsPortInUse(int port)
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return false; // 포트 사용 가능
            }
            catch (SocketException)
            {
                return true; // 포트 사용 중
            }
        }




        #region 자동시작
        private bool _autoStartInitial;     // 첫 상태
        private bool? _autoStartPending;    // 체크박스로 바꾼 상태

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {

            _autoStartPending = true;
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _autoStartPending = false;
        }
        #endregion


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }
    }
}
