using WalkieDohi.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WalkieDohi.Friends.Entity

{
    public class Friend : DohiEntityBase
    {
        /// <summary>
        /// 필드 종류 Name, Ip, Port
        /// </summary>
         

        #region private 필드
        private string _name;
        private string _ip;
        private string _userUuid;
        #endregion

        #region public 필드
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); OnPropertyChanged(nameof(DisplayName)); }
        }


        public string Ip {
            get => _ip;
            set { _ip = value; OnPropertyChanged(nameof(Ip)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string UserUuid
        {
            get => _userUuid;
            set { _userUuid = value; OnPropertyChanged(nameof(UserUuid)); }
        }

        [JsonIgnore]
        public string DisplayName => $"{Name} ({Ip})";
        #endregion

    }
}
