using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace WalkieDohi.Core.app
{
    public static class AutoStartManager
    {
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "WalkieDohi"; // 레지스트리에 표시될 이름

        public static bool IsEnabled()
        {
            try
            {
                using (var rk = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (rk == null) return false;
                    var val = rk.GetValue(AppName) as string;
                    if (string.IsNullOrWhiteSpace(val)) return false;

                    // 레지스트리 값은 `"C:\path\app.exe" --minimized` 같은 형태일 수 있음
                    var exeInReg = ExtractExePath(val);
                    var currentExe = GetExecutablePath();

                    return StringComparer.OrdinalIgnoreCase.Equals(
                        Path.GetFullPath(exeInReg ?? string.Empty),
                        Path.GetFullPath(currentExe ?? string.Empty)
                    );
                }
            }
            catch { return false; }
        }

        public static bool SetEnabled(bool enable, string optionalArgs /* null 또는 "--minimized" 같은 인자 */)
        {
            try
            {
                using (var rk = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (rk == null) return false;

                    if (enable)
                    {
                        var exe = GetExecutablePath();
                        if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return false;

                        // 공백 경로 대비해 따옴표로 감싼다.
                        var value = "\"" + exe + "\"";
                        if (!string.IsNullOrWhiteSpace(optionalArgs))
                            value += " " + optionalArgs;

                        rk.SetValue(AppName, value);
                    }
                    else
                    {
                        rk.DeleteValue(AppName, false);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetExecutablePath()
        {
            try
            {
                // 가장 안정적인 현재 프로세스 exe 경로
                var p = Process.GetCurrentProcess();
                if (p != null && p.MainModule != null)
                    return p.MainModule.FileName;
            }
            catch { /* 일부 환경에서 MainModule 접근 거부될 수 있음 */ }

            try
            {
                // 대체 경로
                var entry = Assembly.GetEntryAssembly();
                if (entry != null) return entry.Location;
            }
            catch { }

            try
            {
                return Assembly.GetExecutingAssembly().Location;
            }
            catch { }

            return null;
        }

        private static string ExtractExePath(string runValue)
        {
            if (string.IsNullOrWhiteSpace(runValue)) return null;

            runValue = runValue.Trim();

            // 따옴표로 감싼 경우: "C:\...\app.exe" [args...]
            if (runValue.StartsWith("\""))
            {
                int endQuote = runValue.IndexOf('\"', 1);
                if (endQuote > 1)
                    return runValue.Substring(1, endQuote - 1);
            }

            // 따옴표 없이 경로가 시작되는 경우: C:\...\app.exe [args...]
            int space = runValue.IndexOf(' ');
            if (space > 0)
                return runValue.Substring(0, space);

            return runValue; // 인자 없음
        }
    }
}
