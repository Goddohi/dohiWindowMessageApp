using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WalkieDohi.Core.app
{
    public static class TrayIconManager
    {
        private static NotifyIcon _tray;          // GC 방지용 정적 참조
        private static bool _initialized;

        // 트레이가 준비되었는지 체크 (Explorer의 작업표시줄 핸들 탐색)
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static void Init(Action onOpenMainWindow, Action onExitApp, string tooltip = "WalkieDohi")
        {
            if (_initialized) return;

            // 1) 탐색기 트레이 준비 대기 (최대 3초, 100ms 간격)
            const int maxWaitMs = 3000;
            var waited = 0;
            while (FindWindow("Shell_TrayWnd", null) == IntPtr.Zero && waited < maxWaitMs)
            {
                Thread.Sleep(100);
                waited += 100;
            }

            // 2) NotifyIcon 생성
            _tray = new NotifyIcon();
            _tray.Text = tooltip;
            _tray.Icon = LoadAppIconSafe(); // 아이콘 로드(안되면 exe 아이콘 fallback)
            _tray.Visible = true;

            // 우클릭 메뉴
            var menu = new ContextMenuStrip();
            var open = new ToolStripMenuItem("열기", null, (s, e) => onOpenMainWindow?.Invoke());
            var exit = new ToolStripMenuItem("종료", null, (s, e) => onExitApp?.Invoke());
            menu.Items.Add(open);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exit);
            _tray.ContextMenuStrip = menu;

            // 좌클릭으로도 열기
            _tray.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    onOpenMainWindow?.Invoke();
            };

            _initialized = true;
        }

        public static void Dispose()
        {
            try
            {
                if (_tray != null)
                {
                    _tray.Visible = false;
                    _tray.Dispose();
                    _tray = null;
                }
                _initialized = false;
            }
            catch { /* ignore */ }
        }

        private static Icon LoadAppIconSafe()
        {
            try
            {
                // 1) 임베디드 리소스(.ico)를 우선 시도
                // 프로젝트에서 Assets\WalkieDohi.ico를 "Embedded Resource"로 넣고 네임스페이스에 맞게 경로 지정
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var resName = FindIconResourceName(asm);
                if (resName != null)
                {
                    using (var s = asm.GetManifestResourceStream(resName))
                    {
                        if (s != null) return new Icon(s);
                    }
                }
            }
            catch { /* fallback */ }

            try
            {
                // 2) exe와 같은 폴더의 아이콘 파일을 절대경로로 로드 (상대경로 금지)
                var exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                var icoPath = Path.Combine(exeDir ?? "", "Assets", "WalkieDohi.ico");
                if (File.Exists(icoPath)) return new Icon(icoPath);
            }
            catch { /* fallback */ }

            try
            {
                // 3) 마지막 fallback: 실행 파일의 연관 아이콘 추출
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private static string FindIconResourceName(Assembly asm)
        {
            // 네임스페이스/폴더에 따라 달라집니다. 일치하는 .ico 리소스 찾아보기
            foreach (var name in asm.GetManifestResourceNames())
            {
                // 예: "WalkieDohi.Assets.WalkieDohi.ico"
                if (name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) &&
                    name.IndexOf("WalkieDohi", StringComparison.OrdinalIgnoreCase) >= 0)
                    return name;
            }
            return null;
        }
    }
}
