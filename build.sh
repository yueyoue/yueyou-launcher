#!/bin/bash
# 本地编译脚本 (Windows 下用 Git Bash 或 WSL 运行)
set -e

echo "=== 悦游网单游戏启动器 编译 ==="

# 安装依赖
pip install -r requirements.txt

# 编译
pyinstaller yueyou_launcher.spec --clean

echo ""
echo "=== 编译完成 ==="
echo "输出文件: dist/悦游网单游戏启动器.exe"
