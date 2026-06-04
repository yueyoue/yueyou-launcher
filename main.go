package main

import (
	"archive/zip"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"syscall"
	"time"
	"unsafe"

	"github.com/lxn/walk"
	. "github.com/lxn/walk/declarative"
)

// Version is set at build time via ldflags
var Version = "1.0.0"

type LauncherWindow struct {
	cfg              Config
	mw               *walk.MainWindow
	statusLbl        *walk.TextLabel
	xianyuClickCount int
	lastXianyuClick  time.Time
}

func main() {
	cfg := loadConfig()
	lw := &LauncherWindow{cfg: cfg}

	err := MainWindow{
		AssignTo: &lw.mw,
		Title:    "悦游网单游戏启动器",
		MinSize:  Size{Width: 860, Height: 640},
		MaxSize:  Size{Width: 860, Height: 640},
		Layout:   VBox{MarginsZero: true, Spacing: 0},
		Children: []Widget{
			// ===== 标题栏 =====
			Composite{
				MinSize: Size{Height: 56},
				MaxSize: Size{Height: 56},
				Layout:  HBox{Margins: Margins{Left: 20, Right: 12}},
				Children: []Widget{
					TextLabel{
						Text:    "🎮 悦游网单游戏启动器",
						MinSize: Size{Width: 280},
						Font:    Font{Family: "Microsoft YaHei", PointSize: 14, Bold: true},
					},
					HSpacer{},
					TextLabel{
						Text: "v" + Version,
						Font: Font{PointSize: 9},
					},
					PushButton{
						Text:    "🔄 检查更新",
						MinSize: Size{Width: 100, Height: 28},
						Font:    Font{PointSize: 9},
						OnClicked: func() {
							lw.checkUpdate(true)
						},
					},
				},
			},
			// ===== 状态栏 =====
			Composite{
				MinSize: Size{Height: 28},
				MaxSize: Size{Height: 28},
				Layout:  HBox{Margins: Margins{Left: 20, Right: 20}},
				Children: []Widget{
					TextLabel{
						AssignTo: &lw.statusLbl,
						Text:     "● 就绪",
					},
					HSpacer{},
				},
			},
			// ===== 主内容区 =====
			Composite{
				Layout: HBox{Margins: Margins{Left: 20, Right: 20, Top: 8, Bottom: 8}, Spacing: 20},
				Children: []Widget{
					// --- 左侧功能栏 ---
					Composite{
						MinSize: Size{Width: 200},
						MaxSize: Size{Width: 200},
						Layout:  VBox{Spacing: 8},
						Children: []Widget{
							PushButton{
								Text:     "🌐 官方网站",
								MinSize:  Size{Height: 40},
								Font:     Font{PointSize: 11},
								OnClicked: func() { openURL(cfg.OfficialURL) },
							},
							PushButton{
								Text:     "📥 下载工具",
								MinSize:  Size{Height: 40},
								Font:     Font{PointSize: 11},
								OnClicked: func() { openURL(cfg.DownloadURL) },
							},
							PushButton{
								Text:     "📖 使用教程",
								MinSize:  Size{Height: 40},
								Font:     Font{PointSize: 11},
								OnClicked: func() { openURL(cfg.TutorialURL) },
							},
							PushButton{
								Text:     "🔍 物品查看器",
								MinSize:  Size{Height: 40},
								Font:     Font{PointSize: 11},
								OnClicked: func() { lw.openExe(cfg.ViewerExe, "物品查看器") },
							},
							VSpacer{MinSize: Size{Height: 8}},
							PushButton{
								Text:     "🛡️ 关闭防火墙",
								MinSize:  Size{Height: 40},
								Font:     Font{PointSize: 11},
								OnClicked: func() { lw.disableFirewall() },
							},
							PushButton{
								Text:     "🛡️ 关闭杀毒软件",
								MinSize:  Size{Height: 40},
								Font:     Font{PointSize: 11},
								OnClicked: func() { lw.disableAntivirus() },
							},
							VSpacer{},
							PushButton{
								Text:     "⚡ 一键关闭服务端",
								MinSize:  Size{Height: 44},
								Font:     Font{PointSize: 11, Bold: true},
								OnClicked: func() { lw.killGameServer() },
							},
						},
					},
					// --- 右侧步骤区 ---
					Composite{
						Layout: VBox{Spacing: 8},
						Children: []Widget{
							TextLabel{
								Text: "游戏启动步骤",
								Font: Font{PointSize: 9},
							},
							makeStepCard("步骤 1", "下载客户端", "下载游戏客户端安装包", func() { openURL(cfg.ClientURL) }),
							makeStepCard("步骤 2", "打开本地列表", "启动服务器列表查看工具", func() { lw.openExe(cfg.LocalListExe, "本地列表") }),
							makeStepCard("步骤 3", "安装客户端补丁", "选择客户端目录后自动解压补丁", func() { lw.installPatch() }),
							makeStepCard("步骤 4", "启动服务器", "启动游戏服务端程序", func() { lw.startServer() }),
							makeStepCard("步骤 5", "打开登陆器", "启动游戏客户端登陆器", func() { lw.openExe(cfg.LauncherExe, "登陆器") }),
						},
					},
				},
			},
			// ===== 底部栏 =====
			Composite{
				MinSize: Size{Height: 32},
				MaxSize: Size{Height: 32},
				Layout:  HBox{Margins: Margins{Left: 20, Right: 20}},
				Children: []Widget{
					LinkLabel{
						Text: fmt.Sprintf(`<a href="%s" style="color:#4a8ac0">%s</a>`, cfg.OfficialURL, cfg.FootNote),
						Font: Font{PointSize: 9},
					},
					HSpacer{},
					LinkLabel{
						Text: fmt.Sprintf(`<a id="xianyu" style="color:#4a8ac0">%s</a>`, cfg.XianyuID),
						Font: Font{PointSize: 9},
						OnLinkActivated: func(link *walk.LinkLabelLink) {
							if link.Id() == "xianyu" {
								now := time.Now()
								if now.Sub(lw.lastXianyuClick) > 3*time.Second {
									lw.xianyuClickCount = 0
								}
								lw.lastXianyuClick = now
								lw.xianyuClickCount++
								if lw.xianyuClickCount >= 10 {
									lw.xianyuClickCount = 0
									lw.openSettings()
								}
							}
						},
					},
				},
			},
		},
	}.Create()

	if err != nil {
		walk.MsgBox(nil, "错误", "启动失败: "+err.Error(), walk.MsgBoxIconError)
		return
	}

	bgBrush, _ := walk.NewSolidColorBrush(walk.Color(0x0D1225))
	lw.mw.SetBackground(bgBrush)
	// 居中窗口
	lw.mw.SetBounds(walk.Rectangle{X: 100, Y: 100, Width: 860, Height: 640})

	// 启动后静默检查更新
	go func() {
		time.Sleep(2 * time.Second)
		lw.checkUpdate(false)
	}()

	lw.mw.Run()
}

