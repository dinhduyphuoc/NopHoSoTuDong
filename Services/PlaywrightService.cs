using Microsoft.Playwright;
using NopHoSoTuDong.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NopHoSoTuDong.Services
{
    public class PlaywrightService
    {
        private readonly string _baseUrl;
        private readonly string _portalPath;

        public PlaywrightService(string baseUrl, string portalPath)
        {
            this._baseUrl = baseUrl;
            this._portalPath = portalPath;
        }

        private async Task<IPage> CreatePageAsync(ApiCredentials? creds = null)
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var context = await browser.NewContextAsync();

            // Nếu có credentials thì set cookie
            if (creds is not null && !string.IsNullOrEmpty(creds.Cookie))
            {
                var uri = new Uri(_baseUrl);
                var cookies = creds.Cookie
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c =>
                    {
                        var parts = c.Split('=', 2);
                        return new Cookie
                        {
                            Name = parts[0].Trim(),
                            Value = parts.Length > 1 ? parts[1].Trim() : "",
                            Domain = uri.Host,
                            Path = "/"
                        };
                    });
                await context.AddCookiesAsync(cookies);
            }

            var page = await context.NewPageAsync();
            var fullUrl = new Uri(new Uri(_baseUrl), _portalPath).ToString();
            await page.GotoAsync(fullUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

            return page;
        }

        private async Task<string> GetTokenAsync(IPage page)
        {
            // Liferay stores authToken in window.Liferay.authToken
            return await page.EvaluateAsync<string>("() => window.Liferay?.authToken || ''");
        }

        private async Task<string> GetGroupIdAsync(IPage page)
        {
            // Liferay stores scope group ID in window.themeDisplay
            return await page.EvaluateAsync<string>("() => window.themeDisplay?.getScopeGroupId?.() || ''");
        }

        public async Task<ApiCredentials> GetAllDataAsync(ApiCredentials? creds = null)
        {
            // Nếu chưa có credential => chỉ lấy token và groupId (public info)
            if (creds is null)
            {
                var page = await CreatePageAsync();
                var token = await GetTokenAsync(page);
                var groupId = await GetGroupIdAsync(page);
                return new ApiCredentials(token, groupId, cookie: "");
            }

            // Nếu đã có credential => có thể lấy được cả userId
            var pageWithCred = await CreatePageAsync(creds);
            var token2 = await GetTokenAsync(pageWithCred);
            var groupId2 = await GetGroupIdAsync(pageWithCred);
            // userId is not stored in credentials anymore; only return token/groupId/cookie
            return new ApiCredentials(
                token: token2,
                groupId: groupId2,
                cookie: creds.Cookie
            );
        }
    }
}
