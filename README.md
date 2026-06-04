# 悦游网单游戏启动器

一款专为网游单机玩家设计的游戏启动器，集成服务端、客户端、登陆器、插件下载等功能。

## 功能特性

- 🌐 一键访问官方网站
- 🛡️ 一键关闭防火墙/杀毒软件
- 📥 快速下载工具和客户端
- 📖 使用教程入口
- 🔍 物品查看器
- ⚡ 一键关闭服务端
- 🎮 五步启动游戏流程
- 🔄 自动版本更新检测
- ⚙️ 隐藏设置面板（点击底部"鱼"字进入）

## 系统要求

- Windows 7 / 8 / 8.1 / 10 / 11
- 无需安装任何依赖，单文件运行

## 编译

本项目使用 GitHub Actions 自动编译：

1. Fork 或 Clone 本仓库
2. 推送代码后自动触发编译
3. 在 Actions 页面下载编译好的 exe 文件

### 手动编译

```bash
# 安装依赖
go mod tidy

# 安装资源工具
go install github.com/tc-hib/go-winres@latest

# 生成资源
go-winres make

# 编译（64位）
go build -ldflags="-s -w -H windowsgui" -o YueyouLauncher.exe .

# 编译（32位，兼容 Win7）
GOOS=windows GOARCH=386 go build -ldflags="-s -w -H windowsgui" -o YueyouLauncher_x86.exe .
```

## 配置

首次运行会自动生成 `config.json`，也可以通过隐藏设置面板修改：

- 点击底部栏的 **鱼** 字进入设置
- 修改所有链接、文件路径、显示文字等

### config.json 示例

```json
{
  "official_url": "http://www.yueyoue.cn",
  "download_url": "http://www.yueyoue.cn",
  "tutorial_url": "http://www.yueyoue.cn",
  "client_url": "http://www.yueyoue.cn",
  "update_url": "",
  "local_list_exe": "tools\\本地列表.exe",
  "patch_file": "patch\\client_patch.zip",
  "server_exe": "server\\GameServer.exe",
  "launcher_exe": "client\\GameLauncher.exe",
  "viewer_exe": "tools\\物品查看器.exe",
  "foot_note": "官网：http://www.yueyoue.cn",
  "xianyu_id": "闲鱼ID：悦游网单"
}
```

## 更新机制

配置 `update_url` 指向一个 JSON 文件，格式：

```json
{
  "version": "1.1.0",
  "download_url": "https://...",
  "changelog": "修复了若干问题"
}
```

## 文件结构

```
├── main.go              # 主程序
├── config.go            # 配置管理
├── go.mod               # Go 模块文件
├── res/
│   ├── icon.png         # 应用图标
│   └── winres.json      # Windows 资源配置
└── .github/
    └── workflows/
        └── build.yml    # GitHub Actions 编译配置
```

## 许可

仅供学习交流使用。
