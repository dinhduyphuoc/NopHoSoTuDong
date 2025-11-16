using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NopHoSoTuDong.Models;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NopHoSoTuDong.Services
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _client;

        public ApiClient(string baseUrl, ApiCredentials creds)
        {
            var baseUri = new Uri(baseUrl);
            var cookies = new CookieContainer();

            foreach (var kv in creds.Cookie.Split(';'))
            {
                var part = kv.Trim();
                if (string.IsNullOrEmpty(part)) continue;
                var idx = part.IndexOf('=');
                if (idx <= 0) continue;
                var name = part[..idx].Trim();
                var value = part[(idx + 1)..].Trim();
                cookies.Add(baseUri, new Cookie(name, value) { Path = "/", Secure = true });
            }

            var handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.All
            };

            _client = new HttpClient(handler) { BaseAddress = baseUri };
            _client.Timeout = TimeSpan.FromSeconds(60);
            HttpHeaderHelper.ApplyCommonApiHeaders(_client.DefaultRequestHeaders, creds, baseUrl);
        }

        // Overloads with CancellationToken + typed errors
        public async Task<string> UploadPdfAsync(string filePath, System.Threading.CancellationToken cancellationToken)
        {
            var endpoint = "/o/rest/v2/filestore/uploadforfileentry";
            var fileName = Path.GetFileName(filePath);

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");

            form.Add(fileContent, "attachment", fileName);
            form.Add(new StringContent(fileName), "fileName");

            var res = await _client.PostAsync(endpoint, form, cancellationToken);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new ApiHttpException(res.StatusCode, body, $"Upload failed {res.StatusCode}");

            return body;
        }

        public async Task<string> GetSuggestDossierNoAsync(System.Threading.CancellationToken cancellationToken)
        {
            var endpoint = "/o/rest/v2/filestoregov/suggest-dossierno";
            var res = await _client.GetAsync(endpoint, cancellationToken);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new ApiHttpException(res.StatusCode, body, $"SuggestNo failed {res.StatusCode}");
            return body;
        }

        public async Task<string> SubmitDossierAsync(DossierForm form, System.Threading.CancellationToken cancellationToken)
        {
            string endpoint = $"/o/rest/v2/filestoregov/{form.FileStoreGovId}/update";
            var content = new MultipartFormDataContent
            {
                { new StringContent(form.FileName ?? ""), "fileName" },
                { new StringContent(form.ServiceCode ?? ""), "serviceCode" },
                { new StringContent(form.IssueDate ?? ""), "issueDate" },
                { new StringContent(form.OwnerType ?? ""), "ownerType" },
                { new StringContent(form.OwnerNo ?? ""), "ownerNo" },
                { new StringContent(form.OwnerName ?? ""), "ownerName" },
                { new StringContent(form.CodeNumber ?? ""), "codeNumber" },
                { new StringContent(form.CodeNotation ?? ""), "codeNotation" },
                { new StringContent(form.AbstractSS ?? ""), "abstractSS" },
                { new StringContent(form.PartType ?? ""), "partType" },
                { new StringContent(form.PartNo ?? ""), "partNo" },
                { new StringContent(form.NewFileEntryId ?? ""), "newFileEntryId" },
                { new StringContent(form.FileStoreGovId ?? ""), "fileStoreGovId" },
                { new StringContent(form.DisplayName ?? ""), "displayName" },
                { new StringContent(form.DossierNo ?? ""), "dossierNo" },
                { new StringContent(form.DepartmentIssue ?? ""), "departmentIssue" },
                { new StringContent(form.IsActive ?? "1"), "isActive" }
            };
 
            var res = await _client.PostAsync(endpoint, content, cancellationToken);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new ApiHttpException(res.StatusCode, body, $"POST {endpoint} failed: {res.StatusCode}");
            return body;
        }
        // ✅ Step 1: Upload file
        public async Task<string> UploadPdfAsync(string filePath)
        {
            var endpoint = "/o/rest/v2/filestore/uploadforfileentry";
            var fileName = Path.GetFileName(filePath);

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");

            form.Add(fileContent, "attachment", fileName);
            form.Add(new StringContent(fileName), "fileName");

            var res = await _client.PostAsync(endpoint, form);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Upload failed {res.StatusCode}: {body}");

            return body;
        }

        // ✅ Step 2: Lấy số hồ sơ tự sinh
        public async Task<string> GetSuggestDossierNoAsync()
        {
            var endpoint = "/o/rest/v2/filestoregov/suggest-dossierno";
            var res = await _client.GetAsync(endpoint);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Lỗi {res.StatusCode}: {body}");
            return body;
        }

        // ✅ Step 3: Cập nhật hồ sơ (sau ký số)
        public async Task<string> SubmitDossierAsync(DossierForm form)
        {
            string endpoint = $"/o/rest/v2/filestoregov/{form.FileStoreGovId}/update";
            var content = new MultipartFormDataContent
            {
                { new StringContent(form.FileName ?? ""), "fileName" },
                { new StringContent(form.ServiceCode ?? ""), "serviceCode" },
                { new StringContent(form.IssueDate ?? ""), "issueDate" },
                { new StringContent(form.OwnerType ?? ""), "ownerType" },
                { new StringContent(form.OwnerNo ?? ""), "ownerNo" },
                { new StringContent(form.OwnerName ?? ""), "ownerName" },
                { new StringContent(form.CodeNumber ?? ""), "codeNumber" },
                { new StringContent(form.CodeNotation ?? ""), "codeNotation" },
                { new StringContent(form.AbstractSS ?? ""), "abstractSS" },
                { new StringContent(form.PartType ?? ""), "partType" },
                { new StringContent(form.PartNo ?? ""), "partNo" },
                { new StringContent(form.NewFileEntryId ?? ""), "newFileEntryId" },
                { new StringContent(form.FileStoreGovId ?? ""), "fileStoreGovId" },
                { new StringContent(form.DisplayName ?? ""), "displayName" },
                { new StringContent(form.DossierNo ?? ""), "dossierNo" },
                { new StringContent(form.DepartmentIssue ?? ""), "departmentIssue" },
                { new StringContent(form.IsActive ?? "1"), "isActive" }
            };

            var res = await _client.PostAsync(endpoint, content);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"POST {endpoint} failed: {res.StatusCode}\n{body}");
            return body;
        }

        public async Task<List<string>> SearchDisplayNamesAsync(string fileName, string fromDate, string toDate = "")
        {
            static string Enc(string s) => Uri.EscapeDataString(s ?? string.Empty);

            var sb = new StringBuilder("/o/rest/v2/filestoregov/search?");
            sb.Append($"fileName={Enc(fileName)}");
            sb.Append("&isSiblingSearch=false");
            sb.Append($"&fromDate={Enc(fromDate)}");
            if (!string.IsNullOrWhiteSpace(toDate))
                sb.Append($"&toDate={Enc(toDate)}");

            var res = await _client.GetAsync(sb.ToString());
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"GET {sb} failed: {res.StatusCode} {body}");

            var list = new List<string>();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (item.TryGetProperty("displayName", out var name))
                        {
                            list.Add(name.GetString() ?? string.Empty);
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // Convenience overload: defaults for today's completed dossiers of the standard file name
        public Task<List<string>> SearchDisplayNamesAsync()
        {
            var today = DateTime.Now.ToString("dd/MM/yyyy");
            const string defaultName = "Phiếu quân nhân dự bị";
            return SearchDisplayNamesAsync(defaultName, today, today);
        }

        public async Task<string> SearchDisplayNamesRawAsync(string fileName, string fromDate, string toDate = "")
        {
            static string Enc(string s) => Uri.EscapeDataString(s ?? string.Empty);
            var sb = new StringBuilder("/o/rest/v2/filestoregov/search?");
            sb.Append($"fileName={Enc(fileName)}");
            sb.Append("&isSiblingSearch=false");
            sb.Append($"&fromDate={Enc(fromDate)}");
            if (!string.IsNullOrWhiteSpace(toDate))
                sb.Append($"&toDate={Enc(toDate)}");

            var res = await _client.GetAsync(sb.ToString());
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"GET {sb} failed: {res.StatusCode} {body}");
            return body;
        }

        // Convenience overload: raw JSON for today's completed dossiers with default file name
        public Task<string> SearchDisplayNamesRawAsync()
        {
            var today = DateTime.Now.ToString("dd/MM/yyyy");
            const string defaultName = "Phiếu quân nhân dự bị";
            return SearchDisplayNamesRawAsync(defaultName, today, today);
        }

        // Paged versions for large result sets
        public async Task<string> SearchDisplayNamesRawPagedAsync(string fileName, string fromDate, string toDate, int start, int end)
        {
            static string Enc(string s) => Uri.EscapeDataString(s ?? string.Empty);
            var sb = new StringBuilder("/o/rest/v2/filestoregov/search?");
            sb.Append($"fileName={Enc(fileName)}");
            sb.Append("&isSiblingSearch=false");
            sb.Append($"&fromDate={Enc(fromDate)}");
            if (!string.IsNullOrWhiteSpace(toDate)) sb.Append($"&toDate={Enc(toDate)}");
            sb.Append($"&start={start}");
            sb.Append($"&end={end}");

            var res = await _client.GetAsync(sb.ToString());
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"GET {sb} failed: {res.StatusCode} {body}");
            return body;
        }

        public async Task<(int total, List<string> names)> SearchDisplayNamesPagedAsync(string fileName, string fromDate, string toDate, int start, int end)
        {
            var json = await SearchDisplayNamesRawPagedAsync(fileName, fromDate, toDate, start, end);
            int total = 0;
            var result = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("total", out var t) && t.ValueKind == JsonValueKind.Number) total = t.GetInt32();
                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (item.TryGetProperty("displayName", out var name))
                            result.Add(name.GetString() ?? string.Empty);
                    }
                }
            }
            catch { }
            return (total, result);
        }

        public async Task<(string username, string email)> GetUserProfileAsync()
        {
            var endpoint = $"/o/rest/v2/employees/byGroupId";
            var res = await _client.GetAsync(endpoint);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"GET {endpoint} failed: {res.StatusCode} {body}");

            string username = "";
            string email = "";
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("fullName", out var sn))
                    username = sn.GetString() ?? "";
                if (root.TryGetProperty("email", out var em))
                    email = em.GetString() ?? "";
            }
            catch { }
            return (username, email);
        }

        public void Dispose() => _client?.Dispose();
    }
}
