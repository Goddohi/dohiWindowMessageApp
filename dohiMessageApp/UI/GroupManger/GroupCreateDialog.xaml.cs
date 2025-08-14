using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WalkieDohi.Entity;
using WalkieDohi.Util;

namespace WalkieDohi.UI.GroupManger
{
    public partial class GroupCreateDialog : Window
    {
        // 체크 선택을 위한 래퍼
        private class SelectableFriend : INotifyPropertyChanged
        {
            private bool _isSelected;

            public string Name { get; set; }
            public string Ip { get; set; }

            public string DisplayName
            {
                get
                {
                    if (!string.IsNullOrEmpty(Name)) return Name;
                    return Ip ?? string.Empty;
                }
            }

            public bool IsSelected
            {
                get { return _isSelected; }
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        var h = PropertyChanged;
                        if (h != null) h(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private List<SelectableFriend> _allFriends;  // 정렬된 원본(체크 상태 유지)
        private List<SelectableFriend> _viewFriends; // 화면 표시(검색 필터된 뷰)

        public GroupEntity ResultGroup { get; private set; }


        public GroupCreateDialog()
        {
            InitializeComponent();
            Init(MainData.GetsortedFriends(), null);
        }

        public GroupCreateDialog(IEnumerable<Friend> allFriends)
        {
            InitializeComponent();
            Init(allFriends, null);
        }

        public GroupCreateDialog(IEnumerable<Friend> allFriends, string suggestedGroupName)
        {
            InitializeComponent();
            Init(allFriends, suggestedGroupName);
        }

        private void Init(IEnumerable<Friend> allFriends, string suggestedGroupName)
        {
            // 1) 정렬: MainData.GetsortedFriends() 우선 사용
            var sorted = MainData.GetsortedFriends(); // ObservableCollection<Friend>
            var friends = (allFriends != null) ? allFriends : MainData.GetsortedFriends();

            // 2) Friend -> SelectableFriend 변환 (초기 순서 = 정렬 결과)
            _allFriends = friends.Select(f => new SelectableFriend
            {
                Name = f != null ? f.Name : null,
                Ip = f != null ? f.Ip : null,
                IsSelected = false
            }).ToList();

            // 3) 첫 화면은 그대로 보여줌(검색 전)
            _viewFriends = _allFriends.ToList();
            FriendListBox.ItemsSource = _viewFriends;

            if (!string.IsNullOrEmpty(suggestedGroupName))
                GroupNameBox.Text = suggestedGroupName;

            Loaded += (s, e) => GroupNameBox.Focus();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text != null ? SearchBox.Text.Trim().ToLowerInvariant() : string.Empty;

            if (string.IsNullOrEmpty(q))
            {
                // 검색 해제 시, 정렬된 원본 순서로 복귀
                _viewFriends = _allFriends.ToList();
            }
            else
            {
                // 원본 순서(이미 정렬됨)를 유지한 채 필터링
                _viewFriends = _allFriends
                    .Where(f =>
                        (!string.IsNullOrEmpty(f.Name) && f.Name.ToLowerInvariant().Contains(q)) ||
                        (!string.IsNullOrEmpty(f.Ip) && f.Ip.ToLowerInvariant().Contains(q)))
                    .ToList();
            }

            FriendListBox.ItemsSource = _viewFriends;
        }

        private void FriendListBox_ItemClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 행 클릭 시 체크 토글 (체크박스 클릭은 그대로 동작)
            var item = FriendListBox.SelectedItem as SelectableFriend;
            if (item != null) item.IsSelected = !item.IsSelected;
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            // 전체 해제
            foreach (var f in _allFriends) f.IsSelected = false;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var groupName = GroupNameBox.Text != null ? GroupNameBox.Text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(groupName))
            {
                MessageBox.Show("그룹 이름을 입력해 주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                GroupNameBox.Focus();
                return;
            }

           
            var selected = _allFriends.Where(f => f.IsSelected).ToList();

            // 본인 자동 포함
            var myIp = NetworkHelper.GetLocalIPv4();
            if (!string.IsNullOrEmpty(myIp) &&
                !selected.Any(f => string.Equals(f.Ip, myIp, StringComparison.OrdinalIgnoreCase)))
            {
                selected.Add(new SelectableFriend { Name = "본인", Ip = myIp, IsSelected = true });
            }

            var ips = selected
                .Select(f => f.Ip)
                .Where(ip => !string.IsNullOrEmpty(ip))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(ip => ip, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ips.Count < 2)
            {
                MessageBox.Show("유효한 IP를 가진 참가자가 2명 이상이어야 합니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var group = new GroupEntity
            {
                GroupName = groupName,
                Ips = ips
            };

            group.MakeRandomKey();

            ResultGroup = group;
            DialogResult = true;
            Close();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var f in _viewFriends)
                f.IsSelected = true;
        }
    }
}
