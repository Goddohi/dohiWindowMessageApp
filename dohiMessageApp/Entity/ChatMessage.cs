using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WalkieDohi.UI;

namespace WalkieDohi.Entity
{
    public class ChatMessage
    {
        /// <summary>
        /// 사용자에게만 남는 객체로 TCP와는 상관X 버전마다 변경 자유
        /// </summary>
        
        public string Sender { get; set; }
        public string Content { get; set; } // 텍스트나 이미지 경로
        public BitmapImage ImageData { get; set; } // 이미지 

        public bool IsImage { get; set; }   // 이미지 여부 구분

        public bool IsFailed { get; set; } = false;


        /// <summary>
        /// 해당메서드는 Display메세지를 반환하면서 받은 메세지의 경우 알림을 설정해줍니다.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="Direction"></param>
        /// <returns></returns>
        public static ChatMessage GetMsgDisplay(MessageEntity msg, MessageDirection Direction)
        {
            if (msg == null) return null;
            if (msg.CheckMessageTypeImage()) return GetMsgDisplay(msg.Sender, msg.FileName, msg.Content, msg.Type, Direction, msg.Group);
            if (msg.CheckMessageTypeText()) return GetMsgDisplay(msg.Sender, msg.Content, "", msg.Type, Direction, msg.Group);

            if (msg.CheckMessageTypeFile()) return GetMsgDisplay(msg.Sender, msg.FileName, "", msg.Type, Direction, msg.Group);

            return null;
        }
        public static ChatMessage GetSendMsgDisplay(string content, string baseData, MessageType messageType, MessageDirection Direction, bool result)
        {
            ChatMessage returnChatMessage = new ChatMessage();
            if (Direction == MessageDirection.Send)
            {
                if (messageType == MessageType.Text) returnChatMessage = GetMsgDisplay("📤 나", content, baseData, messageType, Direction);

                if (messageType == MessageType.File) returnChatMessage = GetMsgDisplay("📤 나(파일 전송)", content, "", messageType, Direction);

                if (messageType == MessageType.Image) returnChatMessage = GetMsgDisplay("📤 나", content, baseData, messageType, Direction);

                returnChatMessage.IsFailed = result;
                return returnChatMessage;
            }
            
            return null;
        }
        public static ChatMessage GetSendMsgDisplay(string content, string baseData, MessageType messageType, MessageDirection Direction)
        {
            if (Direction == MessageDirection.Send)
            {
                if (messageType == MessageType.Text) return GetMsgDisplay("📤 나", content, baseData, messageType, Direction);

                if (messageType == MessageType.File) return GetMsgDisplay("📤 나(파일 전송)", content, "", messageType, Direction);

                if (messageType == MessageType.Image) return GetMsgDisplay("📤 나", content, baseData, messageType, Direction);
            }

            return null;
        }
        private static ChatMessage GetMsgDisplay(string sender, string content, string baseData, MessageType messageType, MessageDirection Direction, GroupEntity group = null)
        {
            if (Direction == MessageDirection.Send)
            {
                if (messageType == MessageType.Text) return new ChatMessage { Sender = "📤 나", Content = content, IsImage = false };

                if (messageType == MessageType.File) return new ChatMessage { Sender = "📤 나(파일 전송)", Content = content, IsImage = false };

                if (messageType == MessageType.Image) return new ChatMessage { Sender = "📤 나", Content = content, ImageData = CreateBitmapImageFromBase64(baseData), IsImage = true };
            }
            if (Direction == MessageDirection.Receive)
            {
                 new ToastWindow(sender, content, group).Show();//싱글도 호환

                if (messageType == MessageType.Image) return new ChatMessage { Sender = sender, Content = content, ImageData = CreateBitmapImageFromBase64(baseData), IsImage = true };

                if (messageType == MessageType.Text) return new ChatMessage { Sender = sender, Content = content, IsImage = false };

                if (messageType == MessageType.File) return new ChatMessage { Sender = $"📥{sender}(파일 받음)", Content = content, IsImage = false };
            }
            return null;
        }

        private static BitmapImage CreateBitmapImageFromBase64(string base64)
        {
            byte[] binaryData = System.Convert.FromBase64String(base64);
            using (var stream = new MemoryStream(binaryData))
            {
                var image = new BitmapImage();
                stream.Position = 0;
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
    }
}
