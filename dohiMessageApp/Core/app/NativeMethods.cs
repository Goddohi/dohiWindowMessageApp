using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;


namespace WalkieDohi.Core.app
{


    public static class NativeMethods
    {

        // 프로그램이 이미 실행일 경우 재실행방지.
        /*
           내가 까먹어서 적는 코드 설명
           1. WM_SHOWME 메시지를 모든 윈도우에 브로드캐스트로 전송 (HWND_BROADCAST)
           2. 현재 실행 중인 앱이 이 메시지를 받으면 창을 앞으로 표시
           3. 결과적으로 중복 실행 방지 + 기존 실행 창 활성화를 구현 가능 
         */
        public const int HWND_BROADCAST = 0xffff;
        public static readonly int WM_SHOWME = RegisterWindowMessage("WM_SHOW_WALKIEDOHI");

        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);

    }

}
