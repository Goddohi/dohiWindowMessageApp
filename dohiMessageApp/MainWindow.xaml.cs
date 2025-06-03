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
            LoadFriends();
            LoadGroups();
            StartReceiver();
            AddStartTab();
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

                    if (msg.CheckMessageTypeFile())
                    {
                        MessageUtil.CheckFileDrietory();

                        File.WriteAllBytes(MessageUtil.GetFilePath(msg.FileName), Convert.FromBase64String(msg.Content));

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

        private void LoadUser()
        {
            var path = "user.json";
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                MainData.currentUser = JsonConvert.DeserializeObject<User>(json);
                NicknameBox.Text = MainData.currentUser.Nickname;
            }
            else
            {
                MainData.currentUser = new User { Nickname = "사용자" };
                NicknameBox.Text = MainData.currentUser.Nickname;
                SaveUser(false);
            }
        }

        private void SaveUser(bool showMessage)
        {
            MainData.currentUser.Nickname = NicknameBox.Text.Trim();
            File.WriteAllText("user.json", JsonConvert.SerializeObject(MainData.currentUser, Formatting.Indented));
            if (showMessage) MessageBox.Show("닉네임이 저장되었습니다.");
        }

        private void LoadFriends()
        {
            var path = "friends.json";
            if (!File.Exists(path))
            {
                MainData.Friends = new List<Friend> {
                    new Friend { Name = "로컬 테스트", Ip = "127.0.0.1", Port = 9000 }
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(MainData.Friends, Formatting.Indented));
                MessageBox.Show("기본 친구 목록을 생성했습니다.");
            }
            else
            {
                MainData.Friends = JsonConvert.DeserializeObject<List<Friend>>(File.ReadAllText(path));
            }
        }

        private void LoadGroups()
        {
            var path = "groups.json";
            if (!File.Exists(path))
            {
                MainData.Groups = new List<GroupEntity>();
                File.WriteAllText(path, JsonConvert.SerializeObject(MainData.Friends, Formatting.Indented));
    
            }
            else
            {
                MainData.Groups = JsonConvert.DeserializeObject<List<GroupEntity>>(File.ReadAllText(path));
            }
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
            SaveUser(true);
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

        private TabBasicinterface AddOrFocusChatTab(MessageEntity msg, int port)
        {
            string key = msg.Group?.GroupName ?? msg.SenderIp;

            if (msg.Group == null)
            {
                if (chatTabs.ContainsKey(key))
                {
                    return chatTabs[key];
                }

                var chatControl = new ChatTabControl { TargetIp = msg.SenderIp, TargetPort = port };
                chatControl.OnSendMessage += async (s, messageText) =>
                {
                    var msgEntity = MessageEntity.OfSendTextMassage(messageText);
                    await msgSender.SendMessageAsync(msg.SenderIp, port, msgEntity);
                };

                chatControl.OnSendFile += async (s, fileInfo) =>
                {
                    var msgEntity = MessageEntity.OfSendFileMassage(fileInfo.Base64Content, fileInfo.FileName);
                    await msgSender.SendMessageAsync(msg.SenderIp, port, msgEntity);
                };

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
                    return chatTabs[key];
                }

                var GroupchatControl = new GroupChatTabControl { TargetGroup = msg.Group, TargetPort = port };
                GroupchatControl.SetGroupMembers(MainData.Friends);
                GroupchatControl.OnSendMessage += async (s, messageText) =>
                {
                    var msgEntity = MessageEntity.OfGroupSendTextMassage(msg.Group,messageText);
                    var tasks = msg.Group.Ips
                                .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                                .Select(ip => msgSender.SendMessageAsync(ip, port, msgEntity));

                    await Task.WhenAll(tasks);

                };

                GroupchatControl.OnSendFile += async (s, fileInfo) =>
                {
                    var msgEntity = MessageEntity.OfGroupSendFileMassage(msg.Group, fileInfo.Base64Content, fileInfo.FileName);
                    var tasks = msg.Group.Ips
                                .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                                .Select(ip => msgSender.SendMessageAsync(ip, port, msgEntity));

                    await Task.WhenAll(tasks);
               
                };

                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Tag = key
                };

                //그전에 처리하긴 하지만 그래도 혹시나하여 안빼고 처리 
                msg.Sender = MainData.GetFriendNameOrReturnOriginal(msg.Sender, msg.SenderIp);
                headerPanel.Children.Add(new TextBlock
                {
                    Text = $"({msg.Group.GroupName}생성자:{msg.Sender}))",
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

            var chatControl = new ChatTabControl
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
                await msgSender.SendMessageAsync(ip, port, msgEntity);
            };
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
            string key = group.GroupName;

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

            var GroupchatControl = new GroupChatTabControl
            {
                TargetGroup = group,
                TargetPort = group.Port
            };
            GroupchatControl.SetGroupMembers(MainData.Friends);
            GroupchatControl.OnSendMessage += async (s, messageText) =>
            {
                var msgEntity = MessageEntity.OfGroupSendTextMassage(group,messageText);
                var tasks = group.Ips
                            .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                            .Select(ip => msgSender.SendMessageAsync(ip, group.Port, msgEntity));

                await Task.WhenAll(tasks);
            };

            GroupchatControl.OnSendFile += async (s, fileInfo) =>
            {
                var msgEntity = MessageEntity.OfGroupSendFileMassage(group, fileInfo.Base64Content, fileInfo.FileName);
                var tasks = group.Ips
                            .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                            .Select(ip => msgSender.SendMessageAsync(ip, group.Port, msgEntity));

                await Task.WhenAll(tasks);
            };

            var headerPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Tag = key
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"({group.GroupName})",
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
    }
}
