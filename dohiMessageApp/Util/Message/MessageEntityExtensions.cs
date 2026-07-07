using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Packet.Messages.Entity;

namespace WalkieDohi.Util
{
    public static class MessageEntityExtensions
    {
        public static ChatMessage ToChatMessage(this MessageEntity msg,bool IsReload = false)
        {
            if (msg == null) return null;

            // IP 기준으로 방향 추론
            MessageDirection direction;

            if (msg.SenderIp == NetworkHelper.GetLocalIPv4())
            {
                direction = MessageDirection.Send;
            }
            else
            {
                direction = MessageDirection.Receive;
                /* 기존 사용자 보호 정책 추가 */
                if (string.IsNullOrWhiteSpace(msg.SenderIp))
                {
                    if (string.Equals(msg.Sender, "📤 나"))
                        direction = MessageDirection.Send;

                }
            }


            ChatMessage display = null;

            if (msg.CheckMessageTypeText)
            {
                display = new TextMessage(msg.Sender, msg.Content, direction, msg.Timestamp , msg.SenderIp, msg.Group,IsReload);
            }
            else if (msg.CheckMessageTypeImage)
            {
                display = new ImageMessage(msg.Sender, msg.FileName, msg.Content, msg.ContentPath, direction, msg.Timestamp, msg.SenderIp, msg.Group, IsReload);
            }
            else if (msg.CheckMessageTypeFile)
            {
                display = new FileMessage(msg.Sender, msg.FileName,msg.ContentPath, direction, msg.Timestamp, msg.SenderIp, msg.Group, IsReload);
            }

            if (display != null)
            {
                display.IsFailed = msg.IsFailed;
                display.FailureText = msg.FailureText;
                display.FailureDetail = msg.FailureDetail;
            }

            return display;
        }
    }
}
