using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util;

namespace WalkieDohi.Packet.Entity
{
    public class PacketEntity
    {
        public PacketType Type { get; set; } 
        public string Data { get; set; }
        public string MyIp { get; set; }
        public string UserUUID { get; set; }
        public static PacketEntity FromObject<T>(PacketType type, T payload,string ip)
        {
            return new PacketEntity
            {
                UserUUID = MainData.currentUser.UserUuid,
                MyIp = NetworkHelper.GetOutboundIPv4ForTarget(ip),
                Type = type,
                Data = JsonConvert.SerializeObject(payload)
            };
        }

        public static T ToObject<T>(PacketEntity packet)
        {
            return JsonConvert.DeserializeObject<T>(packet.Data);
        }

        public void SendFailPacket()
        {
            if (this.Type == PacketType.Message)
            {
                MessageEntity message = ToObject<MessageEntity>(this);
                message.ResultSetFail();
                this.Data = JsonConvert.SerializeObject(message);
            }

        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PacketType
    {
        Message
    }
    



}
