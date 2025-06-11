using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Core;

namespace WalkieDohi.Entity
{
    public class User : DohiEntityBase
    {
        public string Nickname { get; set; } = "사용자";
        public UserPreferences Preferences { get; set; } = new UserPreferences();


        public static User GetDefaultUser()
        {
            return new User()
            {
                Nickname = "사용자",
                Preferences = new UserPreferences{ Port = 9000 }
            };
        }
        //비교



        #region Getter Setter
        public int getPreferencesPort()
        {
            return Preferences.Port;
        }

        #endregion




        #region 조회메서드


        #endregion


    }
    public class UserPreferences
    {
        public int Port { get; set; } = 9000;
    }
}
