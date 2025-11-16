using System.Net;

namespace NopHoSoTuDong.Models
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public ApiCredentials Credentials { get; set; } = new();
        public string RawBody { get; set; } = string.Empty;
        public CookieContainer Cookies { get; set; } = new(); // Dùng để gọi API sau login
    }
}
