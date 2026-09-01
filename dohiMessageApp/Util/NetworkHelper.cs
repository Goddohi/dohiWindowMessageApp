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
        public static bool TryNormalizeIPv4(string ip, out string normalizedIp)
        {
            normalizedIp = null;

            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            string[] parts = ip.Trim().Split('.');
            if (parts.Length != 4)
            {
                return false;
            }

            string[] normalizedParts = new string[4];
            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index].Trim();
                if (part.Length == 0 || part.Length > 3 || !part.All(char.IsDigit))
                {
                    return false;
                }

                int value;
                if (!int.TryParse(part, out value) || value < 0 || value > 255)
                {
                    return false;
                }

                normalizedParts[index] = value.ToString();
            }

            normalizedIp = string.Join(".", normalizedParts);
            return true;
        }

        public static bool AreSameIPv4(string left, string right)
        {
            string normalizedLeft;
            string normalizedRight;

            return TryNormalizeIPv4(left, out normalizedLeft)
                && TryNormalizeIPv4(right, out normalizedRight)
                && string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
        }

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


        public static string GetOutboundIPv4ForTarget(string remoteIpString)
        {
            try
            {
                var remoteIp = IPAddress.Parse(remoteIpString);

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect(new IPEndPoint(remoteIp, MainData.GetPort()));

                    var localEndPoint = socket.LocalEndPoint as IPEndPoint;
                    if (localEndPoint != null)
                        return localEndPoint.Address.ToString();
                }
            }
            catch
            {
                // 실패 시 fallback
                return GetLocalIPv4();
            }

            return GetLocalIPv4();
        }

    }
}
