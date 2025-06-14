using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.Entity
{
    public class PacketEntity
    {
        public PacketType Type { get; set; } 
        public string Data { get; set; }
        public static PacketEntity FromObject<T>(PacketType type, T payload)
        {
            return new PacketEntity
            {
                Type = type,
                Data = JsonConvert.SerializeObject(payload)
            };
        }

        public static T ToObject<T>(PacketEntity packet)
        {
            return JsonConvert.DeserializeObject<T>(packet.Data);
        }

    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PacketType
    {
        Message
    }




}
