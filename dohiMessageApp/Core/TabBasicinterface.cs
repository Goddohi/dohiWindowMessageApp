using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util.Tcp;

namespace WalkieDohi.Core
{
    public class MessageSendRequest
    {
        public string MessageId { get; private set; }
        public string Text { get; private set; }

        public MessageSendRequest(string messageId, string text)
        {
            MessageId = messageId;
            Text = text;
        }
    }

    public class FileSendRequest
    {
        public string MessageId { get; private set; }
        public string FileName { get; private set; }
        public string Base64Content { get; private set; }

        public FileSendRequest(string messageId, string fileName, string base64Content)
        {
            MessageId = messageId;
            FileName = fileName;
            Base64Content = base64Content;
        }
    }

    public delegate Task<SendResult> SendMessageRequestedEventHandler(object sender, MessageSendRequest request);

    public delegate Task<SendResult> SendFileRequestedEventHandler(object sender, FileSendRequest request);

    public interface TabBasicinterface
    {
        event SendMessageRequestedEventHandler OnSendMessage;

        event SendFileRequestedEventHandler OnSendFile;

        void AddReceivedMessage(MessageEntity msg, bool saveImmediately = true);

        void AddReceivedFile(MessageEntity msg, bool saveImmediately = true);


    }
}
