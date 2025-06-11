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
    }
}
