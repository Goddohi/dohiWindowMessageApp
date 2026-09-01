using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Util;
using WalkieDohi.Friends.Entity;
using WalkieDohi.Util.Provider;

namespace WalkieDohi.Util.IO
{
    class FriendJsonFileHandler : FriendFileProvider
    {

        private string filePath => DirectoryManager.GetAppDataDirectoryCombineFileName(fileName);
        private readonly string fileName= "friends.json";

        private string DohifilePath =>
            DirectoryManager.GetAppDataDirectoryCombineFileName("friends.dohi");


        private string JsonFilePath =>
            DirectoryManager.GetAppDataDirectoryCombineFileName("friends.json");

        private ObservableCollection<Friend> ActualFriendFilePath
        {
            get
            {
                try
                {
                    if (File.Exists(DohifilePath))
                        return JsonConvert.DeserializeObject<ObservableCollection<Friend>>(File.ReadAllText(DohifilePath));
                    if (File.Exists(JsonFilePath))
                        return JsonConvert.DeserializeObject<ObservableCollection<Friend>>(File.ReadAllText(JsonFilePath));
                    return new ObservableCollection<Friend>();
                }
                catch
                {
                    return new ObservableCollection<Friend>();
                }
            }
        }

        public void SaveFriends(ObservableCollection<Friend> friends)
        {
            ObservableCollection<Friend> normalizedFriends = NormalizeFriends(friends);
            string json = JsonConvert.SerializeObject(normalizedFriends, Formatting.Indented);
            File.WriteAllText(DohifilePath, json);
            MainData.Friends = normalizedFriends;
        }

        public ObservableCollection<Friend> LoadFriends()
        {
            ObservableCollection<Friend> LoadFriend = ActualFriendFilePath;
            return NormalizeFriends(LoadFriend);
        }

        private ObservableCollection<Friend> NormalizeFriends(IEnumerable<Friend> friends)
        {
            var result = new ObservableCollection<Friend>();
            var seenIps = new HashSet<string>(StringComparer.Ordinal);

            if (friends == null)
            {
                return result;
            }

            foreach (Friend friend in friends)
            {
                if (friend == null)
                {
                    continue;
                }

                string normalizedIp;
                if (!NetworkHelper.TryNormalizeIPv4(friend.Ip, out normalizedIp))
                {
                    result.Add(friend);
                    continue;
                }

                if (seenIps.Contains(normalizedIp))
                {
                    continue;
                }

                seenIps.Add(normalizedIp);
                friend.Ip = normalizedIp;
                result.Add(friend);
            }

            return result;
        }
    }
}
