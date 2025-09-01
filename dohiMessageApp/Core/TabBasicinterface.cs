using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Packet.Messages.Entity;

namespace WalkieDohi.Core
{
    public interface TabBasicinterface
    {
        event EventHandler<string> OnSendMessage;

        event EventHandler<(string FileName, string Base64Content)> OnSendFile;

        void AddReceivedMessage(MessageEntity msg);

        void AddReceivedFile(MessageEntity msg);


    }
}
