using System;
using System.Linq;
using System.Text;
using System.Windows;

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

        private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateCounts();
        }

        // 체크박스 상태 바뀔 때도 다시 계산
        private void LinuxModeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            var text = InputTextBox.Text ?? string.Empty;

            // ✔ 체크박스: true면 리눅스(LF), false면 윈도우(CRLF)
            bool linuxMode = LinuxModeCheckBox != null && LinuxModeCheckBox.IsChecked == true;

            // ✔ 리눅스 모드면 CRLF → LF로 정규화
            string working = linuxMode
                ? text.Replace("\r\n", "\n")
                : text;

            // 공백 제거 텍스트 (줄바꿈/탭 등 포함)
            var noWhite = new string(working.Where(c => !char.IsWhiteSpace(c)).ToArray());

            // 글자 수
            int charWithSpace = working.Length;
            int charWithoutSpace = noWhite.Length;

            // UTF-8 바이트 수
            int bytesWithSpace = Encoding.UTF8.GetByteCount(working);
            int bytesWithoutSpace = Encoding.UTF8.GetByteCount(noWhite);

            // 줄 수 (LF 기준)
            int lineCount = 0;
            if (working.Length > 0)
                lineCount = working.Split(new[] { "\n" }, StringSplitOptions.None).Length;

            // 단어 수 (공백 기준)
            int wordCount = working
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Length;

            // 모드 라벨
            string modeLabel = linuxMode ? "리눅스(LF)" : "윈도우(CRLF)";

            // 표시
            CharCountText.Text =
                $"[{modeLabel}] 글자 수 ─ 공백 포함 {charWithSpace}자 / {bytesWithSpace}byte   |   공백 제외 {charWithoutSpace}자 / {bytesWithoutSpace}byte";

            LineCountText.Text =
                $"줄 수 ─ {lineCount}줄";

            WordCountText.Text =
                $"단어 수 ─ {wordCount}개";
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = string.Empty;
        }
    }
}
