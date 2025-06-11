using System;
using System.IO;
using System.Windows.Media.Imaging;
using WalkieDohi.UI;

namespace WalkieDohi.Entity
{
    public abstract class ChatMessage
    {
        public string Sender { get; set; }
        public bool IsFailed { get; set; } = false;
        public MessageDirection Direction { get; set; }
        public abstract string DisplayContent { get; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public static ChatMessage CreateFromEntity(MessageEntity msg, MessageDirection direction = MessageDirection.Receive)
        {
            if (msg == null) return null;

            if (msg.CheckMessageTypeText())
                return new TextMessage(msg.Sender, msg.Content, direction,msg.Timestamp ,msg.Group);

            if (msg.CheckMessageTypeImage())
                return new ImageMessage(msg.Sender, msg.FileName, msg.Content, direction, msg.Timestamp, msg.Group);

            if (msg.CheckMessageTypeFile())
                return new FileMessage(msg.Sender, msg.FileName, direction, msg.Timestamp, msg.Group);

            return null;
        }

        public static ChatMessage CreateSendMessage(string content, string base64, MessageType type, bool isFailed = false)
        {
            switch (type)
            {
                case MessageType.Text:
                    return new TextMessage("📤 나", content, MessageDirection.Send, DateTime.Now) { IsFailed = isFailed };
                case MessageType.Image:
                    return new ImageMessage("📤 나", content, base64, MessageDirection.Send, DateTime.Now) { IsFailed = isFailed };
                case MessageType.File:
                    return new FileMessage("📤 나", content, MessageDirection.Send, DateTime.Now) { IsFailed = isFailed };
                default:
                    return null;
            }
        }

        public bool isDirectionSend()
        {
            return MessageDirection.Send.Equals(Direction);
        }
        public bool isDirectionReceive()
        {
            return MessageDirection.Receive.Equals(Direction);
        }
    }

    public class TextMessage : ChatMessage
    {
        public string Text { get; }
        public override string DisplayContent => Text;

        public TextMessage(string sender, string text, MessageDirection dir, DateTime timestamp, GroupEntity group = null)
        {
            Sender = FormatSender(sender, dir);
            Direction = dir;
            Text = text;
            Timestamp = timestamp;
            NotifyIfReceive(sender, text, dir, group);
        }

        private void NotifyIfReceive(string sender, string text, MessageDirection dir, GroupEntity group)
        {
            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, text, group).Show();
        }

        private string FormatSender(string sender, MessageDirection dir)
        {
            return dir == MessageDirection.Send ? "📤 나" : sender;
        }
    }

    public class ImageMessage : ChatMessage
    {
        public string FileName { get; }
        public BitmapImage Image { get; }
        public override string DisplayContent => FileName;

        public ImageMessage(string sender, string fileName, string base64, MessageDirection dir, DateTime timestamp, GroupEntity group = null)
        {
            Sender = FormatSender(sender, dir);
            Direction = dir;
            FileName = fileName;
            Image = CreateBitmapImageFromBase64(base64);
            NotifyIfReceive(sender, fileName, dir, group);
        }

        private void NotifyIfReceive(string sender, string content, MessageDirection dir, GroupEntity group)
        {
            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, content, group).Show();
        }

        private string FormatSender(string sender, MessageDirection dir)
        {
            return dir == MessageDirection.Send ? "📤 나" : sender;
        }

        private static BitmapImage CreateBitmapImageFromBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            byte[] binaryData = Convert.FromBase64String(base64);
            using (var stream = new MemoryStream(binaryData))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
    }

    public class FileMessage : ChatMessage
    {
        public string FileName { get; }
        public override string DisplayContent => FileName;

        public FileMessage(string sender, string fileName, MessageDirection dir, DateTime timestamp, GroupEntity group = null)
        {
            Sender = FormatSender(sender, dir);
            Direction = dir;
            FileName = fileName;
            NotifyIfReceive(sender, fileName, dir, group);
        }

        private void NotifyIfReceive(string sender, string content, MessageDirection dir, GroupEntity group)
        {
            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, content, group).Show();
        }

        private string FormatSender(string sender, MessageDirection dir)
        {
            return dir == MessageDirection.Receive ? $"📥{sender}" : sender;
        }
    }
}
