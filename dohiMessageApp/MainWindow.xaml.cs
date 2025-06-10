using WalkieDohi.Entity;
using WalkieDohi.Util;
using WalkieDohi.UC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Drawing;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using WalkieDohi.UI;
using Orientation = System.Windows.Controls.Orientation;
using System.Security.Cryptography;
using WalkieDohi.Core;
using System.Windows.Interop;
using System.Threading.Tasks;
using WalkieDohi.Core.app;

namespace WalkieDohi
{
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        private MessengerReceiver msgReceiver;
        private MessengerSender msgSender = new MessengerSender();
        private Dictionary<string, TabBasicinterface> chatTabs = new Dictionary<string, TabBasicinterface>();
        private StartChatTabControl _startTabControl; // 추가


        public MainWindow()
        {
            InitializeComponent();

            InitTrayIcon();
            LoadUser();
            JsonDataLoadingHelper.LoadFriends();
            JsonDataLoadingHelper.LoadGroups();
            StartReceiver();
            AddStartTab();

            this.SourceInitialized += OnSourceInitialized;
        }
        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WndProc);
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
            NicknameBox.Text = JsonDataLoadingHelper.LoadUser();
        }
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
                string iconPath = "Assets/WalkieDohi.ico";
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

        private void StartReceiver()
        {
            msgReceiver = new MessengerReceiver(9000);
            msgReceiver.OnMessageReceived += async (msg) =>
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    //상대방의 이름이 ip기준으로 내가 저장한 이름으로 출력되도록 수정 
                    msg.Sender = MainData.GetFriendNameOrReturnOriginal(msg.Sender, msg.SenderIp);
                    var tab = AddOrFocusChatTab(msg, 9000);
                    if(tab == null) { return;}
                    if (msg.CheckMessageTypeFile())
                    {
                        MessageUtil.CheckFileDrietory();
                        //파일을 읽어오는 곳
                        File.WriteAllBytes(MessageUtil.GetFilePath(msg.FileName), Convert.FromBase64String(msg.Content));

                        tab.AddReceivedFile(msg);
                    }
                    else if (msg.CheckMessageTypeImage())
                    {
                        MessageUtil.CheckImageDrietory();
                        //파일을 읽어오는 곳
                        File.WriteAllBytes(MessageUtil.GetImagePath(msg.FileName), Convert.FromBase64String(msg.Content));

                        tab.AddReceivedFile(msg);
                    }
                    else
                    {
                        tab.AddReceivedMessage(msg);
                    }
                });
            };
            msgReceiver.Start();
        }

       /// <summary>
       /// 시작 탭
       /// </summary>
        private void AddStartTab()
        {
            _startTabControl = new StartChatTabControl();
            _startTabControl.SetFriends(MainData.Friends);
            _startTabControl.OnStartChat += friend =>
            {
                MainData.GetFriendNameOrReturnOriginal(friend);
                AddChatTab(friend.Name, friend.Ip, friend.Port);

            };

            _startTabControl.SetGroups(MainData.Groups);
            _startTabControl.OnStartGroupChat += group =>
            {
                
                AddChatTab(group);

            };
            var tab = new TabItem
            {
                Header = "➕ 채팅 시작하기",
                Content = _startTabControl
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
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            //알람이 불편하다 하여 수정 
            //trayIcon.ShowBalloonTip(3000, "워키도히", "백그라운드에서 실행 중입니다.", ToolTipIcon.Info);
        }

        protected override void OnClosed(EventArgs e)
        {
            msgReceiver?.Stop();
            base.OnClosed(e);
        }

        private void SaveUserButton_Click(object sender, RoutedEventArgs e)
        {
            JsonDataLoadingHelper.SaveUser(NicknameBox.Text.Trim(), true);
        }

        private void ManageFriends_Click(object sender, RoutedEventArgs e)
        {
            var popup = new FriendManagerWindow { Owner = this };
            popup.ShowDialog();
         
            _startTabControl?.SetFriends(MainData.Friends);
      

        }
        private void ManageGroups_Click(object sender, RoutedEventArgs e)
        {
            var popup = new GroupManagerWindow { Owner = this };
            popup.ShowDialog();
           
               _startTabControl?.SetGroups(MainData.Groups);
            
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingWindow = new SettingWindow
            {
                Owner = this
            };
            settingWindow.ShowDialog();
        }

        #region 탭 생성로직

        private TabBasicinterface AddOrFocusChatTab(MessageEntity msg, int port)
        {
            string key = msg.Group?.GroupName ?? msg.SenderIp;

            if (msg.Group == null)
            {
                if (chatTabs.ContainsKey(key))
                {
                    return chatTabs[key];
                }

                var chatControl = GetChatTab(msg.SenderIp, port);

                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Tag = key
                };

                //그전에 처리하긴 하지만 그래도 혹시나하여 안빼고 처리 
                msg.Sender = MainData.GetFriendNameOrReturnOriginal(msg.Sender, msg.SenderIp);
                headerPanel.Children.Add(new TextBlock
                {
                    Text = $"{msg.Sender}({msg.SenderIp})",
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var closeBtn = new System.Windows.Controls.Button
                {
                    Content = "×",
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    Padding = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontWeight = FontWeights.Bold,
                    Width = 16,
                    Height = 16
                };

                closeBtn.Click += (s, e) =>
            {
                var tabToRemove = ChatTabControlHost.Items.Cast<TabItem>()
                    .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == key);
                if (tabToRemove != null)
                {
                    ChatTabControlHost.Items.Remove(tabToRemove);
                    chatTabs.Remove(key);
                }
            };

                headerPanel.Children.Add(closeBtn);

                var tab = new TabItem { Header = headerPanel, Content = chatControl };
                ChatTabControlHost.Items.Add(tab);
                ChatTabControlHost.SelectedItem = tab;
                chatTabs[key] = chatControl;
                return chatControl;
            }
            else
            {

                if (chatTabs.ContainsKey(key))
                {
                    var chatTab = (GroupChatTabControl)chatTabs[key];

                    var incomingIps = msg.Group.Ips.Distinct().OrderBy(ip => ip).ToList();
                    var existingIps = chatTab.TargetGroup.Ips.Distinct().OrderBy(ip => ip).ToList();

                    if (incomingIps.SequenceEqual(existingIps))
                    {
                        return chatTabs[key]; // 동일한 IP 리스트를 가진 기존 그룹
                    }
                    if (chatTab.TargetGroup.Key == msg.Group.Key) //이전 버전 사용자용 
                    {
                        return chatTabs[key];
                    }
                }

                var GroupchatControl = GetGroupChatTab(msg.Group);
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Tag = key
                };

                //그전에 처리하긴 하지만 그래도 혹시나하여 안빼고 처리 
                msg.Sender = MainData.GetFriendNameOrReturnOriginal(msg.Sender, msg.SenderIp);
                headerPanel.Children.Add(new TextBlock
                {
                    Text = $"({msg.Group.GroupName})",
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var closeBtn = new System.Windows.Controls.Button
                {
                    Content = "×",
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    Padding = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontWeight = FontWeights.Bold,
                    Width = 16,
                    Height = 16
                };

                closeBtn.Click += (s, e) =>
                {
                    var tabToRemove = ChatTabControlHost.Items.Cast<TabItem>()
                        .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == key);
                    if (tabToRemove != null)
                    {
                        ChatTabControlHost.Items.Remove(tabToRemove);
                        chatTabs.Remove(key);
                    }
                };

                headerPanel.Children.Add(closeBtn);

                var tab = new TabItem { Header = headerPanel, Content = GroupchatControl };
                ChatTabControlHost.Items.Add(tab);
                ChatTabControlHost.SelectedItem = tab;
                chatTabs[key] = GroupchatControl;
                return GroupchatControl;
            }
        }

        private void AddChatTab(string name, string ip, int port)
        {
            string key = ip;

            if (chatTabs.ContainsKey(key))
            {
                var existing = ChatTabControlHost.Items.Cast<TabItem>()
                    .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == key);

                if (existing != null)
                {
                    ChatTabControlHost.SelectedItem = existing;
                }
                return;
            }

            var chatControl = GetChatTab(ip, port);

            var headerPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Tag = key
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"{name}({ip})",
                Margin = new Thickness(0, 0, 5, 0)
            });

            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "×",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                Padding = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = System.Windows.Media.Brushes.Gray,
                FontWeight = FontWeights.Bold,
                Width = 16,
                Height = 16
            };

            closeBtn.Click += (s, e) =>
            {
                var tabToRemove = ChatTabControlHost.Items.Cast<TabItem>()
                    .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == key);
                if (tabToRemove != null)
                {
                    ChatTabControlHost.Items.Remove(tabToRemove);
                    chatTabs.Remove(key);
                }
            };

            headerPanel.Children.Add(closeBtn);

            var tab = new TabItem
            {
                Header = headerPanel,
                Content = chatControl
            };

            ChatTabControlHost.Items.Add(tab);
            chatTabs[key] = chatControl;
            ChatTabControlHost.SelectedItem = tab;
        }

        private void AddChatTab(GroupEntity group)
        {
            if (!group.Ips.Contains(NetworkHelper.GetLocalIPv4()))
            {
                MessageBox.Show("본인이 포함되어있지 않은 그룹은 생성불가입니다.");
                return;
            }
            string key = group.GroupName;

            if (chatTabs.ContainsKey(key))
            {
                var chatTab = (GroupChatTabControl)chatTabs[key];

                var incomingIps = group.Ips.Distinct().OrderBy(ip => ip).ToList();
                var existingIps = chatTab.TargetGroup.Ips.Distinct().OrderBy(ip => ip).ToList();

                if (incomingIps.SequenceEqual(existingIps))
                {
                    var existing = ChatTabControlHost.Items.Cast<TabItem>()
                        .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == key);

                    if (existing != null)
                    {
                        ChatTabControlHost.SelectedItem = existing;
                    }
                    return;
                }
                //이전 버전 사용자용
                if (chatTab.TargetGroup.Key == group.Key)
                {
                    var existing = ChatTabControlHost.Items.Cast<TabItem>()
                        .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == key);

                    if (existing != null)
                    {
                        ChatTabControlHost.SelectedItem = existing;
                    }
                    return;
                }
            }
            group.SetRandomKey();
            var GroupchatControl = GetGroupChatTab(group);
            var headerPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Tag = key
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"({GroupchatControl.TargetGroup.GroupName})",
                Margin = new Thickness(0, 0, 5, 0)
            });

            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "×",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                Padding = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = System.Windows.Media.Brushes.Gray,
                FontWeight = FontWeights.Bold,
                Width = 16,
                Height = 16
            };

            closeBtn.Click += (s, e) =>
            {
                var tabToRemove = ChatTabControlHost.Items.Cast<TabItem>()
                    .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == key);
                if (tabToRemove != null)
                {
                    ChatTabControlHost.Items.Remove(tabToRemove);
                    chatTabs.Remove(key);
                }
            };

            headerPanel.Children.Add(closeBtn);

            var tab = new TabItem
            {
                Header = headerPanel,
                Content = GroupchatControl
            };

            ChatTabControlHost.Items.Add(tab);
            chatTabs[key] = GroupchatControl;
            ChatTabControlHost.SelectedItem = tab;
        }

        private GroupChatTabControl GetGroupChatTab(GroupEntity group) {

            var GroupchatControl = new GroupChatTabControl
            {
                TargetGroup = group,
                TargetPort = group.Port
            };
            GroupchatControl.SetGroupMembers(MainData.Friends);
            GroupchatControl.OnSendMessage += async (s, messageText) =>
            {
                var msgEntity = MessageEntity.OfGroupSendTextMassage(GroupchatControl.TargetGroup, messageText);
                var tasks = GroupchatControl.TargetGroup.Ips
                            .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                            .Select(ip => msgSender.SendMessageAsync(ip, GroupchatControl.TargetGroup.Port, msgEntity));

                await Task.WhenAll(tasks);
            };

            GroupchatControl.OnSendFile += async (s, fileInfo) =>
            {
                var msgEntity = MessageEntity.OfGroupSendFileMassage(GroupchatControl.TargetGroup, fileInfo.Base64Content, fileInfo.FileName);
                if (MessageImageUtil.isImagecheck(msgEntity.FileName))
                {
                    msgEntity.Type = MessageType.Image;
                }
                var tasks = GroupchatControl.TargetGroup.Ips
                            .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                            .Select(ip => msgSender.SendMessageAsync(ip, GroupchatControl.TargetGroup.Port, msgEntity));

                await Task.WhenAll(tasks);
            };

            return GroupchatControl;
        }

        private SingleChatTabControl GetChatTab(string ip,int port)
        {
            var chatControl = new SingleChatTabControl
            {
                TargetIp = ip,
                TargetPort = port
            };

            chatControl.OnSendMessage += async (s, messageText) =>
            {
                var msgEntity = MessageEntity.OfSendTextMassage(messageText);
                await msgSender.SendMessageAsync(ip, port, msgEntity);
            };
            chatControl.OnSendFile += async (s, fileInfo) =>
            {
                var msgEntity = MessageEntity.OfSendFileMassage(fileInfo.Base64Content, fileInfo.FileName);
                if (MessageImageUtil.isImagecheck(msgEntity.FileName))
                {
                    msgEntity.Type = MessageType.Image;
                }
                await msgSender.SendMessageAsync(ip, port, msgEntity);
            };
            return chatControl;
        }

        #endregion 탭 생성로직

        #region  탭 외부 호출로직
        public void SelectChatTab(string tabKey,GroupEntity group)
        {
            if(group == null)
            {
                if (chatTabs.ContainsKey(tabKey))
                {
                    var existing = ChatTabControlHost.Items.Cast<TabItem>()
                        .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == tabKey);

                    if (existing != null)
                    {
                        ChatTabControlHost.SelectedItem = existing;
                    }
                }
                return;
            }

            if (chatTabs.ContainsKey(group.GroupName))
            {
                var chatTab = (GroupChatTabControl)chatTabs[group.GroupName];

                var incomingIps = group.Ips.Distinct().OrderBy(ip => ip).ToList();
                var existingIps = chatTab.TargetGroup.Ips.Distinct().OrderBy(ip => ip).ToList();

                if (incomingIps.SequenceEqual(existingIps))
                {
                    var existing = ChatTabControlHost.Items.Cast<TabItem>()
                        .FirstOrDefault(t => t.Header is StackPanel panel && panel.Tag?.ToString() == group.GroupName);

                    if (existing != null)
                    {
                        ChatTabControlHost.SelectedItem = existing;
                    }
                    
                }
            }
        }

        #endregion

    }
}
