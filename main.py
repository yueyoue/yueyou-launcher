# -*- coding: utf-8 -*-
"""
悦游网单游戏启动器 - 主程序
"""

import tkinter as tk
from tkinter import messagebox, filedialog, simpledialog
import subprocess
import threading
import os
import sys
import json
import zipfile
import time

VERSION = "2.0.0"

# ========== 配置管理 ==========

def get_config_path():
    """获取配置文件路径（exe同目录）"""
    if getattr(sys, 'frozen', False):
        base = os.path.dirname(sys.executable)
    else:
        base = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(base, "config.json")

def get_base_dir():
    """获取程序所在目录"""
    if getattr(sys, 'frozen', False):
        return os.path.dirname(sys.executable)
    return os.path.dirname(os.path.abspath(__file__))

DEFAULT_CONFIG = {
    "official_url": "http://www.yueyoue.cn",
    "download_url": "http://www.yueyoue.cn",
    "tutorial_url": "http://www.yueyoue.cn",
    "client_url": "http://www.yueyoue.cn",
    "update_url": "",
    "update_password": "yueyou2024",
    "local_list_exe": "tools\\本地列表.exe",
    "patch_file": "patch\\client_patch.zip",
    "server_exe": "server\\GameServer.exe",
    "launcher_exe": "client\\GameLauncher.exe",
    "viewer_exe": "tools\\物品查看器.exe",
    "foot_note": "官网：http://www.yueyoue.cn",
    "xianyu_id": "闲鱼ID：悦游网单"
}

def load_config():
    """加载配置"""
    cfg = dict(DEFAULT_CONFIG)
    path = get_config_path()
    if os.path.exists(path):
        try:
            with open(path, "r", encoding="utf-8") as f:
                saved = json.load(f)
            cfg.update(saved)
        except Exception:
            pass
    return cfg

def save_config(cfg):
    """保存配置"""
    path = get_config_path()
    with open(path, "w", encoding="utf-8") as f:
        json.dump(cfg, f, ensure_ascii=False, indent=2)

def resolve_path(rel_path):
    """将相对路径转为绝对路径"""
    if os.path.isabs(rel_path):
        return rel_path
    return os.path.join(get_base_dir(), rel_path)


# ========== 主窗口 ==========

