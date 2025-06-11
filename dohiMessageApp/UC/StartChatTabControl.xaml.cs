using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using WalkieDohi.Core;
using WalkieDohi.Entity;

namespace WalkieDohi.UC
{
    /// <summary>
    /// StartChatTabControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StartChatTabControl : UserControl
    {
        public event Action<Friend> OnStartChat;
        public event Action<GroupEntity> OnStartGroupChat;

        public StartChatTabControl()
        {
            InitializeComponent();
            StartChatButton.Click += StartChatButton_Click;
            StartGroupChatButton.Click += StartGroupChatButton_Click;
        }

        public void SetFriends(ObservableCollection<Friend> friends)
        {
            FriendComboBox.ItemsSource = null;
            FriendComboBox.ItemsSource = friends;
            if (friends.Count > 0)
                FriendComboBox.SelectedIndex = 0;
        }

        public void SetGroups(ObservableCollection<GroupEntity> groups)
        {
            GroupComboBox.ItemsSource = null;
            GroupComboBox.ItemsSource = groups;
            if (groups.Count > 0)
                GroupComboBox.SelectedIndex = 0;
        }

        private void StartChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (FriendComboBox.SelectedItem is Friend selected)
            {
                OnStartChat?.Invoke(selected);
            }
            else
            {
                MessageBox.Show("친구를 선택하세요.");
            }
        }

        private void StartGroupChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (GroupComboBox.SelectedItem is GroupEntity selected)
            {
                OnStartGroupChat?.Invoke(selected);
            }
            else
            {
                MessageBox.Show("그룹을 선택하세요.");
            }
        }
    }
}