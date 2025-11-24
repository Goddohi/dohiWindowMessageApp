using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Core;

namespace WalkieDohi.Users.Entity
{
    public class User : DohiEntityBase
    {
        public string Nickname { get; set; } = "사용자";


        // 설치/유저 단위 고유 식별자
        public string UserUuid { get; private set; }

        public UserPreferences Preferences { get; set; } = new UserPreferences();

        /// <summary>
        /// 기본생성자로 초기값이 설정됨 (메서드명칭으로 확실히알려주기위해 생성)
        /// </summary>
        /// <returns></returns>
        public static User GetDefaultUser()
        { 
            User user = new User();
            user.UserUuid = Guid.NewGuid().ToString();
            return user;
        }

        public static Boolean UserChecked(User user)
        {
            if (user == null) { return false; }
            
            // 새로 만들 때 비어 있으면 한 번만 생성 기본사용자들을 위함 2025.11.24기준
            if (string.IsNullOrEmpty(user.UserUuid))
            {
                user.UserUuid = Guid.NewGuid().ToString();
            }
            if (user.Preferences == null)
            {
                user.Preferences = User.GetDefaultUser().Preferences; 
            }

            return true;
        }

        #region    Getter Setter
        public int getPreferencesPort()
        {
            return Preferences.Port;
        }

        #endregion Getter Setter


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
