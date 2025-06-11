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
        #region    비교메서드


        #endregion 비교메서드



        #region    Getter Setter
        public int getPreferencesPort()
        {
            return Preferences.Port;
        }

        #endregion Getter Setter




        #region   조회메서드


        #endregion 조회메서드


    }
    public class UserPreferences
    {
        public int Port { get; set; } = 9000;
        public FriendSortType FriendSortOrder { get; set; } = FriendSortType.ByIp;




    }
    public enum FriendSortType
    {
        ByIp, ByName
    }
}
