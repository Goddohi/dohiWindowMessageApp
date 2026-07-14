using System;
using System.IO;
using System.Windows.Media.Imaging;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util;

namespace WalkieDohi.Util
{
    public static class ChatMessageExtensions
    {
        public static MessageEntity ToEntity(this ChatMessage msg)
        {
            msg.EnsureMessageId();

            var entity = new MessageEntity
            {
                MessageId = msg.MessageId,
                Sender = msg.Sender,
                IsFailed = msg.IsFailed,
                FailureText = msg.IsFailed ? msg.FailureText : "",
                FailureDetail = msg.IsFailed ? msg.FailureDetail : "",
                SenderIp = msg.Ip,
                Timestamp = msg.Timestamp     
            };

            if (msg is TextMessage t)
            {
                entity.Type = MessageType.Text;
                entity.Content = t.Text;
                entity.ContentPath = t.ContentPath;
            }
            else if (msg is ImageMessage i)
            {
                entity.Type = MessageType.Image;
                entity.FileName = i.FileName;
                entity.Content = i.Image != null ? Convert.ToBase64String(ImageToBytes(i.Image)) : "";
                entity.ContentPath = i.ContentPath;
            }
            else if (msg is FileMessage f)
            {
                entity.Type = MessageType.File;
                entity.FileName = f.FileName;
                entity.Content = ""; // 파일은 에바데스
                entity.ContentPath = f.ContentPath;
            }

            return entity;
        }

        private static byte[] ImageToBytes(BitmapImage image)
        {
            if (image == null) return new byte[0];

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }
    }
}
