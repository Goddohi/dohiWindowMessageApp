using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Entity;

namespace WalkieDohi.UI.ViewModel
{
    public class GroupManagerViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<GroupEntity> Groups { get; set; }

        public ObservableCollection<Friend> FriendList { get; set; }

        private GroupEntity _selectedGroup;
        public GroupEntity SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
                UpdateDisplayMembers();
            }
        }

        // UI에 바인딩할 리스트
        public ObservableCollection<GroupMemberDisplay> DisplayMembers { get; set; } = new ObservableCollection<GroupMemberDisplay>();

        private void UpdateDisplayMembers()
        {
            DisplayMembers.Clear();
            if (SelectedGroup?.Ips == null) return;

            foreach (var ip in SelectedGroup.Ips)
            {
                var name = FriendList.FirstOrDefault(f => f.Ip == ip)?.Name ?? "(이름 없음)";
                DisplayMembers.Add(new GroupMemberDisplay { Ip = ip, Name = name });
            }
        }

    }
}
