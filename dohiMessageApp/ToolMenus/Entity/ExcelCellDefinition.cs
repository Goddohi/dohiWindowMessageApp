using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.ToolMenus.Entity
{
   public class ExcelCellDefinition
    {
        public string Header { get; set; }      // 그리드/엑셀 헤더 이름
        public string SheetName { get; set; }   // 시트명 (비어 있으면 첫 번째 시트 사용)
        public string CellAddress { get; set; } // 셀 주소 (예: "D5")
    }
}