// makeStepCard 创建步骤卡片
func makeStepCard(stepNum, title, desc string, onClick func()) Widget {
	return Composite{
		MinSize: Size{Height: 56},
		Layout:  HBox{Margins: Margins{Left: 12, Right: 12, Top: 8, Bottom: 8}},
		Children: []Widget{
			TextLabel{
				Text:     stepNum,
				MinSize:  Size{Width: 50},
				MaxSize:  Size{Width: 50},
				Font:     Font{PointSize: 10, Bold: true},
			},
			Composite{
				Layout: VBox{MarginsZero: true, Spacing: 0},
				Children: []Widget{
					TextLabel{Text: title, Font: Font{PointSize: 11, Bold: true}},
					TextLabel{Text: desc, Font: Font{PointSize: 9}},
				},
			},
			HSpacer{},
			PushButton{
				Text:     "打开",
				MinSize:  Size{Width: 70, Height: 30},
				Font:     Font{PointSize: 9},
				OnClicked: onClick,
			},
		},
	}
}

// openURL 用默认浏览器打开链接
func openURL(url string) {
	cmd := exec.Command("cmd", "/c", "start", url)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	_ = cmd.Start()
}

// openExe 启动指定的 exe 文件
func (lw *LauncherWindow) openExe(relPath, name string) {
	fullPath := resolvePath(relPath)
	if _, err := os.Stat(fullPath); os.IsNotExist(err) {
		walk.MsgBox(lw.mw, "提示", fmt.Sprintf("找不到 %s：\n%s", name, fullPath), walk.MsgBoxIconWarning)
		return
	}
	cmd := exec.Command(fullPath)
	cmd.Dir = filepath.Dir(fullPath)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	if err := cmd.Start(); err != nil {
		walk.MsgBox(lw.mw, "错误", fmt.Sprintf("启动 %s 失败：\n%s", name, err.Error()), walk.MsgBoxIconError)
	}
}

