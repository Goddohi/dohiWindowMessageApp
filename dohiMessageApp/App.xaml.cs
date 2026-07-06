using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using WalkieDohi.Core.app;
using WalkieDohi.Util.Tcp;

namespace WalkieDohi
{
    public partial class App : Application
    {
        private static Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _mutex = new Mutex(true, "WalkieDohi_SingleInstance", out createdNew);

            if (!createdNew)
            {
                // 기존 인스턴스에 메시지 보내서 창 띄우기
                NativeMethods.PostMessage(
                    (IntPtr)NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SHOWME,
                    IntPtr.Zero,
                    IntPtr.Zero);
                Environment.Exit(0);
            }
           

            base.OnStartup(e);

            // 최소화 시작 플래그
            bool minimized = e.Args.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

            // 트레이 먼저 준비
            TrayIconManager.Init(
                onOpenMainWindow: ShowMainWindow,
                onExitApp: () => {
                    TrayIconManager.Dispose();
                    Shutdown();
                },
                tooltip: "WalkieDohi"
            );

            if (minimized)
            {
                InitializeHiddenMainWindow();
                return;
            }

            ShowMainWindow();
        }

        private void InitializeHiddenMainWindow()
        {
            EnsureMainWindowCreated();

            // 숨김 시작이어도 HWND를 만들어 단일 인스턴스 복원 메시지와 수신 초기화가 준비되게 합니다.
            new WindowInteropHelper(MainWindow).EnsureHandle();
            MainWindow.Hide();
        }

        private void EnsureMainWindowCreated()
        {
            if (MainWindow == null)
                MainWindow = new MainWindow();
        }

        private void ShowMainWindow()
        {
            EnsureMainWindowCreated();

            MainWindow.Show();
           // 작업줄 최소화 상태라면 복원
            if (MainWindow.WindowState == WindowState.Minimized)
                MainWindow.WindowState = WindowState.Normal;

            MainWindow.Activate();
            MainWindow.Focus();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIconManager.Dispose(); // 유령 아이콘 방지
            base.OnExit(e);
        }
    
    }
}
