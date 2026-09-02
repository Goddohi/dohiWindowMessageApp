using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WalkieDohi.Core;
using WalkieDohi.Util;

namespace WalkieDohi.Groups.Entity
{
    public class GroupEntity :DohiEntityBase
    {
        private string _groupName;
        private List<string> _ips = new List<string>();
        private List<GroupMemberIdentity> _members = new List<GroupMemberIdentity>();

        public string GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName == value)
                {
                    return;
                }

                _groupName = value;
                OnPropertyChanged(nameof(GroupName));
            }
        }

        public List<string> Ips
        {
            get => _ips;
            set
            {
                _ips = value ?? new List<string>();
                OnPropertyChanged(nameof(Ips));
                OnPropertyChanged(nameof(TooltipText));
            }
        }

        public List<GroupMemberIdentity> Members
        {
            get => _members;
            set
            {
                _members = value ?? new List<GroupMemberIdentity>();
                OnPropertyChanged(nameof(Members));
                OnPropertyChanged(nameof(TooltipText));
            }
        }

        public string Key { get; set; }

        public void MakeRandomKey()
        {
            //키가 아예없는 초기에만 생성되록.
           if(string.IsNullOrWhiteSpace(Key))
                Key = Guid.NewGuid().ToString("N");
            /*
                "N": 32자리, 구분자 없음 → d85b1407333f4b6a8a3a1f6f0c2e9d2f
                "D": 하이픈 포함 기본형 → d85b1407-333f-4b6a-8a3a-1f6f0c2e9d2f
                "B": 중괄호 포함 → {d85b1407-333f-4b6a-8a3a-1f6f0c2e9d2f}
                "P": 괄호 포함 → (d85b1407-333f-4b6a-8a3a-1f6f0c2e9d2f)
                "X": 특수 16진 포맷(드물게 사용) 
             */
        }

        [JsonIgnore] //JSON 직렬화/역직렬화 과정에서 해당 속성(필드)을 무시
        public string TooltipText
        {
            get
            {
                var names = Ips.Select(ip =>
                {
                    if (NetworkHelper.AreSameIPv4(ip, NetworkHelper.GetLocalIPv4()))
                        return $"본인 ({ip})";
                    var member = FindMemberIdentityByIp(ip);
                    var friend = MainData.FindFriendByIdentity(ip, member?.UserUuid);
                    return $"{(friend?.Name ?? "이름 없음")} ({ip})";
                });

                return string.Join("\n", names);
            }
        }

        public void RefreshFriendDisplay()
        {
            OnPropertyChanged(nameof(TooltipText));
        }

        public void RefreshMemberIdentitiesFromFriends()
        {
            var nextMembers = new List<GroupMemberIdentity>();

            foreach (var ip in Ips ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(ip))
                {
                    continue;
                }

                string userUuid = FindMemberIdentityByIp(ip)?.UserUuid;
                var friend = MainData.FindFriendByIp(ip);
                if (!string.IsNullOrWhiteSpace(friend?.UserUuid))
                {
                    userUuid = friend.UserUuid;
                }

                if (NetworkHelper.AreSameIPv4(ip, NetworkHelper.GetLocalIPv4())
                    && !string.IsNullOrWhiteSpace(MainData.currentUser?.UserUuid))
                {
                    userUuid = MainData.currentUser.UserUuid;
                }

                nextMembers.Add(new GroupMemberIdentity
                {
                    Ip = ip,
                    UserUuid = NormalizeUuidOrEmpty(userUuid)
                });
            }

            Members = nextMembers;
        }

        public void UpsertMemberIdentity(string ip, string userUuid)
        {
            if (!NetworkHelper.TryNormalizeIPv4(ip, out string normalizedIp)
                || !MainData.TryNormalizeUserUuid(userUuid, out string normalizedUuid))
            {
                return;
            }

            var member = FindMemberIdentityByUuid(normalizedUuid) ?? FindMemberIdentityByIp(normalizedIp);
            if (member == null)
            {
                member = new GroupMemberIdentity { Ip = normalizedIp };
                Members.Add(member);
                if (!Ips.Any(existing => NetworkHelper.AreSameIPv4(existing, normalizedIp)))
                {
                    Ips.Add(normalizedIp);
                    OnPropertyChanged(nameof(Ips));
                }
            }

            member.UserUuid = normalizedUuid;
            OnPropertyChanged(nameof(Members));
            OnPropertyChanged(nameof(TooltipText));
        }

        public List<string> GetSendTargetIps()
        {
            RefreshMemberIdentitiesFromFriends();

            var targets = new List<string>();
            foreach (var member in Members)
            {
                if (IsCurrentUserUuid(member.UserUuid))
                {
                    continue;
                }

                var friend = MainData.FindFriendByUuid(member.UserUuid);
                string targetIp = !string.IsNullOrWhiteSpace(friend?.Ip)
                    ? friend.Ip
                    : member.Ip;

                string normalizedIp;
                if (!NetworkHelper.TryNormalizeIPv4(targetIp, out normalizedIp))
                {
                    continue;
                }

                if (NetworkHelper.AreSameIPv4(normalizedIp, NetworkHelper.GetLocalIPv4()))
                {
                    continue;
                }

                if (!targets.Any(existing => NetworkHelper.AreSameIPv4(existing, normalizedIp)))
                {
                    targets.Add(normalizedIp);
                }
            }

            return targets;
        }

        public string GetMemberUserUuidByIp(string ip)
        {
            return FindMemberIdentityByIp(ip)?.UserUuid ?? "";
        }

        private GroupMemberIdentity FindMemberIdentityByIp(string ip)
        {
            return Members?.FirstOrDefault(member => NetworkHelper.AreSameIPv4(member.Ip, ip));
        }

        private GroupMemberIdentity FindMemberIdentityByUuid(string userUuid)
        {
            return MainData.TryNormalizeUserUuid(userUuid, out string normalizedUuid)
                ? Members?.FirstOrDefault(member =>
                    string.Equals(member.UserUuid, normalizedUuid, StringComparison.OrdinalIgnoreCase))
                : null;
        }

        private static bool IsCurrentUserUuid(string userUuid)
        {
            return MainData.TryNormalizeUserUuid(userUuid, out string normalizedUuid)
                && string.Equals(normalizedUuid, MainData.currentUser?.UserUuid, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeUuidOrEmpty(string userUuid)
        {
            return MainData.TryNormalizeUserUuid(userUuid, out string normalizedUuid)
                ? normalizedUuid
                : "";
        }
    }

    public class GroupMemberIdentity : DohiEntityBase
    {
        private string _ip;
        private string _userUuid;

        public string Ip
        {
            get => _ip;
            set
            {
                if (_ip == value)
                {
                    return;
                }

                _ip = value;
                OnPropertyChanged(nameof(Ip));
            }
        }

        public string UserUuid
        {
            get => _userUuid;
            set
            {
                if (_userUuid == value)
                {
                    return;
                }

                _userUuid = value;
                OnPropertyChanged(nameof(UserUuid));
            }
        }
    }

    public class GroupMemberDisplay
    {
        public string Ip { get; set; }
        public string Name { get; set; }  // FriendList에서 조회해서 붙임
    }
}
