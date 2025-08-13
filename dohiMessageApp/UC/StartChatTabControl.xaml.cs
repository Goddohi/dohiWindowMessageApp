using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WalkieDohi.Entity;
using WalkieDohi.UI;

namespace WalkieDohi.UC
{
    public partial class StartChatTabControl : UserControl
    {
        public event Action<Friend> OnStartChat;
        public event Action<GroupEntity> OnStartGroupChat;

        private ObservableCollection<Friend> _allFriends = new ObservableCollection<Friend>();
        private ObservableCollection<GroupEntity> _allGroups = new ObservableCollection<GroupEntity>();

        public StartChatTabControl()
        {
            InitializeComponent();

            FriendSearchBox.TextChanged += FriendSearchBox_TextChanged;
            GroupSearchBox.TextChanged += GroupSearchBox_TextChanged;

            FriendListBox.MouseDoubleClick += FriendListBox_MouseDoubleClick;
            GroupListBox.MouseDoubleClick += GroupListBox_MouseDoubleClick;
        }

        private void ManageFriends_Click(object sender, RoutedEventArgs e)
        {
            var popup = new FriendManagerWindow();
            popup.ShowDialog();

            SetFriends(MainData.GetsortedFriends());
        }
        private void ManageGroups_Click(object sender, RoutedEventArgs e)
        {
            var popup = new GroupManagerWindow();
            popup.ShowDialog();

            SetGroups(MainData.Groups); // 최신 그룹 다시 로드
        }

        public void SetFriends(ObservableCollection<Friend> friends)
        {
            _allFriends = friends;
            FriendListBox.ItemsSource = new ObservableCollection<Friend>(_allFriends);
        }

        public void SetGroups(ObservableCollection<GroupEntity> groups)
        {
            _allGroups = groups;
            GroupListBox.ItemsSource = new ObservableCollection<GroupEntity>(_allGroups);
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

        private void GroupSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = GroupSearchBox.Text.Trim().ToLower();

            var filtered = _allGroups.Where(g =>
                (!string.IsNullOrEmpty(g.GroupName) && g.GroupName.ToLower().Contains(keyword)) ||
                g.Ips.Any(ip => ip.ToLower().Contains(keyword))
            );

            GroupListBox.ItemsSource = new ObservableCollection<GroupEntity>(filtered);
        }

        private void FriendListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FriendListBox.SelectedItem is Friend selected)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.ShowChatRoomFromStart(selected.Name,selected.Ip);
                }
            }
        }

        private void GroupListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GroupListBox.SelectedItem is GroupEntity selected)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    selected.MakeRandomKey(); //테스트를 위해서 추가 0814 2025 
                    mainWindow.ShowChatRoomFromStart(selected);
                    e.Handled = true; //버블링 현상 제거
                }
            }
        }

    }
}
