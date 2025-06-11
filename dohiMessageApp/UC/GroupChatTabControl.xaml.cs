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
using Path = System.IO.Path;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using WalkieDohi.UC.ViewModel;
using System.Collections.ObjectModel;

namespace WalkieDohi.UC
{
    /// <summary>
    /// GroupChatTabControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GroupChatTabControl : UserControl, TabBasicinterface
    {

        public GroupEntity TargetGroup { get; set; }
        public int TargetPort { get; set; }

        public event EventHandler<string> OnSendMessage;

        public event EventHandler<(string FileName, string Base64Content)> OnSendFile;

        private Dictionary<ChatMessage, string> ChatFilePaths = new Dictionary<ChatMessage, string>();
        private ChatViewModel viewModel;

        #region 초기화
        public GroupChatTabControl()
        {
            InitializeComponent();
            viewModel = new ChatViewModel();
            this.DataContext = viewModel;
            SendButton.Click += (s, e) => Send();
        }

        public void SetGroupMembers(ObservableCollection<Friend> allFriends)
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
        #endregion

        #region UI 이벤트



        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            SendFileMessageAsync();
        }

        private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                PasteImageIfExists();
            }

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



        /// <summary>
        /// 파일,이미지 채팅 더블클릭시 경로 및 미리보기 띄워주는 메소드
        /// </summary>
        private void ChatList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChatList.SelectedItem is ChatMessage selected)
            {

                if (ChatFilePaths.TryGetValue(selected, out string path))
                {
                    if (ExtendFile.UnExists(path))
                    {
                        MessageBox.Show("파일이 존재하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (selected is ImageMessage)
                    {
                        var preview = new ImagePreviewWindow(path);
                        preview.ShowDialog();
                        return;
                    }
                    if (selected is FileMessage)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                        return;
                    }
                }
                 //클립보드이미지는 경로가 없다.
                if (selected is ImageMessage imageMsg)
                {
                    if (imageMsg.Image != null)
                    {
                        var preview = new ImagePreviewWindow(imageMsg.Image);
                        preview.ShowDialog();
                    }
                    return;
                }
                return;
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
                    Clipboard.SetText(selected.DisplayContent);
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
                    Clipboard.SetText(selected.DisplayContent);
                    e.Handled = true;
                }
            }
        }

        private void ChatList_ScrollChanged(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = GetScrollViewer(ChatList);
            if (scrollViewer == null) return;

            bool isAtBottom = Math.Abs(scrollViewer.VerticalOffset - scrollViewer.ScrollableHeight) < 1.0;
            ScrollToBottomButton.Visibility = isAtBottom ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.ChatMessages.Count > 0)
            {
                var lastItem = viewModel.ChatMessages[viewModel.ChatMessages.Count - 1];
                ChatList.ScrollIntoView(lastItem);
            }
            ScrollToBottomButton.Visibility = Visibility.Collapsed;
        }

        private void ChatList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private async void ChatList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var filePath in droppedFiles)
                {
                    HandleDroppedFile(filePath);
                }
            }
        }

        #endregion





        #region 메세지 로직 
        /// <summary>
        /// 메세지 보내는 로직
        /// </summary>
        private void Send()
        {
            var text = InputBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                OnSendMessage?.Invoke(this, text);
                var display = ChatMessage.CreateSendMessage( text,"", MessageType.Text);
                AddMessage(display, MessageDirection.Send);
                InputBox.Clear();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    InputBox.Focus();
                }), DispatcherPriority.Background);
            }
        }

        private async void SendFileMessageAsync()
        {
            string filePath = GetOpenFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                HandleDroppedFile(filePath);
            }
        }
        private string GetOpenFilePath()
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

        /// <summary>
        /// 파일과 사진을 전송하는 로직 ( 클립보드로 인한 사진은 전송을 담당하지 않는다)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private void HandleDroppedFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            FileInfo fileInfo = new FileInfo(filePath);
            const long MaxFileSize = 20 * 1024 * 1024;


            if (fileInfo.Length > MaxFileSize)
            {
                MessageBox.Show("❗ 20MB를 초과하는 파일은 전송할 수 없습니다.", "파일 용량 초과", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var messageType = MessageType.File;
                byte[] fileData = File.ReadAllBytes(filePath);
                string base64 = Convert.ToBase64String(fileData);

                var fileMessage = MessageEntity.OfSendFileMassage(base64, Path.GetFileName(filePath));

                OnSendFile?.Invoke(this, (fileMessage.FileName, base64));
                if (MessageImageUtil.isImagecheck(fileMessage.FileName))

                {
                    messageType = MessageType.Image;
                }
                var display = ChatMessage.CreateSendMessage(fileMessage.FileName, fileMessage.Content, messageType);
                AddMessage(display, MessageDirection.Send);
                ChatFilePaths[display] = filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 전송 실패: " + ex.Message);
            }
        }

        /// <summary>
        /// 클립보드에 있는 사진 전송로직 (경로보기불가)
        /// </summary>
        /// <param name="base64"></param>
        private void SendClipboardImageMessage(string base64)
        {
            string randomName = ClipboadPasteUtil.GetRandomClipboadImgName();
            var fileMessage = MessageEntity.OfSendFileMassage(base64, randomName);

            OnSendFile?.Invoke(this, (fileMessage.FileName, base64));
            var display = ChatMessage.CreateSendMessage(fileMessage.FileName, fileMessage.Content, MessageType.Image);

            AddMessage(display, MessageDirection.Send);
        }
        /// <summary>
        /// 복사붙여넣기를 했는데 이미지일경우 이미지로 전송로직호출
        /// </summary>
        private void PasteImageIfExists()
        {
            if (Clipboard.ContainsImage())
            {
                BitmapSource image = Clipboard.GetImage();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    byte[] imageBytes = stream.ToArray();
                    string base64 = Convert.ToBase64String(imageBytes);

                    SendClipboardImageMessage(base64);
                }
            }
        }

        //추후 설정으로 추가
        private const int MAX_MESSAGE_COUNT = 200;
        private const int REMOVE_MESSAGE_COUNT = 50;
        /// <summary>
        /// 메세지리스트에 추가를 한다 
        /// </summary>
        /// <param name="display"></param>
        /// <param name="type"></param>
        public void AddMessage(ChatMessage display, MessageDirection type)
        {
            viewModel.ChatMessages.Add(display);

            if (viewModel.ChatMessages.Count > MAX_MESSAGE_COUNT)
            {
                int removeCount = viewModel.ChatMessages.Count - MAX_MESSAGE_COUNT + REMOVE_MESSAGE_COUNT;
                for (int i = 0; i < removeCount; i++)
                {
                    viewModel.ChatMessages.RemoveAt(0);
                }

            }


            //스크롤 내려주는 코드 (스크롤튀는 현상 방지를 위해 느리게 실행)
            Dispatcher.BeginInvoke(
               new Action(() =>
               {
                   if (viewModel.ChatMessages.Count > 0)
                   {
                       var lastItem = viewModel.ChatMessages[viewModel.ChatMessages.Count - 1];
                       ChatList.ScrollIntoView(lastItem);
                   }
               }),
               DispatcherPriority.Background // Normal보다 살짝 늦게 실행
               );
        }

        public void AddReceivedMessage(MessageEntity msg)
        {
            ChatMessage display = ChatMessage.CreateFromEntity(msg);
            AddMessage(display, MessageDirection.Receive);
        }

        
        public void AddReceivedFile(MessageEntity msg)
        {
            string extension = Path.GetExtension(msg.FileName).ToLower();
            if (MessageImageUtil.isImagecheck(msg.FileName))
            {
                msg.Type = MessageType.Image;
            }
            
            ChatMessage display = ChatMessage.CreateFromEntity(msg);
            AddMessage(display, MessageDirection.Receive);
            if (msg.CheckMessageTypeImage())
            {
                ChatFilePaths[display] = MessageUtil.GetImagePath(msg.FileName);
            }
            else
            {
                ChatFilePaths[display] = MessageUtil.GetFilePath(msg.FileName);
            }
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

        #endregion

        #region    내부사용로직

        #endregion

        #region   외부 사용로직 

        public void Cleanup()
        {
            foreach (var msg in viewModel.ChatMessages.OfType<ImageMessage>())
            {
                if (msg.Image is BitmapImage bmp && bmp.StreamSource != null)
                {
                    bmp.StreamSource.Dispose();
                }
            }
            // ViewModel 데이터 정리
            viewModel?.ChatMessages?.Clear();

            // 파일 경로 딕셔너리 정리
            ChatFilePaths?.Clear();

            // 이벤트 핸들러 해제
            OnSendMessage = null;
            OnSendFile = null;

            // 바인딩 해제
            ChatList.ItemsSource = null;
            this.DataContext = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        #endregion

        private const string ChatLogDir = "ChatLogs";

        public void SaveMessages()
        {
            try
            {
                if (TargetGroup == null || string.IsNullOrWhiteSpace(TargetGroup.GroupName)) return;

                var safeName = MakeSafeFileName(TargetGroup.GroupName);
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ChatLogDir, $"group_{safeName}");
                Directory.CreateDirectory(dir);

                var filePath = Path.Combine(dir, "chat_latest.json");
                var entities = viewModel.ChatMessages.Select(m => m.ToEntity()).ToList();
                var json = JsonUtil.Serialize(entities, indented: true);

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("그룹 채팅 저장 실패: " + ex.Message);
            }
        }

        private string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }



    }

}
