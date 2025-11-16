using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NopHoSoTuDong.Models;

namespace NopHoSoTuDong.Services
{
    public class AppConfigService
    {
        private const string ConfigFile = "appsettings.json";

        public async Task<ApiCredentials?> LoadAsync()
        {
            if (!File.Exists(ConfigFile)) return null;

            using var stream = File.OpenRead(ConfigFile);
            var root = await JsonSerializer.DeserializeAsync<ConfigRoot>(stream);
            return root?.ApiCredentials;
        }

        public async Task SaveAsync(ApiCredentials credentials)
        {
            ConfigRoot root;

            if (File.Exists(ConfigFile))
            {
                using var stream = File.OpenRead(ConfigFile);
                root = await JsonSerializer.DeserializeAsync<ConfigRoot>(stream) ?? new ConfigRoot();
            }
            else
            {
                root = new ConfigRoot();
            }

            root.ApiCredentials = credentials;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            using var outStream = File.Create(ConfigFile);
            await JsonSerializer.SerializeAsync(outStream, root, options);
        }

        private class ConfigRoot
        {
            public ApiCredentials ApiCredentials { get; set; } = new();
        }
    }
}
