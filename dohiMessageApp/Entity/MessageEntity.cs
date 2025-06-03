using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WalkieDohi.Core;
using WalkieDohi.Util;

namespace WalkieDohi.Entity
{
    public class MessageEntity : DohiEntityBase
    {
        public MessageType Type { get; set; } // "text" or "file"
        public string Sender { get; set; }
        public GroupEntity Group { get; set; }

        public string SenderIp { get; set; }
        public string Content { get; set; } // 메시지 내용 또는 파일 Base64 문자열
        public string FileName { get; set; } // 파일 이름 (파일일 경우)

        private ResultType resultType = ResultType.Success;
        public ResultType ResultType { get { return resultType; } }

        MessageEntity() {
            
        }

        public static MessageEntity OfTextMassage(string sender, string senderIp, string content)
        {
            return new MessageEntity
            {
                Type = MessageType.Text,
                Sender = sender,
                SenderIp = senderIp,
                Content = content
            };
        }
        public static MessageEntity OfSendTextMassage(string content)
        {
            return new MessageEntity
            {
                Type = MessageType.Text,
                Sender = MainData.currentUser.Nickname,
                SenderIp = NetworkHelper.GetLocalIPv4(),
                Content = content
            };
        }
        public static MessageEntity OfFileMassage(string sender, string senderIp, string content, string fileName)
        {
            return new MessageEntity
            {
                Type = MessageType.File,
                Sender = sender,
                SenderIp = senderIp,
                Content = content,
                FileName = fileName
            };
        }
        public static MessageEntity OfSendFileMassage( string content, string fileName)
        {
            return new MessageEntity
            {
                Type = MessageType.File,
                Sender = MainData.currentUser.Nickname,
                SenderIp = NetworkHelper.GetLocalIPv4(),
                Content = content,
                FileName = fileName
            };
        }


        public static MessageEntity OfGroupTextMassage(GroupEntity group, string sender, string senderIp, string content)
        {
            return new MessageEntity
            {
                Type = MessageType.Text,
                Group = group,
                Sender = sender,
                SenderIp = senderIp,
                Content = content
            };
        }
        public static MessageEntity OfGroupSendTextMassage(GroupEntity group, string content)
        {
            return new MessageEntity
            {
                Type = MessageType.Text,
                Group = group,
                Sender = MainData.currentUser.Nickname,
                SenderIp = NetworkHelper.GetLocalIPv4(),
                Content = content
            };
        }
        public static MessageEntity OfGroupFileMassage(GroupEntity group, string sender, string senderIp, string content, string fileName)
        {
            return new MessageEntity
            {
                Type = MessageType.File,
                Group = group,
                Sender = sender,
                SenderIp = senderIp,
                Content = content,
                FileName = fileName
            };
        }
        public static MessageEntity OfGroupSendFileMassage(GroupEntity group,string content, string fileName)
        {
            return new MessageEntity
            {
                Type = MessageType.File,
                Group = group,
                Sender = MainData.currentUser.Nickname,
                SenderIp = NetworkHelper.GetLocalIPv4(),
                Content = content,
                FileName = fileName
            };
        }


        public void ResultSetFail()
        {
            resultType = ResultType.Fail;
        }
        public void ResultSetSuccess()
        {
            resultType = ResultType.Success;
        }

        public bool CheckMessageTypeFile()
        {

            return this.Type == MessageType.File;
        }
        public bool CheckMessageTypeText()
        {
            return this.Type == MessageType.Text;
        }
    }










    public enum MessageType
    {
        Text,File
    }
    public enum ResultType
    {
        Success, Fail
    }

    public enum MessageDirection
    {
        Send,
        Receive
    }
}
