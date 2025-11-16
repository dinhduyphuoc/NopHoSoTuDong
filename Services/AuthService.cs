using Microsoft.Playwright;
using NopHoSoTuDong.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NetCookie = System.Net.Cookie;

namespace NopHoSoTuDong.Services
{
    
    public class AuthService
    {
        public async Task<LoginResult> LoginAsync(
            string baseUrl,
            string username,
            string password,
            string refererPath,
            string groupId,
            string token)
        {
            var baseUri = new Uri(baseUrl);
            var cookies = new CookieContainer();

            using var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
            };

            using var client = new HttpClient(handler) { BaseAddress = baseUri };

            // Cookie guest
            cookies.Add(baseUri, new NetCookie("COOKIE_SUPPORT", "true") { Path = "/", Secure = true });
            cookies.Add(baseUri, new NetCookie("GUEST_LANGUAGE_ID", "en_US") { Path = "/", Secure = true });

            // GET trước để lấy session
            var refererUrl = new Uri(baseUri, refererPath);
            var preReq = new HttpRequestMessage(HttpMethod.Get, refererUrl);
            preReq.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            await client.SendAsync(preReq); // không cần đọc nội dung

            // POST login
            var post = new HttpRequestMessage(HttpMethod.Post, "/o/v1/opencps/login")
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
            };
            post.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded; charset=UTF-8");

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            post.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            post.Headers.Accept.ParseAdd("application/json, text/plain, */*");
            post.Headers.Referrer = refererUrl;
            post.Headers.Add("Origin", baseUrl);
            post.Headers.Add("groupid", groupId);
            post.Headers.Add("token", token);

            var res = await client.SendAsync(post);
            var body = await res.Content.ReadAsStringAsync();

            var allCookies = new List<NetCookie>();
            foreach (NetCookie c in cookies.GetCookies(baseUri).Cast<NetCookie>()) allCookies.Add(c);
            foreach (NetCookie c in cookies.GetCookies(refererUrl).Cast<NetCookie>()) allCookies.Add(c);

            string cookieStr = string.Join("; ", allCookies.Select(c => $"{c.Name}={c.Value}"));

            var creds = new ApiCredentials
            {
                Token = token,
                GroupId = groupId,
                Cookie = cookieStr
            };

            bool success = res.IsSuccessStatusCode && allCookies.Any(c => c.Name == "SCREEN_NAME");

            return new LoginResult
            {
                Success = success,
                Credentials = creds,
                RawBody = body,
                Cookies = cookies
            };
        }
    }
}
