using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Friends.Entity;
using WalkieDohi.Friends.Views;
using WalkieDohi.Groups.Entity;

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
        }

        private void ManageFriends_Click(object sender, RoutedEventArgs e)
        {
            var popup = new FriendManagerWindow();
            popup.ShowDialog();

            SetFriends(MainData.GetsortedFriends());
        }

        public void SetFriends(ObservableCollection<Friend> friends)
        {
            _allFriends = friends;
            FriendListBox.ItemsSource = new ObservableCollection<Friend>(_allFriends);
        }

        private void FriendSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = FriendSearchBox.Text.Trim().ToLower();

            var filtered = _allFriends.Where(f =>
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


                //var removeItem = new MenuItem { Header = "친구 삭제" };
               // removeItem.Click += (s, ev) => RemoveFriend(item);

                menu.Items.Add(openItem);
                //menu.Items.Add(removeItem);

                menu.IsOpen = true;
            }
        }
    }
}