class YueyouLauncher(tk.Tk):
    def __init__(self):
        super().__init__()
        self.cfg = load_config()
        self.title("悦游网单游戏启动器")
        self.geometry("900x700")
        self.resizable(False, False)
        self.configure(bg="#0D1225")

        # 居中显示
        self.update_idletasks()
        x = (self.winfo_screenwidth() - 900) // 2
        y = (self.winfo_screenheight() - 700) // 2
        self.geometry(f"900x700+{x}+{y}")

        # 闲鱼点击计数
        self.xianyu_click_count = 0
        self.xianyu_last_click = 0

        self._build_ui()
        self._check_update_on_start()

    # ---------- UI 构建 ----------

    def _build_ui(self):
        # 顶部标题栏
        self._build_title_bar()
        # 状态栏
        self._build_status_bar()
        # 底部栏
        self._build_bottom_bar()
        # 主内容区
        self._build_main_content()

    def _build_title_bar(self):
        frame = tk.Frame(self, bg="#0D1225", height=56)
        frame.pack(fill="x", side="top")
        frame.pack_propagate(False)

        tk.Label(frame, text="🎮 悦游网单游戏启动器",
                 font=("Microsoft YaHei", 16, "bold"),
                 fg="white", bg="#0D1225").place(x=20, y=12)

        tk.Label(frame, text=f"v{VERSION}",
                 font=("Microsoft YaHei", 9),
                 fg="gray", bg="#0D1225").place(x=320, y=20)

        update_btn = tk.Button(frame, text="🔄 检查更新",
                               font=("Microsoft YaHei", 9),
                               fg="white", bg="#1a243b",
                               bd=0, relief="flat", cursor="hand2",
                               activebackground="#2a345b",
                               command=lambda: self._check_update(True))
        update_btn.place(x=780, y=14, width=100, height=28)

    def _build_status_bar(self):
        frame = tk.Frame(self, bg="#0D1225", height=28)
        frame.pack(fill="x")
        frame.pack_propagate(False)

        self.status_label = tk.Label(frame, text="● 就绪",
                                     font=("Microsoft YaHei", 9),
                                     fg="LightGreen", bg="#0D1225")
        self.status_label.place(x=20, y=4)

    def _build_bottom_bar(self):
        frame = tk.Frame(self, bg="#0D1225", height=32)
        frame.pack(fill="x", side="bottom")
        frame.pack_propagate(False)

        # 官网链接
        footer = tk.Label(frame, text=self.cfg.get("foot_note", ""),
                          font=("Microsoft YaHei", 9),
                          fg="#4a8ac0", bg="#0D1225", cursor="hand2")
        footer.place(x=20, y=6)
        footer.bind("<Button-1>", lambda e: self._open_url(self.cfg["official_url"]))

        # 闲鱼ID（隐藏入口）
        xianyu = tk.Label(frame, text=self.cfg.get("xianyu_id", ""),
                          font=("Microsoft YaHei", 9),
                          fg="#4a8ac0", bg="#0D1225", cursor="hand2")
        xianyu.place(x=680, y=6)
        xianyu.bind("<Button-1>", self._on_xianyu_click)

    def _build_main_content(self):
        main = tk.Frame(self, bg="#0D1225")
        main.pack(fill="both", expand=True)

        # ===== 左侧功能栏 =====
        left = tk.Frame(main, bg="#0D1225")
        left.place(x=20, y=10, width=210, height=600)

        buttons_left = [
            ("🌐 官方网站", lambda: self._open_url(self.cfg["official_url"])),
            ("📥 下载工具", lambda: self._open_url(self.cfg["download_url"])),
            ("📖 使用教程", lambda: self._open_url(self.cfg["tutorial_url"])),
            ("🔍 物品查看器", lambda: self._open_exe(self.cfg["viewer_exe"], "物品查看器")),
            (None, None),  # 分隔
            ("🛡️ 关闭防火墙", self._toggle_firewall),
            ("🛡️ 关闭杀毒软件", self._toggle_defender),
            (None, None),  # 分隔
            ("⚡ 一键关闭服务端", self._kill_game_server),
        ]

        y = 0
        for text, cmd in buttons_left:
            if text is None:
                y += 8
                continue
            btn = tk.Button(left, text=text,
                           font=("Microsoft YaHei", 11),
                           fg="white", bg="#1a243b",
                           bd=0, relief="flat", cursor="hand2",
                           activebackground="#2a345b",
                           anchor="w", padx=12,
                           command=cmd)
            btn.place(x=0, y=y, width=210, height=40)
            y += 48

        # ===== 右侧步骤区 =====
        right = tk.Frame(main, bg="#0D1225")
        right.place(x=250, y=10, width=630, height=600)

        tk.Label(right, text="游戏启动步骤",
                 font=("Microsoft YaHei", 10), fg="gray", bg="#0D1225"
                 ).place(x=0, y=0)

        steps = [
            ("步骤 1", "下载客户端", "下载游戏客户端安装包",
             lambda: self._open_url(self.cfg["client_url"])),
            ("步骤 2", "打开本地列表", "启动服务器列表查看工具",
             lambda: self._open_exe(self.cfg["local_list_exe"], "本地列表")),
            ("步骤 3", "安装客户端补丁", "选择客户端目录后自动解压补丁",
             self._install_patch),
            ("步骤 4", "启动服务器", "启动游戏服务端程序",
             self._start_server),
            ("步骤 5", "打开登陆器", "启动游戏客户端登陆器",
             lambda: self._open_exe(self.cfg["launcher_exe"], "登陆器")),
        ]

        y = 30
        for num, title, desc, cmd in steps:
            self._make_step_card(right, num, title, desc, 0, y, cmd)
            y += 64

    def _make_step_card(self, parent, num, title, desc, x, y, cmd):
        card = tk.Frame(parent, bg="#1a243b")
        card.place(x=x, y=y, width=630, height=56)

        tk.Label(card, text=num,
                 font=("Microsoft YaHei", 10, "bold"),
                 fg="#4a8ac0", bg="#1a243b").place(x=12, y=8)

        tk.Label(card, text=title,
                 font=("Microsoft YaHei", 11, "bold"),
                 fg="white", bg="#1a243b").place(x=70, y=8)

        tk.Label(card, text=desc,
                 font=("Microsoft YaHei", 9),
                 fg="gray", bg="#1a243b").place(x=70, y=30)

        btn = tk.Button(card, text="打开",
                        font=("Microsoft YaHei", 9),
                        fg="#4a8ac0", bg="#1a243b",
                        bd=1, relief="solid",
                        cursor="hand2",
                        activebackground="#2a345b",
                        command=cmd)
        btn.place(x=545, y=13, width=70, height=30)

    # ---------- 状态 ----------

    def _set_status(self, text, color="LightGreen"):
        self.status_label.config(text=text, fg=color)

    # ---------- 闲鱼隐藏入口 ----------

    def _on_xianyu_click(self, event=None):
        now = time.time()
        if now - self.xianyu_last_click > 3:
            self.xianyu_click_count = 0
        self.xianyu_last_click = now
        self.xianyu_click_count += 1

        if self.xianyu_click_count >= 10:
            self.xianyu_click_count = 0
            self._open_settings()

    # ---------- 功能按钮 ----------

    def _open_url(self, url):
        try:
            if sys.platform == "win32":
                os.startfile(url)
            else:
                subprocess.Popen(["xdg-open", url])
        except Exception as ex:
            messagebox.showerror("错误", f"打开链接失败：{ex}")

    def _open_exe(self, rel_path, name):
        full = resolve_path(rel_path)
        if not os.path.exists(full):
            messagebox.showwarning("提示", f"找不到 {name}：\n{full}")
            return
        try:
            subprocess.Popen(full, cwd=os.path.dirname(full))
        except Exception as ex:
            messagebox.showerror("错误", f"启动 {name} 失败：\n{ex}")

    def _toggle_firewall(self):
        if messagebox.askyesno("确认", "确定要关闭 Windows 防火墙吗？\n这可能会降低系统安全性。"):
            self._set_status("正在关闭防火墙...")
            try:
                subprocess.run(
                    ["netsh", "advfirewall", "set", "allprofiles", "state", "off"],
                    capture_output=True, creationflags=0x08000000  # CREATE_NO_WINDOW
                )
                messagebox.showinfo("成功", "Windows 防火墙已关闭。")
                self._set_status("● 防火墙已关闭")
            except Exception as ex:
                messagebox.showwarning("提示", f"关闭防火墙失败，请右键以管理员身份运行。\n\n{ex}")
                self._set_status("关闭防火墙失败", "red")

    def _toggle_defender(self):
        if messagebox.askyesno("确认", "确定要关闭 Windows Defender 实时保护吗？\n建议同时关闭第三方杀毒软件。"):
            self._set_status("正在关闭杀毒软件...")
            try:
                subprocess.run(
                    ["powershell", "-ExecutionPolicy", "Bypass", "-Command",
                     "Set-MpPreference -DisableRealtimeMonitoring $true"],
                    capture_output=True, creationflags=0x08000000
                )
                messagebox.showinfo("成功", "Windows Defender 实时保护已关闭。\n请手动关闭第三方杀毒软件。")
                self._set_status("● Defender 已关闭")
            except Exception as ex:
                messagebox.showwarning("提示", f"关闭 Defender 失败，可能需要管理员权限。\n\n{ex}")
                self._set_status("关闭杀毒软件失败", "red")

    def _kill_game_server(self):
        if not messagebox.askyesno("确认", "确定要关闭游戏服务端吗？"):
            return
        self._set_status("正在关闭服务端...")
        try:
            server_path = resolve_path(self.cfg["server_exe"])
            exe_name = os.path.basename(server_path)
            subprocess.run(
                ["taskkill", "/F", "/IM", exe_name],
                capture_output=True, creationflags=0x08000000
            )
            messagebox.showinfo("成功", "游戏服务端已关闭。")
            self._set_status("● 服务端已关闭")
        except Exception as ex:
            messagebox.showinfo("提示", f"关闭服务端：\n{ex}")
            self._set_status("● 服务端已关闭")

    def _install_patch(self):
        folder = filedialog.askdirectory(title="请选择游戏客户端所在的文件夹")
        if not folder:
            return
        patch_path = resolve_path(self.cfg["patch_file"])
        if not os.path.exists(patch_path):
            messagebox.showerror("错误", f"找不到补丁文件：\n{patch_path}")
            return
        self._set_status("正在解压补丁...")

        def do_extract():
            try:
                with zipfile.ZipFile(patch_path, 'r') as zf:
                    zf.extractall(folder)
                self.after(0, lambda: messagebox.showinfo("成功",
                    f"客户端补丁已安装到：\n{folder}"))
                self.after(0, lambda: self._set_status("● 补丁安装完成"))
            except Exception as ex:
                self.after(0, lambda: messagebox.showerror("错误", f"解压补丁失败：\n{ex}"))
                self.after(0, lambda: self._set_status("解压补丁失败", "red"))

        threading.Thread(target=do_extract, daemon=True).start()

    def _start_server(self):
        server_path = resolve_path(self.cfg["server_exe"])
        if not os.path.exists(server_path):
            messagebox.showerror("错误", f"找不到服务端程序：\n{server_path}")
            return
        self._set_status("正在启动服务端...")
        try:
            subprocess.Popen(server_path, cwd=os.path.dirname(server_path))
            self._set_status("● 服务端运行中")
        except Exception as ex:
            messagebox.showerror("错误", f"启动服务端失败：\n{ex}")
            self._set_status("启动服务端失败", "red")

    # ---------- 更新检测 ----------

    def _check_update_on_start(self):
        """启动时静默检测更新"""
        self.after(2000, lambda: self._check_update(False))

    def _check_update(self, manual=False):
        update_url = self.cfg.get("update_url", "")
        if not update_url:
            if manual:
                messagebox.showinfo("提示", "未配置更新地址。")
            return

        def do_check():
            try:
                import urllib.request
                req = urllib.request.Request(update_url, headers={"User-Agent": "YueyouLauncher/" + VERSION})
                with urllib.request.urlopen(req, timeout=10) as resp:
                    data = json.loads(resp.read().decode("utf-8"))
                new_ver = data.get("version", "")
                dl_url = data.get("download_url", "")
                changelog = data.get("changelog", "")

                if new_ver == VERSION:
                    if manual:
                        self.after(0, lambda: messagebox.showinfo("提示",
                            f"当前已是最新版本 v{VERSION}"))
                    return

                msg = f"发现新版本 v{new_ver}！\n\n更新内容：\n{changelog}\n\n是否立即更新？"
                if messagebox.askyesno("发现更新", msg):
                    self._open_url(dl_url)
            except Exception as ex:
                if manual:
                    self.after(0, lambda: messagebox.showerror("错误",
                        f"检查更新失败：\n{ex}"))

        threading.Thread(target=do_check, daemon=True).start()

    # ---------- 设置界面 ----------

    def _open_settings(self):
        """弹出密码输入框，验证后打开设置"""
        pwd = simpledialog.askstring("设置验证", "请输入管理密码：", show="*")
        if pwd is None:
            return
        expected = self.cfg.get("update_password", "yueyou2024")
        if pwd != expected:
            messagebox.showerror("错误", "密码错误！")
            return
        self._show_settings_dialog()

    def _show_settings_dialog(self):
        """设置对话框"""
        dlg = tk.Toplevel(self)
        dlg.title("⚙ 设置")
        dlg.geometry("540x580")
        dlg.resizable(False, False)
        dlg.configure(bg="#1a243b")
        dlg.transient(self)
        dlg.grab_set()

        # 居中
        dlg.update_idletasks()
        x = self.winfo_x() + (900 - 540) // 2
        y = self.winfo_y() + (700 - 580) // 2
        dlg.geometry(f"540x580+{x}+{y}")

        fields = [
            ("官方网站:", "official_url"),
            ("下载工具:", "download_url"),
            ("使用教程:", "tutorial_url"),
            ("客户端下载:", "client_url"),
            ("更新检测(JSON):", "update_url"),
            ("管理密码:", "update_password"),
            ("本地列表:", "local_list_exe"),
            ("补丁文件:", "patch_file"),
            ("服务端程序:", "server_exe"),
            ("登陆器:", "launcher_exe"),
            ("物品查看器:", "viewer_exe"),
            ("底部文字:", "foot_note"),
            ("闲鱼ID:", "xianyu_id"),
        ]

        entries = {}
        y_pos = 10

        for label_text, key in fields:
            tk.Label(dlg, text=label_text,
                     font=("Microsoft YaHei", 9),
                     fg="white", bg="#1a243b").place(x=10, y=y_pos + 3)

            entry = tk.Entry(dlg, font=("Microsoft YaHei", 9),
                            bg="#2a345b", fg="white",
                            insertbackground="white",
                            bd=1, relief="solid")
            entry.insert(0, self.cfg.get(key, ""))
            entry.place(x=130, y=y_pos, width=380, height=24)
            entries[key] = entry
            y_pos += 30

        def on_save():
            for key, entry in entries.items():
                self.cfg[key] = entry.get().strip()
            try:
                save_config(self.cfg)
                messagebox.showinfo("成功", "设置已保存，部分设置需要重启生效。", parent=dlg)
                dlg.destroy()
                # 刷新底部栏
                for widget in self.winfo_children():
                    widget.destroy()
                self._build_ui()
            except Exception as ex:
                messagebox.showerror("错误", f"保存失败: {ex}", parent=dlg)

        btn_frame = tk.Frame(dlg, bg="#1a243b")
        btn_frame.place(x=280, y=y_pos + 15)

        tk.Button(btn_frame, text="保存",
                  font=("Microsoft YaHei", 9),
                  fg="white", bg="#4a8ac0",
                  bd=0, relief="flat", cursor="hand2",
                  width=10, command=on_save).pack(side="left", padx=5)

        tk.Button(btn_frame, text="取消",
                  font=("Microsoft YaHei", 9),
                  fg="white", bg="#1a243b",
                  bd=1, relief="solid", cursor="hand2",
                  width=10, command=dlg.destroy).pack(side="left", padx=5)


# ========== 启动 ==========

if __name__ == "__main__":
    app = YueyouLauncher()
    app.mainloop()
