using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.IO;
using Path = System.IO.Path;
using WalkieDohi.Core;
using System.Threading;

namespace WalkieDohi.ChattingRooms.Views
{
    /// <summary>
    /// 김용록 제공 소스 
    /// </summary>
    public partial class PDFPreviewWindow : Window
    {
        public PDFPreviewWindow()
        {
            InitializeComponent();

            this.Title = "PDF 뷰어";
            onLoadedPDF();

            if (_fail)
            {
                throw new InvalidOperationException("PDF 뷰어 초기화 실패");
            }
        }
        public PDFPreviewWindow(string filePath)
        {
            InitializeComponent();
            this.Title = filePath + " PDF 뷰어";
            StartupFilePath = filePath;
            
             onLoadedPDF();

            if (_fail)
            {
                throw new InvalidOperationException("PDF 뷰어 초기화 실패");
            }
            
        }
        private string StartupFilePath { get; set; }


        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : 에러 
        /// </summary>
        /// ---------------------------------------------------------------------
        private bool _fail = false;

        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : 줌
        /// </summary>
        /// ---------------------------------------------------------------------
        private double zoom = 1.0;

        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : 로그 카운트
        /// </summary>
        /// ---------------------------------------------------------------------
        private int LogCount = 0;



        #region [컨트롤 초기화]



