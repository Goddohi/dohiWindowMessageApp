using dohiMessageApp.Core;
using dohiMessageApp.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dohiMessageApp.UI.Model
{
    public class FriendManagerWindowModel : DohiViewModelBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }
        private ObservableCollection<Friend> _friends;
        public ObservableCollection<Friend> Friends
        {
            get => _friends;
            set {
                if (value != _friends) 
                { 
                    _friends = value;
                    OnPropertyChanged(nameof(Friends));
                }
            }
        }

    }
}
