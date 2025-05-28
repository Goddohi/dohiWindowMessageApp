using dohiMessageApp.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dohiMessageApp
{
    public static class MainWindowData
    {
        // 추후 사용할 아이콘 저장 ✅  📤  📩  📁
        public static Dictionary<string, string> receivedFiles = new Dictionary<string, string>();

        public static List<Friend> friends = new List<Friend>();

        public static User currentUser = new User();
    }
}
