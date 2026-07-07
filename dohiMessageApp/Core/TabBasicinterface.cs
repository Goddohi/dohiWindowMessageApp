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
    public delegate Task<SendResult> SendMessageRequestedEventHandler(object sender, string text);

    public delegate Task<SendResult> SendFileRequestedEventHandler(object sender, (string FileName, string Base64Content) fileInfo);

    public interface TabBasicinterface
    {
        event SendMessageRequestedEventHandler OnSendMessage;

        event SendFileRequestedEventHandler OnSendFile;

        void AddReceivedMessage(MessageEntity msg);

        void AddReceivedFile(MessageEntity msg);


    }
}
