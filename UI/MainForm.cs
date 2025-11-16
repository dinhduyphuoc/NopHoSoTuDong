using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using NopHoSoTuDong.Models;
using NopHoSoTuDong.Services;

namespace NopHoSoTuDong.UI
{
    public class MainForm : Form
    {
        // Global hotkeys (Ctrl+Alt+P / Ctrl+Alt+C)
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID_PAUSE = 1;
        private const int HOTKEY_ID_CANCEL = 2;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        // Controls
        private TextBox txtBaseUrl = new();
        private TextBox txtGroupId = new();
        private TextBox txtToken = new();
        private TextBox txtUsername = new();
        private TextBox txtPassword = new();
        private CheckBox chkRemember = new();
        private Button btnLogin = new();
        private Button btnLogout = new();

        private TextBox txtFolder = new();
        private Button btnBrowse = new();
        private TextBox txtCodeNotation = new();
        private CheckBox chkSyncBeforeRun = new();
        private DateTimePicker dtFrom = new();
        private DateTimePicker dtTo = new();

        private Button btnStart = new();
        private Button btnPause = new();
        private Button btnCancel = new();
        private Button btnResetSubmitted = new();
        private Button btnGetCompleted = new();
        private Button btnExportLog = new();
        private Button btnOpenFailed = new();
        private Button btnOpenInvalid = new();

        private ProgressBar progressBar = new();
        private TextBox txtLog = new();

        private ComboBox cboTemplate = new();
        private Button btnEditTemplate = new();
        private Button btnNewTemplate = new();
        private Label lblUserInfo = new();
        private Label lblAccountValue = new();
        private Label lblEmailValue = new();
        private Label lblUsernameLeft = new();
        private Label lblPasswordEmailLeft = new();

        // State
        private CancellationTokenSource? _cts;
        private readonly ManualResetEventSlim _pauseEvent = new(true);
        private bool _paused = false;

        private readonly AuthService _authService = new();