        /// ---------------------------------------------------------------------
        /// <summary>
        /// name         : onLoadedPDF
        /// desc         : PDF 로드 초기화
        /// </summary>
        /// ---------------------------------------------------------------------
        private async void onLoadedPDF()
        {
            _fail = false;
            /// ---------------------------------------------------------------------
            /// 비트 체크
            /// ---------------------------------------------------------------------
            PrintAssemblybitness();

            /// ---------------------------------------------------------------------
            /// 프로세스 체크
            /// ---------------------------------------------------------------------
            PrintAssemblyInfo();

            /// ---------------------------------------------------------------------
            /// dll Loader 체크
            /// ---------------------------------------------------------------------
            CheckedWebView2LoaderPresence();

            /// ---------------------------------------------------------------------
            /// API 버전 체크
            /// ---------------------------------------------------------------------
            PrintRuntimVersionFromAPI();

            /// ---------------------------------------------------------------------
            /// 레지스트리 체크 
            /// ---------------------------------------------------------------------
            PrintRuntimePressenceFromRegistry();

            /// ---------------------------------------------------------------------
            /// 파일 시스템 확인
            /// ---------------------------------------------------------------------
            PrintRuntimePresenceFromFileSystem();


            try
            {
                /// ---------------------------------------------------------------------
                /// 사용할 준비, API 초기화
                /// ---------------------------------------------------------------------
                await EnsureWebViewReady();
            }
            catch (System.DllNotFoundException dllnotfoundexception)
            {
                ExceptionAndOpenFileExplorer("dll 문제 발생", dllnotfoundexception.Message);

                return;
            }
            Status("준비 완료", string.Empty);


            if (!string.IsNullOrWhiteSpace(StartupFilePath))
            {
                /// ---------------------------------------------------------------------
                /// PDF, xaml에 webview2로 로드
                /// ---------------------------------------------------------------------
                LoadPDF(StartupFilePath);
            }

            return;

        }

        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : 프로세스 비트 체크
        /// </summary>
        /// ---------------------------------------------------------------------
        void PrintAssemblybitness()
            {
                var bit = Environment.Is64BitProcess ? "x64" : "x86";
                Status("프로세스 아키텍처", bit);
            }

        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : 프로세스 체크
        /// </summary>
        /// ---------------------------------------------------------------------
        void PrintAssemblyInfo()
        {
            try
            {
                var asm = typeof(CoreWebView2Environment).Assembly.Location;
                Status("WebView2 어셈블리", asm);
            }
            catch (Exception excep)
            {
                ExceptionAndOpenFileExplorer("WebView2 어셈블리 확인 실패", excep.Message);
            }

        }

        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : dll 체크
        /// </summary>
        /// ---------------------------------------------------------------------
        void CheckedWebView2LoaderPresence()
        {
            try
            {
                var asm = typeof(CoreWebView2Environment).Assembly.Location;
                var dir = Path.GetDirectoryName(asm);
                var loader = Path.Combine(dir ?? "", "Webview2Loader.dll"); // 파일 확인

                if (File.Exists(loader)) Status("Loader dll 발견", loader);
                else ExceptionAndOpenFileExplorer("Loader dll 없음(빌드 산출물에 복사 필요)", loader);
            }
            catch (Exception excep)
            {
                ExceptionAndOpenFileExplorer("Loader 확인 실패", excep.Message);

                /* 
                    * 메인 DLL 참조 확인
                    * D:\HISSolutions\HIS\Deploy\Client\Core\Microsoft.Web.WebView2.Core.dll
                    * D:\HISSolutions\HIS\Deploy\Client\Core\Microsoft.Web.WebView2.Wpf.dll
                */
            }


        }



        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : API 버전 체크 
        /// </summary>
        /// ---------------------------------------------------------------------
        void PrintRuntimVersionFromAPI()
        {
            try
            {
                var versionAPI = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (!string.IsNullOrWhiteSpace(versionAPI)) Status("API 버전", versionAPI);
                else Status("API 감지 실패", string.Empty);
            }
            catch (Exception excep)
            {
                if (excep.Message.Contains("WebView2Loader.dll"))
                {
                    ExceptionAndOpenFileExplorer("API 버전 확인 실패_WebView2Loader.dll문제", "빌드했을 때, 실행 파일(.exe)이 있는 위치에 WebView2Loader.dll 파일이 있는지 확인해주세요.\n" + excep.Message);

                    /*
                        * DLL 위치 확인
                        * TA 때문인지, D:\HISSolutions\HIS\Deploy\Client\EConsent\WebView2Loader.dll 에 존재하는데,
                        * D:\HISSolutions\HIS\Deploy\Client\WebView2Loader.dll 에도 있어야 함.
                    */
                }
                else ExceptionAndOpenFileExplorer("API 버전 확인 실패", excep.Message);
            }
        }




        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : 레지스트리 체크 
        /// </summary>
        /// ---------------------------------------------------------------------
        void PrintRuntimePressenceFromRegistry()
        {
            try
            {
                var hkim = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients");
                var l = (hkim != null) ? string.Join(",", hkim.GetSubKeyNames()) : "";
                Status("레지스트리 확인(HKLM Clients)", string.IsNullOrEmpty(l) ? "(none)" : l);
            }
            catch (Exception excep)
            {
                ExceptionAndOpenFileExplorer("레지스트리 확인(HKLM Clients) 실패", excep.Message);
            }


            try
            {
                var hkcu = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\EdgeUpdate\Clients");
                var u = (hkcu != null) ? string.Join(",", hkcu.GetSubKeyNames()) : "";
                Status("레지스트리 확인(HKCU Clients)", string.IsNullOrEmpty(u) ? "(none)" : u);

            }
            catch (Exception excep)
            {
                ExceptionAndOpenFileExplorer("레지스트리 확인(HKCU Clients) 실패", excep.Message);
            }
        }



        /// ---------------------------------------------------------------------
        /// <summary>
        /// desc         : 파일 시스템 확인(설치 경로가 User인지, Program Files인지)
        /// </summary>
        /// ---------------------------------------------------------------------
        void PrintRuntimePresenceFromFileSystem()
        {
            try
            {
                var sysPath = @"C:\Program Files (x86)\Microsoft\EdgeWebView\Application";
                Status("파일 시스템(FS Sys) 확인", (Directory.Exists(sysPath) ? "있음" : "없음"));

                var usrPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "EdgeWebView", "Application");
                Status("파일 시스템(FS User) 확인", (Directory.Exists(usrPath) ? "있음" : "없음"));
            }
            catch (Exception excep)
            {
                Status("파일 시스템 확인 실패", excep.Message);
            }
        }




