using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace YueyouLauncher
{
    public partial class Form1 : Form
    {
        private Config cfg;
        private Label statusLabel;
        private int xianyuClickCount = 0;
        private DateTime lastXianyuClick = DateTime.MinValue;

        public Form1()
        {
            InitializeComponent();
            cfg = LoadConfig();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "悦游网单游戏启动器";
            this.Size = new Size(880, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(0x0D, 0x12, 0x25);
            this.ForeColor = Color.White;
            this.Font = new Font("Microsoft YaHei", 9f);

            // ===== 标题栏 =====
            Panel titlePanel = new Panel();
            titlePanel.Dock = DockStyle.Top;
            titlePanel.Height = 56;
            titlePanel.BackColor = Color.FromArgb(0x0D, 0x12, 0x25);
            titlePanel.Padding = new Padding(20, 0, 12, 0);

            Label titleLabel = new Label();
            titleLabel.Text = "🎮 悦游网单游戏启动器";
            titleLabel.Font = new Font("Microsoft YaHei", 14f, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(20, 14);
            titlePanel.Controls.Add(titleLabel);

            Label versionLabel = new Label();
            versionLabel.Text = "v" + (string.IsNullOrEmpty(AppVersion) ? "1.0.0" : AppVersion);
            versionLabel.Font = new Font("Microsoft YaHei", 9f);
            versionLabel.ForeColor = Color.Gray;
            versionLabel.AutoSize = true;
            versionLabel.Location = new Point(600, 20);
            titlePanel.Controls.Add(versionLabel);

            Button updateBtn = CreateButton("🔄 检查更新", 100, 28, 9f, false);
            updateBtn.Location = new Point(720, 12);
            updateBtn.Click += (s, e) => CheckUpdate(true);
            titlePanel.Controls.Add(updateBtn);

            this.Controls.Add(titlePanel);

            // ===== 状态栏 =====
            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 28;
            statusPanel.BackColor = Color.FromArgb(0x0D, 0x12, 0x25);
            statusPanel.Padding = new Padding(20, 0, 20, 0);

            statusLabel = new Label();
            statusLabel.Text = "● 就绪";
            statusLabel.ForeColor = Color.LightGreen;
            statusLabel.Font = new Font("Microsoft YaHei", 9f);
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(20, 6);
            statusPanel.Controls.Add(statusLabel);

            this.Controls.Add(statusPanel);

            // ===== 主内容区 =====
            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(20, 8, 20, 8);

            // --- 左侧功能栏 ---
            Panel leftPanel = new Panel();
            leftPanel.Location = new Point(20, 8);
            leftPanel.Size = new Size(200, 480);

            int y = 0;
            leftPanel.Controls.Add(MakeButton("🌐 官方网站", 40, y, () => OpenURL(cfg.OfficialURL)));
            y += 48;
            leftPanel.Controls.Add(MakeButton("📥 下载工具", 40, y, () => OpenURL(cfg.DownloadURL)));
            y += 48;
            leftPanel.Controls.Add(MakeButton("📖 使用教程", 40, y, () => OpenURL(cfg.TutorialURL)));
            y += 48;
            leftPanel.Controls.Add(MakeButton("🔍 物品查看器", 40, y, () => OpenExe(cfg.ViewerExe, "物品查看器")));
            y += 56;
            leftPanel.Controls.Add(MakeButton("🛡️ 关闭防火墙", 40, y, () => ToggleFirewall()));
            y += 48;
            leftPanel.Controls.Add(MakeButton("🛡️ 关闭杀毒软件", 40, y, () => ToggleDefender()));
            y += 56;

            Button killBtn = MakeButton("⚡ 一键关闭服务端", 44, y, () => KillGameServer());
            killBtn.Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold);
            leftPanel.Controls.Add(killBtn);

            mainPanel.Controls.Add(leftPanel);

            // --- 右侧步骤区 ---
            Panel rightPanel = new Panel();
            rightPanel.Location = new Point(240, 8);
            rightPanel.Size = new Size(600, 480);

            Label stepsTitle = new Label();
            stepsTitle.Text = "游戏启动步骤";
            stepsTitle.Font = new Font("Microsoft YaHei", 9f);
            stepsTitle.ForeColor = Color.Gray;
            stepsTitle.AutoSize = true;
            stepsTitle.Location = new Point(0, 0);
            rightPanel.Controls.Add(stepsTitle);

            int sy = 30;
            rightPanel.Controls.Add(MakeStepCard("步骤 1", "下载客户端", "下载游戏客户端安装包", () => OpenURL(cfg.ClientURL), ref sy));
            rightPanel.Controls.Add(MakeStepCard("步骤 2", "打开本地列表", "启动服务器列表查看工具", () => OpenExe(cfg.LocalListExe, "本地列表"), ref sy));
            rightPanel.Controls.Add(MakeStepCard("步骤 3", "安装客户端补丁", "选择客户端目录后自动解压补丁", () => InstallPatch(), ref sy));
            rightPanel.Controls.Add(MakeStepCard("步骤 4", "启动服务器", "启动游戏服务端程序", () => StartServer(), ref sy));
            rightPanel.Controls.Add(MakeStepCard("步骤 5", "打开登陆器", "启动游戏客户端登陆器", () => OpenExe(cfg.LauncherExe, "登陆器"), ref sy));

            mainPanel.Controls.Add(rightPanel);

            this.Controls.Add(mainPanel);

            // ===== 底部栏 =====
            Panel bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 32;
            bottomPanel.BackColor = Color.FromArgb(0x0D, 0x12, 0x25);
            bottomPanel.Padding = new Padding(20, 0, 20, 0);

            LinkLabel footerLink = new LinkLabel();
            footerLink.Text = cfg.FootNote;
            footerLink.LinkColor = Color.FromArgb(0x4a, 0x8a, 0xc0);
            footerLink.ActiveLinkColor = Color.White;
            footerLink.AutoSize = true;
            footerLink.Location = new Point(20, 8);
            footerLink.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo(cfg.OfficialURL) { UseShellExecute = true }); } catch { } };
            bottomPanel.Controls.Add(footerLink);

            LinkLabel xianyuLink = new LinkLabel();
            xianyuLink.Text = cfg.XianyuID;
            xianyuLink.LinkColor = Color.FromArgb(0x4a, 0x8a, 0xc0);
            xianyuLink.ActiveLinkColor = Color.White;
            xianyuLink.AutoSize = true;
            xianyuLink.Location = new Point(600, 8);
            xianyuLink.LinkClicked += (s, e) =>
            {
                var now = DateTime.Now;
                if (now.Subtract(lastXianyuClick).TotalSeconds > 3)
                    xianyuClickCount = 0;
                lastXianyuClick = now;
                xianyuClickCount++;
                if (xianyuClickCount >= 10)
                {
                    xianyuClickCount = 0;
                    OpenSettings();
                }
            };
            bottomPanel.Controls.Add(xianyuLink);

            this.Controls.Add(bottomPanel);
        }

        private Button MakeButton(string text, int height, int y, Action onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(200, height);
            btn.Location = new Point(0, y);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(0x1a, 0x24, 0x3b);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Microsoft YaHei", 11f);
            btn.Cursor = Cursors.Hand;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Button CreateButton(string text, int width, int height, float fontSize, bool bold)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(width, height);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.FromArgb(0x1a, 0x24, 0x3b);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Microsoft YaHei", fontSize, bold ? FontStyle.Bold : FontStyle.Regular);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private Panel MakeStepCard(string stepNum, string title, string desc, Action onClick, ref int y)
        {
            Panel card = new Panel();
            card.Size = new Size(600, 56);
            card.Location = new Point(0, y);
            card.BackColor = Color.FromArgb(0x1a, 0x24, 0x3b);

            Label numLabel = new Label();
            numLabel.Text = stepNum;
            numLabel.Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold);
            numLabel.ForeColor = Color.FromArgb(0x4a, 0x8a, 0xc0);
            numLabel.AutoSize = true;
            numLabel.Location = new Point(12, 8);
            card.Controls.Add(numLabel);

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(70, 8);
            card.Controls.Add(titleLabel);

            Label descLabel = new Label();
            descLabel.Text = desc;
            descLabel.Font = new Font("Microsoft YaHei", 9f);
            descLabel.ForeColor = Color.Gray;
            descLabel.AutoSize = true;
            descLabel.Location = new Point(70, 30);
            card.Controls.Add(descLabel);

            Button openBtn = new Button();
            openBtn.Text = "打开";
            openBtn.Size = new Size(70, 30);
            openBtn.Location = new Point(510, 13);
            openBtn.FlatStyle = FlatStyle.Flat;
            openBtn.FlatAppearance.BorderColor = Color.FromArgb(0x4a, 0x8a, 0xc0);
            openBtn.ForeColor = Color.FromArgb(0x4a, 0x8a, 0xc0);
            openBtn.BackColor = Color.Transparent;
            openBtn.Cursor = Cursors.Hand;
            openBtn.Click += (s, e) => onClick();
            card.Controls.Add(openBtn);

            y += 64;
            return card;
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null)
                statusLabel.Text = text;
        }

        private void OpenURL(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("打开链接失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void OpenExe(string relPath, string name)
        {
            string fullPath = ResolvePath(relPath);
            if (!File.Exists(fullPath))
            {
                MessageBox.Show("找不到 " + name + "：\n" + fullPath, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(fullPath);
                psi.WorkingDirectory = Path.GetDirectoryName(fullPath);
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception ex) { MessageBox.Show("启动 " + name + " 失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ToggleFirewall()
        {
            if (MessageBox.Show("确定要关闭 Windows 防火墙吗？\n这可能会降低系统安全性。", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            SetStatus("正在关闭防火墙...");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netsh", "advfirewall set allprofiles state off");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                p.WaitForExit();
                MessageBox.Show("Windows 防火墙已关闭。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("● 防火墙已关闭");
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭防火墙失败，请右键以管理员身份运行本程序。\n\n" + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("关闭防火墙失败");
            }
        }

        private void ToggleDefender()
        {
            if (MessageBox.Show("确定要关闭 Windows Defender 实时保护吗？\n建议同时关闭第三方杀毒软件。", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            SetStatus("正在关闭杀毒软件...");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powershell", "-ExecutionPolicy Bypass -Command \"Set-MpPreference -DisableRealtimeMonitoring $true\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                p.WaitForExit();
                MessageBox.Show("Windows Defender 实时保护已关闭。\n请手动关闭第三方杀毒软件。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("● Defender 已关闭");
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭 Defender 失败，可能需要管理员权限。\n请右键以管理员身份运行本程序。\n\n" + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("关闭杀毒软件失败");
            }
        }

        private void KillGameServer()
        {
            if (MessageBox.Show("确定要关闭游戏服务端吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            SetStatus("正在关闭服务端...");
            try
            {
                string serverPath = ResolvePath(cfg.ServerExe);
                string exeName = Path.GetFileName(serverPath);
                ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /IM " + exeName);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                p.WaitForExit();
                MessageBox.Show("游戏服务端已关闭。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("● 服务端已关闭");
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭服务端：\n" + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("● 服务端已关闭");
            }
        }

        private void InstallPatch()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "请选择游戏客户端所在的文件夹";
            if (fbd.ShowDialog() != DialogResult.OK)
                return;

            string patchPath = ResolvePath(cfg.PatchFile);
            if (!File.Exists(patchPath))
            {
                MessageBox.Show("找不到补丁文件：\n" + patchPath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SetStatus("正在解压补丁...");
            try
            {
                ZipFile.ExtractToDirectory(patchPath, fbd.SelectedPath);
                MessageBox.Show("客户端补丁已安装到：\n" + fbd.SelectedPath, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("● 补丁安装完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show("解压补丁失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("解压补丁失败");
            }
        }

        private void StartServer()
        {
            string serverPath = ResolvePath(cfg.ServerExe);
            if (!File.Exists(serverPath))
            {
                MessageBox.Show("找不到服务端程序：\n" + serverPath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SetStatus("正在启动服务端...");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(serverPath);
                psi.WorkingDirectory = Path.GetDirectoryName(serverPath);
                psi.UseShellExecute = true;
                Process.Start(psi);
                SetStatus("● 服务端运行中");
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动服务端失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("启动服务端失败");
            }
        }

        private void CheckUpdate(bool manual)
        {
            if (string.IsNullOrEmpty(cfg.UpdateURL))
            {
                if (manual) MessageBox.Show("未配置更新地址。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                WebClient wc = new WebClient();
                string json = wc.DownloadString(cfg.UpdateURL);
                string ver = GetJsonValue(json, "version");
                string dlUrl = GetJsonValue(json, "download_url");
                string changelog = GetJsonValue(json, "changelog");
                string currentVer = string.IsNullOrEmpty(AppVersion) ? "1.0.0" : AppVersion;

                if (ver == currentVer)
                {
                    if (manual) MessageBox.Show("当前已是最新版本 v" + currentVer, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string msg = string.Format("发现新版本 v{0}！\n\n更新内容：\n{1}\n\n是否立即更新？", ver, changelog);
                if (MessageBox.Show(msg, "发现更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    OpenURL(dlUrl);
            }
            catch (Exception ex)
            {
                if (manual) MessageBox.Show("检查更新失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSettings()
        {
            Form settingsForm = new Form();
            settingsForm.Text = "⚙ 设置";
            settingsForm.Size = new Size(540, 540);
            settingsForm.StartPosition = FormStartPosition.CenterParent;
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            settingsForm.MaximizeBox = false;
            settingsForm.MinimizeBox = false;
            settingsForm.BackColor = Color.FromArgb(0x1a, 0x24, 0x3b);
            settingsForm.ForeColor = Color.White;
            settingsForm.Font = new Font("Microsoft YaHei", 9f);

            int y = 10;
            AddSettingsField(settingsForm, "官方网站:", "OfficialURL", cfg.OfficialURL, ref y);
            AddSettingsField(settingsForm, "下载工具:", "DownloadURL", cfg.DownloadURL, ref y);
            AddSettingsField(settingsForm, "使用教程:", "TutorialURL", cfg.TutorialURL, ref y);
            AddSettingsField(settingsForm, "客户端下载:", "ClientURL", cfg.ClientURL, ref y);
            AddSettingsField(settingsForm, "更新检测(JSON):", "UpdateURL", cfg.UpdateURL, ref y);
            y += 10;
            AddSettingsField(settingsForm, "本地列表:", "LocalListExe", cfg.LocalListExe, ref y);
            AddSettingsField(settingsForm, "补丁文件:", "PatchFile", cfg.PatchFile, ref y);
            AddSettingsField(settingsForm, "服务端程序:", "ServerExe", cfg.ServerExe, ref y);
            AddSettingsField(settingsForm, "登陆器:", "LauncherExe", cfg.LauncherExe, ref y);
            AddSettingsField(settingsForm, "物品查看器:", "ViewerExe", cfg.ViewerExe, ref y);
            y += 10;
            AddSettingsField(settingsForm, "底部文字:", "FootNote", cfg.FootNote, ref y);
            AddSettingsField(settingsForm, "闲鱼ID:", "XianyuID", cfg.XianyuID, ref y);

            Button saveBtn = new Button();
            saveBtn.Text = "保存";
            saveBtn.Size = new Size(80, 30);
            saveBtn.Location = new Point(350, y + 10);
            saveBtn.FlatStyle = FlatStyle.Flat;
            saveBtn.BackColor = Color.FromArgb(0x4a, 0x8a, 0xc0);
            saveBtn.ForeColor = Color.White;
            saveBtn.Click += (s, ev) =>
            {
                foreach (Control c in settingsForm.Controls)
                {
                    if (c is TextBox && c.Tag != null)
                    {
                        string key = c.Tag.ToString();
                        string val = ((TextBox)c).Text;
                        switch (key)
                        {
                            case "OfficialURL": cfg.OfficialURL = val; break;
                            case "DownloadURL": cfg.DownloadURL = val; break;
                            case "TutorialURL": cfg.TutorialURL = val; break;
                            case "ClientURL": cfg.ClientURL = val; break;
                            case "UpdateURL": cfg.UpdateURL = val; break;
                            case "LocalListExe": cfg.LocalListExe = val; break;
                            case "PatchFile": cfg.PatchFile = val; break;
                            case "ServerExe": cfg.ServerExe = val; break;
                            case "LauncherExe": cfg.LauncherExe = val; break;
                            case "ViewerExe": cfg.ViewerExe = val; break;
                            case "FootNote": cfg.FootNote = val; break;
                            case "XianyuID": cfg.XianyuID = val; break;
                        }
                    }
                }
                try
                {
                    SaveConfig(cfg);
                    MessageBox.Show("设置已保存，部分设置需要重启生效。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    settingsForm.Close();
                }
                catch (Exception ex) { MessageBox.Show("保存失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            settingsForm.Controls.Add(saveBtn);

            Button cancelBtn = new Button();
            cancelBtn.Text = "取消";
            cancelBtn.Size = new Size(80, 30);
            cancelBtn.Location = new Point(440, y + 10);
            cancelBtn.FlatStyle = FlatStyle.Flat;
            cancelBtn.BackColor = Color.FromArgb(0x1a, 0x24, 0x3b);
            cancelBtn.ForeColor = Color.White;
            cancelBtn.Click += (s, ev) => settingsForm.Close();
            settingsForm.Controls.Add(cancelBtn);

            settingsForm.ShowDialog(this);
        }

        private void AddSettingsField(Form form, string label, string key, string value, ref int y)
        {
            Label lbl = new Label();
            lbl.Text = label;
            lbl.Location = new Point(10, y + 3);
            lbl.AutoSize = true;
            form.Controls.Add(lbl);

            TextBox tb = new TextBox();
            tb.Text = value ?? "";
            tb.Tag = key;
            tb.Location = new Point(120, y);
            tb.Width = 390;
            tb.BackColor = Color.FromArgb(0x2a, 0x34, 0x5b);
            tb.ForeColor = Color.White;
            tb.BorderStyle = BorderStyle.FixedSingle;
            form.Controls.Add(tb);

            y += 30;
        }

        // ===== Utility methods =====

        private string ResolvePath(string p)
        {
            if (Path.IsPathRooted(p)) return p;
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(dir, p);
        }

        private static string configPath()
        {
            return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "config.json");
        }

        private static Config LoadConfig()
        {
            Config cfg = Config.Default;
            string path = configPath();
            if (!File.Exists(path)) return cfg;
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                cfg.OfficialURL = GetJsonValue(json, "official_url") ?? cfg.OfficialURL;
                cfg.DownloadURL = GetJsonValue(json, "download_url") ?? cfg.DownloadURL;
                cfg.TutorialURL = GetJsonValue(json, "tutorial_url") ?? cfg.TutorialURL;
                cfg.ClientURL = GetJsonValue(json, "client_url") ?? cfg.ClientURL;
                cfg.UpdateURL = GetJsonValue(json, "update_url") ?? cfg.UpdateURL;
                cfg.LocalListExe = GetJsonValue(json, "local_list_exe") ?? cfg.LocalListExe;
                cfg.PatchFile = GetJsonValue(json, "patch_file") ?? cfg.PatchFile;
                cfg.ServerExe = GetJsonValue(json, "server_exe") ?? cfg.ServerExe;
                cfg.LauncherExe = GetJsonValue(json, "launcher_exe") ?? cfg.LauncherExe;
                cfg.ViewerExe = GetJsonValue(json, "viewer_exe") ?? cfg.ViewerExe;
                cfg.FootNote = GetJsonValue(json, "foot_note") ?? cfg.FootNote;
                cfg.XianyuID = GetJsonValue(json, "xianyu_id") ?? cfg.XianyuID;
            }
            catch { }
            return cfg;
        }

        private static void SaveConfig(Config cfg)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendFormat("  \"official_url\": \"{0}\",\n", EscapeJson(cfg.OfficialURL));
            sb.AppendFormat("  \"download_url\": \"{0}\",\n", EscapeJson(cfg.DownloadURL));
            sb.AppendFormat("  \"tutorial_url\": \"{0}\",\n", EscapeJson(cfg.TutorialURL));
            sb.AppendFormat("  \"client_url\": \"{0}\",\n", EscapeJson(cfg.ClientURL));
            sb.AppendFormat("  \"update_url\": \"{0}\",\n", EscapeJson(cfg.UpdateURL));
            sb.AppendFormat("  \"local_list_exe\": \"{0}\",\n", EscapeJson(cfg.LocalListExe));
            sb.AppendFormat("  \"patch_file\": \"{0}\",\n", EscapeJson(cfg.PatchFile));
            sb.AppendFormat("  \"server_exe\": \"{0}\",\n", EscapeJson(cfg.ServerExe));
            sb.AppendFormat("  \"launcher_exe\": \"{0}\",\n", EscapeJson(cfg.LauncherExe));
            sb.AppendFormat("  \"viewer_exe\": \"{0}\",\n", EscapeJson(cfg.ViewerExe));
            sb.AppendFormat("  \"foot_note\": \"{0}\",\n", EscapeJson(cfg.FootNote));
            sb.AppendFormat("  \"xianyu_id\": \"{0}\"\n", EscapeJson(cfg.XianyuID));
            sb.AppendLine("}");
            File.WriteAllText(configPath(), sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string GetJsonValue(string json, string key)
        {
            string search = "\"" + key + "\"";
            int idx = json.IndexOf(search);
            if (idx < 0) return null;
            idx = json.IndexOf(":", idx);
            if (idx < 0) return null;
            idx++;
            while (idx < json.Length && json[idx] == ' ') idx++;
            if (idx >= json.Length) return null;
            if (json[idx] == '"')
            {
                idx++;
                int end = json.IndexOf('"', idx);
                if (end < 0) return null;
                return json.Substring(idx, end - idx);
            }
            int endIdx = json.IndexOfAny(new char[] { ',', '}', '\n' }, idx);
            if (endIdx < 0) endIdx = json.Length;
            return json.Substring(idx, endIdx - idx).Trim();
        }

        private static string AppVersion
        {
            get
            {
                try { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"; }
                catch { return "1.0.0"; }
            }
        }
    }

    public class Config
    {
        public string OfficialURL = "http://www.yueyoue.cn";
        public string DownloadURL = "http://www.yueyoue.cn";
        public string TutorialURL = "http://www.yueyoue.cn";
        public string ClientURL = "http://www.yueyoue.cn";
        public string UpdateURL = "";
        public string LocalListExe = "tools\\本地列表.exe";
        public string PatchFile = "patch\\client_patch.zip";
        public string ServerExe = "server\\GameServer.exe";
        public string LauncherExe = "client\\GameLauncher.exe";
        public string ViewerExe = "tools\\物品查看器.exe";
        public string FootNote = "官网：http://www.yueyoue.cn";
        public string XianyuID = "闲鱼ID：悦游网单";

        public static Config Default => new Config();
    }
}
