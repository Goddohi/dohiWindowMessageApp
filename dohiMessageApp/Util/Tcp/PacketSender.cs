using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.ChattingRooms.Entity;
using WalkieDohi.Packet.Entity;

namespace WalkieDohi.Util.Tcp
{
    public class PacketSender
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

        public async Task<SendResult> SendPacketAsync(string ip, int port, PacketEntity packet)
        {
            try
            {
                string json = JsonConvert.SerializeObject(packet);
                byte[] body = Encoding.UTF8.GetBytes(json);
                byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));

                using (var client = new TcpClient())
                {
                    await ConnectAsync(client, ip, port);
                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(length, 0, 4);
                        await stream.WriteAsync(body, 0, body.Length);
                        await stream.FlushAsync();
                    }
                }

                return SendResult.Success(ip);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[패킷 전송 실패] {ex.Message}");
                packet?.SendFailPacket();
                return SendResult.Fail(ip, ex.Message);
            }
        }

        private static async Task ConnectAsync(TcpClient client, string ip, int port)
        {
            var connectTask = client.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(ConnectTimeout);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                client.Close();
                throw new TimeoutException($"연결 시간이 초과되었습니다. ({ConnectTimeout.TotalSeconds:0}초)");
            }

            await connectTask;
        }
    }

}
