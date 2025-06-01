using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Newtonsoft.Json;
using WalkieDohi.Entity;


namespace WalkieDohi.Util
{
    public class MessengerSender
    {
        public async Task SendMessageAsync(string ip, int port, MessageEntity message)
        {
            try
            {

                string json = JsonConvert.SerializeObject(message);  // 자동으로 JSON 생성
                byte[] data = Encoding.UTF8.GetBytes(json);

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ip, port);
                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                        await stream.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                message.ResultSetFail();
                Console.WriteLine($"전송 실패: {ex.Message}");
            }
        }
    }
}
