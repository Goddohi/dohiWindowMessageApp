using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Interop;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.Tcp;
using WalkieDohi.Core.app;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using System.Windows.Input;
using WalkieDohi.Groups.Entity;
using WalkieDohi.Friends.Views;
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.ChattingRooms.UserControls;
using WalkieDohi.Friends.UserControls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace WalkieDohi
{
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        private PacketReceiver MainReceiver;
        private MessengerSender msgSender = new MessengerSender();
        private ChatRoomListTabControl _chatRoomListTabControl;
        private FriendMainListView _startTabControl;

        public MainWindow()
        {
            InitializeComponent();
            ChatListManager.LoadChatList();
            LoadUser();
            LoadFriend();

            StartReceiver();
            AddStartTab();
            AddChatRoomTab();

            this.SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
        }
        private void LoadFriend()
        {
            MainData.Friends = new Util.IO.FriendJsonFileHandler().LoadFriends();
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_SHOWME)
            {
                ShowMainWindow();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void LoadUser()
        {
            MainData.currentUser = new Util.IO.UserJsonFileHandler().LoadUser();
            NicknameBox.Text = MainData.currentUser.Nickname;
        }
        /*  App.xaml.cs -> TrayIconManager.cs 호출 방식으로  이관
        private void InitTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "워키도히"
            };

            try
            {
                string iconPath = "AppResources/Assets/WalkieDohi.ico";
        
                if (File.Exists(iconPath))
                    trayIcon.Icon = new Icon(iconPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("아이콘 설정 실패: " + ex.Message);
            }

            trayIcon.DoubleClick += (s, e) => ShowMainWindow();
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("열기", null, (s, e) => ShowMainWindow());
            trayIcon.ContextMenuStrip.Items.Add("종료", null, (s, e) => ExitApplication());
        }
        */
        private void StartReceiver()
        {
            MainReceiver = new PacketReceiver(MainData.GetPort());
            MainReceiver.OnMessageReceived += async (msg) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    msg.Sender = MainData.GetFriendNameOrReturnOriginal(msg.Sender, msg.SenderIp);
                    ChatListManager.UpdateChatList(msg);
                    _chatRoomListTabControl?.HandleIncomingMessage(msg);
                });
            };
            MainReceiver.Start();
        }

        private void AddStartTab()
        {
            _startTabControl = new FriendMainListView();
            _startTabControl.SetFriends(MainData.GetsortedFriends());
            _startTabControl.OnStartChat += friend =>
            {
                MainData.GetFriendNameOrReturnOriginal(friend);
                ChatListManager.UpdateChatList(friend.Name, friend.Ip);
            };
            _startTabControl.OnStartGroupChat += group =>
            {
                ChatListManager.UpdateChatList(group);
            };

            var tab = new TabItem
            {
                Header = "친구리스트",
                Content = _startTabControl,
                Name =  "FriendMainList"
            };
            ChatTabControlHost.Items.Add(tab);
        }

        private void AddChatRoomTab()
        {
            _chatRoomListTabControl = new ChatRoomListTabControl();

            var tab = new TabItem
            {
                Header = "💬 채팅방",
                Content = _chatRoomListTabControl
            };

            ChatTabControlHost.Items.Add(tab);
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            TrayIconManager.Dispose(); // 유령 아이콘 방지
            Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            MainReceiver?.Stop();
            base.OnClosed(e);
        }

        private void SaveUserButton_Click(object sender, RoutedEventArgs e)
        {
            MainData.currentUser.Nickname = NicknameBox.Text.Trim();
            if (new Util.IO.UserJsonFileHandler().SaveUser(MainData.currentUser))
            {
                MessageBox.Show("닉네임이 저장되었습니다.");
            }
        }

        private void ManageFriends_Click(object sender, RoutedEventArgs e)
        {
            var popup = new FriendManagerWindow { Owner = this };
            popup.ShowDialog();
            _startTabControl?.SetFriends(MainData.GetsortedFriends());
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingWindow = new Users.Views.SettingWindow { Owner = this };
            settingWindow.ShowDialog();
            _startTabControl?.SetFriends(MainData.GetsortedFriends());
        }

        public void ShowChatRoomFromStart(string name, string ip)
        {
            ChatListManager.UpdateChatList(MainData.GetFriendNameOrReturnOriginal(name,ip), ip);
            ActivateChatRoomTab();

            _chatRoomListTabControl?.SelectChatByKey(ip);
        }
        public void ShowChatRoomFromStart(GroupEntity group)
        {

            bool notExisting = !group.Ips.Contains(NetworkHelper.GetLocalIPv4());
            if (notExisting)
            {
                MessageBox.Show("본인이 포함된 그룹만 가능합니다.");
                return;
            }

            if (group != null)
                ChatListManager.UpdateChatList(group);
            else
                return;

            ActivateChatRoomTab();

            _chatRoomListTabControl?.SelectChatByKey(group.Key);
        }


        public void BringToFrontAndShowChat(string name,string ip)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();

            ChatListManager.UpdateChatList(MainData.GetFriendNameOrReturnOriginal(name, ip), ip);
            ActivateChatRoomTab();
            _chatRoomListTabControl?.SelectChatByKey(ip);


        }
        public void BringToFrontAndShowChat(GroupEntity group) 
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (group == null)
            {
                return;
            }
            ChatListManager.UpdateChatList(group);
            ActivateChatRoomTab();
            _chatRoomListTabControl?.SelectChatByKey(group.Key);
        }

        private void ActivateChatRoomTab()
        {
            foreach (TabItem tab in ChatTabControlHost.Items)
            {
                if (tab.Content is ChatRoomListTabControl)
                {
                    ChatTabControlHost.SelectedItem = tab;
                    break;
                }
            }
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var selectTab = ChatTabControlHost.SelectedItem as TabItem;
                if (selectTab == null)
                {
                    return;
                }

                if (string.Equals(selectTab.Name, "FriendMainList"))
                {
                    this.Close();
                }
                else
                {
                    ChatTabControlHost.SelectedIndex = 0;
                }
                
            }
        }

        private bool _isToolsPanelOpen = false;

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isToolsPanelOpen)
            {
                // 닫기
                var sb = (Storyboard)FindResource("HideToolsPanelStoryboard");
                sb.Begin();
                ToolsPanel.IsHitTestVisible = false;
            }
            else
            {
                // 열기
                ToolsPanel.IsHitTestVisible = true;
                var sb = (Storyboard)FindResource("ShowToolsPanelStoryboard");
                sb.Begin();
            }

            _isToolsPanelOpen = !_isToolsPanelOpen;
        }


        private void GameButton_Click(object sender, RoutedEventArgs e)
        {
            var gameWindow = new Games.Views.MiniGameWindow();
            gameWindow.Owner = this;
            gameWindow.Show();

        }

        private void GitButton_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/Goddohi/dohiWindowMessageApp";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true  // 기본 브라우저로 열기 위한 옵션
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("URL을 열 수 없습니다.\n" + ex.Message,
                                "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isToolsPanelOpen)
                return;

            var clicked = e.OriginalSource as DependencyObject;

            // 클릭한 요소가 메뉴 버튼이나 패널 내부인지 검사
            while (clicked != null)
            {
                if (clicked == MenuButton || clicked == ToolsPanel)
                {
                    // 메뉴 안 / 버튼 클릭이면 닫지 않음
                    return;
                }

                clicked = VisualTreeHelper.GetParent(clicked);
            }

            // 여기까지 왔다는 건 메뉴 밖을 클릭한 경우 → 닫기
            var sb = (Storyboard)FindResource("HideToolsPanelStoryboard");
            sb.Begin();
            ToolsPanel.IsHitTestVisible = false;
            _isToolsPanelOpen = false;
        }

        private void TextCountToolButton_Click(object sender, RoutedEventArgs e)
        {
            var textToolWindow = new ToolMenus.Views.TextCountToolWindow();
            textToolWindow.Owner = this;
            textToolWindow.Show();
        }
    }
}