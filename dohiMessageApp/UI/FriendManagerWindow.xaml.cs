using dohiMessageApp.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace dohiMessageApp.UI
{
    /// <summary>
    /// FriendManagerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FriendManagerWindow : Window
    {

        private readonly string filePath = "friends.json";
        public List<Friend> Friends { get; private set; }
        public FriendManagerWindow()
        {
            InitializeComponent();
            Friends = new List<Friend>(MainWindowData.friends); // 복사본
            RefreshList();
        }

        private void RefreshList()
        {
            FriendList.ItemsSource = null;
            FriendList.ItemsSource = Friends.Select(f => $"{f.Name} ({f.Ip})");
        }

        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string ip = IpBox.Text.Trim();
            

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("모든 항목을 입력해주세요.");
                return;
            }

            if (!IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("올바른 IP 주소를 입력하세요.");
                return;
            }


            if (Friends.Any(f => f.Name == name))
            {
                MessageBox.Show("같은 이름의 친구가 이미 존재합니다.");
                return;
            }

            Friends.Add(new Friend { Name = name, Ip = ip, Port = 9000 });
            SaveFriends();
            RefreshList();
            NameBox.Clear(); IpBox.Clear(); 
        }

        private void RemoveFriend_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FriendList.SelectedIndex;
            if (selectedIndex < 0) return;

            var friend = Friends[selectedIndex];
            if (MessageBox.Show($"{friend.Name}을 삭제할까요?", "삭제 확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Friends.RemoveAt(selectedIndex);
                SaveFriends();
                RefreshList();
            }
        }

        private void SaveFriends()
        {
            string json = JsonConvert.SerializeObject(Friends, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
