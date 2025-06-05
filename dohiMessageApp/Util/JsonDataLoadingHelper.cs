using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WalkieDohi.Entity;

namespace WalkieDohi.Util
{
    public static class JsonDataLoadingHelper
    {
        public static string LoadUser()
        {
            var path = "user.json";
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                MainData.currentUser = JsonConvert.DeserializeObject<User>(json);
            }
            else
            {
                MainData.currentUser = new User { Nickname = "사용자" };
                SaveUser(MainData.currentUser.Nickname,false);
            }
            return MainData.currentUser.Nickname;
        }

        public static void SaveUser(string nickName,bool showMessage)
        {
            MainData.currentUser.Nickname = nickName.Trim();
            File.WriteAllText("user.json", JsonConvert.SerializeObject(MainData.currentUser, Formatting.Indented));
            if (showMessage) MessageBox.Show("닉네임이 저장되었습니다.");
        }

        public static void LoadFriends()
        {
            var path = "friends.json";
            if (!File.Exists(path))
            {
                MainData.Friends = new List<Friend> {
                    new Friend { Name = "로컬 테스트", Ip = "127.0.0.1", Port = 9000 }
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(MainData.Friends, Formatting.Indented));
                MessageBox.Show("기본 친구 목록을 생성했습니다.");
            }
            else
            {
                MainData.Friends = JsonConvert.DeserializeObject<List<Friend>>(File.ReadAllText(path));
            }
        }

        public static void LoadGroups()
        {
            var path = "groups.json";
            if (!File.Exists(path))
            {
                MainData.Groups = new List<GroupEntity>();
                File.WriteAllText(path, JsonConvert.SerializeObject(MainData.Friends, Formatting.Indented));

            }
            else
            {
                MainData.Groups = JsonConvert.DeserializeObject<List<GroupEntity>>(File.ReadAllText(path));
            }
        }
    }
}
