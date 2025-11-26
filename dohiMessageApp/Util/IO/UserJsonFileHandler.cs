using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Users.Entity;
using WalkieDohi.Util.Provider;

namespace WalkieDohi.Util.IO
{
    class UserJsonFileHandler : UserFileProvider
    {
        private string DohifilePath =>
            DirectoryManager.GetAppDataDirectoryCombineFileName("user.dohi");

        
        private string JsonFilePath =>
            DirectoryManager.GetAppDataDirectoryCombineFileName("user.json");

        private User ActualUserFilePath
        {
            get
            {
                if (File.Exists(DohifilePath))
                    return JsonConvert.DeserializeObject<User>(File.ReadAllText(DohifilePath));
                if (File.Exists(JsonFilePath))
                    return JsonConvert.DeserializeObject<User>(File.ReadAllText(JsonFilePath));
                return User.GetDefaultUser();
            }
        }

        public User LoadUser()
        {
            // 이전버전 사용자를 위한 자동 업데이트 로직
            User user = ActualUserFilePath;
            SaveUser(user);
            return user;
        }

        public bool SaveUser(User user)
        {
            //임시
            if (User.UserChecked(user) == false) { return false; }
            try
            {
                File.WriteAllText(DohifilePath, JsonConvert.SerializeObject(user, Formatting.Indented));
                MainData.currentUser = user;
                return true;
            }
            catch(Exception e)
            {
                return false; 
            }
        }


        public void MergePreferences(User fileUser)
        {
            var defaultPrefs = User.GetDefaultUser().Preferences;
            var userPrefs = fileUser.Preferences;

            if (userPrefs == null)
            {
                fileUser.Preferences = defaultPrefs;
                return;
            }

            var props = typeof(UserPreferences).GetProperties();
            bool isDefaultcnt = false;
            foreach (var prop in props)
            {
                var userValue = prop.GetValue(userPrefs);
                var isDefault = userValue == null || (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)userValue));

                if (isDefault)
                {
                    var defaultValue = prop.GetValue(defaultPrefs);
                    prop.SetValue(userPrefs, defaultValue);
                }
            }
            if (isDefaultcnt)
            {
                SaveUser(fileUser);
            }
        }
    }
}
