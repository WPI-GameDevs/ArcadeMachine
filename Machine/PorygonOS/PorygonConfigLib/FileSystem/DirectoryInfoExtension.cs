using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorygonOS.Core
{
    public static class DirectoryInfoExtension
    {
        public static DirectoryInfo Copy(this DirectoryInfo src, string dst)
        {
            DirectoryInfo dstInfo = Directory.CreateDirectory(dst);

            FileInfo[] files = src.GetFiles();
            foreach(FileInfo file in files)
            {
                string dstFile = Path.Combine(dst, file.Name);
                file.CopyTo(dstFile, true);
            }

            DirectoryInfo[] childDirectories = src.GetDirectories();

            foreach(DirectoryInfo child in childDirectories)
            {
                string childPath = Path.Combine(dst, child.Name);
                Copy(child, childPath);
            }

            return dstInfo;
        }
    }
}
