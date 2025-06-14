using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Entity;

namespace WalkieDohi.Util.Tcp
{
    public class PacketSender
    {
        public async Task SendPacketAsync(string ip, int port, PacketEntity packet)
        {
            try
            {
                string json = JsonConvert.SerializeObject(packet);
                byte[] body = Encoding.UTF8.GetBytes(json);
                byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ip, port);
                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(length, 0, 4);
                        await stream.WriteAsync(body, 0, body.Length);
                        await stream.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[패킷 전송 실패] {ex.Message}");
            }
        }
    }

}
