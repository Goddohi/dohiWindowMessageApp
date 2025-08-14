using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using WalkieDohi.Entity;
using WalkieDohi.Util.IO;

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
                c.Group.Key == group.Key 
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
                _chatList.Insert(0, existing);
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

        public static void ChangeNameChatListItem(string key,string GroupName)
        {
            var item = _chatList.FirstOrDefault(c => c.UniqueKey == key);
            if (item != null)
            {
                if(item.Group != null)
                {
                    item.Group.GroupName = GroupName;
                    item.Name = GroupName;
                }
            }

            SaveChatList();
        }

        public static void DeleteChatLog(string key)
        {
            // 저장된 채팅 파일 삭제
            try
            {
                //string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatLogs", key);

                string dir = Path.Combine(DirectoryManager.GetAppDataDirectoryCombineFileName("ChatLogs"), key);
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true); // 하위 파일 포함 삭제
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("채팅 로그 폴더 삭제 실패: " + ex.Message);
            }
        }


        #region 저장/불러오기

        private static readonly string ChatListSavePath = DirectoryManager.GetAppDataDirectoryCombineFileName("ChatList.json");

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

        public string UniqueKey => Group?.Key ?? Ip;
    }

}
