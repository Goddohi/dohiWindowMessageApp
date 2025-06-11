using WalkieDohi.Entity;
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
using WalkieDohi;
using WalkieDohi.UI.ViewModel;
using System.Collections.ObjectModel;
using WalkieDohi.Util.IO;
using WalkieDohi.Util.Provider;

namespace WalkieDohi.UI
{
    /// <summary>
    /// FriendManagerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FriendManagerWindow : Window
    {
        public FriendManagerWindowViewModel viewModel = new FriendManagerWindowViewModel();
        FriendFileProvider friendFilePrvider = new FriendJsonFileHandler();
        
        public FriendManagerWindow()
        {
            InitializeComponent();
            this.DataContext = viewModel;
            viewModel.Friends = new ObservableCollection<Friend>(MainData.Friends); // 복사본

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

            // 자기 자신을 제외한 IP 중복 검사 (수정모드가 아닐때는 -1이므로 영향 없음)
            bool duplicateIp = viewModel.Friends
                .Where((f, index) => index != editIndex)
                .Any(f => f.Ip == ip);
            if (duplicateIp)
            {
                MessageBox.Show("같은 IP가 이미 존재합니다.");
                return;
            }

            if (isEditMode)
            {
                if (editIndex >= 0 && editIndex < viewModel.Friends.Count)
                {
                    viewModel.Friends[editIndex].Name = name;
                    viewModel.Friends[editIndex].Ip = ip;
                    FriendList.Items.Refresh();
                    SaveFriends();
                }
                FriendUpdateCancleLogic();
                return;
            }      
            
            viewModel.Friends.Add(new Friend { Name = name, Ip = ip});
            SaveFriends();

            AddBoxAllClear();
        }

        private void RemoveFriend_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FriendList.SelectedIndex;
            if (isEditMode)
            {
                selectedIndex = editIndex;
            }
                
            if (selectedIndex < 0) return;

            var friend = viewModel.Friends[selectedIndex];

            string removeShow = isEditMode ? $"수정중이신 {friend.Name}을 삭제할까요?" : $"{friend.Name}을 삭제할까요?";

            if (MessageBox.Show(removeShow, "삭제 확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                viewModel.Friends.RemoveAt(selectedIndex);
                SaveFriends();
                FriendUpdateCancleLogic() ;
            }
        }

        private void SaveFriends()
        {
            friendFilePrvider.SaveFriends(viewModel.Friends);
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

        public Friend SelectedFriend { get; set; }
        /// <summary>
        /// 수정모드 
        /// </summary>
        private bool isEditMode = false;
        private int editIndex = -1;
        private void FriendList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FriendList.SelectedItem is Friend friend)
            {
                NameBox.Text = friend.Name;
                var ipParts = friend.Ip.Split('.');
                if (ipParts.Length == 4)
                {
                    IpBox1.Text = ipParts[0];
                    IpBox2.Text = ipParts[1];
                    IpBox3.Text = ipParts[2];
                    IpBox4.Text = ipParts[3];
                }

                isEditMode = true;
                editIndex = FriendList.SelectedIndex;
                btnUpdateCancle.Visibility = Visibility.Visible;
                btnAddFriend.Content = "수정 완료";
            }
        }

        private void UpdateCancle_Click(object sender, RoutedEventArgs e)
        {
            FriendUpdateCancleLogic();
        }

        private void FriendUpdateCancleLogic()
        {
            isEditMode = false;
            btnUpdateCancle.Visibility = Visibility.Hidden;
            editIndex = -1;
            btnAddFriend.Content = "추가";
            AddBoxAllClear();
        }
        private void FriendUpdateStarteLogic()
        {
            isEditMode = false;
            btnUpdateCancle.Visibility = Visibility.Hidden;
            editIndex = -1;
            btnAddFriend.Content = "추가";
            AddBoxAllClear();
        }
    }
}


