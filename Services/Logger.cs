using System;
using System.IO;

namespace NopHoSoTuDong.Services
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "autosigner.log");

        public static void Write(string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
            Console.WriteLine(line);
        }
    }
}
