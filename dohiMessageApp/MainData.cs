using dohiMessageApp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dohiMessageApp
{
    public static class MainData
    {
        


        private static List<Friend> friends = new List<Friend>();

        public static List<Friend> Friends
        {
            get { return friends; }
            set {
                if (friends != value)
                {
                    friends = value;
                }
            }
            
        }

        public static User currentUser = new User();

        public static Dictionary<string, string> receivedFiles = new Dictionary<string, string>();
    }
}
