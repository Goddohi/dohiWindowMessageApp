using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using WalkieDohi.Entity;

namespace WalkieDohi.Util
{
    public class MessengerReceiver
    {
        private readonly int port;
        private TcpListener listener;
        private bool isRunning = false;

        public event Action<MessageEntity> OnMessageReceived;

        public MessengerReceiver(int port)
        {
            this.port = port;
        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;

            Task.Run(() => ListenLoop());
            Console.WriteLine($"[수신 대기 중] 포트: {port}");
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
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string json = await reader.ReadToEndAsync();

                try
                {
                    var message = JsonConvert.DeserializeObject<MessageEntity>(json);

                    // 메시지 처리
                    OnMessageReceived?.Invoke(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JSON 파싱 실패] {ex.Message}");
                }
            }
        }
    }
    }
