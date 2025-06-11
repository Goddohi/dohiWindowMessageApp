using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Entity;

namespace WalkieDohi.Util.Provider
{
    interface UserFileProvider
    {
        User LoadUser();

        bool SaveUser(User user);
    }
}