// setStatus 更新状态栏
func (lw *LauncherWindow) setStatus(text string) {
	if lw.statusLbl != nil {
		lw.statusLbl.SetText(text)
	}
}

// disableFirewall 关闭 Windows 防火墙
func (lw *LauncherWindow) disableFirewall() {
	if walk.MsgBox(lw.mw, "确认", "确定要关闭 Windows 防火墙吗？\n这可能会降低系统安全性。", walk.MsgBoxYesNo|walk.MsgBoxIconWarning) != walk.DlgCmdYes {
		return
	}
	lw.setStatus("正在关闭防火墙...")
	cmd := exec.Command("netsh", "advfirewall", "set", "allprofiles", "state", "off")
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	out, err := cmd.CombinedOutput()
	if err != nil {
		walk.MsgBox(lw.mw, "错误", "关闭防火墙失败：\n"+string(out), walk.MsgBoxIconError)
		lw.setStatus("关闭防火墙失败")
	} else {
		walk.MsgBox(lw.mw, "成功", "Windows 防火墙已关闭。", walk.MsgBoxIconInformation)
		lw.setStatus("● 防火墙已关闭")
	}
}

// disableAntivirus 关闭 Windows Defender
func (lw *LauncherWindow) disableAntivirus() {
	if walk.MsgBox(lw.mw, "确认", "确定要关闭 Windows Defender 实时保护吗？\n建议同时关闭第三方杀毒软件。", walk.MsgBoxYesNo|walk.MsgBoxIconWarning) != walk.DlgCmdYes {
		return
	}
	lw.setStatus("正在关闭杀毒软件...")
	psScript := `Set-MpPreference -DisableRealtimeMonitoring $true`
	cmd := exec.Command("powershell", "-ExecutionPolicy", "Bypass", "-Command", psScript)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	out, err := cmd.CombinedOutput()
	if err != nil {
		walk.MsgBox(lw.mw, "提示", "关闭 Defender 可能需要管理员权限。\n请右键以管理员身份运行本程序。\n\n"+string(out), walk.MsgBoxIconWarning)
		lw.setStatus("关闭杀毒软件失败（需要管理员权限）")
	} else {
		walk.MsgBox(lw.mw, "成功", "Windows Defender 实时保护已关闭。\n请手动关闭第三方杀毒软件。", walk.MsgBoxIconInformation)
		lw.setStatus("● Defender 已关闭")
	}
}

// killGameServer 关闭游戏服务端进程
func (lw *LauncherWindow) killGameServer() {
	if walk.MsgBox(lw.mw, "确认", "确定要关闭游戏服务端吗？", walk.MsgBoxYesNo|walk.MsgBoxIconQuestion) != walk.DlgCmdYes {
		return
	}
	lw.setStatus("正在关闭服务端...")
	serverPath := resolvePath(lw.cfg.ServerExe)
	exeName := filepath.Base(serverPath)
	cmd := exec.Command("taskkill", "/F", "/IM", exeName)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	out, err := cmd.CombinedOutput()
	if err != nil {
		walk.MsgBox(lw.mw, "提示", "关闭服务端：\n"+string(out), walk.MsgBoxIconInformation)
	} else {
		walk.MsgBox(lw.mw, "成功", "游戏服务端已关闭。", walk.MsgBoxIconInformation)
	}
	lw.setStatus("● 服务端已关闭")
}

// installPatch 选择客户端文件夹并解压补丁
func (lw *LauncherWindow) installPatch() {
	folder := browseForFolder(lw.mw, "请选择游戏客户端所在的文件夹")
	if folder == "" {
		return
	}
	patchPath := resolvePath(lw.cfg.PatchFile)
	if _, err := os.Stat(patchPath); os.IsNotExist(err) {
		walk.MsgBox(lw.mw, "错误", "找不到补丁文件：\n"+patchPath, walk.MsgBoxIconError)
		return
	}
	lw.setStatus("正在解压补丁...")
	if err := unzip(patchPath, folder); err != nil {
		walk.MsgBox(lw.mw, "错误", "解压补丁失败：\n"+err.Error(), walk.MsgBoxIconError)
		lw.setStatus("解压补丁失败")
	} else {
		walk.MsgBox(lw.mw, "成功", "客户端补丁已安装到：\n"+folder, walk.MsgBoxIconInformation)
		lw.setStatus("● 补丁安装完成")
	}
}

