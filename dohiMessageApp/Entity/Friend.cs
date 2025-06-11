using WalkieDohi.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.Entity

{
    public class Friend : DohiEntityBase
    {
        /// <summary>
        /// 필드 종류 Name, Ip, Port
        /// </summary>
         

        #region private 필드
        private string _name;
        private string _ip;
        #endregion

        #region public 필드
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }


        public string Ip {
            get => _ip;
            set { _ip = value; OnPropertyChanged(nameof(Ip)); }
        }
        #endregion

    }
}
