﻿using WalkieDohi.Entity;
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

namespace WalkieDohi
{
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        private MessengerReceiver msgReceiver;
        private MessengerSender msgSender = new MessengerSender();
        private Dictionary<string, ChatTabControl> chatTabs = new Dictionary<string, ChatTabControl>();

        public MainWindow()
        {
            InitializeComponent();
            InitTrayIcon();
            LoadUser();
            LoadFriends();
            StartReceiver();
            AddStartTab();
        }

        private void InitTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "도히 메신저"
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
                    var tab = AddOrFocusChatTab(msg.Sender, msg.SenderIp, 9000);

                    if (msg.Type == "file")
                    {
                        string folderPath = @"C:\\ReceivedFiles";
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        string fullPath = System.IO.Path.Combine(folderPath, msg.FileName);
                        File.WriteAllBytes(fullPath, Convert.FromBase64String(msg.Content));

                        tab.AddReceivedFile(msg.Sender, msg.FileName, fullPath);
                    }
                    else
                    {
                        tab.AddMessage(msg.Sender, msg.Content, messageType.Receive);
                    }
                });
            };
            msgReceiver.Start();
        }

        private ChatTabControl AddOrFocusChatTab(string name, string ip, int port)
        {
            string key = ip;

            if (chatTabs.ContainsKey(key))
            {
                return chatTabs[key];
            }

            var chatControl = new ChatTabControl { TargetIp = ip, TargetPort = port };
            chatControl.OnSendMessage += async (s, messageText) =>
            {
                var msg = new MessageEntity
                {
                    Type = "text",
                    Sender = MainData.currentUser.Nickname,
                    SenderIp = NetworkHelper.GetLocalIPv4(),
                    Content = messageText
                };
                await msgSender.SendMessageAsync(ip, port, msg);
            };

            chatControl.OnSendFile += async (s, fileInfo) =>
            {
                var msgEntity = new MessageEntity
                {
                    Type = "file",
                    Sender = MainData.currentUser.Nickname,
                    SenderIp = NetworkHelper.GetLocalIPv4(),
                    Content = fileInfo.Base64Content,
                    FileName = fileInfo.FileName
                };
                await msgSender.SendMessageAsync(ip, port, msgEntity);
            };

            var tab = new TabItem { Header = $"{name}({ip})", Content = chatControl };
            ChatTabControlHost.Items.Add(tab);
            ChatTabControlHost.SelectedItem = tab;
            chatTabs[key] = chatControl;
            return chatControl;
        }

        private void AddStartTab()
        {
            var startControl = new StartChatTabControl();
            startControl.SetFriends(MainData.Friends);
            startControl.OnStartChat += friend =>
            {
                AddChatTab(friend.Name, friend.Ip, friend.Port);
            };

            var tab = new TabItem
            {
                Header = "➕ 채팅 시작하기",
                Content = startControl
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
            trayIcon.ShowBalloonTip(3000, "워키도히", "백그라운드에서 실행 중입니다.", ToolTipIcon.Info);
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
                var msgEntity = new MessageEntity
                {
                    Type = "text",
                    Sender = MainData.currentUser.Nickname,
                    SenderIp = NetworkHelper.GetLocalIPv4(),
                    Content = messageText
                };
                await msgSender.SendMessageAsync(ip, port, msgEntity);
            };

            var tab = new TabItem
            {
                Header = $"{name}({ip})",
                Content = chatControl
            };

            ChatTabControlHost.Items.Add(tab);
            chatTabs[key] = chatControl;
            ChatTabControlHost.SelectedItem = tab;
        }
    }
}
