using System;
using System.IO;
using System.Windows.Media.Imaging;
using WalkieDohi.Entity;
using WalkieDohi.Util;

namespace WalkieDohi.Util
{
    public static class ChatMessageExtensions
    {
        public static MessageEntity ToEntity(this ChatMessage msg)
        {
            var entity = new MessageEntity
            {
                Sender = msg.Sender,
                IsFailed = msg.IsFailed,
                Group = null // 필요 시 msg.Group에서 복원 가능
            };

            if (msg is TextMessage t)
            {
                entity.Type = MessageType.Text;
                entity.Content = t.Text;
            }
            else if (msg is ImageMessage i)
            {
                entity.Type = MessageType.Image;
                entity.FileName = i.FileName;
                entity.Content = i.Image != null ? Convert.ToBase64String(ImageToBytes(i.Image)) : "";
            }
            else if (msg is FileMessage f)
            {
                entity.Type = MessageType.File;
                entity.FileName = f.FileName;
                entity.Content = ""; // FileMessage에는 Base64 없음
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
