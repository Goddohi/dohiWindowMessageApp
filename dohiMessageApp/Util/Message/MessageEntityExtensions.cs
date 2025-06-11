using WalkieDohi.Entity;

namespace WalkieDohi.Util
{
    public static class MessageEntityExtensions
    {
        public static ChatMessage ToChatMessage(this MessageEntity msg)
        {
            if (msg == null) return null;

            // IP 기준으로 방향 추론
            var direction = msg.SenderIp == NetworkHelper.GetLocalIPv4()
                ? MessageDirection.Send
                : MessageDirection.Receive;

            if (msg.CheckMessageTypeText())
            {
                return new TextMessage(msg.Sender, msg.Content, direction, msg.Group);
            }

            if (msg.CheckMessageTypeImage())
            {
                return new ImageMessage(msg.Sender, msg.FileName, msg.Content, direction, msg.Group);
            }

            if (msg.CheckMessageTypeFile())
            {
                return new FileMessage(msg.Sender, msg.FileName, direction, msg.Group);
            }

            return null;
        }
    }
}
