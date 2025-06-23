using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WalkieDohi.Core;
using WalkieDohi.Data;
using WalkieDohi.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.Tcp;

namespace WalkieDohi.UC
{
    public partial class ChatRoomListTabControl : UserControl
    {
        private Dictionary<string, TabBasicinterface> _chatControls = new Dictionary<string, TabBasicinterface>();

        public ChatRoomListTabControl()
        {
            InitializeComponent();
            ChatRoomListBox.ItemsSource = ChatListManager.GetChatList();
        }

        private void ChatRoomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatRoomListBox.SelectedItem is ChatListItem item)
            {
                string key = item.UniqueKey;
                if (!_chatControls.TryGetValue(key, out var chatControl))
                {
                    chatControl = CreateChatControl(item);
                    _chatControls[key] = chatControl;
                }
                ChatContentArea.Content = chatControl;
            }
        }

        private TabBasicinterface CreateChatControl(ChatListItem item)
        {
            if (item.Group != null)
            {
                var control = new GroupChatTabControl
                {
                    TargetGroup = item.Group
                };
                control.LoadLatestMessages();
                control.SetGroupMembers(MainData.Friends);

                control.OnSendMessage += async (s, text) =>
                {
                    var msg = MessageEntity.OfGroupSendTextMassage(item.Group, text);
                    var tasks = item.Group.Ips
                        .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                        .Select(ip => new MessengerSender().SendMessageAsync(ip, msg));
                    await System.Threading.Tasks.Task.WhenAll(tasks);
                };

                control.OnSendFile += async (s, fileInfo) =>
                {
                    var msg = MessageEntity.OfGroupSendFileMassage(item.Group, fileInfo.Base64Content, fileInfo.FileName);
                    if (MessageImageUtil.isImagecheck(msg.FileName))
                        msg.Type = MessageType.Image;

                    var tasks = item.Group.Ips
                        .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                        .Select(ip => new MessengerSender().SendMessageAsync(ip, msg));
                    await System.Threading.Tasks.Task.WhenAll(tasks);
                };

                return control;
            }
            else
            {
                var control = new SingleChatTabControl
                {
                    TargetIp = item.Ip
                };
                control.LoadLatestMessages();

                control.OnSendMessage += async (s, text) =>
                {
                    var msg = MessageEntity.OfSendTextMassage(text);
                    await new MessengerSender().SendMessageAsync(item.Ip, msg);
                };

                control.OnSendFile += async (s, fileInfo) =>
                {
                    var msg = MessageEntity.OfSendFileMassage(fileInfo.Base64Content, fileInfo.FileName);
                    if (MessageImageUtil.isImagecheck(msg.FileName))
                        msg.Type = MessageType.Image;
                    await new MessengerSender().SendMessageAsync(item.Ip, msg);
                };

                return control;
            }
        }

        public void HandleIncomingMessage(MessageEntity msg)
        {
            string key = msg.Group?.GroupName ?? msg.SenderIp;

            if (!_chatControls.TryGetValue(key, out var chatControl))
            {
                ChatListManager.UpdateChatList(msg);
                
                var list = ChatListManager.GetChatList();
                var item = list.FirstOrDefault(c => c.UniqueKey == key);
                if (item == null) return;

                chatControl = CreateChatControl(item);
                _chatControls[key] = chatControl;
            }

            if (msg.CheckMessageTypeFile)
            {
                MessageUtil.CheckFileDrietory();
                File.WriteAllBytes(MessageUtil.GetFilePath(msg.FileName), Convert.FromBase64String(msg.Content));
                chatControl.AddReceivedFile(msg);
            }
            else if (msg.CheckMessageTypeImage)
            {
                MessageUtil.CheckImageDrietory();
                File.WriteAllBytes(MessageUtil.GetImagePath(msg.FileName), Convert.FromBase64String(msg.Content));
                chatControl.AddReceivedFile(msg);
            }
            else
            {
                chatControl.AddReceivedMessage(msg);
            }
        }

        private void ChatRoomListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var style = new Style(typeof(ListBoxItem));
            var contextMenu = new ContextMenu();
            var leaveItem = new MenuItem { Header = "채팅방 나가기" };
            leaveItem.Click += LeaveChatRoom_Click;
            contextMenu.Items.Add(leaveItem);

            style.Setters.Add(new Setter(ListBoxItem.ContextMenuProperty, contextMenu));
            ChatRoomListBox.ItemContainerStyle = style;
        }


        private void LeaveChatRoom_Click(object sender, RoutedEventArgs e)
        {
            if (ChatRoomListBox.SelectedItem is ChatListItem item)
            {
                string key = item.UniqueKey;

                // 채팅 컨트롤 제거 전 리소스 정리
                if (_chatControls.TryGetValue(key, out var control))
                {

                    (control as IDisposable)?.Dispose();

                    if (ChatContentArea.Content == control)
                        ChatContentArea.Content = null;

                    _chatControls.Remove(key);
                }

                // 채팅 리스트에서 제거
                ChatListManager.RemoveChatListItem(key);

                // UI 갱신
                ChatRoomListBox.ItemsSource = null;
                ChatRoomListBox.ItemsSource = ChatListManager.GetChatList();
                /*
                // 저장된 채팅 파일 삭제
                try
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatLogs", key);
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true); // 하위 파일 포함 삭제
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("채팅 로그 폴더 삭제 실패: " + ex.Message);
                }
                */
            }
        }



        public void SelectChatByKey(string key)
        {
            var list = ChatListManager.GetChatList();
            var item = list.FirstOrDefault(c => c.UniqueKey == key);
            if (item != null)
            {
                ChatRoomListBox.SelectedItem = item;
            }
        }


    }
}