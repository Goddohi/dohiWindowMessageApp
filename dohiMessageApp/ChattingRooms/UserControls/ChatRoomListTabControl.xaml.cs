using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WalkieDohi.Core;
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.Util;
using WalkieDohi.Util.IO;
using System.Windows.Input;
using WalkieDohi.Util.Tcp;
using WalkieDohi.Groups.Views;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Commons.Views;

namespace WalkieDohi.ChattingRooms.UserControls
{
    public partial class ChatRoomListTabControl : UserControl
    {
        private Dictionary<string, TabBasicinterface> _chatControls = new Dictionary<string, TabBasicinterface>();
        private ICollectionView _chatRoomsView;
        private bool _isContextMenuInitialized = false;

        public ChatRoomListTabControl()
        {
            InitializeComponent();
            _chatRoomsView = CollectionViewSource.GetDefaultView(ChatListManager.GetChatList());
            _chatRoomsView.Filter = FilterChatRoom;
            ChatRoomListBox.ItemsSource = _chatRoomsView;
            MainData.FriendsChanged += MainData_FriendsChanged;
            Unloaded += ChatRoomListTabControl_Unloaded;
        }

        private void ChatRoomListTabControl_Unloaded(object sender, RoutedEventArgs e)
        {
            MainData.FriendsChanged -= MainData_FriendsChanged;
            Unloaded -= ChatRoomListTabControl_Unloaded;
        }

        private bool FilterChatRoom(object item)
        {
            if (!(item is ChatListItem chatItem))
            {
                return false;
            }

            string keyword = ChatRoomSearchBox?.Text?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(keyword))
            {
                return true;
            }

            string searchText = $"{chatItem.RoomName} {chatItem.RoomSummary} {chatItem.Ip} {chatItem.Group?.TooltipText}".ToLowerInvariant();
            return searchText.Contains(keyword);
        }

        private void RefreshChatRoomList()
        {
            _chatRoomsView?.Refresh();
        }

        private void RefreshOpenSingleChatHeaders()
        {
            foreach (var chatControl in _chatControls.Values.OfType<SingleChatTabControl>())
            {
                chatControl.RefreshHeader();
            }
        }