// startServer 启动游戏服务端
func (lw *LauncherWindow) startServer() {
	serverPath := resolvePath(lw.cfg.ServerExe)
	if _, err := os.Stat(serverPath); os.IsNotExist(err) {
		walk.MsgBox(lw.mw, "错误", "找不到服务端程序：\n"+serverPath, walk.MsgBoxIconWarning)
		return
	}
	lw.setStatus("正在启动服务端...")
	cmd := exec.Command(serverPath)
	cmd.Dir = filepath.Dir(serverPath)
	cmd.SysProcAttr = &syscall.SysProcAttr{HideWindow: true}
	if err := cmd.Start(); err != nil {
		walk.MsgBox(lw.mw, "错误", "启动服务端失败：\n"+err.Error(), walk.MsgBoxIconError)
		lw.setStatus("启动服务端失败")
	} else {
		lw.setStatus("● 服务端运行中")
	}
}

// checkUpdate 检查版本更新
func (lw *LauncherWindow) checkUpdate(manual bool) {
	if lw.cfg.UpdateURL == "" {
		if manual {
			walk.MsgBox(lw.mw, "提示", "未配置更新地址。", walk.MsgBoxIconInformation)
		}
		return
	}
	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Get(lw.cfg.UpdateURL)
	if err != nil {
		if manual {
			walk.MsgBox(lw.mw, "错误", "检查更新失败：\n"+err.Error(), walk.MsgBoxIconError)
		}
		return
	}
	defer resp.Body.Close()
	var info UpdateInfo
	if err := json.NewDecoder(resp.Body).Decode(&info); err != nil {
		if manual {
			walk.MsgBox(lw.mw, "错误", "解析更新信息失败。", walk.MsgBoxIconError)
		}
		return
	}
	if info.Version == Version {
		if manual {
			walk.MsgBox(lw.mw, "提示", "当前已是最新版本 v"+Version, walk.MsgBoxIconInformation)
		}
		return
	}
	msg := fmt.Sprintf("发现新版本 v%s！\n\n更新内容：\n%s\n\n是否立即更新？", info.Version, info.Changelog)
	if walk.MsgBox(lw.mw, "发现更新", msg, walk.MsgBoxYesNo|walk.MsgBoxIconQuestion) == walk.DlgCmdYes {
		openURL(info.DownloadURL)
	}
}

// openSettings 打开隐藏设置界面
func (lw *LauncherWindow) openSettings() {
	cfg := lw.cfg
	var dlg *walk.Dialog
	var db *walk.DataBinder

	Dialog{
		AssignTo: &dlg,
		Title:    "⚙ 设置",
		MinSize:  Size{Width: 520, Height: 520},
		Layout:   VBox{},
		DataBinder: DataBinder{
			AssignTo:   &db,
			DataSource: &cfg,
		},
		Children: []Widget{
			ScrollView{
				Layout: VBox{Spacing: 6},
				Children: []Widget{
					GroupBox{
						Title:  "链接设置",
						Layout: Grid{Columns: 2},
						Children: []Widget{
							Label{Text: "官方网站:"},
							LineEdit{Text: Bind("OfficialURL")},
							Label{Text: "下载工具:"},
							LineEdit{Text: Bind("DownloadURL")},
							Label{Text: "使用教程:"},
							LineEdit{Text: Bind("TutorialURL")},
							Label{Text: "客户端下载:"},
							LineEdit{Text: Bind("ClientURL")},
							Label{Text: "更新检测(JSON):"},
							LineEdit{Text: Bind("UpdateURL")},
						},
					},
					GroupBox{
						Title:  "文件路径（相对于启动器目录）",
						Layout: Grid{Columns: 2},
						Children: []Widget{
							Label{Text: "本地列表:"},
							LineEdit{Text: Bind("LocalListExe")},
							Label{Text: "补丁文件:"},
							LineEdit{Text: Bind("PatchFile")},
							Label{Text: "服务端程序:"},
							LineEdit{Text: Bind("ServerExe")},
							Label{Text: "登陆器:"},
							LineEdit{Text: Bind("LauncherExe")},
							Label{Text: "物品查看器:"},
							LineEdit{Text: Bind("ViewerExe")},
						},
					},
					GroupBox{
						Title:  "显示设置",
						Layout: Grid{Columns: 2},
						Children: []Widget{
							Label{Text: "底部文字:"},
							LineEdit{Text: Bind("FootNote")},
							Label{Text: "闲鱼ID:"},
							LineEdit{Text: Bind("XianyuID")},
						},
					},
				},
			},
			Composite{
				Layout: HBox{},
				Children: []Widget{
					HSpacer{},
					PushButton{
						Text: "保存",
						OnClicked: func() {
							if err := db.Submit(); err != nil {
								walk.MsgBox(dlg, "错误", "保存失败: "+err.Error(), walk.MsgBoxIconError)
								return
							}
							if err := saveConfig(cfg); err != nil {
								walk.MsgBox(dlg, "错误", "保存配置文件失败: "+err.Error(), walk.MsgBoxIconError)
								return
							}
							lw.cfg = cfg
							walk.MsgBox(dlg, "成功", "设置已保存，部分设置需要重启生效。", walk.MsgBoxIconInformation)
							dlg.Accept()
						},
					},
					PushButton{
						Text:     "取消",
						OnClicked: func() { dlg.Cancel() },
					},
				},
			},
		},
	}.Run(lw.mw)
}

