using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NopHoSoTuDong.Services
{
    public static class FolderScanner
    {
        public static List<string> GetPdfFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"❌ Folder không tồn tại: {folderPath}");
                return new List<string>();
            }

            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly).ToList();
            if (!pdfFiles.Any())
                Console.WriteLine("⚠️ Không tìm thấy file PDF nào trong thư mục.");

            return pdfFiles;
        }
    }
}
