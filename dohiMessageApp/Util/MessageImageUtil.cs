using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkieDohi.Util
{
    public static class MessageImageUtil
    {

        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        public static bool isImagecheck(string FileName)
        {
            if (FileName == null) return false;
            string ext = Path.GetExtension(FileName).ToLower();

            if (SupportedImageExtensions.Contains(ext))
            {
                return true;
            }
            return false;
        }
    }
}
