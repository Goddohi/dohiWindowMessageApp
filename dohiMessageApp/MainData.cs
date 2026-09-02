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

            friend.Name = FindFriendByIp(friend.Ip)?.Name ?? friend.Name;
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
            name = FindFriendByIp(ip)?.Name ?? name;
            return name;
        }

        public static string GetSingleChatDisplayName(string ip)
        {
            return FindFriendByIp(ip)?.Name ?? "미등록 친구";
        }

        public static Friend FindFriendByIp(string ip)
        {
            return MainData.Friends?.FirstOrDefault(f => NetworkHelper.AreSameIPv4(f.Ip, ip));
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
