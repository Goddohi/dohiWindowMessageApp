using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Friends.Entity;
using WalkieDohi.Groups.Entity;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.IO;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Path = System.IO.Path;
using WalkieDohi.ChattingRooms.ViewModels;
using WalkieDohi.ChattingRooms.Views;
using System.Text.RegularExpressions;
using WalkieDohi.Util.Tcp;

namespace WalkieDohi.ChattingRooms.UserControls
{
    /// <summary>
    /// GroupChatTabControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GroupChatTabControl : UserControl, TabBasicinterface
    {

        public GroupEntity TargetGroup { get; set; }
        public int TargetPort { get; set; }

        public event SendMessageRequestedEventHandler OnSendMessage;

        public event SendFileRequestedEventHandler OnSendFile;

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
            if (GroupTitleText != null)
            {
                GroupTitleText.Text = TargetGroup.GroupName;
            }

            if (GroupMemberSummaryText != null)
            {
                GroupMemberSummaryText.Text = $"{members.Count}명 참여";
            }
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
                // 텍스트가 있을경우 경우 리턴
                // 해당 사유 : 엑셀 복사시 사진도 복사붙여넣기 되는 현상 제거
                /* 엑셀 복사 window 클립보드 데이터 우선순위
                 * 1. Bitmap
                 * 2. HTML Format
                 * 3. UncodeText
                 * 4. CSV
                 * 5. Text
                 */
                if (Clipboard.ContainsText())
                {
                    return;
                }
                try
                {
                    var base64 = MessageImageUtil.ClipboardPasteImageIfExistsReturnBase64String();
                    SendClipboardImageMessage(base64);
                }
                catch (InvalidOperationException)
                {
                    return; // 클립보드에 이미지 없으면 종료
                }
                
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
                if (string.IsNullOrWhiteSpace(selected.ContentPath))
                {
                    //클립보드이미지는 경로가 없다.
                    if (selected is ImageMessage imageMsg)
                    {
                        if (imageMsg.Image != null)
                        {
                            var preview = new ImagePreviewWindow(imageMsg.Image);
                            //preview.ShowDialog(); //ShowDialog는 이전 UI를 사용할 수 없도록 제한을 합니다.
                            preview.Show();
                        }
                        return;
                    }
                }
                else 
                {
                    if (ExtendFile.UnExists(selected.ContentPath))
                    {
                        MessageBox.Show("파일이 존재하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (selected is ImageMessage)
                    {
                        var preview = new ImagePreviewWindow(selected.ContentPath);
                        // preview.Topmost = true; // 이건 그냥 필기용으로 주석   항상위로 띄워주는데 외부앱도 최상단으로.
                        //preview.ShowDialog(); //ShowDialog는 이전 UI를 사용할 수 없도록 제한을 합니다.
                        preview.Show();
                        return;
                    }
                    if (selected is FileMessage)
                    {
                        string ext = Path.GetExtension(selected.ContentPath).ToLower();

                        if (ext == ".pdf")
                        {
                            PDFPreviewWindow preview = null;
                            try
                             {
                                preview = new PDFPreviewWindow(selected.ContentPath);  // 초기화 하기전에 close를 
                                //preview.ShowDialog(); //모달 창 -> 부모를 블락 닫을땐 DialogResult를 사용
                                preview.Show(); //모델리스 창  -> 부모 사용가능 닫을때  this.Close(); 해도됌
                                return;
                            }
                            catch (Exception)
                            {
                                preview?.ForceCleanup();   // 직접 구현한 외부 DLL close 메모리 관리
                                preview = null;
                                return;
                            }
                        }
                        else
                        {

                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{selected.ContentPath}\"");
                            return;
                        }
                    }
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
                    ClipboardExtension.ChatCopy(selected);
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
                    ClipboardExtension.ChatCopy(selected);
                    e.Handled = true;
                }
            }
        }