        private void MainData_FriendsChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(MainData_FriendsChanged));
                return;
            }

            ChatListManager.RefreshSingleChatNamesFromFriends();
            RefreshChatRoomList();
            RefreshOpenSingleChatHeaders();
        }

        private void ChatRoomSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshChatRoomList();
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

                if (chatControl is SingleChatTabControl singleChatControl)
                {
                    singleChatControl.RefreshHeader();
                }
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

                control.OnSendMessage += async (s, request) =>
                {
                    ChatListManager.UpdateChatList(item.Group);
                    var tasks = item.Group.Ips
                        .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                        .Select(ip =>
                        {
                            var msg = MessageEntity.OfGroupSendTextMassage(item.Group, request.Text, request.MessageId);
                            return new MessengerSender().SendMessageAsync(ip, msg);
                        });
                    var results = await System.Threading.Tasks.Task.WhenAll(tasks);
                    return SendResult.Aggregate(results, ResolveSendTargetName);
                };

                control.OnSendFile += async (s, request) =>
                {
                    ChatListManager.UpdateChatList(item.Group);
                    var tasks = item.Group.Ips
                        .Where(ip => ip != NetworkHelper.GetLocalIPv4())
                        .Select(ip =>
                        {
                            var msg = MessageEntity.OfGroupSendFileMassage(item.Group, request.Base64Content, request.FileName, "", request.MessageId);
                            if (MessageImageUtil.isImagecheck(msg.FileName))
                                msg.Type = MessageType.Image;

                            return new MessengerSender().SendMessageAsync(ip, msg);
                        });
                    var results = await System.Threading.Tasks.Task.WhenAll(tasks);
                    return SendResult.Aggregate(results, ResolveSendTargetName);

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

                control.OnSendMessage += async (s, request) =>
                {
                    ChatListManager.UpdateChatList(item.Name, item.Ip);
                    var msg = MessageEntity.OfSendTextMassage(request.Text, request.MessageId);
                    return await new MessengerSender().SendMessageAsync(item.Ip, msg);
                };

                control.OnSendFile += async (s, request) =>
                {
                    ChatListManager.UpdateChatList(item.Name, item.Ip);
                    var msg = MessageEntity.OfSendFileMassage(request.Base64Content, request.FileName, "", request.MessageId);
                    if (MessageImageUtil.isImagecheck(msg.FileName))
                        msg.Type = MessageType.Image;
                    return await new MessengerSender().SendMessageAsync(item.Ip, msg);
                };

                return control;
            }
        }

        private static string ResolveSendTargetName(string ip)
        {
            return MainData.GetFriendNameOrReturnOriginal("", ip);
        }

        public void HandleIncomingMessage(MessageEntity msg)
        {
            if (msg == null)
                return;

            msg.EnsureMessageId();

            string key = msg.Group?.Key ?? msg.SenderIp;
            string roomKey = msg.Group != null
                ? ChatLogStore.GetGroupRoomKey(msg.Group)
                : ChatLogStore.GetSingleRoomKey(msg.SenderIp);

            bool alreadySaved = ChatLogStore.HasMessage(roomKey, msg.MessageId);
            bool displayOnlyLocalEcho = IsLocalhostEcho(msg) && _chatControls.ContainsKey(key);
            if (alreadySaved && !displayOnlyLocalEcho)
            {
                return;
            }

            bool saveIncoming = !alreadySaved && !displayOnlyLocalEcho;

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
                chatControl.AddReceivedFile(msg, saveIncoming);
            }
            else if (msg.CheckMessageTypeImage)
            {
                MessageUtil.CheckImageDrietory();
                File.WriteAllBytes(MessageUtil.GetImagePath(msg.FileName), Convert.FromBase64String(msg.Content));
                chatControl.AddReceivedFile(msg, saveIncoming);
            }
            else
            {
                chatControl.AddReceivedMessage(msg, saveIncoming);
            }
        }

        private static bool IsLocalhostEcho(MessageEntity msg)
        {
            if (msg == null
                || string.IsNullOrWhiteSpace(msg.SenderUserUuid)
                || !string.Equals(msg.SenderUserUuid, MainData.currentUser?.UserUuid, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return IPAddress.TryParse(msg.SenderIp, out var senderIp) && IPAddress.IsLoopback(senderIp);
        }

        private MenuItem _miGroupRename;
        private void ChatRoomListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isContextMenuInitialized)
            {
                return;
            }

            _isContextMenuInitialized = true;
            var contextMenu = new ContextMenu();
            var leaveItem = new MenuItem { Header = "채팅방 나가기" };
            leaveItem.Click += LeaveChatRoom_Click;

            contextMenu.Items.Add(leaveItem);

            var logClearItem = new MenuItem { Header = "채팅내용 지우기" };
            logClearItem.Click += ClearChatRoom_Click;

            contextMenu.Items.Add(logClearItem);

            _miGroupRename = new MenuItem { Header = "그룹 이름변경" };
            _miGroupRename.Click += GroupNameChange_Click;
            contextMenu.Items.Add(_miGroupRename); // 항상 추가해두고 가시성만 토글

            // 우클릭 시 선택 보정(권장)
            ChatRoomListBox.PreviewMouseRightButtonDown += ChatRoomListBox_PreviewMouseRightButtonDown;
            // 메뉴 뜨기 직전에 가시성 토글
            ChatRoomListBox.ContextMenuOpening += ChatRoomListBox_ContextMenuOpening;
            ChatRoomListBox.ContextMenu = contextMenu;
        }

        private void GroupNameChange_Click(object sender, RoutedEventArgs e)
        {
            if (ChatRoomListBox.SelectedItem is ChatListItem item)
            {
                string key = item.UniqueKey;

                var title = "그룹채팅방 이름 변경";
                var ask = $"어떤 이름으로 변경하시겠습니까?";
                var popup = new InputDialog(title, item.Group.GroupName, ask);

                popup.ShowDialog();
                if (string.IsNullOrWhiteSpace(popup.ResponseText)==false)
                {

                    // 채팅 컨트롤 제거 전 리소스 정리
                    if (_chatControls.TryGetValue(key, out var control))
                    {

                        (control as IDisposable)?.Dispose();

                        if (ChatContentArea.Content == control)
                            ChatContentArea.Content = null;

                        _chatControls.Remove(key);
                    }

                    ChatListManager.ChangeNameChatListItem(key, popup.ResponseText);
                    // UI 갱신
                    RefreshChatRoomList();
                }
            }
        }

        private void LeaveChatRoom_Click(object sender, RoutedEventArgs e)
        {
            if (ChatRoomListBox.SelectedItem is ChatListItem item)
            {
                var title = "채팅방 나가기";
                var ask = $"이 {item.DisplayName} 채팅방에서 나가시겠습니까?";
                    

                var confirm = MessageBox.Show(ask, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

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

                string chatpathKey = string.Copy(key);
                if (item.Group != null)
                {
                    chatpathKey = $"group_{DirectoryManager.MakeSafeFileName(chatpathKey)}"; //해당 문제점으로 이름 같으면 다뜸 key가 필요할듯
                }
                //채팅로그 삭제
                ChatListManager.DeleteChatLog(chatpathKey);

                // UI 갱신
                RefreshChatRoomList();

            }
        }


        private void ClearChatRoom_Click(object sender, RoutedEventArgs e)
        {
            if (ChatRoomListBox.SelectedItem is ChatListItem item)
            {
                var title = "채팅내용 지우기 ";
                var ask = $"이 {item.DisplayName} 채팅방의 채팅내용을 지우시겠습니까?";


                var confirm = MessageBox.Show(ask, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                string key = item.UniqueKey;

                // 채팅 컨트롤 제거 전 리소스 정리
                if (_chatControls.TryGetValue(key, out var control))
                {

                    (control as IDisposable)?.Dispose();

                    if (ChatContentArea.Content == control)
                        ChatContentArea.Content = null;

                    _chatControls.Remove(key);
                }
                string chatpathKey = string.Copy(key);
                if (item.Group != null)
                {
                    chatpathKey = $"group_{DirectoryManager.MakeSafeFileName(chatpathKey)}"; 
                }
                //채팅로그 삭제
                ChatListManager.DeleteChatLog(chatpathKey);

                // UI 갱신
                RefreshChatRoomList();

            }
        }



        private void ChatRoomListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            var lbi = FindParent<ListBoxItem>(dep);
            if (lbi != null) lbi.IsSelected = true;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T t) return t;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
        private void ChatRoomListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var item = ChatRoomListBox.SelectedItem as ChatListItem;
            if (item == null)
            {
                e.Handled = true; // 선택된 게 없으면 메뉴 띄우지 않음
                return;
            }

            // 그룹 채팅이면 보이기, 1:1이면 숨기기
            _miGroupRename.Visibility = (item.Group != null)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }


        public void SelectChatByKey(string key)
        {
            ChatRoomSearchBox.Clear();
            RefreshChatRoomList();

            var list = ChatListManager.GetChatList();
            var item = list.FirstOrDefault(c => c.UniqueKey == key);
            if (item != null)
            {
                ChatRoomListBox.SelectedItem = item;
            }
        }




        private void ManageGroups_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new GroupCreateDialog()
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() == true && dlg.ResultGroup != null)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.ShowChatRoomFromStart(dlg.ResultGroup);
                    e.Handled = true; //버블링 현상 제거
                }


            }
        }

    }
}
