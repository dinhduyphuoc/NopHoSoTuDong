using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace NopHoSoTuDong.Services
{
    public static class NameExtractor
    {
        public static string ExtractCleanName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            // Bỏ STT đầu: ví dụ "1665 ", "01 - ", "2025_0003."
            fileName = Regex.Replace(fileName, @"^\s*\d+[\s\-._]*", "");
            // Decode %20 → khoảng trắng
            fileName = HttpUtility.UrlDecode(fileName);
            // Chuẩn hóa khoảng trắng
            fileName = Regex.Replace(fileName, @"\s+", " ").Trim();
            return fileName;
        }
    }
}
