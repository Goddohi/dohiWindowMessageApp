using Microsoft.Win32;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WalkieDohi.Entity;
using WalkieDohi.UI;
using WalkieDohi.Util;

namespace WalkieDohi.UC
{
    /// <summary>
    /// ChatTabControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChatTabControl : UserControl
    {
        public string TargetIp { get; set; }
        public int TargetPort { get; set; }

        public event EventHandler<string> OnSendMessage;

        public event EventHandler<(string FileName, string Base64Content)> OnSendFile;


        public ChatTabControl()
        {
            InitializeComponent();
            SendButton.Click += (s, e) => Send();
        }

        private void Send()
        {
            var text = InputBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                OnSendMessage?.Invoke(this, text);
                AddMessage("📤 나", text, messageType.Send);
                InputBox.Clear();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    InputBox.Focus();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        public void AddMessage(string sender, string message, messageType type)
        {
            ChatList.Items.Add($"{sender}: {message}");

            if (type == messageType.Receive)
            {
                new ToastWindow($"📨 {sender}님이 보냄", message).Show();
            }

            // 자동 스크롤 처리
            ScrollViewer scroll = GetScrollViewer(ChatList);
            if (scroll != null)
            {
                bool isAtBottom = scroll.VerticalOffset >= scroll.ScrollableHeight - 10;
                if (isAtBottom)
                {
                    ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
                }
            }
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            SendFileMessageAsync();
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
                    AddMessage("📤 나 → 파일 전송", fileMessage.FileName, messageType.Send);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("파일 전송 실패: " + ex.Message);
                }
            }

        }


        public void AddReceivedFile(string sender, string fileName, string fullPath)
        {
            string display = $"📥 {sender} 파일 받음: {fileName}";
            new ToastWindow($"📨 {sender}님이 보냄", fileName).Show();
            ChatList.Items.Add(display);
            receivedFiles[display] = fullPath;
        }

        private Dictionary<string, string> receivedFiles = new Dictionary<string, string>();

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
        private bool IsScrolledToBottom()
        {
            var scrollViewer = GetScrollViewer(ChatList);
            if (scrollViewer == null) return true;

            // 거의 바닥에 있는 경우만 true
            return scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 10;
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

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    return; //기본 Enter 동작 안막고 통과
                }
                else
                {
                    e.Handled = true; // 기본 Enter 동작 막기
                    Send(); // 메시지 전송
                }
            }
        }


    }

    public enum messageType
    {
        Send,
        Receive
    }
}