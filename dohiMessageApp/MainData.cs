using WalkieDohi.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi
{
    public static class MainData
    {
        public static int port = 9000;
        private static List<GroupEntity> groups = new List<GroupEntity>();

        public static List<GroupEntity> Groups
        {
            get { return groups; }
            set
            {
                if (groups != value)
                {
                    groups = value;
                }
            }

        }


        private static List<Friend> friends = new List<Friend>();

        public static List<Friend> Friends
        {
            get { return friends; }
            set {
                if (friends != value)
                {
                    friends = value;
                }
            }
            
        }

        /// <summary>
        /// IP 주소에 해당하는 친구의 이름으로 수정하여 반환합니다.
        /// 친구 목록에서 IP가 일치하는 첫 번째 친구의 이름을 찾습니다.
        /// 일치하는 친구가 없을 경우, 원래의 이름으로 다시 제공합니다.
        /// </summary>
        /// <param name="friend"> Friend 객체</param>
        /// <returns>해당 IP의 이름, IP가 일치하는 경우가 없을 경우 원래의 이름으로 다시 제공합니다.</returns>
        public static Friend GetFriendNameOrReturnOriginal(Friend friend)
        {
            friend.Name = MainData.Friends.FirstOrDefault(f => f.Ip == friend.Ip)?.Name ?? friend.Name;
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
            name = MainData.Friends.FirstOrDefault(f => f.Ip == ip)?.Name ?? name;
            return name;
        }



        public static User currentUser = new User();

        public static Dictionary<string, string> receivedFiles = new Dictionary<string, string>();




    }
}
