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
            if (FriendListBox.SelectedItem is Friend selected)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.ShowChatRoomFromStart(selected.Name,selected.Ip);
                }
            }
        }


    }
}
