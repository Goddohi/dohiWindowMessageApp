using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        public static ObservableCollection<ChatListItem> GetChatList() => _chatList;



        #region 업데이트 리스트 로직
        public static void UpdateChatList(MessageEntity msg)
        {
            if (msg.IsGroupMessage && msg.Group != null)
            {
                UpdateChatList(msg.Group);
            }
            else if (msg.IsSingleMessage)
            {
                string name = MainData.GetFriendNameOrReturnOriginal(msg.Sender, msg.SenderIp);
                UpdateChatList(name, msg.SenderIp);
            }
        }

        public static void UpdateChatList(GroupEntity group)
        {
            // 그룹 기준으로 동일한 항목 찾기
            var existing = _chatList.FirstOrDefault(c =>
                c.Group != null &&
                c.Group.GroupName == group.GroupName &&
                c.Group.Ips.Distinct().OrderBy(ip => ip).SequenceEqual(group.Ips.Distinct().OrderBy(ip => ip))
            );

            if (existing != null)
            {
                _chatList.Remove(existing);       // 기존 위치 제거
                _chatList.Insert(0, existing);    // 맨 위로 이동
            }
            else
            {
                _chatList.Insert(0, new ChatListItem
                {
                    Name = group.GroupName,
                    Ip = null,
                    Group = group
                });
            }
            SaveChatList();
        }

        public static void UpdateChatList(string name, string ip)
        {
            var existing = _chatList.FirstOrDefault(c => c.Ip == ip);
            if (existing != null)
            {
                _chatList.Remove(existing);
                _chatList.Insert(0, existing); // 최근 사용을 위로
            }
            else
            {
                _chatList.Insert(0, new ChatListItem { Name = name, Ip = ip, Group = null });
            }
            SaveChatList();
        }

        #endregion

        public static void RemoveChatListItem(string key)
        {
            var item = _chatList.FirstOrDefault(c => c.UniqueKey == key);
            if (item != null)
                _chatList.Remove(item);

            SaveChatList();
        }

        #region 저장/불러오기

        private static readonly string ChatListSavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatList");

        public static void SaveChatList()
        {
            try
            {
                var dir = Path.GetDirectoryName(ChatListSavePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(_chatList, Formatting.Indented);
                File.WriteAllText(ChatListSavePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // 필요시 로그 출력
            }
        }

        public static void LoadChatList()
        {
            try
            {
                if (!File.Exists(ChatListSavePath)) return;

                var json = File.ReadAllText(ChatListSavePath, Encoding.UTF8);
                var list = JsonConvert.DeserializeObject<ObservableCollection<ChatListItem>>(json);

                _chatList = list ?? new ObservableCollection<ChatListItem>();
            }
            catch
            {
                _chatList = new ObservableCollection<ChatListItem>();
            }
        }

#endregion



    }

    public class ChatListItem
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public GroupEntity Group { get; set; }

        public string DisplayName => Group == null ? $"👤 {Name} ({Ip})" : $"👥 {Name}";

        public string UniqueKey => Group?.GroupName ?? Ip;
    }

}
