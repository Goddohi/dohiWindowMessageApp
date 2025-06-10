using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using WalkieDohi.Core.app;

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
        }
    }
}
