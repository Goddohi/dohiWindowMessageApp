using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Friends.Entity;
using WalkieDohi.Friends.Views;
using WalkieDohi.Groups.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.IO;

namespace WalkieDohi.Friends.UserControls
{
    public partial class FriendMainListView : UserControl
    {
        public event Action<Friend> OnStartChat;
        public event Action<GroupEntity> OnStartGroupChat;

        private ObservableCollection<Friend> _allFriends = new ObservableCollection<Friend>();
        private ObservableCollection<GroupEntity> _allGroups = new ObservableCollection<GroupEntity>();

        public FriendMainListView()
        {
            InitializeComponent();

            FriendSearchBox.TextChanged += FriendSearchBox_TextChanged;

            FriendListBox.MouseDoubleClick += FriendListBox_MouseDoubleClick;
            FriendListBox.PreviewMouseRightButtonDown += FriendListBox_PreviewMouseRightButtonDown;
        }

        private void ManageFriends_Click(object sender, RoutedEventArgs e)
        {
            var popup = new FriendManagerWindow
            {
                Owner = Window.GetWindow(this)
            };
            popup.ShowDialog();

            SetFriends(MainData.GetsortedFriends());
        }

        public void SetFriends(ObservableCollection<Friend> friends)
        {
            _allFriends = friends;
            ApplyFriendFilter();
        }

        private void FriendSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFriendFilter();
        }

        private void ApplyFriendFilter()
        {
            string keyword = FriendSearchBox.Text.Trim().ToLower();

            var filtered = _allFriends.Where(f =>
                string.IsNullOrEmpty(keyword) ||
                (!string.IsNullOrEmpty(f.Name) && f.Name.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(f.Ip) && f.Ip.ToLower().Contains(keyword))
            );

            FriendListBox.ItemsSource = new ObservableCollection<Friend>(filtered);
        }



        private void FriendListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            var item = listBox?.SelectedItem as Friend;
            if (item != null)
            {
                OpenChat();
            }
        }

        private void OpenChat()
        {
            if (FriendListBox.SelectedItem is Friend selected)
            {
                if(selected == null) return;
               

                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.ShowChatRoomFromStart(selected.Name, selected.Ip);
                }
            }
        }

        private void OpenFriendEditor(Friend friend)
        {
            if (friend == null)
            {
                return;
            }

            var popup = new FriendManagerWindow(friend)
            {
                Owner = Window.GetWindow(this)
            };
            popup.ShowDialog();

            SetFriends(MainData.GetsortedFriends());
        }

        private void FriendListBox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            var item = listBox?.SelectedItem as Friend;

            if (item != null)
            {
                // 예시: 우클릭 시 메뉴 표시
                ContextMenu menu = new ContextMenu();


                var openItem = new MenuItem { Header = "채팅 열기" };
                openItem.Click += (s, ev) => OpenChat();

                var editItem = new MenuItem { Header = "친구 수정" };
                editItem.Click += (s, ev) => OpenFriendEditor(item);

                var removeItem = new MenuItem { Header = "친구 삭제" };
                removeItem.Click += (s, ev) => RemoveFriend(item);

                menu.Items.Add(openItem);
                menu.Items.Add(editItem);
                menu.Items.Add(new Separator());
                menu.Items.Add(removeItem);

                menu.IsOpen = true;
            }
        }

        private void RemoveFriend(Friend friend)
        {
            if (friend == null)
            {
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(friend.Name)
                ? friend.Ip
                : friend.Name;

            var confirm = MessageBox.Show(
                $"{displayName} 친구를 삭제할까요?",
                "친구 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var friends = new ObservableCollection<Friend>(MainData.Friends);
            Friend target = FindMatchingFriend(friends, friend);
            if (target == null)
            {
                return;
            }

            friends.Remove(target);
            new FriendJsonFileHandler().SaveFriends(friends);
            ChatListManager.RefreshSingleChatNamesFromFriends();
            SetFriends(MainData.GetsortedFriends());
        }

        private static Friend FindMatchingFriend(IEnumerable<Friend> friends, Friend target)
        {
            return friends.FirstOrDefault(friend =>
                ReferenceEquals(friend, target) ||
                (!string.IsNullOrWhiteSpace(target.UserUuid)
                    && string.Equals(friend.UserUuid, target.UserUuid, StringComparison.Ordinal)) ||
                NetworkHelper.AreSameIPv4(friend.Ip, target.Ip));
        }

        private void FriendListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (item != null)
            {
                item.IsSelected = true;
            }
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T target)
                {
                    return target;
                }

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }
    }
}
