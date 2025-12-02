using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ClosedXML.Excel;
using WalkieDohi.ToolMenus.Entity;
using System.Windows.Controls;

namespace WalkieDohi.ToolMenus.Views
{
    public partial class ExcelCellCollectorWindow : Window
    {
        private readonly List<string> _filePaths = new List<string>();
        private readonly List<ExcelCellDefinition> _2cellDefs = new List<ExcelCellDefinition>();
        private readonly ObservableCollection<ExcelCellDefinition> _cellDefs = new ObservableCollection<ExcelCellDefinition>();
        private DataTable _table;

        public ExcelCellCollectorWindow()
        {
            InitializeComponent();

            // 셀 정의 바인딩(휘발성)
            dgCellDefs.ItemsSource = _cellDefs;
            _cellDefs.CollectionChanged += (s, e) =>
             {
                 UpdateRowNumbers();
             };
        }

        private void BtnAddFiles_Click(object sender, RoutedEventArgs e)
        {
           try { 
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Filter = "Excel Files|*.xlsx;*.xlsm;*.xls",
                    Multiselect = true,
                    Title = "값을 가져올 엑셀 파일들을 선택하세요"
                };

                bool? result = dlg.ShowDialog(this);

                // 취소 또는 닫기 눌렀을 때는 그냥 아무 것도 안 하고 리턴
                if (result != true)
                {
                    return;
                }
                if (dlg.ShowDialog() == true)
                {
                    foreach (string f in dlg.FileNames)
                    {
                        if (!_filePaths.Contains(f))
                        {
                            _filePaths.Add(f);
                            lstFiles.Items.Add(f);
                        }
                    }
                }

                RefreshGrid();
           }
           catch (Exception ex)
           {   
            MessageBox.Show(this,
                "파일 추가 중 오류가 발생했습니다.\n" + ex.Message,
                "엑셀크롤링..",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
           }
        }

        private void BtnAddCellDef_Click(object sender, RoutedEventArgs e)
        {
            _cellDefs.Add(new ExcelCellDefinition());
        }

        private void BtnDeleteCellDef_Click(object sender, RoutedEventArgs e)
        {
            ExcelCellDefinition def = dgCellDefs.SelectedItem as ExcelCellDefinition;
            if (def != null)
            {
                _cellDefs.Remove(def);
            }
        }

        private void BtnReGridResult_Click(object sender, RoutedEventArgs e)
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (_filePaths.Count == 0 || _cellDefs.Count == 0)
            {
                dgResult.ItemsSource = null;
                return;
            }

            // DataTable 생성
            _table = new DataTable();
            _table.Columns.Add("파일명");

            foreach (var def in _cellDefs)
            {
                string header;

                if (string.IsNullOrWhiteSpace(def.Header) == false)
                {
                    header = def.Header;
                }
                else if (string.IsNullOrWhiteSpace(def.SheetName))
                {
                    // 시트명 비어 있으면 셀 주소만
                    header = def.CellAddress;
                }
                else
                {
                    header = def.SheetName + "!" + def.CellAddress;
                }

                _table.Columns.Add(header);
            }
            // 파일별로 값 채우기
            foreach (var file in _filePaths)
            {
                DataRow row = _table.NewRow();
                row[0] = Path.GetFileName(file);

                using (var wb = new XLWorkbook(file))
                {
                    for (int i = 0; i < _cellDefs.Count; i++)
                    {
                        row[i + 1] = ReadCell(wb, _cellDefs[i]);
                    }
                }

                _table.Rows.Add(row);
            }

            dgResult.ItemsSource = _table.DefaultView;
        }
        private object ReadCell(XLWorkbook wb, ExcelCellDefinition def)
        {
            try
            {
                if (wb == null || def == null || string.IsNullOrWhiteSpace(def.CellAddress))
                    return string.Empty;

                IXLWorksheet ws;

                if (string.IsNullOrWhiteSpace(def.SheetName))
                {
                    //시트명 비어 있으면: 첫 번째 시트 사용
                    ws = wb.Worksheet(1); // 1-based index
                }
                else
                {
                    // 시트명 있을 때는 해당 시트 찾기
                    if (!wb.Worksheets.Contains(def.SheetName))
                        return "#NOSHEET";

                    ws = wb.Worksheet(def.SheetName);
                }

                return ws.Cell(def.CellAddress).Value;
            }
            catch
            {
                return "#ERR";
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_table == null || _table.Rows.Count == 0)
            {
                MessageBox.Show("내보낼 데이터가 없습니다.");
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Excel Workbook|*.xlsx";
            dlg.FileName = "ExcelCellSummary.xlsx";

            if (dlg.ShowDialog() != true)
                return;

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Sheet1");

                for (int c = 0; c < _table.Columns.Count; c++)
                {
                    ws.Cell(1, c + 1).Value = _table.Columns[c].ColumnName;
                }

                for (int r = 0; r < _table.Rows.Count; r++)
                {
                    for (int c = 0; c < _table.Columns.Count; c++)
                    {
                        ws.Cell(r + 2, c + 1).Value = (XLCellValue)_table.Rows[r][c];
                    }
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);
            }

            MessageBox.Show("저장 완료!");
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // 1) 파일 목록 초기화
            _filePaths.Clear();
            lstFiles.Items.Clear();

            // 2) 셀 정의 초기화
            _cellDefs.Clear();
            dgCellDefs.Items.Refresh();

            // 3) 결과 그리드 초기화
            _table = null;
            dgResult.ItemsSource = null;
        }

        private void UpdateRowNumbers()
        {
            for(int i = 0; i<_cellDefs.Count; i++)
            {
                _cellDefs[i].RowNumber = i + 1;
            }
            dgCellDefs.Items.Refresh();
        }
    }
}