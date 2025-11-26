using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WalkieDohi.ToolMenus.Views
{
    /// <summary>
    /// TextCountToolWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TextCountToolWindow : Window
    {
        public TextCountToolWindow()
        {
            InitializeComponent();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCounts();
        }

        private void LinuxModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCounts();
        }

        private void EncodingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCounts();
        }

        private string GetEncodingMode()
        {
            var item = EncodingComboBox?.SelectedItem as ComboBoxItem;
            var tag = item?.Tag as string;
            return string.IsNullOrEmpty(tag) ? "UTF8" : tag;
        }

        private void UpdateCounts()
        {
            // 아직 XAML 요소들이 완전히 만들어지기 전에 호출되면 그냥 리턴
            if (!IsLoaded || CharCountText == null || LineCountText == null ||
                WordCountText == null || ByteInfoText == null || InputTextBox == null)
            {
                return;
            }

            var text = InputTextBox.Text ?? string.Empty;

            // 1) 줄바꿈을 1글자로 보기 위한 정규화 (문자/줄/단어용)
            //    CRLF(\r\n) → LF(\n)
            string normalized = text.Replace("\r\n", "\n");

            // 2) 윈도우/리눅스 모드 (바이트 계산에만 사용)
            bool linuxMode = LinuxModeCheckBox != null && LinuxModeCheckBox.IsChecked == true;

            // 2-1) 바이트 계산용 문자열
            //  - 윈도우 모드: CRLF 그대로(text)
            //  - 리눅스 모드: LF만(normalized)
            string byteBase = linuxMode ? normalized : text;

            // === 문자 기준 계산 (항상 normalized 사용) ===

            // 공백 제거 텍스트 (문자 기준)
            var noWhiteChar = new string(normalized.Where(c => !char.IsWhiteSpace(c)).ToArray());

            // 글자 수 (UTF-16 코드 유닛 기준이지만 줄바꿈은 1개로 처리됨)
            int charWithSpace = normalized.Length;
            int charWithoutSpace = noWhiteChar.Length;

            // 줄 수 (LF 기준)
            int lineCount = 0;
            if (normalized.Length > 0)
                lineCount = normalized.Split(new[] { "\n" }, StringSplitOptions.None).Length;

            // 단어 수 (공백 기준)
            int wordCount = normalized
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Length;

            // === 바이트/코드 계산 (byteBase 기준) ===

            // 공백 제거 텍스트 (바이트 기준)
            var noWhiteByte = new string(byteBase.Where(c => !char.IsWhiteSpace(c)).ToArray());

            // UTF-8
            int utf8With = Encoding.UTF8.GetByteCount(byteBase);
            int utf8Without = Encoding.UTF8.GetByteCount(noWhiteByte);

            // CP949 (EUC-KR)
            Encoding cp949 = Encoding.GetEncoding("euc-kr");
            int cp949With = cp949.GetByteCount(byteBase);
            int cp949Without = cp949.GetByteCount(noWhiteByte);

            // ANSI (시스템 기본)
            Encoding ansi = Encoding.Default;
            int ansiWith = ansi.GetByteCount(byteBase);
            int ansiWithout = ansi.GetByteCount(noWhiteByte);

            // UTF-16 코드 유닛 (글자 수와 동일 기준으로 보고 싶으면 normalized 기준 사용)
            int utf16With = normalized.Length;
            int utf16Without = noWhiteChar.Length;

            // === 공통 텍스트 ===

            string modeLabel = linuxMode ? "리눅스(LF)" : "윈도우(CRLF)";

            CharCountText.Text =
                $"[{modeLabel}] 글자 수 ─ 공백 포함 {charWithSpace}자   |   공백 제외 {charWithoutSpace}자";

            LineCountText.Text = $"줄 수 ─ {lineCount}줄";
            WordCountText.Text = $"단어 수 ─ {wordCount}개";

            // === 드롭박스 선택에 따라 바이트/코드 표시 ===

            string encMode = GetEncodingMode();

            switch (encMode)
            {
                case "UTF8":
                    ByteInfoText.Text =
                        $"UTF-8 바이트 ─ 공백 포함 {utf8With}byte   |   공백 제외 {utf8Without}byte";
                    break;

                case "CP949":
                    ByteInfoText.Text =
                        $"CP949(EUC-KR) 바이트 ─ 공백 포함 {cp949With}byte   |   공백 제외 {cp949Without}byte";
                    break;

                case "ANSI":
                    ByteInfoText.Text =
                        $"ANSI(시스템 기본) 바이트 ─ 공백 포함 {ansiWith}byte   |   공백 제외 {ansiWithout}byte";
                    break;

                case "UTF16":
                    ByteInfoText.Text =
                        $"UTF-16 코드 유닛 ─ 공백 포함 {utf16With}개   |   공백 제외 {utf16Without}개";
                    break;

                default:
                    ByteInfoText.Text =
                        $"UTF-8 바이트 ─ 공백 포함 {utf8With}byte   |   공백 제외 {utf8Without}byte";
                    break;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = string.Empty;
        }
    }
}
