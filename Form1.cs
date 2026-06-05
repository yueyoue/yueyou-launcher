using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
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
            this.Text = "\u60a6\u6e38\u7f51\u5355\u6e38\u620f\u542f\u52a8\u5668";
            this.Size = new Size(880, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(0x0D, 0x12, 0x25);
            this.ForeColor = Color.White;
            this.Font = new Font("Microsoft YaHei", 9f);

            // ===== Title bar =====
            Panel titlePanel = new Panel();
            titlePanel.Dock = DockStyle.Top;
            titlePanel.Height = 56;
            titlePanel.BackColor = Color.FromArgb(0x0D, 0x12, 0x25);
            titlePanel.Padding = new Padding(20, 0, 12, 0);

            Label titleLabel = new Label();
            titleLabel.Text = "\ud83c\udfae \u60a6\u6e38\u7f51\u5355\u6e38\u620f\u542f\u52a8\u5668";
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

            Button updateBtn = CreateButton("\ud83d\udd04 \u68c0\u67e5\u66f4\u65b0", 100, 28, 9f, false);
            updateBtn.Location = new Point(720, 12);
            updateBtn.Click += (s, e) => CheckUpdate(true);
            titlePanel.Controls.Add(updateBtn);

            this.Controls.Add(titlePanel);

            // ===== Status bar =====
            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 28;
            statusPanel.BackColor = Color.FromArgb(0x0D, 0x12, 0x25);
            statusPanel.Padding = new Padding(20, 0, 20, 0);

            statusLabel = new Label();
            statusLabel.Text = "\u25cf \u5c31\u7eea";
            statusLabel.ForeColor = Color.LightGreen;
            statusLabel.Font = new Font("Microsoft YaHei", 9f);
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(20, 6);
            statusPanel.Controls.Add(statusLabel);

            this.Controls.Add(statusPanel);

            // ===== Main content =====
            Panel mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(20, 8, 20, 8);

            // --- Left sidebar ---
            Panel leftPanel = new Panel();
            leftPanel.Location = new Point(20, 8);
            leftPanel.Size = new Size(200, 480);

            int y = 0;
            leftPanel.Controls.Add(MakeButton("\ud83c\udf10 \u5b98\u65b9\u7f51\u7ad9", 40, y, () => OpenURL(cfg.OfficialURL)));
            y += 48;
            leftPanel.Controls.Add(MakeButton("\ud83d\udce5 \u4e0b\u8f7d\u5de5\u5177", 40, y, () => OpenURL(cfg.DownloadURL)));
            y += 48;
            leftPanel.Controls.Add(MakeButton("\ud83d\udcd6 \u4f7f\u7528\u6559\u7a0b", 40, y, () => OpenURL(cfg.TutorialURL)));
            y += 48;
            leftPanel.Controls.Add(MakeButton("\ud83d\udd0d \u7269\u54c1\u67e5\u770b\u5668", 40, y, () => OpenExe(cfg.ViewerExe, "\u7269\u54c1\u67e5\u770b\u5668")));
            y += 56;
            leftPanel.Controls.Add(MakeButton("\ud83d\udee1\ufe0f \u5173\u95ed\u9632\u706b\u5899", 40, y, () => ToggleFirewall()));
            y += 48;
            leftPanel.Controls.Add(MakeButton("\ud83d\udee1\ufe0f \u5173\u95ed\u6740\u6bd2\u8f6f\u4ef6", 40, y, () => ToggleDefender()));
            y += 56;

            Button killBtn = MakeButton("\u26a1 \u4e00\u952e\u5173\u95ed\u670d\u52a1\u7aef", 44, y, () => KillGameServer());
            killBtn.Font = new Font("Microsoft YaHei", 11f, FontStyle.Bold);
            leftPanel.Controls.Add(killBtn);

            mainPanel.Controls.Add(leftPanel);

            // --- Right steps area ---
            Panel rightPanel = new Panel();
            rightPanel.Location = new Point(240, 8);
            rightPanel.Size = new Size(600, 480);

            Label stepsTitle = new Label();
            stepsTitle.Text = "\u6e38\u620f\u542f\u52a8\u6b65\u9aa4";
            stepsTitle.Font = new Font("Microsoft YaHei", 9f);
            stepsTitle.ForeColor = Color.Gray;
            stepsTitle.AutoSize = true;
            stepsTitle.Location = new Point(0, 0);
            rightPanel.Controls.Add(stepsTitle);

            int sy = 30;
            rightPanel.Controls.Add(MakeStepCard("\u6b65\u9aa4 1", "\u4e0b\u8f7d\u5ba2\u6237\u7aef", "\u4e0b\u8f7d\u6e38\u620f\u5ba2\u6237\u7aef\u5b89\u88c5\u5305", () => OpenURL(cfg.ClientURL), ref sy));
            rightPanel.Controls.Add(MakeStepCard("\u6b65\u9aa4 2", "\u6253\u5f00\u672c\u5730\u5217\u8868", "\u542f\u52a8\u670d\u52a1\u5668\u5217\u8868\u67e5\u770b\u5de5\u5177", () => OpenExe(cfg.LocalListExe, "\u672c\u5730\u5217\u8868"), ref sy));
            rightPanel.Controls.Add(MakeStepCard("\u6b65\u9aa4 3", "\u5b89\u88c5\u5ba2\u6237\u7aef\u8865\u4e01", "\u9009\u62e9\u5ba2\u6237\u7aef\u76ee\u5f55\u540e\u81ea\u52a8\u89e3\u538b\u8865\u4e01", () => InstallPatch(), ref sy));
            rightPanel.Controls.Add(MakeStepCard("\u6b65\u9aa4 4", "\u542f\u52a8\u670d\u52a1\u5668", "\u542f\u52a8\u6e38\u620f\u670d\u52a1\u7aef\u7a0b\u5e8f", () => StartServer(), ref sy));
            rightPanel.Controls.Add(MakeStepCard("\u6b65\u9aa4 5", "\u6253\u5f00\u767b\u9646\u5668", "\u542f\u52a8\u6e38\u620f\u5ba2\u6237\u7aef\u767b\u9646\u5668", () => OpenExe(cfg.LauncherExe, "\u767b\u9646\u5668"), ref sy));

            mainPanel.Controls.Add(rightPanel);

            this.Controls.Add(mainPanel);

            // ===== Bottom bar =====
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
            footerLink.LinkClicked += (s, e) => { try { Process.Start(cfg.OfficialURL); } catch { } };
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
            openBtn.Text = "\u6253\u5f00";
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
            try { Process.Start(url); }
            catch (Exception ex) { MessageBox.Show("\u6253\u5f00\u94fe\u63a5\u5931\u8d25\uff1a" + ex.Message, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void OpenExe(string relPath, string name)
        {
            string fullPath = ResolvePath(relPath);
            if (!File.Exists(fullPath))
            {
                MessageBox.Show("\u627e\u4e0d\u5230 " + name + "\uff1a\n" + fullPath, "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(fullPath);
                psi.WorkingDirectory = Path.GetDirectoryName(fullPath);
                Process.Start(psi);
            }
            catch (Exception ex) { MessageBox.Show("\u542f\u52a8 " + name + " \u5931\u8d25\uff1a\n" + ex.Message, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ToggleFirewall()
        {
            if (MessageBox.Show("\u786e\u5b9a\u8981\u5173\u95ed Windows \u9632\u706b\u5899\u5417\uff1f\n\u8fd9\u53ef\u80fd\u4f1a\u964d\u4f4e\u7cfb\u7edf\u5b89\u5168\u6027\u3002", "\u786e\u8ba4", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            SetStatus("\u6b63\u5728\u5173\u95ed\u9632\u706b\u5899...");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netsh", "advfirewall set allprofiles state off");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                p.WaitForExit();
                MessageBox.Show("Windows \u9632\u706b\u5899\u5df2\u5173\u95ed\u3002", "\u6210\u529f", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("\u25cf \u9632\u706b\u5899\u5df2\u5173\u95ed");
            }
            catch (Exception ex)
            {
                MessageBox.Show("\u5173\u95ed\u9632\u706b\u5899\u5931\u8d25\uff0c\u8bf7\u53f3\u952e\u4ee5\u7ba1\u7406\u5458\u8eab\u4efd\u8fd0\u884c\u672c\u7a0b\u5e8f\u3002\n\n" + ex.Message, "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("\u5173\u95ed\u9632\u706b\u5899\u5931\u8d25");
            }
        }

        private void ToggleDefender()
        {
            if (MessageBox.Show("\u786e\u5b9a\u8981\u5173\u95ed Windows Defender \u5b9e\u65f6\u4fdd\u62a4\u5417\uff1f\n\u5efa\u8bae\u540c\u65f6\u5173\u95ed\u7b2c\u4e09\u65b9\u6740\u6bd2\u8f6f\u4ef6\u3002", "\u786e\u8ba4", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            SetStatus("\u6b63\u5728\u5173\u95ed\u6740\u6bd2\u8f6f\u4ef6...");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powershell", "-ExecutionPolicy Bypass -Command \"Set-MpPreference -DisableRealtimeMonitoring $true\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                p.WaitForExit();
                MessageBox.Show("Windows Defender \u5b9e\u65f6\u4fdd\u62a4\u5df2\u5173\u95ed\u3002\n\u8bf7\u624b\u52a8\u5173\u95ed\u7b2c\u4e09\u65b9\u6740\u6bd2\u8f6f\u4ef6\u3002", "\u6210\u529f", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("\u25cf Defender \u5df2\u5173\u95ed");
            }
            catch (Exception ex)
            {
                MessageBox.Show("\u5173\u95ed Defender \u5931\u8d25\uff0c\u53ef\u80fd\u9700\u8981\u7ba1\u7406\u5458\u6743\u9650\u3002\n\u8bf7\u53f3\u952e\u4ee5\u7ba1\u7406\u5458\u8eab\u4efd\u8fd0\u884c\u672c\u7a0b\u5e8f\u3002\n\n" + ex.Message, "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatus("\u5173\u95ed\u6740\u6bd2\u8f6f\u4ef6\u5931\u8d25");
            }
        }

        private void KillGameServer()
        {
            if (MessageBox.Show("\u786e\u5b9a\u8981\u5173\u95ed\u6e38\u620f\u670d\u52a1\u7aef\u5417\uff1f", "\u786e\u8ba4", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            SetStatus("\u6b63\u5728\u5173\u95ed\u670d\u52a1\u7aef...");
            try
            {
                string serverPath = ResolvePath(cfg.ServerExe);
                string exeName = Path.GetFileName(serverPath);
                ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /IM " + exeName);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                p.WaitForExit();
                MessageBox.Show("\u6e38\u620f\u670d\u52a1\u7aef\u5df2\u5173\u95ed\u3002", "\u6210\u529f", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("\u25cf \u670d\u52a1\u7aef\u5df2\u5173\u95ed");
            }
            catch (Exception ex)
            {
                MessageBox.Show("\u5173\u95ed\u670d\u52a1\u7aef\uff1a\n" + ex.Message, "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("\u25cf \u670d\u52a1\u7aef\u5df2\u5173\u95ed");
            }
        }

        private void InstallPatch()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "\u8bf7\u9009\u62e9\u6e38\u620f\u5ba2\u6237\u7aef\u6240\u5728\u7684\u6587\u4ef6\u5939";
            if (fbd.ShowDialog() != DialogResult.OK)
                return;

            string patchPath = ResolvePath(cfg.PatchFile);
            if (!File.Exists(patchPath))
            {
                MessageBox.Show("\u627e\u4e0d\u5230\u8865\u4e01\u6587\u4ef6\uff1a\n" + patchPath, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SetStatus("\u6b63\u5728\u89e3\u538b\u8865\u4e01...");
            try
            {
                ExtractZip(patchPath, fbd.SelectedPath);
                MessageBox.Show("\u5ba2\u6237\u7aef\u8865\u4e01\u5df2\u5b89\u88c5\u5230\uff1a\n" + fbd.SelectedPath, "\u6210\u529f", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus("\u25cf \u8865\u4e01\u5b89\u88c5\u5b8c\u6210");
            }
            catch (Exception ex)
            {
                MessageBox.Show("\u89e3\u538b\u8865\u4e01\u5931\u8d25\uff1a\n" + ex.Message, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("\u89e3\u538b\u8865\u4e01\u5931\u8d25");
            }
        }

        private void StartServer()
        {
            string serverPath = ResolvePath(cfg.ServerExe);
            if (!File.Exists(serverPath))
            {
                MessageBox.Show("\u627e\u4e0d\u5230\u670d\u52a1\u7aef\u7a0b\u5e8f\uff1a\n" + serverPath, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SetStatus("\u6b63\u5728\u542f\u52a8\u670d\u52a1\u7aef...");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(serverPath);
                psi.WorkingDirectory = Path.GetDirectoryName(serverPath);
                Process.Start(psi);
                SetStatus("\u25cf \u670d\u52a1\u7aef\u8fd0\u884c\u4e2d");
            }
            catch (Exception ex)
            {
                MessageBox.Show("\u542f\u52a8\u670d\u52a1\u7aef\u5931\u8d25\uff1a\n" + ex.Message, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("\u542f\u52a8\u670d\u52a1\u7aef\u5931\u8d25");
            }
        }

        private void CheckUpdate(bool manual)
        {
            if (string.IsNullOrEmpty(cfg.UpdateURL))
            {
                if (manual) MessageBox.Show("\u672a\u914d\u7f6e\u66f4\u65b0\u5730\u5740\u3002", "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                WebClient wc = new WebClient();
                string json = wc.DownloadString(cfg.UpdateURL);
                // Simple JSON parse for {"version":"x.x.x","download_url":"...","changelog":"..."}
                string ver = GetJsonValue(json, "version");
                string dlUrl = GetJsonValue(json, "download_url");
                string changelog = GetJsonValue(json, "changelog");
                string currentVer = string.IsNullOrEmpty(AppVersion) ? "1.0.0" : AppVersion;

                if (ver == currentVer)
                {
                    if (manual) MessageBox.Show("\u5f53\u524d\u5df2\u662f\u6700\u65b0\u7248\u672c v" + currentVer, "\u63d0\u793a", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string msg = string.Format("\u53d1\u73b0\u65b0\u7248\u672c v{0}\uff01\n\n\u66f4\u65b0\u5185\u5bb9\uff1a\n{1}\n\n\u662f\u5426\u7acb\u5373\u66f4\u65b0\uff1f", ver, changelog);
                if (MessageBox.Show(msg, "\u53d1\u73b0\u66f4\u65b0", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    OpenURL(dlUrl);
            }
            catch (Exception ex)
            {
                if (manual) MessageBox.Show("\u68c0\u67e5\u66f4\u65b0\u5931\u8d25\uff1a\n" + ex.Message, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSettings()
        {
            Form settingsForm = new Form();
            settingsForm.Text = "\u2699 \u8bbe\u7f6e";
            settingsForm.Size = new Size(540, 540);
            settingsForm.StartPosition = FormStartPosition.CenterParent;
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            settingsForm.MaximizeBox = false;
            settingsForm.MinimizeBox = false;
            settingsForm.BackColor = Color.FromArgb(0x1a, 0x24, 0x3b);
            settingsForm.ForeColor = Color.White;
            settingsForm.Font = new Font("Microsoft YaHei", 9f);

            int y = 10;
            AddSettingsField(settingsForm, "\u5b98\u65b9\u7f51\u7ad9:", "OfficialURL", cfg.OfficialURL, ref y);
            AddSettingsField(settingsForm, "\u4e0b\u8f7d\u5de5\u5177:", "DownloadURL", cfg.DownloadURL, ref y);
            AddSettingsField(settingsForm, "\u4f7f\u7528\u6559\u7a0b:", "TutorialURL", cfg.TutorialURL, ref y);
            AddSettingsField(settingsForm, "\u5ba2\u6237\u7aef\u4e0b\u8f7d:", "ClientURL", cfg.ClientURL, ref y);
            AddSettingsField(settingsForm, "\u66f4\u65b0\u68c0\u6d4b(JSON):", "UpdateURL", cfg.UpdateURL, ref y);
            y += 10;
            AddSettingsField(settingsForm, "\u672c\u5730\u5217\u8868:", "LocalListExe", cfg.LocalListExe, ref y);
            AddSettingsField(settingsForm, "\u8865\u4e01\u6587\u4ef6:", "PatchFile", cfg.PatchFile, ref y);
            AddSettingsField(settingsForm, "\u670d\u52a1\u7aef\u7a0b\u5e8f:", "ServerExe", cfg.ServerExe, ref y);
            AddSettingsField(settingsForm, "\u767b\u9646\u5668:", "LauncherExe", cfg.LauncherExe, ref y);
            AddSettingsField(settingsForm, "\u7269\u54c1\u67e5\u770b\u5668:", "ViewerExe", cfg.ViewerExe, ref y);
            y += 10;
            AddSettingsField(settingsForm, "\u5e95\u90e8\u6587\u5b57:", "FootNote", cfg.FootNote, ref y);
            AddSettingsField(settingsForm, "\u95f2\u9c7cID:", "XianyuID", cfg.XianyuID, ref y);

            Button saveBtn = new Button();
            saveBtn.Text = "\u4fdd\u5b58";
            saveBtn.Size = new Size(80, 30);
            saveBtn.Location = new Point(350, y + 10);
            saveBtn.FlatStyle = FlatStyle.Flat;
            saveBtn.BackColor = Color.FromArgb(0x4a, 0x8a, 0xc0);
            saveBtn.ForeColor = Color.White;
            saveBtn.Click += (s, ev) =>
            {
                // Collect values from text boxes
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
                    MessageBox.Show("\u8bbe\u7f6e\u5df2\u4fdd\u5b58\uff0c\u90e8\u5206\u8bbe\u7f6e\u9700\u8981\u91cd\u542f\u751f\u6548\u3002", "\u6210\u529f", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    settingsForm.Close();
                }
                catch (Exception ex) { MessageBox.Show("\u4fdd\u5b58\u5931\u8d25: " + ex.Message, "\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            settingsForm.Controls.Add(saveBtn);

            Button cancelBtn = new Button();
            cancelBtn.Text = "\u53d6\u6d88";
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
                // Manual JSON parsing for .NET 3.5 compatibility
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
            // non-string value
            int endIdx = json.IndexOfAny(new char[] { ',', '}', '\n' }, idx);
            if (endIdx < 0) endIdx = json.Length;
            return json.Substring(idx, endIdx - idx).Trim();
        }

        private static void ExtractZip(string zipPath, string destDir)
        {
            ZipFile.ExtractToDirectory(zipPath, destDir);
        }

        private static string AppVersion
        {
            get
            {
                try { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
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
        public string LocalListExe = "tools\\\u672c\u5730\u5217\u8868.exe";
        public string PatchFile = "patch\\client_patch.zip";
        public string ServerExe = "server\\GameServer.exe";
        public string LauncherExe = "client\\GameLauncher.exe";
        public string ViewerExe = "tools\\\u7269\u54c1\u67e5\u770b\u5668.exe";
        public string FootNote = "\u5b98\u7f51\uff1ahttp://www.yueyoue.cn";
        public string XianyuID = "\u95f2\u9c7cID\uff1a\u60a6\u6e38\u7f51\u5355";

        public static Config Default
        {
            get { return new Config(); }
        }
    }
}
