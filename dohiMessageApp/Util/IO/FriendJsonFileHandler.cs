using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WalkieDohi.Entity;
using WalkieDohi.Util.Provider;

namespace WalkieDohi.Util.IO
{
    class FriendJsonFileHandler : FriendFileProvider
    {
        private readonly string filePath = "friends.json";

        public void SaveFriends(ObservableCollection<Friend> friends)
        {
            string json = JsonConvert.SerializeObject(friends, Formatting.Indented);
            File.WriteAllText(filePath, json);
            MainData.Friends = friends;
        }

        public ObservableCollection<Friend> LoadFriends()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return JsonConvert.DeserializeObject<ObservableCollection<Friend>>(File.ReadAllText(filePath));
                }
            }catch(Exception e) {
                MessageBox.Show("친구파일을 불러오지 못하였습니다.\n" + e.Message);
            }

            ObservableCollection<Friend> friends = new ObservableCollection<Friend>();
            friends.Add(new Friend { Name = "로컬 테스트", Ip = "127.0.0.1" });
            File.WriteAllText(filePath, JsonConvert.SerializeObject(friends, Formatting.Indented));
            return friends;

        }
    }
}
