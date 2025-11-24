using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Packet.Entity;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util.Tcp;

namespace WalkieDohi.Util
{
    public class MessengerSender
    {
        private readonly PacketSender _packetSender = new PacketSender();

        public async Task SendMessageAsync(string ip, MessageEntity message)
        {

            var packet = PacketEntity.FromObject(PacketType.Message, message,ip);
            try
            {
                await _packetSender.SendPacketAsync(ip, MainData.GetPort(), packet);
            }
            catch (Exception)
            {
                message.ResultSetFail();
            }
        }
    }

}
