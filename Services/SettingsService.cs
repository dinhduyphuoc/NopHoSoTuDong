using System;
using System.IO;
using System.Text.Json;
using NopHoSoTuDong.Models;

namespace NopHoSoTuDong.Services
{
    public static class SettingsService
    {
        public static string GetOutputSettingsPath()
            => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        public static string GetProjectSettingsPath()
        {
            try
            {
                string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
                return Path.Combine(root, "appsettings.json");
            }
            catch { return GetOutputSettingsPath(); }
        }

        public static AppSettings Load()
        {
            var output = GetOutputSettingsPath();
            var project = GetProjectSettingsPath();
            try
            {
                if (File.Exists(output))
                {
                    var json = File.ReadAllText(output);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) return s;
                }
                if (File.Exists(project))
                {
                    var json = File.ReadAllText(project);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) return s;
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings settings, Action<string>? log = null)
        {
            var output = GetOutputSettingsPath();
            var project = GetProjectSettingsPath();
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            TryWriteAllText(output, json, "output", log);
            TryWriteAllText(project, json, "project", log);
        }

        private static void TryWriteAllText(string path, string content, string label, Action<string>? log)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(path))
                {
                    try
                    {
                        var attrs = File.GetAttributes(path);
                        if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
                        }
                    }
                    catch { }
                }
                File.WriteAllText(path, content);
            }
            catch (Exception ex)
            {
                log?.Invoke($"[WARN] Cannot write {label} appsettings.json at {path}: {ex.Message}");
            }
        }
    }
}

