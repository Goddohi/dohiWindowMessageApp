using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using WalkieDohi.Entity;

namespace WalkieDohi.Data
{
    public static class ChatListManager
    {
        private static ObservableCollection<ChatListItem> _chatList = new ObservableCollection<ChatListItem>();

        public static void UpdateChatList(string name, string ip)
        {
            if (!_chatList.Any(c => c.Ip == ip))
                _chatList.Add(new ChatListItem { Name = name, Ip = ip ,Group = null});
        }
        public static void UpdateChatList(GroupEntity group)
        {
            if (!_chatList.Any(c =>
                c.Group != null &&
                c.Group.GroupName == group.GroupName &&
                c.Group.Ips.Distinct().OrderBy(ip => ip).SequenceEqual(group.Ips.Distinct().OrderBy(ip => ip))
            ))
            {
                _chatList.Add(new ChatListItem
                {
                    Name = group.GroupName,
                    Ip = null,
                    Group = group
                });
            }
        }


        public static ObservableCollection<ChatListItem> GetChatList() => _chatList;
    }

    public class ChatListItem
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public GroupEntity Group { get; set; }
    }

}
