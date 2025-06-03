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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WalkieDohi.Core;
using WalkieDohi.Entity;
using WalkieDohi.UI;
using WalkieDohi.Util;

namespace WalkieDohi.UC
{
    /// <summary>
    /// GroupChatTabControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GroupChatTabControl : UserControl , TabBasicinterface
    {

        public GroupEntity TargetGroup { get; set; }
        public int TargetPort { get; set; }

        public event EventHandler<string> OnSendMessage;

        public event EventHandler<(string FileName, string Base64Content)> OnSendFile;

        private Dictionary<string, string> receivedFiles = new Dictionary<string, string>();
        public GroupChatTabControl()
        {
            InitializeComponent();
            SendButton.Click += (s, e) => Send();
        }

        #region UI 이벤트

        public void SetGroupMembers(List<Friend> allFriends)
        {
            if (TargetGroup == null) return;

            var members = TargetGroup.Ips.Select(ip =>
            {
                var name = allFriends.FirstOrDefault(f => f.Ip == ip)?.Name;
                return new
                {
                    Ip = ip,
                    DisplayText = (ip == NetworkHelper.GetLocalIPv4()) ? "본인" : $"{name ?? "(이름 없음)"} ({ip})"
                };
            }).ToList();

            GroupMemberList.ItemsSource = members;
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            SendFileMessageAsync();
        }

        private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {//쉬프트+엔터
                return;
            }

            e.Handled = true; // 기본 Enter 동작 막기
            Send(); // 메시지 전송


        }


        private void ChatList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChatList.SelectedItem is string selected && receivedFiles.TryGetValue(selected, out string path))
            {
                if (File.Exists(path))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else
                {
                    MessageBox.Show("파일이 존재하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion






        /// <summary>
        /// 메세지 보내는 로직
        /// </summary>
        private void Send()
        {
            var text = InputBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                OnSendMessage?.Invoke(this, text);
                var display = GetMsgDisplay("", text, MessageType.Text, MessageDirection.Send);
                AddMessage(display, MessageDirection.Send);
                InputBox.Clear();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    InputBox.Focus();
                }), DispatcherPriority.Background);
            }
        }


        public void AddMessage(string display, MessageDirection type)
        {
            ChatList.Items.Add(display);

            // 스크롤 처리
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    // 실행할 UI 작업
                    ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
                }),
                DispatcherPriority.Normal
                );
        }

        public void AddReceivedMessage(MessageEntity msg)
        {
            string display = GetMsgDisplay(msg, MessageDirection.Receive);
            AddMessage(display, MessageDirection.Receive);
        }

        public void AddReceivedFile(MessageEntity msg)
        {
            string display = GetMsgDisplay(msg, MessageDirection.Receive);
            AddMessage(display, MessageDirection.Receive);
            receivedFiles[display] = MessageUtil.GetFilePath(msg.FileName);
        }

        /// <summary>
        /// 해당메서드는 Display메세지를 반환하면서 받은 메세지의 경우 알림을 설정해줍니다.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="Direction"></param>
        /// <returns></returns>
        public string GetMsgDisplay(MessageEntity msg, MessageDirection Direction)
        {
            if (msg == null) return "메세지 없음(에러)";
            if (msg.CheckMessageTypeText()) return GetMsgDisplay(msg.Sender, msg.Content, msg.Type, Direction);

            if (msg.CheckMessageTypeFile()) return GetMsgDisplay(msg.Sender, msg.FileName, msg.Type, Direction);

            return "메세지 없음(잘못된 타입)";
        }
        public string GetMsgDisplay(string Sender, string Content, MessageType messageType, MessageDirection Direction)
        {
            if (Direction == MessageDirection.Send)
            {
                if (messageType == MessageType.Text) return $"📤 나 : {Content}";

                if (messageType == MessageType.File) return $"📤 나(파일 전송) : {Content}";

            }
            if (Direction == MessageDirection.Receive)
            {
                new ToastWindow($"📨 {Sender}님이 보냄", Content).Show();

                if (messageType == MessageType.Text) return $"{Sender}: {Content}";

                if (messageType == MessageType.File) return $"📥{Sender}(파일 받음): {Content}";
            }
            return "메세지 없음(잘못된 타입)";
        }


        private string getOpenFilePath()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "보낼 파일 선택";
                dialog.Filter = "모든 파일 (*.*)|*.*";
                if (dialog.ShowDialog() == true)
                {
                    return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("파일 오픈 실패: " + ex.Message);
            }
            return "";

        }


        private async void SendFileMessageAsync()
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

                    var fileMessage = MessageEntity.OfSendFileMassage(base64, System.IO.Path.GetFileName(filePath));

                    OnSendFile?.Invoke(this, (fileMessage.FileName, base64));

                    var display = GetMsgDisplay("", fileMessage.FileName, MessageType.File, MessageDirection.Send);
                    AddMessage(display, MessageDirection.Send);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("파일 전송 실패: " + ex.Message);
                }
            }

        }






        private bool IsScrolledToBottom(ScrollViewer scroll)
        {
            // ScrollableHeight와 VerticalOffset의 차이가 작으면 맨 아래로 판단
            return scroll.VerticalOffset >= scroll.ScrollableHeight;
        }

        private ScrollViewer GetScrollViewer(DependencyObject obj)
        {
            if (obj is ScrollViewer viewer) return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }






    }

}
