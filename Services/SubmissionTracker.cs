using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NopHoSoTuDong.Services
{
    public class SubmissionTracker
    {
        private readonly string _logPath;

        public SubmissionTracker(string folderPath, string logFileName = ".submitted.txt")
        {
            _logPath = Path.Combine(folderPath, logFileName);
        }

        public HashSet<string> LoadProcessed()
        {
            try
            {
                if (!File.Exists(_logPath))
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var lines = File.ReadAllLines(_logPath)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Trim());

                return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // If any error occurs, return empty set to avoid blocking the flow
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public void MarkProcessed(string fileName)
        {
            try
            {
                // ensure uniqueness
                var existing = LoadProcessed();
                if (!existing.Contains(fileName))
                {
                    using var sw = new StreamWriter(_logPath, append: true);
                    sw.WriteLine(fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"! Khong the ghi danh dau file '{fileName}': {ex.Message}");
            }
        }
    }
}
