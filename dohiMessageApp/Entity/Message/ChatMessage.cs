using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WalkieDohi.UI;
using WalkieDohi.Util;

namespace WalkieDohi.Entity
{
    public abstract class ChatMessage
    {
        public string Sender { get; set; }
        public bool IsFailed { get; set; } = false;
        public MessageDirection Direction { get; set; }

        [JsonIgnore]
        public bool IsReload { get; set; } = false;

        /// <summary>
        /// 메세지별 보여줄 컨텐츠
        /// </summary>
        public abstract string DisplayContent { get; }
        public string ContentPath { get; set; }

        public string Ip { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public static ChatMessage CreateFromEntity(MessageEntity msg, string path, MessageDirection direction = MessageDirection.Receive)
        {
            if (msg == null) return null;

            if (msg.CheckMessageTypeText)
                return new TextMessage(msg.Sender, msg.Content, direction,msg.Timestamp ,msg.SenderIp,msg.Group);

            if (msg.CheckMessageTypeImage)
                return new ImageMessage(msg.Sender, msg.FileName, msg.Content, path, direction, msg.Timestamp, msg.SenderIp, msg.Group);

            if (msg.CheckMessageTypeFile)
                return new FileMessage(msg.Sender, msg.FileName, path, direction, msg.Timestamp, msg.SenderIp, msg.Group);

            return null;
        }

        public static ChatMessage CreateSendMessage(string content, string base64, string path, MessageType type,bool isFailed = false)
        {
            switch (type)
            {
                case MessageType.Text:
                    return new TextMessage("📤 나", content, MessageDirection.Send, DateTime.Now,NetworkHelper.GetLocalIPv4()) { IsFailed = isFailed };
                case MessageType.Image:
                    return new ImageMessage("📤 나", content, base64, path, MessageDirection.Send, DateTime.Now, NetworkHelper.GetLocalIPv4()) { IsFailed = isFailed };
                case MessageType.File:
                    return new FileMessage("📤 나", content, path, MessageDirection.Send, DateTime.Now, NetworkHelper.GetLocalIPv4()) { IsFailed = isFailed };
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

        public TextMessage(string sender, string text, MessageDirection dir, DateTime timestamp,string ip,GroupEntity group = null, bool isReload = false)
        {
            Sender = FormatSender(sender, dir);
            Direction = dir;
            Text = text;
            Timestamp = timestamp;
            Ip = ip;
            IsReload = isReload;
            NotifyIfReceive(sender, ip, text, dir, group);
        }

        private void NotifyIfReceive(string sender, string ip, string text, MessageDirection dir, GroupEntity group)
        {
            if (this.IsReload)
                return;

            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, ip, text, group).Show();
        }

        private string FormatSender(string sender, MessageDirection dir)
        {
            if (this.IsReload)
                return sender;
            return dir == MessageDirection.Send ? "📤 나" : sender;
        }
    }

    public class ImageMessage : ChatMessage
    {
        public string FileName { get; }
        public BitmapImage Image { get; }
        public override string DisplayContent => FileName;


        public ImageMessage(string sender, string fileName, string base64, string path, MessageDirection dir, DateTime timestamp, string ip, GroupEntity group = null, bool isReload = false)
        {
            Sender = FormatSender(sender, dir);
            Direction = dir;
            FileName = fileName;
            ContentPath = path;
            Ip = ip;
            Image = MessageImageUtil.LoadImageFromBase64(base64);
            IsReload = isReload;
            NotifyIfReceive(sender,ip, fileName, dir, group);
        }

        private void NotifyIfReceive(string sender,string ip, string content, MessageDirection dir, GroupEntity group)
        {
            if (this.IsReload)
                return;
            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, ip, content, group).Show();
        }

        private string FormatSender(string sender, MessageDirection dir)
        {
            if (this.IsReload)
                return sender;
            return dir == MessageDirection.Send ? "📤 나" : sender;
        }

    }

    public class FileMessage : ChatMessage
    {
        public string FileName { get; }
        public override string DisplayContent => FileName;

        public FileMessage(string sender, string fileName, string path, MessageDirection dir, DateTime timestamp, string ip, GroupEntity group = null, bool isReload = false)
        {
            Sender = FormatSender(sender, dir);
            Direction = dir;
            FileName = fileName;
            ContentPath = path;
            Ip = ip;
            IsReload = isReload;
            NotifyIfReceive(sender, ip, fileName, dir, group);
        }

        private void NotifyIfReceive(string sender,string ip, string content, MessageDirection dir, GroupEntity group)
        {
            if (this.IsReload)
                return;
            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, ip, content, group).Show();
        }

        private string FormatSender(string sender, MessageDirection dir)
        {
            if (this.IsReload)
                return sender;
            return dir == MessageDirection.Receive ? $"📥{sender}" : sender;
        }
    }
}
