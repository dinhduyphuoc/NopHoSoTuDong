using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NopHoSoTuDong.Models;
using System.Collections.Generic;

namespace NopHoSoTuDong.Services
{
    public class AutomationRunner
    {
        private readonly IProgress<string> _log;

        // Regex validate: <STT> <H? và tên> <dd-mm-yyyy>.pdf
        /* private static readonly Regex PdfNameRegex = new Regex(
            @"^(\d{1,5})\s+([A-Za-zÀ-?'’\-\s]+)\s+(\d{2}-\d{2}-\d{4})\.pdf$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        */
        // NAME_FULL_RE (Python) ported to C#
        private static readonly Regex PdfNameRegex = new Regex(
            @"^(\d{1,5})\s+([A-Za-zÀ-ỹ'’](?:[A-Za-zÀ-ỹ'’\s\-]*[A-Za-zÀ-ỹ'’])?)\s+(\d{2}-\d{2}-\d{4})\.pdf$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // Pattern is enforced in the main loop to allow specific logging for format vs date errors.

        public AutomationRunner(IProgress<string> log)
        {
            _log = log;
        }

        public async Task RunAsync(AppSettings settings, string folderPath, string codeNotation, ManualResetEventSlim pauseEvent, IProgress<(int current, int total)>? progress, CancellationToken token, string? templatePathOverride = null)
        {
            using var api = new ApiClient(settings.BaseUrl, settings.Credentials);
            var signer = new VgcaSigner();
            var tracker = new SubmissionTracker(folderPath);
            var failedTracker = new SubmissionTracker(folderPath, ".failed.txt");
            var invalidTracker = new SubmissionTracker(folderPath, ".invalid.txt");
            var processed = tracker.LoadProcessed();
            var allPdfFiles = FolderScanner.GetPdfFiles(folderPath);
            _log.Report($"Tim thay {allPdfFiles.Count} PDF trong folder");
            // Khong goi API pre-check. Chi su dung .submitted.txt de skip.

            // Build normalized keys from processed items
            string NormalizeKey(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                s = Path.GetFileNameWithoutExtension(s);
                s = Regex.Replace(s, "\\s+", " ").Trim();
                var formD = s.Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder(formD.Length);
                foreach (var ch in formD)
                {
                    var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (cat != UnicodeCategory.NonSpacingMark)
                        sb.Append(ch);
                }
                var noDia = sb.ToString().Normalize(NormalizationForm.FormC);
                return noDia.ToLowerInvariant();
            }

            var processedKeys = new List<string>();
            foreach (var p in processed)
            {
                var k = NormalizeKey(p);
                if (!string.IsNullOrEmpty(k)) processedKeys.Add(k);
            }

            var pdfFiles = allPdfFiles
                .Where(f =>
                {
                    var keyLocal = NormalizeKey(Path.GetFileName(f) ?? string.Empty);
                    foreach (var pk in processedKeys)
                    {
                        if (string.IsNullOrEmpty(pk)) continue;
                        if (keyLocal.StartsWith(pk) || keyLocal.Contains(pk))
                            return false;
                    }
                    return true;
                })
                .ToList();

            // Initialize/resume queue from file list
            var queue = new QueueStore(folderPath);
            queue.Load();
            queue.InitializeFromFiles(pdfFiles);

            // Reset stale 'done/failed' entries to 'pending' if the file is not marked processed
            int resetCount = 0;
            foreach (var e in queue.Entries)
            {
                var name = Path.GetFileName(e.FilePath) ?? string.Empty;
                var k = NormalizeKey(name);
                bool isProcessed = processedKeys.Any(pk => !string.IsNullOrEmpty(pk) && (k.StartsWith(pk) || k.Contains(pk)));
                if (!isProcessed && e.Status != "pending" && e.Status != "invalid")
                {
                    e.Status = "pending";
                    e.Attempts = 0;
                    resetCount++;
                }
            }
            if (resetCount > 0)
            {
                queue.Save();
                _log.Report($"Khoi phuc {resetCount} muc hang doi ve 'pending' vi chua duoc danh dau da nop.");
            }
            int total = queue.Entries.Count(e => e.Status != "invalid");
            if (total == 0)
            {
                _log.Report("Khong co file nao can xu ly.");
                return;
            }

            _log.Report($"Se xu ly {total} file; bo qua {allPdfFiles.Count - pdfFiles.Count} file da danh dau.");
            progress?.Report((0, total));
            int done = 0;
            int successCount = 0;
            int errorCount = 0;

            var entriesByPath = queue.Entries.ToDictionary(e => e.FilePath, StringComparer.OrdinalIgnoreCase);
            const int maxTotalAttempts = 3; // used for inline retries only; do not finalize failed entries by attempts

            bool _retryRound = false;
            do
            {
                _retryRound = false;
                foreach (var filePath in pdfFiles)
            {
                token.ThrowIfCancellationRequested();
                await Task.Run(() => pauseEvent.Wait(token));
                if (!entriesByPath.TryGetValue(filePath, out var entry) || entry.Status != "pending")
                    continue;

                try
                {
                    // Safety skip if now marked processed (e.g., via Get Completed or previous step)
                    var keyLocalNow = NormalizeKey(Path.GetFileName(filePath) ?? string.Empty);
                    if (processedKeys.Any(pk => keyLocalNow.StartsWith(pk) || keyLocalNow.Contains(pk)))
                    {
                        _log.Report($"Bo qua do da danh dau: {Path.GetFileName(filePath)}");
                        entry.Status = "done";
                        done++;
                        successCount++;
                        queue.Save();
                        progress?.Report((done, total));
                        continue;
                    }

                    var originalName = Path.GetFileName(filePath) ?? string.Empty;
                    var m = PdfNameRegex.Match(originalName);
                    if (!m.Success)
                    {
                        _log.Report($"Bo qua file sai dinh dang (doi ten roi chay lai): {originalName}");
                        entry.Status = "invalid"; // keep for user to fix name
                        errorCount++;
                        total = Math.Max(0, total - 1);
                        try { invalidTracker.MarkProcessed(originalName); } catch { }
                        queue.Save();
                        progress?.Report((done, total));
                        continue;
                    }
                    var dateStr = m.Groups[3].Value;
                    if (!DateTime.TryParseExact(dateStr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime issueDate))
                    {
                        _log.Report($"Sai dinh dang ngay trong file: {originalName}");
                        continue;
                    }
                    string stt = m.Groups[1].Value.Trim();
                    string ownerName = m.Groups[2].Value.Trim();

                    string tempFileName = $"{stt} {ownerName}.pdf";
                    string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                    File.Copy(filePath, tempFilePath, true);

                    _log.Report($"Dang upload: {tempFileName}");
                    var uploadRes = await api.UploadPdfAsync(tempFilePath, token);
                    token.ThrowIfCancellationRequested();
                    await Task.Run(() => pauseEvent.Wait(token));

                    if (string.IsNullOrWhiteSpace(uploadRes))
                    {
                        _log.Report("Upload that bai: response rong hoac loi mang");
                        entry.Attempts++;
                        entry.Status = "failed";
                        queue.Save();
                        try { File.Delete(tempFilePath); } catch { }
                        continue;
                    }

                    if (!uploadRes.TrimStart().StartsWith("{"))
                    {
                        _log.Report($"Upload tra ve khong phai JSON: {uploadRes}");
                        entry.Attempts++;
                        entry.Status = "failed";
                        queue.Save();
                        try { File.Delete(tempFilePath); } catch { }
                        continue;
                    }

                    var parsed = JsonDocument.Parse(uploadRes);
                    var fileEntryId = parsed.RootElement.GetProperty("fileEntryId").GetString();
                    var fileStoreGovId = parsed.RootElement.GetProperty("fileStoreGovId").GetString();
                    if (string.IsNullOrEmpty(fileEntryId) || string.IsNullOrEmpty(fileStoreGovId))
                    {
                        _log.Report("Upload loi: thieu fileEntryId hoac fileStoreGovId");
                        entry.Attempts++;
                        entry.Status = entry.Attempts >= maxTotalAttempts ? "done" : "failed";
                        if (entry.Status == "done") { done++; errorCount++; progress?.Report((done, total)); }
                        queue.Save();
                        try { File.Delete(tempFilePath); } catch { }
                        continue;
                    }

                    _log.Report($"Upload thanh cong. fileEntryId = {fileEntryId}");

                    // Sign with retry, then skip on persistent failure
                    string? newFileEntryId = null;
                    const int maxSignRetries = 3;
                    for (int attempt = 1; attempt <= maxSignRetries; attempt++)
                    {
                        newFileEntryId = await signer.SignAsync(fileEntryId, token);
                        token.ThrowIfCancellationRequested();
                        await Task.Run(() => pauseEvent.Wait(token));
                        if (!string.IsNullOrEmpty(newFileEntryId))
                            break;

                        _log.Report($"Ky so that bai (lan {attempt}).");
                        if (attempt < maxSignRetries)
                        {
                            try { await Task.Delay(TimeSpan.FromSeconds(3 * attempt), token); } catch { }
                        }
                    }

                    if (string.IsNullOrEmpty(newFileEntryId))
                    {
                        _log.Report($"Ky so that bai sau {maxSignRetries} lan. Bo qua file nay.");
                        entry.Attempts++;
                        entry.Status = "failed";
                        try { failedTracker.MarkProcessed($"{stt} {ownerName}.pdf"); } catch { }
                        try { File.Delete(tempFilePath); } catch { }
                        queue.Save();
                        continue;
                    }

                    _log.Report($"Ky so thanh cong. newFileEntryId = {newFileEntryId}");

                    // Get next dossier number
                    var dossierJson = await api.GetSuggestDossierNoAsync(token);
                    var dossierNo = JsonDocument.Parse(dossierJson).RootElement.GetProperty("generateDossierNo").GetString();
                    string nextDossierNo = dossierNo ?? string.Empty;
                    var matchNo = Regex.Match(nextDossierNo, @"(\d+)$");
                    if (matchNo.Success)
                    {
                        int num = int.Parse(matchNo.Groups[1].Value);
                        string prefix = nextDossierNo.Substring(0, matchNo.Index);
                        int next = num + 1;
                        int pad = matchNo.Groups[1].Value.Length;
                        string nextNumStr = next.ToString("D" + pad);
                        nextDossierNo = $"{prefix}{nextNumStr}";
                    }

                    string deptIssue = codeNotation.Replace("/", " ").Trim();

                    var form = new DossierForm
                    {
                        FileStoreGovId = fileStoreGovId,
                        NewFileEntryId = newFileEntryId,
                        FileName = "Phi?u quân nhân d? b?",
                        DisplayName = $"{stt} {ownerName}.pdf",
                        ServiceCode = "1.001805",
                        IssueDate = issueDate.ToString("dd/MM/yyyy"),
                        OwnerType = "1",
                        OwnerName = ownerName,
                        CodeNumber = "",
                        CodeNotation = codeNotation,
                        AbstractSS = $"Phi?u quân nhân d? b? c?a d?ng chí {ownerName}.",
                        PartType = "1",
                        PartNo = "TP02",
                        DossierNo = nextDossierNo,
                        DepartmentIssue = deptIssue,
                        IsActive = "1"
                    };

                    // Apply template values if template file exists
                    try
                    {
                        string templatePath = templatePathOverride ?? Path.Combine(AppContext.BaseDirectory, "Templates", "phieu_quan_nhan_du_bi.json");
                        if (!File.Exists(templatePath))
                        {
                            string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
                            templatePath = templatePathOverride ?? Path.Combine(projectRoot, "Templates", "phieu_quan_nhan_du_bi.json");
                        }
                        if (File.Exists(templatePath))
                        {
                            var json = File.ReadAllText(templatePath);
                            var template = JsonSerializer.Deserialize<DossierTemplate>(json);
                            if (template != null)
                            {
                                string ApplyTokens(string s) => (s ?? string.Empty)
                                    .Replace("<stt>", stt)
                                    .Replace("<ownerName>", ownerName)
                                    .Replace("<issueDate>", issueDate.ToString("dd/MM/yyyy"))
                                    .Replace("<codeNotation>", codeNotation)
                                    .Replace("<dossierNo>", nextDossierNo)
                                    .Replace("<deptIssue>", deptIssue);

                                form.FileName = template.FileName ?? form.FileName;
                                form.DisplayName = ApplyTokens(template.DisplayNamePattern ?? form.DisplayName);
                                form.ServiceCode = template.ServiceCode ?? form.ServiceCode;
                                form.OwnerType = template.OwnerType ?? form.OwnerType;
                                form.AbstractSS = ApplyTokens(template.AbstractSSPattern ?? form.AbstractSS);
                                form.PartType = template.PartType ?? form.PartType;
                                form.PartNo = template.PartNo ?? form.PartNo;
                                form.IsActive = template.IsActive ?? form.IsActive;
                            }
                        }
                    }
                    catch { }

                    string updateRes = string.Empty;
                    Exception? lastSubmitEx = null;
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        try
                        {
                            updateRes = await api.SubmitDossierAsync(form, token);
                            lastSubmitEx = null;
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (ex is ApiHttpException aex && (aex.StatusCode == System.Net.HttpStatusCode.Unauthorized || aex.StatusCode == System.Net.HttpStatusCode.Forbidden))
                            {
                                _log.Report("Het phien dang nhap hoac khong du quyen. Dung ca batch.");
                                throw new AuthException("Het phien dang nhap hoac khong du quyen");
                            }
                            lastSubmitEx = ex;
                            _log.Report($"Thu nop ho so lan {attempt} that bai: {ex.Message}");
                            // backoff nho giua cac lan
                            try { await Task.Delay(TimeSpan.FromSeconds(2 * attempt), token); } catch { }
                        }
                    }
                    if (lastSubmitEx != null)
                    {
                        _log.Report($"Nop ho so that bai sau 3 lan: {lastSubmitEx.Message}. Se thu lai sau.");
                        // Ghi lai de co the thu lai sau
                        entry.Attempts++;
                        entry.Status = "failed";
                        try { failedTracker.MarkProcessed($"{stt} {ownerName}.pdf"); } catch { }
                        queue.Save();
                        continue;
                    }
                    _log.Report($"Da nop ho so cho {ownerName} (DossierNo: {nextDossierNo})");

                    // Mark processed by displayName and cleanup temp file
                    var dnDone = $"{stt} {ownerName}.pdf";
                    tracker.MarkProcessed(dnDone);
                    var nkDone = NormalizeKey(dnDone);
                    if (!string.IsNullOrEmpty(nkDone)) processedKeys.Add(nkDone);
                    try { File.Delete(tempFilePath); } catch { }
                    entry.Status = "done";
                    done++;
                    successCount++;
                    queue.Save();
                    progress?.Report((done, total));
                }
                catch (OperationCanceledException)
                {
                    _log.Report("Da huy tien trinh theo yeu cau");
                    throw;
                }
                catch (Exception ex)
                {
                    _log.Report($"Loi khi xu ly {Path.GetFileName(filePath)}: {ex.Message}{(ex.InnerException != null ? " => " + ex.InnerException.Message : string.Empty)}");
                    entry.Attempts++;
                    entry.Status = "failed";
                    queue.Save();
                }
            }
                // End foreach filePath
                if (queue.Entries.Any(e => e.Status == "failed"))
                {
                    foreach (var e in queue.Entries.Where(e => e.Status == "failed")) e.Status = "pending";
                    queue.Save();
                    _retryRound = true;
                    try { await Task.Delay(TimeSpan.FromSeconds(5), token); } catch { }
                }
            } while (_retryRound);

            // List all invalid names to console and UI
            try
            {
                var invalidSet = invalidTracker.LoadProcessed();
                if (invalidSet.Count > 0)
                {
                    _log.Report($"Danh sach sai ten ({invalidSet.Count}):");
                    foreach (var name in invalidSet)
                    {
                        Console.WriteLine("[INVALID] " + name);
                        _log.Report(" - " + name);
                    }
                }
            }
            catch { }

            _log.Report($"Tong: {total} - Thanh cong: {successCount} - Loi: {errorCount}");
            _log.Report("Hoan tat nop ho so hang loat");
        }
    }
}


