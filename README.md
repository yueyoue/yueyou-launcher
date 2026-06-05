# 悦游网单游戏启动器

网游单机游戏集成启动器，支持客户端下载、补丁安装、服务端管理等功能。

## 功能

- 🌐 官方网站快捷入口
- 🛡️ 一键关闭防火墙/杀毒软件
- 📥 下载工具、使用教程链接
- 🔍 物品查看器
- ⚡ 一键关闭服务端
- 游戏启动五步骤引导
- 自动版本检测与更新
- 隐藏设置面板（点击"闲鱼ID：悦游网单"中的"鱼"字10次）

## 技术栈

- Python 3.11+
- tkinter (GUI)
- PyInstaller (打包)

## 本地编译

```bash
pip install -r requirements.txt
pyinstaller yueyou_launcher.spec --clean
```

输出文件在 `dist/悦游网单游戏启动器.exe`

## 自动编译

推送到 GitHub 后，通过 GitHub Actions 自动编译。

创建 tag 并推送到 GitHub 即可触发自动 Release：
```bash
git tag v2.0.0
git push origin v2.0.0
```

## 配置文件

首次运行会在 exe 同目录生成 `config.json`，也可通过隐藏设置面板修改。

## 管理员权限

关闭防火墙和杀毒软件功能需要管理员权限，建议右键以管理员身份运行。
