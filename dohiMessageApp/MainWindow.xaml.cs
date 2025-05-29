using dohiMessageApp.Entity;
using dohiMessageApp.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using dohiMessageApp.UI;
using System.Windows.Forms; 
using System.Drawing;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace dohiMessageApp
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {

        private MessengerReceiver msgReceiver;
        private MessengerSender msgSender = new MessengerSender();
        private NotifyIcon trayIcon;

        #region 생성자
        public MainWindow()
        {

            InitializeComponent();
            DataSetting();
            SendStartSetting();

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,  //new Icon("icon.ico"),
                Visible = true,
                Text = "도히 메신저"
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            trayIcon.BalloonTipClicked += (s, e) =>
            {
                ShowMainWindow(); // 창 복원 함수
            };

            // 우클릭 메뉴
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("열기", null, (s, e) => ShowMainWindow());
            trayIcon.ContextMenuStrip.Items.Add("종료", null, (s, e) => ExitApplication());

        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;        // 닫기 방지
            this.Hide();            // 창 숨기기
            trayIcon.ShowBalloonTip(1000, "도히 메신저", "백그라운드에서 실행 중입니다.", ToolTipIcon.Info);
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

        #endregion 생성자



        #region 설정메소드
        public void DataSetting()
        {
            LoadUser();
            LoadFriends();
        }
        private void SendStartSetting()
        {
            msgReceiver = new MessengerReceiver(9000);
            msgReceiver.OnMessageReceived += (msg) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (msg.Type == "text")
                    {
                        MessageList.Items.Add($"📩 {msg.Sender}({msg.SenderIp}): {msg.Content}");
                        // 트레이 알림 띄우기
                        trayIcon.ShowBalloonTip(
                            2000,                        // 표시 시간 (ms)
                            $"📨 {msg.Sender}님이 보냄",   // 제목
                            msg.Content,                // 내용
                            ToolTipIcon.Info            // 아이콘 종류 (Info, Warning, Error, None)
                        );
                    }
                    else if (msg.Type == "file")
                    {
                        string folderPath = @"C:\ReceivedFiles";
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        string fullPath = System.IO.Path.Combine(folderPath, msg.FileName);
                        File.WriteAllBytes(fullPath, Convert.FromBase64String(msg.Content));

                        string listItem = $"📁 파일 수신 {msg.Sender}({msg.SenderIp}) : {msg.FileName}";
                        MessageList.Items.Add(listItem);
                        MainData.receivedFiles[listItem] = fullPath;
                    }
                });
            };
            msgReceiver.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            msgReceiver?.Stop();
            base.OnClosed(e);
        }

        #endregion 설정메소드

        #region UI 이벤트


        private void InputBox_KeyDown(object sender,KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (Keyboard.IsKeyDown(Key.LeftAlt))
                {
                    //한줄추가
                    InputBox.Text = InputBox.Text + Environment.NewLine;
                }
                else
                {
                    SendTextMessageAsync();
                    return;
                }
            }
        }
        private void SendButton_Click(object senderBtn, RoutedEventArgs e)
        {
            SendTextMessageAsync();
        }


        private void SendFileButton_Click(object senderBtn, RoutedEventArgs e)
        {
            SendFileMessageAsync();
        }

        private void MessageList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MessageList.SelectedItem is string selectedItem)
            {
                if (MainData.receivedFiles.ContainsKey(selectedItem))
                {
                    string path = MainData.receivedFiles[selectedItem];

                    if (File.Exists(path))
                    {
                        // 탐색기에서 파일 위치 열기 + 파일 선택
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                    }
                    else
                    {
                        MessageBox.Show("파일이 존재하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        private void SaveUserButton_Click(object sender, RoutedEventArgs e)
        {
            SaveUser(true);
        }

        private void ManageFriends_Click(object sender, RoutedEventArgs e)
        {
            var popup = new FriendManagerWindow();
            popup.Owner = this;
            popup.ShowDialog();

            // 팝업에서 수정된 결과 반영
            //MainData.Friends = popup.Friends; (저장눌러야 수정되도록)

            FriendComboBox.ItemsSource = null;
            FriendComboBox.ItemsSource = MainData.Friends;
            FriendComboBox.DisplayMemberPath = "Name";
            if (MainData.Friends.Count > 0)
                FriendComboBox.SelectedIndex = 0;
        }

        #endregion UI 이벤트 끝



        private void LoadUser()
        {
            string path = "user.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                MainData.currentUser = JsonConvert.DeserializeObject<User>(json);
                NicknameBox.Text = MainData.currentUser.Nickname;
            }
            else
            {
                MainData.currentUser.Nickname = "사용자";
                NicknameBox.Text = MainData.currentUser.Nickname;
                SaveUser(false);
            }
        }

        private void SaveUser(bool messageYN)
        {
            MainData.currentUser.Nickname = NicknameBox.Text.Trim();
            string json = JsonConvert.SerializeObject(MainData.currentUser, Formatting.Indented);
            File.WriteAllText("user.json", json);
            if (messageYN)
            {
                MessageBox.Show("닉네임이 저장되었습니다.");
            }
        }

        private void LoadFriends()
        {
            string path = "friends.json";
            if (!File.Exists(path))
            {
                // 파일이 없으면 기본 친구 리스트 생성
                MainData.Friends = new List<Friend>
                {
                    new Friend { Name = "로컬 테스트", Ip = "127.0.0.1", Port = 9000 }
                };

                string json = JsonConvert.SerializeObject(MainData.Friends, Formatting.Indented);
                File.WriteAllText(path, json);
                MessageBox.Show("친구 목록 파일(friends.json)이 없어 생성했습니다.");
            }
            else  // (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                MainData.Friends = JsonConvert.DeserializeObject<List<Friend>>(json);

                
            }
            FriendComboBox.ItemsSource = MainData.Friends;
            FriendComboBox.DisplayMemberPath = "Name";
            FriendComboBox.SelectedIndex = 0; // 첫 번째 친구 선택
        }


        private Friend checkSelectedFriend()
        {
            if (FriendComboBox.SelectedItem is null)
            {
                MessageBox.Show("보낼 친구를 선택하세요.");
                return null;
            }

            return (Friend)FriendComboBox.SelectedItem;

        }
        async void SendTextMessageAsync()
        {
            string text = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var message = new MessageEntity
            {
                Type = "text",
                Sender = MainData.currentUser.Nickname,
                SenderIp = NetworkHelper.GetLocalIPv4(),
                Content = text,
                FileName = null
            };


            await SendMessageAsync(message);
            InputBox.Clear();
        }

        async void SendFileMessageAsync()
        {
            string filePath = getOpenFilePath();
            if (!filePath.Equals(""))
            {
                FileInfo fileInfo = new FileInfo(filePath);

                //  10MB 초과 파일 체크
                const long MaxFileSize = 10 * 1024 * 1024; // 10MB

                if (fileInfo.Length > MaxFileSize)
                {
                    MessageBox.Show("❗ 10MB를 초과하는 파일은 전송할 수 없습니다.", "파일 용량 초과", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    string base64 = Convert.ToBase64String(fileData);

                    var fileMessage = new MessageEntity
                    {
                        Type = "file",
                        Sender = MainData.currentUser.Nickname,
                        SenderIp = NetworkHelper.GetLocalIPv4(),
                        Content = base64,
                        FileName = System.IO.Path.GetFileName(filePath)
                    };

                    await SendMessageAsync(fileMessage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("파일 전송 실패: " + ex.Message);
                }
            }

        }

        private string getOpenFilePath()
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "보낼 파일 선택";
                dialog.Filter = "모든 파일 (*.*)|*.*";
                if (dialog.ShowDialog() == true)
                {
                    return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 오픈 실패: " + ex.Message);
            }
            return "";
            
        }

        async Task SendMessageAsync(MessageEntity message)
        {
            Friend selectFriend = checkSelectedFriend();


            await msgSender.SendMessageAsync(selectFriend.Ip, selectFriend.Port, message);
            if (message.Type =="text") {
                MessageList.Items.Add($"📤 나 →{selectFriend.Name}: {message.Content}");
            }
            if (message.Type == "file")
            {
                MessageList.Items.Add($"📤 나 → {selectFriend.Name} 파일 전송: {message.FileName}");
            }
        }

    }
}
