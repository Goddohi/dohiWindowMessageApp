using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.Entity
{
    public class MessageEntity
    {
        public string Type { get; set; } // "text" or "file"
        public string Sender { get; set; }
        public string SenderIp { get; set; }
        public string Content { get; set; } // 메시지 내용 또는 파일 Base64 문자열
        public string FileName { get; set; } // 파일 이름 (파일일 경우)
    }
}
