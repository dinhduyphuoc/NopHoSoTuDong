using System.Text.Json.Serialization;

namespace NopHoSoTuDong.Models
{
    public class AppSettings
    {
        [JsonPropertyName("BaseUrl")]
        public string BaseUrl { get; set; } = string.Empty;

        [JsonPropertyName("Credentials")]
        public ApiCredentials Credentials { get; set; } = new ApiCredentials();

        // UI login persistence
        [JsonPropertyName("RememberMe")]
        public bool RememberMe { get; set; } = false;

        [JsonPropertyName("LoginUsername")]
        public string LoginUsername { get; set; } = string.Empty;

        [JsonPropertyName("LoginPassword")]
        public string LoginPassword { get; set; } = string.Empty;
    }
}
