using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.UI;

namespace WalkieDohi.Entity
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Content { get; set; } // 텍스트나 이미지 경로
        public bool IsImage { get; set; }   // 이미지 여부 구분


        /// <summary>
        /// 해당메서드는 Display메세지를 반환하면서 받은 메세지의 경우 알림을 설정해줍니다.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="Direction"></param>
        /// <returns></returns>
        public static ChatMessage GetMsgDisplay(MessageEntity msg, MessageDirection Direction)
        {
            if (msg == null) return null;
            if (msg.CheckMessageTypeText()) return GetMsgDisplay(msg.Sender, msg.Content, msg.Type, Direction);

            if (msg.CheckMessageTypeFile()) return GetMsgDisplay(msg.Sender, msg.FileName, msg.Type, Direction);

            return null;
        }
        public static ChatMessage GetMsgDisplay(string sender, string content, MessageType messageType, MessageDirection Direction)
        {
            if (Direction == MessageDirection.Send)
            {
                if (messageType == MessageType.Text) return new ChatMessage{ Sender="📤 나", Content=content};

                if (messageType == MessageType.File) return new ChatMessage{ Sender = "📤 나(파일 전송)", Content = content };

            }
            if (Direction == MessageDirection.Receive)
            {
                new ToastWindow($"📨 {sender}님이 보냄", content).Show();

                if (messageType == MessageType.Text) return new ChatMessage{ Sender = sender, Content = content }; 

                if (messageType == MessageType.File) return new ChatMessage { Sender = $"📥{sender}(파일 받음)", Content = content }; 
            }
            return null;
        }
    }
}
