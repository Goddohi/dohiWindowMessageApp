using WalkieDohi.ChattingRooms.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net;
using WalkieDohi.Util;
using WalkieDohi.Users.Entity;
using WalkieDohi.Friends.Entity;

namespace WalkieDohi
{
    public static class MainData
    {
        public static int GetPort() { return MainData.currentUser.getPreferencesPort(); }
        public static event Action FriendsChanged;

        private static ObservableCollection<Friend> friends = new ObservableCollection<Friend>();

        public static ObservableCollection<Friend> Friends
        {
            get { return friends; }
            set {
                if (friends != value)
                {
                    friends = value;
                    FriendsChanged?.Invoke();
                }
            }
            
        }

        public static void NotifyFriendsChanged()
        {
            FriendsChanged?.Invoke();
        }

        public static User currentUser = new User();

        /// <summary>
        /// IP 주소에 해당하는 친구의 이름으로 수정하여 반환합니다.
        /// 친구 목록에서 IP가 일치하는 첫 번째 친구의 이름을 찾습니다.
        /// 일치하는 친구가 없을 경우, 원래의 이름으로 다시 제공합니다.
        /// </summary>
        /// <param name="friend"> Friend 객체</param>
        /// <returns>해당 IP의 이름, IP가 일치하는 경우가 없을 경우 원래의 이름으로 다시 제공합니다.</returns>
        public static Friend GetFriendNameOrReturnOriginal(Friend friend)
        {
            if (friend == null)
            {
                return null;
            }

            friend.Name = FindFriendByIdentity(friend.Ip, friend.UserUuid)?.Name ?? friend.Name;
            return friend;
        }

        /// <summary>
        /// IP 주소에 해당하는 친구의 이름으로 수정하여 반환합니다.
        /// 친구 목록에서 IP가 일치하는 첫 번째 친구의 이름을 찾습니다.
        /// 일치하는 친구가 없을 경우, 원래의 이름으로 다시 제공합니다.
        /// </summary>
        /// <param name="ip">찾고자하는 ip</param>
        /// <returns>해당 IP의 이름, IP가 일치하는 경우가 없을 경우 원래의 이름으로 다시 제공합니다.</returns>
        public static string GetFriendNameOrReturnOriginal(string name,string ip)
        {
            name = FindFriendByIdentity(ip, null)?.Name ?? name;
            return name;
        }

        public static string GetFriendNameOrReturnOriginal(string name, string ip, string userUuid)
        {
            name = FindFriendByIdentity(ip, userUuid)?.Name ?? name;
            return name;
        }

        public static string GetSingleChatDisplayName(string ip)
        {
            return FindFriendByIdentity(ip, null)?.Name ?? "미등록 친구";
        }

        public static string GetSingleChatDisplayName(string ip, string userUuid)
        {
            return FindFriendByIdentity(ip, userUuid)?.Name ?? "미등록 친구";
        }

        public static Friend FindFriendByIp(string ip)
        {
            return MainData.Friends?.FirstOrDefault(f => NetworkHelper.AreSameIPv4(f.Ip, ip));
        }

        public static Friend FindFriendByUuid(string userUuid)
        {
            string normalizedUuid;
            if (!TryNormalizeUserUuid(userUuid, out normalizedUuid))
            {
                return null;
            }

            return MainData.Friends?.FirstOrDefault(f =>
                string.Equals(f.UserUuid, normalizedUuid, StringComparison.OrdinalIgnoreCase));
        }

        public static Friend FindFriendByIdentity(string ip, string userUuid)
        {
            string normalizedUuid;
            if (TryNormalizeUserUuid(userUuid, out normalizedUuid))
            {
                var byUuid = FindFriendByUuid(normalizedUuid);
                if (byUuid != null)
                {
                    return byUuid;
                }

                var byIp = FindFriendByIp(ip);
                if (byIp != null && string.IsNullOrWhiteSpace(byIp.UserUuid))
                {
                    return byIp;
                }

                return null;
            }

            return FindFriendByIp(ip);
        }

        public static string ResolveIncomingSingleChatIp(string senderIp, string senderUserUuid)
        {
            var friend = FindFriendByIdentity(senderIp, senderUserUuid);
            return !string.IsNullOrWhiteSpace(friend?.Ip)
                ? friend.Ip
                : senderIp;
        }

        public static bool TryAttachFriendUuidByIp(string ip, string userUuid)
        {
            if (string.IsNullOrWhiteSpace(ip)
                || !TryNormalizeUserUuid(userUuid, out string normalizedUuid)
                || string.Equals(normalizedUuid, currentUser?.UserUuid, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!NetworkHelper.TryNormalizeIPv4(ip, out string normalizedIp))
            {
                return false;
            }

            var friend = FindFriendByUuid(normalizedUuid);
            if (friend == null)
            {
                friend = FindFriendByIp(normalizedIp);
            }

            if (friend == null
                || (!string.IsNullOrWhiteSpace(friend.UserUuid)
                    && !string.Equals(friend.UserUuid, normalizedUuid, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (string.Equals(friend.UserUuid, normalizedUuid, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            friend.UserUuid = normalizedUuid;
            return true;
        }

        public static bool TryNormalizeUserUuid(string value, out string normalized)
        {
            normalized = "";
            Guid parsed;
            if (!Guid.TryParse(value, out parsed))
            {
                return false;
            }

            normalized = parsed.ToString("D");
            return true;
        }
        
        public static ObservableCollection<Friend> GetsortedFriends()
        {
            var sortType = MainData.currentUser.Preferences.FriendSortOrder;

            return (sortType == FriendSortType.ByIp)
                ? new ObservableCollection<Friend>(MainData.Friends.OrderBy(f => IPAddress.Parse(f.Ip), new IPAddressComparer()))
                : new ObservableCollection<Friend>(MainData.Friends.OrderBy(f => f.Name));
        }

    }
}
