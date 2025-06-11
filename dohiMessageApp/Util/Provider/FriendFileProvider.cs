using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Entity;

namespace WalkieDohi.Util.Provider
{
    interface FriendFileProvider
    {

        void SaveFriends(ObservableCollection<Friend> friends);

        ObservableCollection<Friend> LoadFriends();



    }
}
