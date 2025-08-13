using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WalkieDohi.Core;
using WalkieDohi.Util;

namespace WalkieDohi.Entity
{
    public class GroupEntity :DohiEntityBase
    {
        public string GroupName {  get; set; }
        public string[] Ips { get; set; } = Array.Empty<string>();

        public string Key { get; set; }

        public void MakeRandomKey()
        {
            //키가 아예없는 초기에만 생성되록.
           if(string.IsNullOrWhiteSpace(Key))
                Key = Guid.NewGuid().ToString();
        }

        [JsonIgnore]
        public string TooltipText
        {
            get
            {
                var names = Ips.Select(ip =>
                {
                    if (ip == NetworkHelper.GetLocalIPv4())
                        return $"본인 ({ip})";
                    var friend = MainData.Friends.FirstOrDefault(f => f.Ip == ip);
                    return $"{(friend?.Name ?? "이름 없음")} ({ip})";
                });

                return string.Join("\n", names);
            }
        }
    }
}
