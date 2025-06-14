using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalkieDohi.Entity;

namespace WalkieDohi.Util.Tcp
{
    public class PacketReceiver
    {
        private readonly int port;
        private TcpListener listener;
        private bool isRunning = false;

        public event Action<MessageEntity> OnMessageReceived;

        public PacketReceiver(int port)
        {
            this.port = port;
        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;
            Task.Run(() => ListenLoop());
        }

        public void Stop()
        {
            isRunning = false;
            listener.Stop();
        }

        private async Task ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[수신 오류] {ex.Message}");
                }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    byte[] lengthBytes = await ReadExactAsync(stream, 4);
                    int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

                    byte[] bodyBytes = await ReadExactAsync(stream, length);
                    string json = Encoding.UTF8.GetString(bodyBytes);

                    var packet = JsonConvert.DeserializeObject<PacketEntity>(json);
                    switch (packet?.Type)
                    {
                        case PacketType.Message:
                            var message = JsonConvert.DeserializeObject<MessageEntity>(packet.Data);
                            OnMessageReceived?.Invoke(message);
                            break;

                        default:
                            Console.WriteLine($"알 수 없는 패킷 타입: {packet?.Type}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[클라이언트 처리 오류] {ex.Message}");
            }
        }


        private async Task<byte[]> ReadExactAsync(NetworkStream stream, int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset, length - offset);
                if (bytesRead == 0)
                    throw new IOException("연결이 끊겼습니다.");

                offset += bytesRead;
            }

            return buffer;
        }

    }
}
