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
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _ip;
        public string Ip {
            get => _ip;
            set { _ip = value; OnPropertyChanged(nameof(Ip)); }
        }
        private int _port;
        public int Port {
            get => _port;
            set { _port = value; OnPropertyChanged(nameof(Port)); }
        }

    }
}
