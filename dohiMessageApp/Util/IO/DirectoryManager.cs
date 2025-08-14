using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.Util.IO
{
    public static class DirectoryManager
    {
        //private static string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// AppData\Roaming\WalkieDohi 경로 반환 (없으면 자동 생성)
        /// </summary>
        public static string GetAppDataDirectory()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WalkieDohi");

            // 폴더 없으면 생성
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }



        /// <summary>
        /// AppData\Roaming\WalkieDohi 경로에 파일이름 붙여서 반환 (폴더가 없으면 자동 생성)
        /// </summary>
        public static string GetAppDataDirectoryCombineFileName(string filename)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WalkieDohi");

            // 폴더 없으면 생성
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return Path.Combine(folderPath, filename);
        }

        public static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

    }
}
