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
        private const string FOLDER_IMAGE_PATH = @"C:\ReceivedFiles\Image";
        public static string GetFilePath(string FileName)
        {
            return System.IO.Path.Combine(FOLDER_PATH, FileName);
        }
        public static string GetImagePath(string FileName)
        {
            return System.IO.Path.Combine(FOLDER_IMAGE_PATH, FileName);
        }

        public static void CheckFileDrietory()
        {
            if (!Directory.Exists(FOLDER_PATH))
                Directory.CreateDirectory(FOLDER_PATH);
        }
        public static void CheckImageDrietory()
        {
            if (!Directory.Exists(FOLDER_PATH))
                Directory.CreateDirectory(FOLDER_PATH);
            if (!Directory.Exists(FOLDER_IMAGE_PATH))
                Directory.CreateDirectory(FOLDER_IMAGE_PATH);
        }
    }
}
