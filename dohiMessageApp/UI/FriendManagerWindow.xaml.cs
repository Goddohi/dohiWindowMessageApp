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
using dohiMessageApp;
using dohiMessageApp.UI.ViewModel;
using System.Collections.ObjectModel;

namespace dohiMessageApp.UI
{
    /// <summary>
    /// FriendManagerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FriendManagerWindow : Window
    {
        public FriendManagerWindowViewModel viewModel = new FriendManagerWindowViewModel();
        private readonly string filePath = "friends.json";
        
        public FriendManagerWindow()
        {
            InitializeComponent();
            this.DataContext = viewModel;
            viewModel.Friends = new ObservableCollection<Friend>(MainData.Friends); // 복사본
            RefreshList();

        }

        private void RefreshList()
        {
            //FriendList.ItemsSource = null;
           // FriendList.ItemsSource = Friends.Select(f => $"{f.Name} ({f.Ip})");
        }

        #region 클리어 로직
        private void AddBoxAllClear()
        {
            NameBox.Clear();
            AddBoxIpClear();
        }

        private void AddBoxIpClear()
        {
            IpBox1.Clear(); IpBox2.Clear(); IpBox3.Clear(); IpBox4.Clear();
        }

        #endregion

        private string GetIpFullstring() {
            return IpBox1.Text.Trim() + "." + IpBox2.Text.Trim() + "." + IpBox3.Text.Trim() + "." + IpBox4.Text.Trim();
        }


        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string ip = GetIpFullstring();


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

            if (viewModel.Friends.Any(f => f.Ip == ip))
            {
                MessageBox.Show("같은 ip가 이미 존재합니다.");
                return;
            }

            viewModel.Friends.Add(new Friend { Name = name, Ip = ip, Port = 9000 });
            SaveFriends();
            RefreshList();
            AddBoxAllClear();
        }

        private void RemoveFriend_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FriendList.SelectedIndex;
            if (selectedIndex < 0) return;

            var friend = viewModel.Friends[selectedIndex];
            if (MessageBox.Show($"{friend.Name}을 삭제할까요?", "삭제 확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                viewModel.Friends.RemoveAt(selectedIndex);
                SaveFriends();
                RefreshList();
            }
        }

        private void SaveFriends()
        {
            string json = JsonConvert.SerializeObject(viewModel.Friends, Formatting.Indented);
            File.WriteAllText(filePath, json);
            MainData.Friends = viewModel.Friends.ToList();
        }

        private void IpBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 숫자가 아니면 입력 차단
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void txtAddBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                e.Handled = true; // 기본 Tab 동작 막기
                if (sender is TextBox textBox)
                {
                    switch (textBox.Name)
                    {
                        case "NameBox":
                            IpBox1.Focus();
                            break;
                        case "IpBox1":
                            IpBox2.Focus();
                            break;
                        case "IpBox2":
                            IpBox3.Focus();
                            break;
                        case "IpBox3":
                            IpBox4.Focus();
                            break;
                        case "IpBox4":
                            btnAddFriend.Focus();
                            break;
                    }
                    
                }
            }
        }



    }
}


