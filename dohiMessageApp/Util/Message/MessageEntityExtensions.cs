using WalkieDohi.Entity;

namespace WalkieDohi.Util
{
    public static class MessageEntityExtensions
    {
        public static ChatMessage ToChatMessage(this MessageEntity msg)
        {
            if (msg == null) return null;

            // IP 기준으로 방향 추론
            MessageDirection direction;

            if (msg.SenderIp == null) //이전내역받기는 ip안불러옴
            { 
                direction = MessageDirection.ReLoad; 
            }
            else if (msg.SenderIp == NetworkHelper.GetLocalIPv4())
            {
                direction = MessageDirection.Send;
            }
            else
            { 
                direction = MessageDirection.Receive; 
            }


            if (msg.CheckMessageTypeText())
            {
                return new TextMessage(msg.Sender, msg.Content, direction,msg.Timestamp ,msg.Group);
            }

            if (msg.CheckMessageTypeImage())
            {
                return new ImageMessage(msg.Sender, msg.FileName, msg.Content, direction, msg.Timestamp, msg.Group);
            }

            if (msg.CheckMessageTypeFile())
            {
                return new FileMessage(msg.Sender, msg.FileName, direction, msg.Timestamp, msg.Group);
            }

            return null;
        }
    }
}
