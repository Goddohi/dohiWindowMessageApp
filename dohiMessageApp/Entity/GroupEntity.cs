using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WalkieDohi.Core;

namespace WalkieDohi.Entity
{
    public class GroupEntity :DohiEntityBase
    {
        public string GroupName {  get; set; }
        public string[] Ips { get; set; } = Array.Empty<string>();
        public int Port { get; set; } = 9000;

        public string Key { get; set; }

        public void SetRandomKey()
        {
            Key = Guid.NewGuid().ToString();
        }
    }
}
