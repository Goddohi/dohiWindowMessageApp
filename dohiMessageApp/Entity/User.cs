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

        /// <summary>
        /// 기본생성자로 초기값이 설정됨 (메서드명칭으로 확실히알려주기위해 생성)
        /// </summary>
        /// <returns></returns>
        public static User GetDefaultUser()
        {
            return new User();
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
