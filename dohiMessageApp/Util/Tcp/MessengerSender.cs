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

        public async Task<SendResult> SendMessageAsync(string ip, MessageEntity message)
        {
            try
            {
                if (message == null)
                {
                    return SendResult.Fail(ip, "전송할 메시지가 없습니다.");
                }

                message.EnsureMessageId();
                var packet = PacketEntity.FromObject(PacketType.Message, message, ip);
                var result = await _packetSender.SendPacketAsync(ip, MainData.GetPort(), packet);

                if (result.Succeeded)
                {
                    message.ResultSetSuccess();
                }
                else
                {
                    message.ResultSetFail();
                }

                return result;
            }
            catch (Exception ex)
            {
                message?.ResultSetFail();
                return SendResult.Fail(ip, ex.Message);
            }
        }

        public async Task<PacketRequestResult<UserProfileEntity>> RequestUserProfileAsync(string ip)
        {
            string normalizedIp;
            if (!NetworkHelper.TryNormalizeIPv4(ip, out normalizedIp))
            {
                return PacketRequestResult<UserProfileEntity>.Fail(ip, "올바른 IP 주소가 아닙니다.");
            }

            var packet = PacketEntity.FromObject(PacketType.ProfileRequest, new { RequestedAt = DateTime.Now }, normalizedIp);
            return await _packetSender.SendPacketAndReadResponseAsync<UserProfileEntity>(
                normalizedIp,
                MainData.GetPort(),
                packet,
                PacketType.ProfileResponse,
                TimeSpan.FromSeconds(3));
        }
    }

}
