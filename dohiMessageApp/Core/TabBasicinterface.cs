using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Entity;

namespace WalkieDohi.Core
{
   interface TabBasicinterface
    {
         event EventHandler<string> OnSendMessage;

        event EventHandler<(string FileName, string Base64Content)> OnSendFile;

        void AddReceivedMessage(MessageEntity msg);

        void AddReceivedFile(MessageEntity msg);

        void SaveMessagesOnClose();

    }
}