// browseForFolder 弹出文件夹选择对话框
func browseForFolder(owner walk.Form, title string) string {
	var path string
	var dlg *walk.Dialog
	var edit *walk.LineEdit

	Dialog{
		AssignTo: &dlg,
		Title:    title,
		MinSize:  Size{Width: 450, Height: 130},
		Layout:   VBox{},
		Children: []Widget{
			TextLabel{Text: "请输入或浏览游戏客户端安装目录："},
			LineEdit{AssignTo: &edit},
			Composite{
				Layout: HBox{},
				Children: []Widget{
					HSpacer{},
					PushButton{
						Text: "浏览...",
						OnClicked: func() {
							folder := showFolderDialog(dlg)
							if folder != "" {
								edit.SetText(folder)
							}
						},
					},
					PushButton{
						Text: "确定",
						OnClicked: func() {
							path = edit.Text()
							dlg.Accept()
						},
					},
					PushButton{
						Text:     "取消",
						OnClicked: func() { dlg.Cancel() },
					},
				},
			},
		},
	}.Run(owner)

	return path
}

// showFolderDialog 调用 Windows 原生文件夹选择对话框
func showFolderDialog(owner walk.Form) string {
	type browseInfo struct {
		hwndOwner       uintptr
		pidlRoot        uintptr
		pszDisplayName  *uint16
		lpszTitle       *uint16
		ulFlags         uint32
		lpfn            uintptr
		lParam          uintptr
		iImage          int32
	}

	var bi browseInfo
	if owner != nil {
		bi.hwndOwner = uintptr(owner.Handle())
	}
	bi.lpszTitle, _ = syscall.UTF16PtrFromString("请选择游戏客户端文件夹")
	bi.ulFlags = 0x00000040 // BIF_RETURNONLYFSDIRS

	shell32 := syscall.NewLazyDLL("shell32.dll")
	ole32 := syscall.NewLazyDLL("ole32.dll")

	shBrowseForFolder := shell32.NewProc("SHBrowseForFolderW")
	shGetPathFromIDList := shell32.NewProc("SHGetPathFromIDListW")
	coTaskMemFree := ole32.NewProc("CoTaskMemFree")

	ret, _, _ := shBrowseForFolder.Call(uintptr(unsafe.Pointer(&bi)))
	if ret == 0 {
		return ""
	}
	defer coTaskMemFree.Call(ret)

	buf := make([]uint16, 260)
	shGetPathFromIDList.Call(ret, uintptr(unsafe.Pointer(&buf[0])))
	return syscall.UTF16ToString(buf)
}

// unzip 解压 zip 文件到目标目录
func unzip(src, dest string) error {
	r, err := zip.OpenReader(src)
	if err != nil {
		return err
	}
	defer r.Close()

	for _, f := range r.File {
		fpath := filepath.Join(dest, f.Name)
		if !strings.HasPrefix(filepath.Clean(fpath), filepath.Clean(dest)+string(os.PathSeparator)) {
			return fmt.Errorf("非法路径: %s", fpath)
		}
		if f.FileInfo().IsDir() {
			os.MkdirAll(fpath, os.ModePerm)
			continue
		}
		os.MkdirAll(filepath.Dir(fpath), os.ModePerm)
		outFile, err := os.Create(fpath)
		if err != nil {
			return err
		}
		rc, err := f.Open()
		if err != nil {
			outFile.Close()
			return err
		}
		_, err = io.Copy(outFile, rc)
		rc.Close()
		outFile.Close()
		if err != nil {
			return err
		}
	}
	return nil
}