        /// ---------------------------------------------------------------------
        /// <summary>
        /// name         : EnsureWebViewReady
        /// desc         : PDF 로드하기 위한 초기화
        /// </summary>
        /// ---------------------------------------------------------------------
        private async Task EnsureWebViewReady()
        {
            if (Web.CoreWebView2 != null) return; // 이미 초기화 되어있으면 패스


            var env = await CoreWebView2Environment.CreateAsync();
            await Web.EnsureCoreWebView2Async(env);


            /// ---------------------------------------------------------------------
            /// 기본 메뉴 활성화
            /// ---------------------------------------------------------------------
            Web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

            /// ---------------------------------------------------------------------
            /// 개발자 도구 활성화
            /// ---------------------------------------------------------------------
            Web.CoreWebView2.Settings.AreDevToolsEnabled = true;
            Web.NavigationCompleted += (s, e) => Status("pdf 로드", e.IsSuccess ? "완료" : "실패");
        }

        #endregion //컨트롤 초기화




        /// ---------------------------------------------------------------------
        /// <summary>
        /// name         : onOpenPDF
        /// desc         : 현재 컴퓨터에서 파일 찾기
        /// </summary>
        /// ---------------------------------------------------------------------
        private void onOpenPDF()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "PDF 파일 (*.pdf)|*.pdf";
            dlg.Multiselect = false;

            if (dlg.ShowDialog() == true)
            {
                /// ---------------------------------------------------------------------
                /// PDF, xaml에 webview2로 로드
                /// ---------------------------------------------------------------------
                LoadPDF(dlg.FileName);
            }
        }


        /// ---------------------------------------------------------------------
        /// <summary>
        /// name         : LoadPDF
        /// desc         : 현재 컴퓨터에서 파일 선택 시, xaml에 webview2로 로드
        /// </summary>
        /// ---------------------------------------------------------------------
        private void LoadPDF(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    Status("경로가 비어 있음", string.Empty);
                }


                if (!File.Exists(path))
                {
                    Status("파일 없음", string.Empty);
                }


                var uri = new Uri(path);
                Web.Source = uri; /// webview2에 로드

                Status("pdf 로딩 중", path);

            }

            catch (Exception excep)
            {
                Status("pdf 파일 불러오는 도중 오류", excep.Message);
            }

        }


        private async Task ApplyZoom()
        {
            try
            {
                if (Web?.CoreWebView2 == null) return;
                var script = "document.body.style.zoom=" + zoom.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";";
                await Web.ExecuteScriptAsync(script);

                Status("줌", zoom.ToString("0.0"));
            }
            catch (Exception excep)
            {
                Status("줌 실패", excep.Message);
            }

        }


        /// ---------------------------------------------------------------------
        /// <summary>
        /// name         : Status
        /// desc         : 로그처럼 쓰기 위한 용도(테스트용)
        /// update date  : 최종 수정 일자, 수정자, 수정개요 
        /// </summary>
        /// ---------------------------------------------------------------------
        private void Status(string sujectText, string messageText)
        {
            // 디버그 용 
        }

        /// ---------------------------------------------------------------------
        /// <summary>
        /// name         : ExceptionAndCloseWindow
        /// desc         : 사용자에게 해당 창을 PDF뷰어대신 파일탐색기로 전환하여 사용자 불편함을 제공하지 않기 위함
        /// update date  : 최종 수정 일자, 수정자, 수정개요 
        /// </summary>
        /// ---------------------------------------------------------------------
        private void ExceptionAndOpenFileExplorer(string sujectText, string messageText)
        {
            // 디버그 용 
            Status(sujectText, messageText);
            if(_fail){
                return;
            }
            OpenFileExplorer();
            _fail = true;
        }

        /// ---------------------------------------------------------------------
        /// <summary>
        /// name         : OpenFileExplorer
        /// desc         : 해당 파일의 경로에 파일 탐색기를 열어줍니다.
        /// update date  : 최종 수정 일자, 수정자, 수정개요 
        /// </summary>
        /// ---------------------------------------------------------------------
        private void OpenFileExplorer()
        {
            // 디버그 용 
            if (ExtendFile.UnExists(StartupFilePath))
            {
                MessageBox.Show("파일이 존재하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{StartupFilePath}\"");
            return;
        }

        private CancellationTokenSource _cts = new CancellationTokenSource();
        public void ForceCleanup()
        {
            // 여기서 모든 비관리/비동기 리소스 정리
            try { _cts.Cancel(); } catch { }
            Web?.Dispose();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ForceCleanup();
        }
    }
}