        private void ChatList_ScrollChanged(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = GetScrollViewer(ChatList);
            if (scrollViewer == null) return;

            if (scrollViewer.VerticalOffset <= 0)
            {
                LoadNextOldMessageFile();
            }

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
                    await HandleDroppedFileAsync(filePath);
                }
            }
        }

        #endregion





        #region 메세지 로직 
        /// <summary>
        /// 메세지 보내는 로직
        /// </summary>
        private async void Send()
        {
            var text = InputBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                string messageId = MessageEntity.CreateMessageId();
                var display = ChatMessage.CreateSendMessage( text,"","", MessageType.Text, false, messageId);
                display.IsSending = true;
                InputBox.Clear();

                AddMessage(display, MessageDirection.Send, saveImmediately: false);

                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    InputBox.Focus();
                }), DispatcherPriority.Background);

                var result = await RequestSendMessageAsync(messageId, text);
                display.ApplySendResult(result);
                SaveOldMessages();
            }
        }

        private async Task<SendResult> RequestSendMessageAsync(string messageId, string text)
        {
            var handler = OnSendMessage;
            if (handler == null)
            {
                return SendResult.Fail("", "메시지 전송 처리기가 연결되지 않았습니다.");
            }

            try
            {
                return await handler(this, new MessageSendRequest(messageId, text));
            }
            catch (Exception ex)
            {
                return SendResult.Fail("", ex.Message);
            }
        }

        private async Task<SendResult> RequestSendFileAsync(string messageId, string fileName, string base64Content)
        {
            var handler = OnSendFile;
            if (handler == null)
            {
                return SendResult.Fail("", "파일 전송 처리기가 연결되지 않았습니다.");
            }

            try
            {
                return await handler(this, new FileSendRequest(messageId, fileName, base64Content));
            }
            catch (Exception ex)
            {
                return SendResult.Fail("", ex.Message);
            }
        }

        private async void SendFileMessageAsync()
        {
            string filePath = GetOpenFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                await HandleDroppedFileAsync(filePath);
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
        private async Task HandleDroppedFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return;

            FileInfo fileInfo = new FileInfo(filePath);
            const long MaxFileSize = 200 * 1024 * 1024;


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
                string messageId = MessageEntity.CreateMessageId();

                var fileMessage = MessageEntity.OfSendFileMassage(base64, Path.GetFileName(filePath), "", messageId);

                if (MessageImageUtil.isImagecheck(fileMessage.FileName))
                {
                    messageType = MessageType.Image;
                }
                var display = ChatMessage.CreateSendMessage(fileMessage.FileName, fileMessage.Content, filePath, messageType, false, messageId);
                display.IsSending = true;
                AddMessage(display, MessageDirection.Send, saveImmediately: false);

                var result = await RequestSendFileAsync(messageId, fileMessage.FileName, base64);
                display.ApplySendResult(result);
                SaveOldMessages();
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
        private async void SendClipboardImageMessage(string base64)
        {
            string randomName = MessageImageUtil.GetRandomClipboadImgName();
            string messageId = MessageEntity.CreateMessageId();
            var fileMessage = MessageEntity.OfSendFileMassage(base64, randomName, "", messageId);
            string filePath = "";
            var display = ChatMessage.CreateSendMessage(fileMessage.FileName, fileMessage.Content, filePath, MessageType.Image, false, messageId);
            display.IsSending = true;
            AddMessage(display, MessageDirection.Send, saveImmediately: false);

            var result = await RequestSendFileAsync(messageId, fileMessage.FileName, base64);
            display.ApplySendResult(result);
            SaveOldMessages();
        }


        /// <summary>
        /// 메세지리스트에 추가를 한다 
        /// </summary>
        /// <param name="display"></param>
        /// <param name="type"></param>
        public void AddMessage(ChatMessage display, MessageDirection type, bool saveImmediately = true)
        {
            viewModel.ChatMessages.Add(display);

            if (saveImmediately)
            {
                SaveOldMessages();
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
            string path = "";
            ChatMessage display = ChatMessage.CreateFromEntity(msg, path);
            AddMessage(display, MessageDirection.Receive);
        }

        
        public void AddReceivedFile(MessageEntity msg)
        {
            string extension = Path.GetExtension(msg.FileName).ToLower();
            if (MessageImageUtil.isImagecheck(msg.FileName))
            {
                msg.Type = MessageType.Image;
            }
            string filePath = "";

            if (msg.CheckMessageTypeImage)
            {
                filePath = MessageUtil.GetImagePath(msg.FileName);
            }
            else
            {
                filePath = MessageUtil.GetFilePath(msg.FileName);
            }

            ChatMessage display = ChatMessage.CreateFromEntity(msg, filePath);

            AddMessage(display, MessageDirection.Receive);

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

        private string GetRoomKey()
        {
            if (TargetGroup != null)
            {
                return ChatLogStore.GetGroupRoomKey(TargetGroup);
            }
           
            throw new InvalidOperationException("대상이 없습니다.");
        }


        private void SaveOldMessages()
        {
            try
            {
                // 새 메시지 추출
                var newMessages = viewModel.ChatMessages
                    .Where(m => !m.IsReload && !m.IsSending && !m.IsSaved)
                    .OrderBy(m => m.Timestamp)
                    .ToList();

                if (!newMessages.Any()) return;

                ChatLogStore.SaveMessages(GetRoomKey(), newMessages);

                foreach (var message in newMessages)
                {
                    message.IsSaved = true;
                }

                if (_messageStoreInitialized)
                {
                    _loadedMessageCount += newMessages.Count;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("메시지 저장 실패: " + ex.Message);
            }
        }



        private int _loadedMessageCount;
        private bool _hasMoreMessages;
        private bool _messageStoreInitialized;

        private void InitializeMessageFiles()
        {
            _loadedMessageCount = 0;
            _hasMoreMessages = true;
            _messageStoreInitialized = true;
        }

        public void LoadLatestMessages()
        {
            InitializeMessageFiles(); // 저장소 페이지 상태 초기화

            LoadNextOldMessageFile(); // 최신 1개 로드
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (viewModel.ChatMessages.Count > 0)
                {
                    var last = viewModel.ChatMessages.Last();
                    ChatList.ScrollIntoView(last);
                }
            }), DispatcherPriority.Background);
        }

        public void LoadNextOldMessageFile()
        {
            if (!_hasMoreMessages)
                return;

            try
            {
                var messages = ChatLogStore.LoadMessages(GetRoomKey(), _loadedMessageCount, ChatLogStore.DefaultPageSize);
                if (messages.Count == 0)
                {
                    _hasMoreMessages = false;
                    return;
                }

                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    viewModel.ChatMessages.Insert(0, messages[i]);
                }

                _loadedMessageCount += messages.Count;
                _hasMoreMessages = messages.Count >= ChatLogStore.DefaultPageSize;

                var loadLastItem = viewModel.ChatMessages[messages.Count - 1];
                ChatList.ScrollIntoView(loadLastItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show("이전 메시지 로딩 실패: " + ex.Message);
            }
        }

        private void BtnDoodle_Click(object sender, RoutedEventArgs e)
        {
            // 낙서 버튼 누르면 아래에서 슥 올라오게
            DoodlePad.Show();
        }

        private void DoodlePad_DoodleCompleted(object sender, string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return;

            SendClipboardImageMessage(base64);   // 그대로 쏘면 끝
        }

    }

}
