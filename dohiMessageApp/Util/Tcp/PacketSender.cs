using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
                using (var client = new TcpClient())
                {
                    await ConnectAsync(client, ip, port, ConnectTimeout);
                    using (var stream = client.GetStream())
                    {
                        await WritePacketAsync(stream, packet);
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

        public async Task<PacketRequestResult<T>> SendPacketAndReadResponseAsync<T>(string ip, int port, PacketEntity packet, PacketType expectedResponseType, TimeSpan responseTimeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await ConnectAsync(client, ip, port, ConnectTimeout);
                    using (var stream = client.GetStream())
                    {
                        await WritePacketAsync(stream, packet);

                        PacketEntity response = await ReadPacketAsync(stream, responseTimeout);
                        if (response == null || response.Type != expectedResponseType)
                        {
                            return PacketRequestResult<T>.Fail(ip, "응답 패킷이 올바르지 않습니다.");
                        }

                        return PacketRequestResult<T>.Success(ip, PacketEntity.ToObject<T>(response));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[패킷 요청 실패] {ex.Message}");
                return PacketRequestResult<T>.Fail(ip, ex.Message);
            }
        }

        private static async Task ConnectAsync(TcpClient client, string ip, int port, TimeSpan timeout)
        {
            var connectTask = client.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                client.Close();
                throw new TimeoutException($"연결 시간이 초과되었습니다. ({timeout.TotalSeconds:0}초)");
            }

            await connectTask;
        }

        private static async Task WritePacketAsync(NetworkStream stream, PacketEntity packet)
        {
            string json = JsonConvert.SerializeObject(packet);
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));

            await stream.WriteAsync(length, 0, 4);
            await stream.WriteAsync(body, 0, body.Length);
            await stream.FlushAsync();
        }

        private static async Task<PacketEntity> ReadPacketAsync(NetworkStream stream, TimeSpan timeout)
        {
            byte[] lengthBytes = await ReadExactAsync(stream, 4, timeout);
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

            if (length <= 0)
            {
                throw new InvalidOperationException("응답 길이가 올바르지 않습니다.");
            }

            byte[] bodyBytes = await ReadExactAsync(stream, length, timeout);
            string json = Encoding.UTF8.GetString(bodyBytes);
            return JsonConvert.DeserializeObject<PacketEntity>(json);
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length, TimeSpan timeout)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                var readTask = stream.ReadAsync(buffer, offset, length - offset);
                var timeoutTask = Task.Delay(timeout);
                var completedTask = await Task.WhenAny(readTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"응답 시간이 초과되었습니다. ({timeout.TotalSeconds:0}초)");
                }

                int bytesRead = await readTask;
                if (bytesRead == 0)
                {
                    throw new IOException("연결이 끊겼습니다.");
                }

                offset += bytesRead;
            }

            return buffer;
        }
    }

    public class PacketRequestResult<T>
    {
        public bool Succeeded { get; private set; }
        public string Ip { get; private set; }
        public string ErrorMessage { get; private set; }
        public T Payload { get; private set; }

        public static PacketRequestResult<T> Success(string ip, T payload)
        {
            return new PacketRequestResult<T>
            {
                Succeeded = true,
                Ip = ip,
                Payload = payload
            };
        }

        public static PacketRequestResult<T> Fail(string ip, string errorMessage)
        {
            return new PacketRequestResult<T>
            {
                Succeeded = false,
                Ip = ip,
                ErrorMessage = errorMessage
            };
        }
    }
}
