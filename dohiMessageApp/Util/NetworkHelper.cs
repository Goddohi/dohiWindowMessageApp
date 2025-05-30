using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.Util
{
    public static class NetworkHelper
    {
        public static string GetLocalIPv4()
        {
            string localIp = "";
            foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4만 필터링
                {
                    localIp = ip.ToString();
                    break;
                }
            }

            return string.IsNullOrEmpty(localIp) ? "127.0.0.1" : localIp;
        }

    }
}
