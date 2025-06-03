using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Core;

namespace WalkieDohi.Entity
{
    public class GroupEntity :DohiEntityBase
    {
        public string GroupName {  get; set; }
        public string[] Ips { get; set; } = Array.Empty<string>();
        public int Port { get; set; } = 9000;
    }
}
