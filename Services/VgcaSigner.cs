using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NopHoSoTuDong.Services
{
    public class VgcaSigner
    {
        public async Task<string?> SignAsync(string fileEntryId, CancellationToken cancellationToken = default)
        {
            // Best-effort: ensure no stale VGCA signer UI is lingering
            Console.WriteLine($"[VGCA] Dang ket noi den VGCA de ky. fileEntryId={fileEntryId}");

            using var ws = new ClientWebSocket();
            var uri = new Uri("wss://127.0.0.1:8987/SignApproved");

            ws.Options.SetRequestHeader("Origin", "https://motcua.mod.gov.vn");
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            await ws.ConnectAsync(uri, cancellationToken);
            Console.WriteLine("[VGCA] Da ket noi thanh cong den WebSocket local.");

            var msgObj = new
            {
                FileUploadHandler = $"https://motcua.mod.gov.vn/o/rest/v2/vgca/fileupload/{fileEntryId}",
                SessionId = "",
                FileName = $"https://motcua.mod.gov.vn/o/rest/v2/filestore/viewFile/{fileEntryId}"
            };

            var msgJson = JsonSerializer.Serialize(msgObj);
            await ws.SendAsync(Encoding.UTF8.GetBytes(msgJson), WebSocketMessageType.Text, true, cancellationToken);
            Console.WriteLine("[VGCA] Da gui yeu cau ky so.");

            // Khi VGCA mo giao dien ky, se tu dong chay kyso.ahk
            RunAutoHotkeyScript();

            var buffer = new byte[8192];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(2));

            while (!cts.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("[VGCA] Ket noi VGCA da dong.");
                    break;
                }

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("[VGCA] Phan hoi: " + text);
                try
                {
                    using var doc = JsonDocument.Parse(text);
                    if (doc.RootElement.TryGetProperty("Status", out var status) && status.GetInt32() == 0)
                    {
                        string? newFileEntryId = null;

                        var fsEl = doc.RootElement.GetProperty("FileServer");

                        if (fsEl.ValueKind == JsonValueKind.Object)
                        {
                            newFileEntryId = fsEl.GetProperty("fileEntryId").GetString();
                        }
                        else if (fsEl.ValueKind == JsonValueKind.String)
                        {
                            var innerJson = fsEl.GetString();
                            if (!string.IsNullOrEmpty(innerJson))
                            {
                                using var innerDoc = JsonDocument.Parse(innerJson);
                                newFileEntryId = innerDoc.RootElement.GetProperty("fileEntryId").GetString();
                            }
                        }

                        if (!string.IsNullOrEmpty(newFileEntryId))
                        {
                            Console.WriteLine($"[VGCA] Ky so thanh cong. newFileEntryId={newFileEntryId}");
                            return newFileEntryId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[VGCA] Loi parse JSON: " + ex.Message);
                }
            }

            Console.WriteLine("[VGCA] Khong nhan duoc phan hoi ky so thanh cong.");
            return null;
        }

        private void RunAutoHotkeyScript()
        {
            try
            {
                // Tim script trong output (bin/Debug/net8.0-windows/Scripts/)
                string scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "kyso.ahk");

                // Neu khong co (khi chay debug) thi tim trong project root
                if (!File.Exists(scriptPath))
                {
                    string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
                    scriptPath = Path.Combine(projectRoot, "Scripts", "kyso.ahk");
                }

                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine($"[VGCA] Khong tim thay script AutoHotkey: {scriptPath}");
                    return;
                }

                // Chay script bang cmd /c start de khong block chuong trinh
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{scriptPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(psi);
                Console.WriteLine($"[VGCA] Da chay script AutoHotkey: {scriptPath}");

                // Cho mot khoang ngan de UI VGCA mo truoc khi AHK thao tac
                // Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VGCA] Khong the chay kyso.ahk: " + ex.Message);
            }
        }
    }
}

