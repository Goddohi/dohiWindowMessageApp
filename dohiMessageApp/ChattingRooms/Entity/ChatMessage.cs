using Newtonsoft.Json;
using System;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WalkieDohi.ChattingRooms.Views;
using WalkieDohi.Groups.Entity;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.Tcp;

namespace WalkieDohi.ChattingRooms.Entity
{
    public abstract class ChatMessage : INotifyPropertyChanged
    {
        private bool _isFailed;
        private bool _isSending;
        private string _failureText = "전송 실패";
        private string _failureDetail = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public string MessageId { get; set; }
        public string Sender { get; set; }
        public bool IsFailed
        {
            get { return _isFailed; }
            set
            {
                if (_isFailed == value) return;

                _isFailed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FailedVisibility));
            }
        }

        [JsonIgnore]
        public bool IsSending
        {
            get { return _isSending; }
            set
            {
                if (_isSending == value) return;

                _isSending = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SendingVisibility));
            }
        }

        public string FailureText
        {
            get { return string.IsNullOrWhiteSpace(_failureText) ? "전송 실패" : _failureText; }
            set
            {
                var next = string.IsNullOrWhiteSpace(value) ? "전송 실패" : value;
                if (_failureText == next) return;

                _failureText = next;
                OnPropertyChanged();
            }
        }

        public string FailureDetail
        {
            get { return _failureDetail ?? ""; }
            set
            {
                var next = value ?? "";
                if (_failureDetail == next) return;

                _failureDetail = next;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public Visibility FailedVisibility
        {
            get { return IsFailed ? Visibility.Visible : Visibility.Collapsed; }
        }

        [JsonIgnore]
        public Visibility SendingVisibility
        {
            get { return IsSending ? Visibility.Visible : Visibility.Collapsed; }
        }

        public MessageDirection Direction { get; set; }

        [JsonIgnore]
        public bool IsReload { get; set; } = false;

        [JsonIgnore]
        public bool IsSaved { get; set; } = false;

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

            ChatMessage display = null;

            if (msg.CheckMessageTypeText)
                display = new TextMessage(msg.Sender, msg.Content, direction,msg.Timestamp ,msg.SenderIp,msg.Group);

            else if (msg.CheckMessageTypeImage)
                display = new ImageMessage(msg.Sender, msg.FileName, msg.Content, path, direction, msg.Timestamp, msg.SenderIp, msg.Group);

            else if (msg.CheckMessageTypeFile)
                display = new FileMessage(msg.Sender, msg.FileName, path, direction, msg.Timestamp, msg.SenderIp, msg.Group);

            if (display != null)
            {
                display.MessageId = msg.MessageId;
                display.IsFailed = msg.IsFailed;
                display.FailureText = msg.FailureText;
                display.FailureDetail = msg.FailureDetail;
            }

            return display;
        }

        public static ChatMessage CreateSendMessage(string content, string base64, string path, MessageType type,bool isFailed = false, string messageId = null)
        {
            var nextMessageId = string.IsNullOrWhiteSpace(messageId) ? MessageEntity.CreateMessageId() : messageId;
            switch (type)
            {
                case MessageType.Text:
                    return new TextMessage("📤 나", content, MessageDirection.Send, DateTime.Now,NetworkHelper.GetLocalIPv4()) { IsFailed = isFailed, MessageId = nextMessageId };
                case MessageType.Image:
                    return new ImageMessage("📤 나", content, base64, path, MessageDirection.Send, DateTime.Now, NetworkHelper.GetLocalIPv4()) { IsFailed = isFailed, MessageId = nextMessageId };
                case MessageType.File:
                    return new FileMessage("📤 나", content, path, MessageDirection.Send, DateTime.Now, NetworkHelper.GetLocalIPv4()) { IsFailed = isFailed, MessageId = nextMessageId };
                default:
                    return null;
            }
        }

        public void EnsureMessageId()
        {
            if (string.IsNullOrWhiteSpace(MessageId))
            {
                MessageId = MessageEntity.CreateMessageId();
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

        protected string FormatSender(string sender, MessageDirection dir)
        {
            if (this.IsReload)
                return sender;
            return dir == MessageDirection.Send ? "📤 나" : sender;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ApplySendResult(SendResult result)
        {
            if (result == null)
            {
                IsSending = false;
                IsFailed = false;
                FailureText = "";
                FailureDetail = "";
                return;
            }

            IsSending = false;
            IsFailed = result.Failed;
            FailureText = result.FailureText;
            FailureDetail = result.FailureDetail;
        }
    }

    public class TextMessage : ChatMessage
    {
        public string Text { get; }
        public override string DisplayContent => Text;

        public TextMessage(string sender, string text, MessageDirection dir, DateTime timestamp,string ip,GroupEntity group = null, bool isReload = false)
        {
            IsReload = isReload;
            Sender = FormatSender(sender, dir);
            Direction = dir;
            Text = text;
            Timestamp = timestamp;
            Ip = ip;
            NotifyIfReceive(sender, ip, text, dir, group);
        }

        private void NotifyIfReceive(string sender, string ip, string text, MessageDirection dir, GroupEntity group)
        {
            if (this.IsReload)
                return;

            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, ip, text, group).Show();
        }
    }

    public class ImageMessage : ChatMessage
    {
        public string FileName { get; }
        public BitmapImage Image { get; }
        public override string DisplayContent => FileName;


        public ImageMessage(string sender, string fileName, string base64, string path, MessageDirection dir, DateTime timestamp, string ip, GroupEntity group = null, bool isReload = false)
        {
            IsReload = isReload;
            Sender = FormatSender(sender, dir);
            Direction = dir;
            FileName = fileName;
            ContentPath = path;
            Ip = ip;
            Image = MessageImageUtil.LoadImageFromBase64(base64);
            NotifyIfReceive(sender,ip, fileName, dir, group);
        }

        private void NotifyIfReceive(string sender,string ip, string content, MessageDirection dir, GroupEntity group)
        {
            if (this.IsReload)
                return;
            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, ip, content, group).Show();
        }



    }

    public class FileMessage : ChatMessage
    {
        public string FileName { get; }
        public override string DisplayContent => FileName;

        public FileMessage(string sender, string fileName, string path, MessageDirection dir, DateTime timestamp, string ip, GroupEntity group = null, bool isReload = false)
        {
            IsReload = isReload;

            Sender = FormatSender(sender, dir);
            Direction = dir;
            FileName = fileName;
            ContentPath = path;
            Ip = ip;
            NotifyIfReceive(sender, ip, fileName, dir, group);
        }

        private void NotifyIfReceive(string sender,string ip, string content, MessageDirection dir, GroupEntity group)
        {
            if (this.IsReload)
                return;
            if (dir == MessageDirection.Receive)
                new ToastWindow(sender, ip, content, group).Show();
        }
    }
}
