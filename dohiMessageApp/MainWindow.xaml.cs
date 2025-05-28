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

namespace dohiMessageApp
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private MessengerReceiver msgReceiver;
        private MessengerSender msgSender = new MessengerSender();


        public MainWindow()
        {

            InitializeComponent();
            DataSetting();
            SendStartSetting();
        }
        public void DataSetting()
        {
            LoadUser();
            LoadFriends();
        }
        public void SendStartSetting()
        {
            msgReceiver = new MessengerReceiver(9000);
            msgReceiver.OnMessageReceived += (msg) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (msg.Type == "text")
                    {
                        MessageList.Items.Add($"📩 {msg.Sender}({msg.SenderIp}): {msg.Content}");
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
                        MainWindowData.receivedFiles[listItem] = fullPath;
                    }
                });
            };
            msgReceiver.Start();

        }

        /*
         * 
         */
        protected override void OnClosed(EventArgs e)
        {
            msgReceiver?.Stop();
            base.OnClosed(e);
        }






        #region UI 이벤트
        private async void SendButton_Click(object senderBtn, RoutedEventArgs e)
        {
            string text = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var message = new MessageEntity
            {
                Type = "text",
                Sender = MainWindowData.currentUser.Nickname,
                SenderIp = NetworkHelper.GetLocalIPv4(),
                Content = text,
                FileName = null
            };


            await SendMessageAsync(message);
            InputBox.Clear();
        }


        private async void SendFileButton_Click(object senderBtn, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "보낼 파일 선택";
            dialog.Filter = "모든 파일 (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
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
                        Sender = MainWindowData.currentUser.Nickname,
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

        private void MessageList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MessageList.SelectedItem is string selectedItem)
            {
                if (MainWindowData.receivedFiles.ContainsKey(selectedItem))
                {
                    string path = MainWindowData.receivedFiles[selectedItem];

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
            MainWindowData.friends = popup.Friends;
            FriendComboBox.ItemsSource = null;
            FriendComboBox.ItemsSource = MainWindowData.friends;
            FriendComboBox.DisplayMemberPath = "Name";
            if (MainWindowData.friends.Count > 0)
                FriendComboBox.SelectedIndex = 0;
        }

        #endregion


        private void LoadUser()
        {
            string path = "user.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                MainWindowData.currentUser = JsonConvert.DeserializeObject<User>(json);
                
            }
            else
            {
                MainWindowData.currentUser.Nickname = "사용자";   
                SaveUser(false);
            }
            NicknameBox.Text = MainWindowData.currentUser.Nickname;
        }

        private void SaveUser(bool messageYN)
        {
            MainWindowData.currentUser.Nickname = NicknameBox.Text.Trim();
            string json = JsonConvert.SerializeObject(MainWindowData.currentUser, Formatting.Indented);
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
                MainWindowData.friends = new List<Friend>
            {
                new Friend { Name = "로컬 테스트", Ip = "127.0.0.1", Port = 9000 }
            };

                string json = JsonConvert.SerializeObject(MainWindowData.friends, Formatting.Indented);
                File.WriteAllText(path, json);
                MessageBox.Show("친구 목록 파일(friends.json)이 없어 생성했습니다.");
            }
            else  // (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                MainWindowData.friends = JsonConvert.DeserializeObject<List<Friend>>(json);

                
            }
            FriendComboBox.ItemsSource = MainWindowData.friends;
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
