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
    /// <summary>
    /// 메세지 수신 송신용 TCP로 전송되는 양식으로 가능한 필드 변경자체(추가는 해도 되나 기존 내용은 삭제자제)
    /// </summary>
    public class MessageEntity : DohiEntityBase
    {
        public MessageType Type { get; set; }
        public string Sender { get; set; }
        public GroupEntity Group { get; set; }

        /// <summary>
        /// 수신인은 탭에서 관리하므로 Receiver가 필요없음.
        /// </summary>
        public string SenderIp { get; set; }
        public string Content { get; set; } // 메시지 내용 또는 파일 Base64 문자열
        public string FileName { get; set; } // 파일 이름 (파일일 경우)
        public bool IsFailed { get; set; } = false;




        public MessageEntity() {}

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
            IsFailed = true;
        }
        public void ResultSetSuccess()
        {
            IsFailed = false;
        }



        public bool CheckMessageTypeFile()
        {

            return this.Type == MessageType.File;
        }
        public bool CheckMessageTypeText()
        {
            return this.Type == MessageType.Text;
        }
        public bool CheckMessageTypeImage()
        {
            return this.Type == MessageType.Image;
        }
    }










    public enum MessageType
    {
        Text,File, Image
    }

    public enum MessageDirection
    {
        Send,
        Receive,
        ReLoad
    }
}
