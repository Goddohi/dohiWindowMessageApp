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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WalkieDohi.Core;
using WalkieDohi.Entity;
using WalkieDohi.UI;
using WalkieDohi.Util;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = System.Windows.Controls.ContextMenu;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace WalkieDohi.UC
{
    /// <summary>
    /// ChatTabControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChatTabControl : UserControl, TabBasicinterface
    {
        public string TargetIp { get; set; }
        public int TargetPort { get; set; }

        public event EventHandler<string> OnSendMessage;

        public event EventHandler<(string FileName, string Base64Content)> OnSendFile;

        private Dictionary<ChatMessage, string> receivedFiles = new Dictionary<ChatMessage, string>();

        public ChatTabControl()
        {
            InitializeComponent();
            SendButton.Click += (s, e) => Send();
        }


        #region UI 이벤트

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
            if (ChatList.SelectedItem is ChatMessage selected && receivedFiles.TryGetValue(selected, out string path))
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


        private void ChatList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ChatList.SelectedItem == null) return;

            // ContextMenu 생성
            var menu = new ContextMenu();
            var copyItem = new MenuItem { Header = "복사" };
            copyItem.Click += (s, args) =>
            {
                var selected = ChatList.SelectedItem as ChatMessage;
                if (selected != null)
                {
                    Clipboard.SetText(selected.Content);
                }
            };
            menu.Items.Add(copyItem);
            menu.IsOpen = true;
        }
        private void ChatList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var selected = ChatList.SelectedItem as ChatMessage;
                if (selected != null)
                {
                    Clipboard.SetText(selected.Content);
                    e.Handled = true;
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
                var display = ChatMessage.GetMsgDisplay("", text, MessageType.Text, MessageDirection.Send);
                AddMessage(display, MessageDirection.Send);
                InputBox.Clear();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    InputBox.Focus();
                }), DispatcherPriority.Background);
            }
        }

        public void AddMessage(ChatMessage display, MessageDirection type)
        {
            ChatList.Items.Add(display);
            //스크롤 내려주는 코드 
            Dispatcher.BeginInvoke(
               new Action(() =>
               {
                   if (ChatList.Items.Count > 0)
                   {
                       var lastItem = ChatList.Items[ChatList.Items.Count - 1];
                       ChatList.ScrollIntoView(lastItem);
                   }
               }),
               DispatcherPriority.Background // Normal보다 살짝 늦게 실행
           );
        }

        public void AddReceivedMessage(MessageEntity msg)
        {
            var display = ChatMessage.GetMsgDisplay(msg, MessageDirection.Receive);
            AddMessage(display, MessageDirection.Receive);
        }

        public void AddReceivedFile(MessageEntity msg)
        {
            var display = ChatMessage.GetMsgDisplay(msg, MessageDirection.Receive);
            AddMessage(display, MessageDirection.Receive);
            receivedFiles[display] = MessageUtil.GetFilePath(msg.FileName);
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

                    var display = ChatMessage.GetMsgDisplay("", fileMessage.FileName, MessageType.File, MessageDirection.Send);
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