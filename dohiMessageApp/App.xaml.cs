using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.Core.app;
using WalkieDohi.Util.IO;

namespace WalkieDohi
{
    public partial class App : Application
    {
        private static Mutex _mutex;
        private MessageReceiverService _messageReceiverService;

        public MessageReceiverService MessageReceiverService => _messageReceiverService;

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

            LoadApplicationData();
            _messageReceiverService = new MessageReceiverService();

            bool minimized = e.Args.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

            TrayIconManager.Init(
                onOpenMainWindow: ShowMainWindow,
                onExitApp: () =>
                {
                    _messageReceiverService?.Dispose();
                    TrayIconManager.Dispose();
                    Shutdown();
                },
                tooltip: "WalkieDohi"
            );

            if (minimized)
            {
                InitializeHiddenMainWindow();
                StartMessageReceiver();
                return;
            }

            ShowMainWindow();
            StartMessageReceiver();
        }

        private void LoadApplicationData()
        {
            MainData.currentUser = new UserJsonFileHandler().LoadUser();
            MainData.Friends = new FriendJsonFileHandler().LoadFriends();
            ChatListManager.LoadChatList();
        }

        private void StartMessageReceiver()
        {
            _messageReceiverService?.Start();
        }

        private void InitializeHiddenMainWindow()
        {
            EnsureMainWindowCreated();

            // 숨김 시작이어도 HWND를 만들어 단일 인스턴스 복원 메시지가 준비되게 합니다.
            new WindowInteropHelper(MainWindow).EnsureHandle();
            MainWindow.Hide();
        }

        private void EnsureMainWindowCreated()
        {
            if (MainWindow == null)
                MainWindow = new MainWindow(_messageReceiverService);
        }

        private void ShowMainWindow()
        {
            EnsureMainWindowCreated();

            MainWindow.Show();
            if (MainWindow.WindowState == WindowState.Minimized)
                MainWindow.WindowState = WindowState.Normal;

            MainWindow.Activate();
            MainWindow.Focus();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _messageReceiverService?.Dispose();
            TrayIconManager.Dispose(); // 유령 아이콘 방지
            base.OnExit(e);
        }
    }
}
