
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WalkieDohi.Entity;

namespace WalkieDohi.Util
{
    public static class TempChatStorageHelper
    {
        private static readonly string TempChatFilePath = Path.Combine(Path.GetTempPath(), "walkiedohi_temp_chat.json");

        public static void SaveOldMessage(ChatMessage msg)
        {
            try
            {
                var list = new List<ChatMessage>();
                if (File.Exists(TempChatFilePath))
                {
                    string json = File.ReadAllText(TempChatFilePath);
                    list = JsonConvert.DeserializeObject<List<ChatMessage>>(json) ?? new List<ChatMessage>();
                }

                list.Add(msg);
                File.WriteAllText(TempChatFilePath, JsonConvert.SerializeObject(list));
            }
            catch (Exception ex)
            {
                Console.WriteLine("임시 채팅 저장 실패: " + ex.Message);
            }
        }

        public static void DeleteTempFile()
        {
            try
            {
                if (File.Exists(TempChatFilePath))
                {
                    File.Delete(TempChatFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("임시 채팅 파일 삭제 실패: " + ex.Message);
            }
        }

        public static void CleanUpOnStartup()
        {
            DeleteTempFile();
        }
    }
}
