using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WalkieDohi.Core;
using WalkieDohi.Util;

namespace WalkieDohi.Groups.Entity
{
    public class GroupEntity :DohiEntityBase
    {
        public string GroupName {  get; set; }
        public List<string> Ips { get; set; } = new List<string>();// Array.Empty<string>();

        public string Key { get; set; }

        public void MakeRandomKey()
        {
            //키가 아예없는 초기에만 생성되록.
           if(string.IsNullOrWhiteSpace(Key))
                Key = Guid.NewGuid().ToString("N");
            /*
                "N": 32자리, 구분자 없음 → d85b1407333f4b6a8a3a1f6f0c2e9d2f
                "D": 하이픈 포함 기본형 → d85b1407-333f-4b6a-8a3a-1f6f0c2e9d2f
                "B": 중괄호 포함 → {d85b1407-333f-4b6a-8a3a-1f6f0c2e9d2f}
                "P": 괄호 포함 → (d85b1407-333f-4b6a-8a3a-1f6f0c2e9d2f)
                "X": 특수 16진 포맷(드물게 사용) 
             */
        }

        [JsonIgnore] //JSON 직렬화/역직렬화 과정에서 해당 속성(필드)을 무시
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
    public class GroupMemberDisplay
    {
        public string Ip { get; set; }
        public string Name { get; set; }  // FriendList에서 조회해서 붙임
    }
}
