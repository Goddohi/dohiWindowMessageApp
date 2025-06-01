using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalkieDohi.Core;

namespace WalkieDohi.Entity
{
    public class User : DohiEntityBase
    {
        public string Nickname { get; set; } = "사용자";
    }
}