        public MainForm()
        {
            Text = "Nộp Hồ Sơ Tự Động";
            try { Font = new System.Drawing.Font("Segoe UI", 10F); } catch { }
            try { MinimumSize = new System.Drawing.Size(1000, 700); MaximizeBox = true; FormBorderStyle = FormBorderStyle.Sizable; } catch { }

            BuildLayout();
            LoadSettingsIntoUI();
            try { ToggleLoginUi(false); } catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Ctrl+Alt+P => Pause/Resume, Ctrl+Alt+C => Cancel
            RegisterHotKey(Handle, HOTKEY_ID_PAUSE, MOD_CONTROL | MOD_ALT, (uint)Keys.P);
            RegisterHotKey(Handle, HOTKEY_ID_CANCEL, MOD_CONTROL | MOD_ALT, (uint)Keys.C);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { UnregisterHotKey(this.Handle, HOTKEY_ID_PAUSE); } catch { }
            try { UnregisterHotKey(this.Handle, HOTKEY_ID_CANCEL); } catch { }
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_PAUSE)
                {
                    TogglePause();
                }
                else if (id == HOTKEY_ID_CANCEL)
                {
                    CancelRun();
                }
            }
            base.WndProc(ref m);
        }

        private void BuildLayout()
        {
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(10)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            main.RowStyles.Add(new RowStyle()); // Đăng nhập
            main.RowStyles.Add(new RowStyle()); // Tệp & Tham số
            main.RowStyles.Add(new RowStyle()); // Bộ lọc
            main.RowStyles.Add(new RowStyle()); // Hành động
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Nhật ký

            // Đăng nhập
            var grpLogin = new GroupBox { Text = "Đăng nhập", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var tlLogin = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            tlLogin.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlLogin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlLogin.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblUsernameLeft.Text = "Tài khoản"; lblUsernameLeft.AutoSize = true; lblUsernameLeft.Anchor = AnchorStyles.Left;
            lblPasswordEmailLeft.Text = "Mật khẩu"; lblPasswordEmailLeft.AutoSize = true; lblPasswordEmailLeft.Anchor = AnchorStyles.Left;
            txtUsername.Dock = DockStyle.Fill;
            txtPassword.Dock = DockStyle.Fill;
            txtPassword.UseSystemPasswordChar = true;
            btnLogin.Text = "Đăng nhập"; btnLogin.AutoSize = true; btnLogin.Click += async (s, e) => await HandleLoginAsync();
            btnLogout.Text = "Đăng xuất"; btnLogout.AutoSize = true; btnLogout.Click += (s, e) => HandleLogout();
            chkRemember.Text = "Nhớ tôi"; chkRemember.AutoSize = true;
            tlLogin.Controls.Add(lblUsernameLeft, 0, 0); tlLogin.Controls.Add(txtUsername, 1, 0); tlLogin.Controls.Add(btnLogin, 2, 0);
            tlLogin.Controls.Add(lblPasswordEmailLeft, 0, 1); tlLogin.Controls.Add(txtPassword, 1, 1); tlLogin.Controls.Add(chkRemember, 2, 1);
            tlLogin.Controls.Add(btnLogout, 2, 0);
            // Value labels to replace inputs after login
            lblAccountValue.AutoSize = true; lblAccountValue.Anchor = AnchorStyles.Left; lblAccountValue.Visible = false;
            lblEmailValue.AutoSize = true; lblEmailValue.Anchor = AnchorStyles.Left; lblEmailValue.Visible = false;
            tlLogin.Controls.Add(lblAccountValue, 1, 0);
            tlLogin.Controls.Add(lblEmailValue, 1, 1);
            // Tag the username/password labels for later visibility toggling
            try
            {
                var _lblUser = tlLogin.GetControlFromPosition(0, 0) as Label; if (_lblUser != null) _lblUser.Name = "lblUsername";
                var _lblPass = tlLogin.GetControlFromPosition(0, 1) as Label; if (_lblPass != null) _lblPass.Name = "lblPassword";
            }
            catch { }
            // Label hien thi thong tin tai khoan/email (khong dau)
            lblUserInfo.Name = "lblUserInfo";
            lblUserInfo.AutoSize = true;
            lblUserInfo.Text = "Cưa đăng nhập";
            lblUserInfo.Visible = false;
            tlLogin.Controls.Add(lblUserInfo, 0, 3);
            tlLogin.SetColumnSpan(lblUserInfo, 3);
            grpLogin.Controls.Add(tlLogin);

            // Tệp & Tham số
            var grpFiles = new GroupBox { Text = "Tệp & Tham số", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var tlFiles = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };
            tlFiles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlFiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlFiles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var lblFolder = new Label { Text = "Folder PDF", AutoSize = true, Anchor = AnchorStyles.Left };
            btnBrowse.Text = "..."; btnBrowse.Click += (s, e) => BrowseFolder();
            var lblCodeNotation = new Label { Text = "CodeNotation", AutoSize = true, Anchor = AnchorStyles.Left };
            txtFolder.Dock = DockStyle.Fill; txtCodeNotation.Dock = DockStyle.Fill;
            tlFiles.Controls.Add(lblFolder, 0, 0); tlFiles.Controls.Add(txtFolder, 1, 0); tlFiles.Controls.Add(btnBrowse, 2, 0);
            tlFiles.Controls.Add(lblCodeNotation, 0, 1); tlFiles.Controls.Add(txtCodeNotation, 1, 1);
            grpFiles.Controls.Add(tlFiles);

            // Bộ lọc
            var grpFilters = new GroupBox { Text = "Bộ lọc", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var flt = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = true };
            chkSyncBeforeRun.Text = "Sync API trước khi chạy"; chkSyncBeforeRun.AutoSize = true; chkSyncBeforeRun.Checked = true;
            var lblFrom = new Label { Text = "From", AutoSize = true, Margin = new Padding(8, 6, 3, 6) };
            var lblTo = new Label { Text = "To", AutoSize = true, Margin = new Padding(8, 6, 3, 6) };
            dtFrom.Format = DateTimePickerFormat.Custom; dtFrom.CustomFormat = "dd/MM/yyyy"; dtFrom.Value = DateTime.Now.Date;
            dtTo.Format = DateTimePickerFormat.Custom; dtTo.CustomFormat = "dd/MM/yyyy"; dtTo.Value = DateTime.Now.Date;
            flt.Controls.Add(chkSyncBeforeRun); flt.Controls.Add(lblFrom); flt.Controls.Add(dtFrom); flt.Controls.Add(lblTo); flt.Controls.Add(dtTo);
            grpFilters.Controls.Add(flt);

            // Hành động
            var grpActions = new GroupBox { Text = "Hành động", Dock = DockStyle.Top, AutoSize = false, Height = 120, MinimumSize = new Size(0, 80), Padding = new Padding(10) };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            // Template controls
            actions.Controls.Add(new Label { Text = "Template", AutoSize = true, Anchor = AnchorStyles.Left });
            cboTemplate.DropDownStyle = ComboBoxStyle.DropDownList; cboTemplate.Width = 240; cboTemplate.DisplayMember = "Name";
            actions.Controls.Add(cboTemplate);
            btnEditTemplate.Text = "Sửa template"; actions.Controls.Add(btnEditTemplate);
            btnNewTemplate.Text = "Tạo template mới"; actions.Controls.Add(btnNewTemplate);
            LoadTemplatesIntoCombo();

            // Action buttons
            btnStart.Text = "Bắt đầu"; btnStart.Click += async (s, e) => await StartRunAsync(); actions.Controls.Add(btnStart);
            btnPause.Text = "Tạm dừng"; btnPause.Click += (s, e) => TogglePause(); actions.Controls.Add(btnPause);
            btnCancel.Text = "Hủy"; btnCancel.Click += (s, e) => CancelRun(); actions.Controls.Add(btnCancel);
            btnResetSubmitted.Text = "Reset Submitted"; btnResetSubmitted.Click += (s, e) => ResetSubmitted(); actions.Controls.Add(btnResetSubmitted);
            btnGetCompleted.Text = "Get Completed"; btnGetCompleted.Click += async (s, e) => await GetCompletedImplAsync(); actions.Controls.Add(btnGetCompleted);
            btnExportLog.Text = "Xuất nhật ký (CSV)"; btnExportLog.Click += (s, e) => ExportLog(); actions.Controls.Add(btnExportLog);
            btnOpenFailed.Text = "Mở danh sách lỗi"; btnOpenFailed.Click += (s, e) => OpenFailed(); actions.Controls.Add(btnOpenFailed);
            btnOpenInvalid.Text = "Sai tên"; btnOpenInvalid.Click += (s, e) => OpenInvalid(); actions.Controls.Add(btnOpenInvalid);
            progressBar.Dock = DockStyle.Top; progressBar.Height = 16; progressBar.Margin = new Padding(0, 6, 0, 0);
            grpActions.Controls.Add(actions); grpActions.Controls.Add(progressBar);

            // Nhật ký
            txtLog.Multiline = true; txtLog.ReadOnly = true; txtLog.ScrollBars = ScrollBars.Vertical; txtLog.Dock = DockStyle.Fill;

            // Assemble
            main.Controls.Add(grpLogin);
            main.Controls.Add(grpFiles);
            main.Controls.Add(grpFilters);
            main.Controls.Add(grpActions);
            main.Controls.Add(txtLog);
            Controls.Clear(); Controls.Add(main);
            ResumeLayout(true);

            // Template editor handlers
            // btnEditTemplate.Click += (s, e) => { try { new TemplateEditorForm().ShowDialog(this); } catch (Exception ex) { Log("[ERROR] Open editor: " + ex.Message); } };
            btnNewTemplate.Click += (s, e) => CreateDefaultTemplateAndOpen();

            // Normalize Vietnamese labels and ensure buttons size to content
            try
            {
                // Group title and template label
                grpActions.Text = "Tiến trình";
                // Ensure action panel grows to fit
                actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                // First control in 'actions' is the template label
                if (actions.Controls.Count > 0 && actions.Controls[0] is Label tplLbl)
                    tplLbl.Text = "Mẫu";

                // Template buttons
                btnEditTemplate.Text = "Sửa mẫu"; btnEditTemplate.AutoSize = true;
                btnNewTemplate.Text = "Tạo mẫu mới"; btnNewTemplate.AutoSize = true;

                // Action buttons
                btnStart.Text = "Bắt đầu"; btnStart.AutoSize = true;
                btnPause.Text = "Tạm dừng"; btnPause.AutoSize = true;
                btnCancel.Text = "Hủy"; btnCancel.AutoSize = true;
                btnResetSubmitted.Text = "Đặt lại đã nộp"; btnResetSubmitted.AutoSize = true;
                btnGetCompleted.Text = "Lấy hồ sơ hoàn tất"; btnGetCompleted.AutoSize = true;
                btnExportLog.Text = "Xuất nhật ký (CSV)"; btnExportLog.AutoSize = true;
                btnOpenFailed.Text = "Mở danh sách lỗi"; btnOpenFailed.AutoSize = true;
                btnOpenInvalid.Text = "Sai tên"; btnOpenInvalid.AutoSize = true;
            }
            catch { }
        }

        private void LoadTemplatesIntoCombo()
        {
            try
            {
                cboTemplate.Items.Clear();
                string runtimeDir = Path.Combine(AppContext.BaseDirectory, "Templates");
                string devDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\\..\\..\\Templates"));
                if (Directory.Exists(runtimeDir))
                    foreach (var f in Directory.GetFiles(runtimeDir, "*.json")) cboTemplate.Items.Add(new FileInfo(f));
                if (Directory.Exists(devDir))
                    foreach (var f in Directory.GetFiles(devDir, "*.json"))
                    {
                        var name = Path.GetFileName(f);
                        if (!Enumerable.Cast<object>(cboTemplate.Items).Any(it => (it as FileInfo)?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true))
                            cboTemplate.Items.Add(new FileInfo(f));
                    }
                if (cboTemplate.Items.Count > 0) { cboTemplate.SelectedIndex = 0; }
            }
            catch { }
        }

        private void CreateDefaultTemplateAndOpen()
        {
            try
            {
                string devDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\\..\\..\\Templates"));
                string runtimeDir = Path.Combine(AppContext.BaseDirectory, "Templates");
                string dir = Directory.Exists(devDir) ? devDir : runtimeDir;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "phieu_quan_nhan_du_bi.json");
                var tpl = new DossierTemplate
                {
                    FileName = "<stt> <ownerName>.pdf",
                    DisplayNamePattern = "Phiếu quân nhân dự bị - <ownerName>",
                    AbstractSSPattern = "Phiếu quân nhân dự bị của <ownerName>.",
                    ServiceCode = "",
                    OwnerType = "1",
                    PartType = "1",
                    PartNo = "TP02",
                    IsActive = "1"
                };
                var json = System.Text.Json.JsonSerializer.Serialize(tpl, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json, Encoding.UTF8);
                // try { new TemplateEditorForm().ShowDialog(this); } catch { }
                LoadTemplatesIntoCombo();
            }
            catch (Exception ex) { Log("[ERROR] New template: " + ex.Message); }
        }

        private string? GetSelectedTemplatePath()
        {
            try
            {
                if (cboTemplate.SelectedItem is FileInfo fi) return fi.FullName;
            }
            catch { }
            return null;
        }

        // Actions
        private async Task HandleLoginAsync()
        {
            var baseUrl = txtBaseUrl.Text.Trim().TrimEnd('/');
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text;

            btnLogin.Enabled = false;
            try
            {
                Log("[INFO] Đang đăng nhập vào tài khoản " + username + ".");

                const string portalPath = "/web/mot-cua-bo-quoc-phong/mot-cua-dien-tu";

                // Lấy token/groupId ở chế độ khách
                var pw = new PlaywrightService(baseUrl, portalPath);
                var anon = await pw.GetAllDataAsync();

                // Gọi API đăng nhập
                var result = await _authService.LoginAsync(baseUrl, username, password, portalPath, anon.GroupId, anon.Token);

                if (result.Success)
                {
                    // Làm mới token/groupId theo phiên đã xác thực
                    try
                    {
                        var pwAuth = new PlaywrightService(baseUrl, portalPath);
                        var refreshed = await pwAuth.GetAllDataAsync(result.Credentials);
                        if (!string.IsNullOrWhiteSpace(refreshed.Token)) result.Credentials.Token = refreshed.Token;
                        if (!string.IsNullOrWhiteSpace(refreshed.GroupId)) result.Credentials.GroupId = refreshed.GroupId;
                    }
                    catch { }

                    Log("[OK] Đăng nhập thành công");

                    // Lưu cấu hình
                    var settingsFromResponse = new AppSettings
                    {
                        BaseUrl = baseUrl,
                        RememberMe = chkRemember.Checked,
                        LoginUsername = chkRemember.Checked ? username : string.Empty,
                        Credentials = new ApiCredentials
                        {
                            Token = result.Credentials.Token,
                            GroupId = result.Credentials.GroupId,
                            Cookie = result.Credentials.Cookie
                        }
                    };
                    SaveSettings(settingsFromResponse);

                    // Cập nhật UI
                    txtToken.Text = result.Credentials.Token;
                    txtGroupId.Text = result.Credentials.GroupId;

                    await FetchAndShowUserInfoAsync(baseUrl);
                    ToggleLoginUi(true);
                }
                else
                {
                    Log("[ERROR] Đăng nhập thất bại");
                    Log(result.RawBody);
                }
            }
            catch (Exception ex)
            {
                Log("[ERROR] Login: " + ex.Message);
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }
        private async Task FetchAndShowUserInfoAsync(string baseUrl)
        {
            try
            {
                var settings = ReadSettings();
                using var api = new ApiClient(baseUrl, settings.Credentials);
                var (username, email) = await api.GetUserProfileAsync();
                try { lblUserInfo.Text = $"Tài khoản: {username}{Environment.NewLine}Email: {email}"; } catch { }
                try { lblAccountValue.Text = username; lblEmailValue.Text = email; } catch { }
                Log("[OK] Lấy thông tin người dùng: " + username + " | " + email);
            }
            catch (Exception ex)
            {
                Log("[ERROR] Lấy thông tin người dùng: " + ex.Message);
            }
        }

        private void ToggleLoginUi(bool loggedIn)
        {
            try
            {
                // Đăng nhập
                txtUsername.Enabled = !loggedIn;
                txtPassword.Enabled = !loggedIn;
                chkRemember.Enabled = !loggedIn;
                btnLogin.Enabled = !loggedIn;
                btnLogout.Enabled = loggedIn;
                // Visible theo trang thai dang nhap
                try
                {
                    btnLogin.Visible = !loggedIn;
                    btnLogout.Visible = loggedIn;
                    // Replace inputs with value labels in-place
                    txtUsername.Visible = !loggedIn;
                    txtPassword.Visible = !loggedIn;
                    chkRemember.Visible = !loggedIn;
                    lblAccountValue.Visible = loggedIn;
                    lblEmailValue.Visible = loggedIn;
                    // Keep left labels visible; switch "Mat khau" to "Email" on login
                    lblUsernameLeft.Visible = true;
                    lblPasswordEmailLeft.Visible = true;
                    lblPasswordEmailLeft.Text = loggedIn ? "Email" : "Mat khau";
                    // Do not display the summary label below when using in-place labels
                    lblUserInfo.Visible = false;
                }
                catch { }

                // Kết nối (base url có thể khoá sau khi login)
                txtBaseUrl.Enabled = !loggedIn;
                txtGroupId.Enabled = false; // luôn readonly vì được lấy tự động
                txtToken.Enabled = false;   // luôn readonly vì được lấy tự động

                // Hành động
                btnStart.Enabled = loggedIn && _cts == null;
                btnPause.Enabled = false;
                btnCancel.Enabled = false;
                btnResetSubmitted.Enabled = loggedIn;
                btnGetCompleted.Enabled = loggedIn;
                btnExportLog.Enabled = true;   // luôn cho phép xuất log
                btnOpenFailed.Enabled = true;  // luôn cho phép mở failed
                btnOpenInvalid.Enabled = true; // luôn cho phép mở invalid

                // Template
                cboTemplate.Enabled = loggedIn;
                btnEditTemplate.Enabled = true;
                btnNewTemplate.Enabled = loggedIn;
            }
            catch { /* no-op */ }
        }

        private void HandleLogout()
        {
            try
            {
                txtToken.Text = txtGroupId.Text = string.Empty;
                if (!chkRemember.Checked) { txtUsername.Text = txtPassword.Text = string.Empty; }
                Log("[INFO] Đã đăng xuất.");
                try { ToggleLoginUi(false); lblUserInfo.Text = "Chua dang nhap"; } catch { }
            }
            catch (Exception ex) { Log("[ERROR] Logout: " + ex.Message); }
        }

        private async Task StartRunAsync()
        {
            if (_cts != null) { Log("[WARN] Đang chạy rồi"); return; }
            var folder = txtFolder.Text.Trim();
            if (!Directory.Exists(folder)) { Log("[WARN] Folder PDF không hợp lệ"); return; }
            var s = ReadSettings();
            _cts = new CancellationTokenSource();
            _paused = false; _pauseEvent.Set();
            btnStart.Enabled = false; btnPause.Enabled = true; btnCancel.Enabled = true;
            try
            {
                var runner = new AutomationRunner(new Progress<string>(Log));
                var prog = new Progress<(int current, int total)>(p =>
                {
                    if (p.total <= 0) p = (p.current, 1);
                    if (progressBar.Maximum != p.total) progressBar.Maximum = p.total;
                    progressBar.Value = Math.Max(0, Math.Min(p.current, p.total));
                });
                Log("[INFO] Bắt đầu chạy tiến trình...");
                await runner.RunAsync(s, folder, txtCodeNotation.Text.Trim(), _pauseEvent, prog, _cts.Token, GetSelectedTemplatePath());
                Log("[OK] Hoàn tất.");
            }
            catch (OperationCanceledException) { Log("[INFO] Đã hủy."); }
            catch (Exception ex) { Log("[ERROR] Run: " + ex.Message); }
            finally
            {
                _cts = null; btnStart.Enabled = true; btnPause.Enabled = false; btnCancel.Enabled = false; progressBar.Value = 0;
            }
        }

        private void TogglePause()
        {
            if (_cts == null) return;
            _paused = !_paused;
            if (_paused) { _pauseEvent.Reset(); btnPause.Text = "Tiếp tục"; Log("[INFO] Tạm dừng"); }
            else { _pauseEvent.Set(); btnPause.Text = "Tạm dừng"; Log("[INFO] Tiếp tục"); }
        }

        private void CancelRun()
        {
            if (_cts == null) return;
            _cts.Cancel(); _pauseEvent.Set(); Log("[INFO] Yêu cầu hủy");
        }

        private void ResetSubmitted()
        {
            try
            {
                var folder = txtFolder.Text.Trim();
                var path = Path.Combine(folder, ".submitted.txt");
                if (File.Exists(path)) { File.Delete(path); Log("[OK] Đã xóa .submitted.txt"); }
                else Log("[INFO] Không có .submitted.txt");
            }
            catch (Exception ex) { Log("[ERROR] ResetSubmitted: " + ex.Message); }
        }

        private async Task GetCompletedImplAsync()
        {
            try
            {
                var baseUrl = txtBaseUrl.Text.Trim().TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl)) { Log("[WARN] Base URL khong hop le"); return; }

                var settings = ReadSettings();
                if (settings?.Credentials == null || string.IsNullOrWhiteSpace(settings.Credentials.Token))
                {
                    Log("[WARN] Chua dang nhap hoac thieu thong tin xac thuc");
                    return;
                }

                var folder = txtFolder.Text.Trim();
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    Log("[WARN] Folder PDF khong hop le");
                    return;
                }

                string fromDate = dtFrom.Value.ToString("dd/MM/yyyy");
                string toDate = dtTo.Value.ToString("dd/MM/yyyy");

                // Co gang lay bo loc fileName tu template neu khong co token, neu khong de trong de lay tat ca
                string fileNameFilter = string.Empty;
                try
                {
                    var tplPath = GetSelectedTemplatePath();
                    if (!string.IsNullOrEmpty(tplPath) && File.Exists(tplPath))
                    {
                        var json = File.ReadAllText(tplPath, Encoding.UTF8);
                        var tpl = System.Text.Json.JsonSerializer.Deserialize<DossierTemplate>(json);
                        if (tpl != null)
                        {
                            if (!string.IsNullOrWhiteSpace(tpl.DisplayNamePattern) && !tpl.DisplayNamePattern.Contains("<"))
                                fileNameFilter = tpl.DisplayNamePattern.Trim();
                            else if (!string.IsNullOrWhiteSpace(tpl.FileName) && !tpl.FileName.Contains("<"))
                                fileNameFilter = tpl.FileName.Trim();
                        }
                    }
                }
                catch { }

                Log($"[INFO] Dang lay danh sach hoan tat tu {fromDate} den {toDate}...");

                using var api = new ApiClient(baseUrl, settings.Credentials);
                var tracker = new SubmissionTracker(folder);
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Build normalized keys for local PDFs to only mark matched ones
                static string NormalizeKey(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                    s = Path.GetFileNameWithoutExtension(s);
                    s = System.Text.RegularExpressions.Regex.Replace(s, "\\s+", " ").Trim();
                    var formD = s.Normalize(NormalizationForm.FormD);
                    var sb = new StringBuilder(formD.Length);
                    foreach (var ch in formD)
                    {
                        var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                        if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                            sb.Append(ch);
                    }
                    var noDia = sb.ToString().Normalize(NormalizationForm.FormC);
                    return noDia.ToLowerInvariant();
                }

                var localFiles = FolderScanner.GetPdfFiles(folder);
                var localKeys = new List<string>();
                foreach (var f in localFiles)
                {
                    var k = NormalizeKey(Path.GetFileName(f) ?? string.Empty);
                    if (!string.IsNullOrEmpty(k)) localKeys.Add(k);
                }

                int total = -1;
                int page = 0;
                const int pageSize = 100;
                int added = 0;

                while (true)
                {
                    int start = page * pageSize;
                    int end = start + pageSize;

                    var (t, names) = await api.SearchDisplayNamesPagedAsync(fileNameFilter, fromDate, toDate, start, end);
                    if (total < 0) total = t;

                    if (names != null && names.Count > 0)
                    {
                        foreach (var n in names)
                        {
                            if (string.IsNullOrWhiteSpace(n)) continue;
                            if (!seen.Add(n)) continue;

                            var apiKey = NormalizeKey(n);
                            if (string.IsNullOrEmpty(apiKey)) continue;

                            bool matchedLocal = false;
                            foreach (var lk in localKeys)
                            {
                                if (string.IsNullOrEmpty(lk)) continue;
                                if (lk.StartsWith(apiKey) || lk.Contains(apiKey))
                                {
                                    matchedLocal = true;
                                    break;
                                }
                            }

                            if (matchedLocal)
                            {
                                tracker.MarkProcessed(n);
                                added++;
                            }
                        }
                    }

                    if (names == null || names.Count == 0 || seen.Count >= total) break;
                    page++;
                }

                Log($"[OK] Da cap nhat {added} muc vao .submitted.txt");
            }
            catch (Exception ex) { Log("[ERROR] GetCompleted: " + ex.Message); }
        }

        private void ExportLog()
        {
            try
            {
                using var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv|All Files (*.*)|*.*", FileName = "logs.csv" };
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                var lines = txtLog.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                sb.AppendLine("Time,Message");
                foreach (var ln in lines)
                {
                    var t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    sb.AppendLine($"{t},\"{ln.Replace("\"", "\"\"")}\"");
                }
                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                Log("[OK] Đã xuất CSV: " + sfd.FileName);
            }
            catch (Exception ex) { Log("[ERROR] ExportLog: " + ex.Message); }
        }

        private void OpenFailed()
        {
            try
            {
                var folder = txtFolder.Text.Trim();
                var path = Path.Combine(folder, ".failed.txt");
                if (File.Exists(path)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{path}\"", UseShellExecute = true });
                else if (Directory.Exists(folder)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{folder}\"", UseShellExecute = true });
            }
            catch (Exception ex) { Log("[ERROR] OpenFailed: " + ex.Message); }
        }

        private void OpenInvalid()
        {
            try
            {
                var folder = txtFolder.Text.Trim();
                var path = Path.Combine(folder, ".invalid.txt");
                if (File.Exists(path)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{path}\"", UseShellExecute = true });
                else if (Directory.Exists(folder)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{folder}\"", UseShellExecute = true });
            }
            catch (Exception ex) { Log("[ERROR] OpenInvalid: " + ex.Message); }
        }

        private void BrowseFolder()
        {
            using var f = new FolderBrowserDialog();
            if (f.ShowDialog(this) == DialogResult.OK) txtFolder.Text = f.SelectedPath;
        }

        // Settings
        private string OutputSettingsPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private string ProjectSettingsPath => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\\..\\..\\appsettings.json"));

        private AppSettings ReadSettings()
        {
            try
            {
                string path = File.Exists(OutputSettingsPath) ? OutputSettingsPath : ProjectSettingsPath;
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var s = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) return s;
                }
            }
            catch { }
            return new AppSettings();
        }

        private void SaveSettings(AppSettings s)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(s, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(OutputSettingsPath, json, Encoding.UTF8);
                try { File.WriteAllText(ProjectSettingsPath, json, Encoding.UTF8); } catch { }
            }
            catch (Exception ex) { Log("[WARN] Lưu cấu hình: " + ex.Message); }
        }

        private void LoadSettingsIntoUI()
        {
            var s = ReadSettings();
            txtBaseUrl.Text = s.BaseUrl;
            txtGroupId.Text = s.Credentials.GroupId;
            txtToken.Text = s.Credentials.Token;
            if (s.RememberMe)
            {
                txtUsername.Text = s.LoginUsername;
                chkRemember.Checked = true;
            }
        }

        private void Log(string message)
        {
            if (txtLog.InvokeRequired) { txtLog.BeginInvoke(new Action<string>(Log), message); return; }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
