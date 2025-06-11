using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WalkieDohi.Entity;
using WalkieDohi.Util.Provider;

namespace WalkieDohi.Util.IO
{
    class UserJsonFileHandler : UserFileProvider
    {

        private readonly string filePath = "user.json";
        public User LoadUser()
        {
            if (File.Exists(filePath))
            {
                return JsonConvert.DeserializeObject<User>(File.ReadAllText(filePath));
            }

            User user = User.GetDefaultUser();
            SaveUser(user);
            return user;
        }

        public bool SaveUser(User user)
        {
            //임시
            if(user == null) { return false; }
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(user, Formatting.Indented));
                MainData.currentUser = user;
                return true;
            }
            catch(Exception e)
            {
                return false; 
            }
        }
    }
}
