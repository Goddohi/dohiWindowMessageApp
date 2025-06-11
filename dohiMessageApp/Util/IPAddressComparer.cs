using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.Util
{
    public class IPAddressComparer : IComparer<IPAddress>
    {
        public int Compare(IPAddress x, IPAddress y)
        {
            if (x == null || y == null) return 0;

            byte[] xBytes = x.GetAddressBytes();
            byte[] yBytes = y.GetAddressBytes();

            for (int i = 0; i < xBytes.Length && i < yBytes.Length; i++)
            {
                int result = xBytes[i].CompareTo(yBytes[i]);
                if (result != 0)
                    return result;
            }

            return xBytes.Length.CompareTo(yBytes.Length);
        }
    }
}
