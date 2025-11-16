using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NopHoSoTuDong.Services
{
    public class QueueEntry
    {
        public string FilePath { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending|done|failed
        public int Attempts { get; set; } = 0;
    }

    public class QueueStore
    {
        private readonly string _path;
        public List<QueueEntry> Entries { get; private set; } = new();

        public QueueStore(string folderPath)
        {
            _path = Path.Combine(folderPath, ".queue.json");
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_path)) { Entries = new(); return; }
                var json = File.ReadAllText(_path);
                var list = JsonSerializer.Deserialize<List<QueueEntry>>(json);
                Entries = list ?? new();
            }
            catch { Entries = new(); }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Entries, new JsonSerializerOptions { WriteIndented = true });
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(_path)) File.Replace(tmp, _path, null); else File.Move(tmp, _path);
            }
            catch { }
        }

        public void InitializeFromFiles(IEnumerable<string> files)
        {
            var map = Entries.ToDictionary(e => e.FilePath, StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                if (!map.ContainsKey(f))
                {
                    Entries.Add(new QueueEntry { FilePath = f, DisplayName = Path.GetFileName(f) ?? "", Status = "pending", Attempts = 0 });
                }
            }
            Save();
        }
    }
}
