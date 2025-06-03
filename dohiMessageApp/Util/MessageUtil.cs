using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace WalkieDohi.Util
{
    public static class MessageUtil
    {
        private const string FOLDER_PATH = @"C:\ReceivedFiles";
        public static string GetFilePath(string FileName)
        {
            return System.IO.Path.Combine(FOLDER_PATH, FileName);
        }

        public static void CheckFileDrietory()
        {
            if (!Directory.Exists(FOLDER_PATH))
                Directory.CreateDirectory(FOLDER_PATH);
        }
    }
}
