using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using WalkieDohi.Entity;
using WalkieDohi.Util;

namespace WalkieDohi.UI
{
    public partial class GroupManagerWindow : Window
    {
        public ObservableCollection<GroupEntity> Groups { get; set; } = new ObservableCollection<GroupEntity>(MainData.Groups);
        public ObservableCollection<Friend> FriendList { get; set; } = new ObservableCollection<Friend>(MainData.Friends);

        private GroupEntity _selectedGroup;

        private static readonly string GroupJsonPath =
    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.json");

        public GroupManagerWindow()
        {
            InitializeComponent();

            // 그룹 불러오기
            //Groups = LoadGroupsFromFile();
            GroupList.ItemsSource = Groups;

            DataContext = this;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // 종료 시 저장
            SaveGroupsToFile();
        }

        public ObservableCollection<GroupEntity> LoadGroupsFromFile()
        {
            if (!File.Exists(GroupJsonPath))
                return new ObservableCollection<GroupEntity>();

            var json = File.ReadAllText(GroupJsonPath);
            var groups = JsonConvert.DeserializeObject<ObservableCollection<GroupEntity>>(json);
            return groups ?? new ObservableCollection<GroupEntity>();
        }

        private void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            var name = GroupNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("그룹명을 입력해주세요.");
                return;
            }
            
            var newGroup = new GroupEntity
            {
                GroupName = name,
                Ips = new[] { NetworkHelper.GetLocalIPv4() }
            };
            Groups.Add(newGroup);
            GroupNameBox.Clear();
            SaveGroupsToFile(); // 변경 사항 저장
        }

        private void GroupList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedGroup = GroupList.SelectedItem as GroupEntity;
            RefreshMemberDisplay();
        }

        private void RefreshMemberDisplay()
        {
            if (_selectedGroup == null)
            {
                MemberList.ItemsSource = null;
                return;
            }

            var displays = _selectedGroup.Ips
                .Select(ip =>
                {
                    string name;
                    if (ip == NetworkHelper.GetLocalIPv4())
                    {
                        name = "본인";
                    }
                    else
                    {
                        name = FriendList.FirstOrDefault(f => f.Ip == ip)?.Name ?? "(이름 없음)";
                    }
                    return new GroupMemberDisplay { Ip = ip, DisplayText = $"{name} ({ip})" };
                }).ToList();

            MemberList.ItemsSource = displays;
        }

        private void AddMember_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null)
            {
                MessageBox.Show("먼저 그룹을 선택하세요.");
                return;
            }

            var selectWindow = new FriendSelectWindow(FriendList);
            selectWindow.Owner = this;

            if (selectWindow.ShowDialog() == true)
            {
                var selected = selectWindow.SelectedFriend;
                if (selected != null && !_selectedGroup.Ips.Contains(selected.Ip))
                {
                    _selectedGroup.Ips = _selectedGroup.Ips.Append(selected.Ip).ToArray();
                    RefreshMemberDisplay();
                    SaveGroupsToFile(); // 변경 사항 저장
                }
            }
        }


        /// <summary>
        /// Json으로 바로 적용
        /// </summary>
        public void SaveGroupsToFile()
        {
            var json = JsonConvert.SerializeObject(Groups, Formatting.Indented);
            File.WriteAllText(GroupJsonPath, json);
            MainData.Groups = Groups.ToList<GroupEntity>();
        }
        /// <summary>
        /// 그룹원 삭제
        /// </summary>
        private void RemoveMember_Click(object sender, RoutedEventArgs e)
        {

            if (_selectedGroup == null) return;
            var selected = MemberList.SelectedItem as GroupMemberDisplay;
            if (selected == null) return;
            if(selected.Ip == NetworkHelper.GetLocalIPv4())
            {
                MessageBox.Show("본인은 제외할 수 없습니다.");
                return;
            }
            _selectedGroup.Ips = _selectedGroup.Ips.Where(ip => ip != selected.Ip).ToArray();
            RefreshMemberDisplay();
            SaveGroupsToFile(); 
        }

        private void RenameGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;

            var dialog = new InputDialog("그룹 이름 변경", _selectedGroup.GroupName,"그룹이름은 채팅방의 Key입니다.");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                _selectedGroup.GroupName = dialog.ResponseText;
                GroupList.Items.Refresh();
            }
            SaveGroupsToFile(); // 변경 사항 저장
        }

        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGroup == null) return;
            if (MessageBox.Show("정말 삭제하시겠습니까?", "확인", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Groups.Remove(_selectedGroup);
                _selectedGroup = null;
                RefreshMemberDisplay();
                SaveGroupsToFile(); // 변경 사항 저장
            }
        }

        private class GroupMemberDisplay
        {
            public string Ip { get; set; }
            public string DisplayText { get; set; }
        }
    }
}
