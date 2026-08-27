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
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Groups.Entity;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util.IO;

namespace WalkieDohi.ChattingRooms.Data
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

        #region 리스트 삭제 로직
        public static void RemoveChatListItem(string key)
        {
            var item = _chatList.FirstOrDefault(c => c.UniqueKey == key);
            if (item != null)
                _chatList.Remove(item);

            SaveChatList();
        }
        #endregion

        #region 리스트 이름 변경 로직 (시험삼아 그룹만)
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
        #endregion

        #region 채팅 내역만 삭제 로직 
        public static void DeleteChatLog(string key)
        {
            try
            {
                ChatLogStore.DeleteRoom(key);
            }
            catch (Exception ex)
            {
                MessageBox.Show("채팅 DB 로그 삭제 실패: " + ex.Message);
            }
        }
        #endregion

        #region 저장/불러오기

        private static readonly string ChatListSavePath = DirectoryManager.GetAppDataDirectoryCombineFileName("ChatList.json");

        private static string DohifilePath =>
            DirectoryManager.GetAppDataDirectoryCombineFileName("ChatList.dohi");


        private static string JsonFilePath =>
            DirectoryManager.GetAppDataDirectoryCombineFileName("ChatList.json");
        private static string ActualFilePath
        {
            get
            {
                if (File.Exists(DohifilePath))
                    return DohifilePath;
                if (File.Exists(JsonFilePath))
                    return JsonFilePath;
                return JsonFilePath;
            }
        }


        public static void SaveChatList()
        {
            try
            {
                var dir = Path.GetDirectoryName(DohifilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(_chatList, Formatting.Indented);
                File.WriteAllText(DohifilePath, json, Encoding.UTF8);
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
                if (!File.Exists(ActualFilePath)) return;

                var json = File.ReadAllText(ActualFilePath, Encoding.UTF8);
                var list = JsonConvert.DeserializeObject<ObservableCollection<ChatListItem>>(json);

                _chatList = list ?? new ObservableCollection<ChatListItem>();
            }
            catch
            {
                _chatList = new ObservableCollection<ChatListItem>();
            }
        }



        #endregion
    
        
        #region 채팅방이름 불러오는 로직  (개인화로인한 추가)
        public static string GetNameChatListByKey(string key)
        {
            
            var item = _chatList.FirstOrDefault(c => c.UniqueKey == key);
            if (item != null)
            {
                if (item.Group == null)
                    return item.Name;

                if (item.Group != null)
                    return item.Group.GroupName;

            }

            return "";
        }
        #endregion


    }

    public class ChatListItem
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public GroupEntity Group { get; set; }

        [JsonIgnore]
        public bool IsGroup => Group != null;

        [JsonIgnore]
        public string RoomName => Group == null ? Name : Group.GroupName;

        [JsonIgnore]
        public string RoomSummary
        {
            get
            {
                if (Group == null)
                {
                    return Ip;
                }

                int memberCount = Group.Ips?.Count ?? 0;
                return $"{memberCount}명 참여";
            }
        }

        [JsonIgnore]
        public string ChatIconGlyph => IsGroup ? "\uE716" : "\uE77B";

        [JsonIgnore]
        public string DisplayName => Group == null ? $"{Name} ({Ip})" : Name;

        public string UniqueKey => Group?.Key ?? Ip;
    }

}
