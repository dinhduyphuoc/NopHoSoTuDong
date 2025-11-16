using System;
using System.Net.Http.Headers;
using NopHoSoTuDong.Models;

namespace NopHoSoTuDong.Services
{
    public static class HttpHeaderHelper
    {
        public static void ApplyCommonApiHeaders(HttpRequestHeaders headers, ApiCredentials creds, string baseUrl)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (creds == null) throw new ArgumentNullException(nameof(creds));

            headers.Accept.Clear();
            headers.Accept.ParseAdd("application/json, text/plain, */*");
            headers.AcceptEncoding.ParseAdd("gzip, deflate, br");
            headers.AcceptLanguage.ParseAdd("vi-VN,vi;q=0.9,fr-FR;q=0.8,fr;q=0.7,en-US;q=0.6,en;q=0.5");

            if (!headers.Contains("X-Requested-With"))
                headers.Add("X-Requested-With", "XMLHttpRequest");

            // Liferay/OpenCPS custom headers (case-insensitive at server, add canonical forms)
            SetOrReplace(headers, "Token", creds.Token);
            SetOrReplace(headers, "groupId", creds.GroupId);
            SetOrReplace(headers, "GroupId", creds.GroupId);
            SetOrReplace(headers, "groupid", creds.GroupId);

            headers.UserAgent.Clear();
            headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
            try
            {
                var baseTrim = baseUrl.TrimEnd('/');
                headers.Referrer = new Uri($"{baseTrim}/web/mot-cua-bo-quoc-phong/mot-cua-dien-tu");
                SetOrReplace(headers, "Origin", baseTrim);
            }
            catch { }
        }

        private static void SetOrReplace(HttpRequestHeaders headers, string name, string? value)
        {
            try
            {
                if (headers.Contains(name)) headers.Remove(name);
            }
            catch { }
            if (!string.IsNullOrWhiteSpace(value)) headers.Add(name, value);
        }
    }
}

