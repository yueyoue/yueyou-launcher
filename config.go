package main

import (
	"encoding/json"
	"os"
	"path/filepath"
)

// Config holds all configurable paths and URLs
type Config struct {
	// URLs
	OfficialURL string `json:"official_url"`
	DownloadURL string `json:"download_url"`
	TutorialURL string `json:"tutorial_url"`
	ClientURL   string `json:"client_url"`
	UpdateURL   string `json:"update_url"` // URL to check for updates (JSON)

	// File paths (relative to launcher dir)
	LocalListExe string `json:"local_list_exe"`
	PatchFile    string `json:"patch_file"`
	ServerExe    string `json:"server_exe"`
	LauncherExe  string `json:"launcher_exe"`
	ViewerExe    string `json:"viewer_exe"`

	// Display
	FootNote string `json:"foot_note"`
	XianyuID string `json:"xianyu_id"`
}

// UpdateInfo is the JSON structure returned by the update URL
type UpdateInfo struct {
	Version     string `json:"version"`
	DownloadURL string `json:"download_url"`
	Changelog   string `json:"changelog"`
}

var defaultConfig = Config{
	OfficialURL: "http://www.yueyoue.cn",
	DownloadURL: "http://www.yueyoue.cn",
	TutorialURL: "http://www.yueyoue.cn",
	ClientURL:   "http://www.yueyoue.cn",
	UpdateURL:   "",

	LocalListExe: "tools\\本地列表.exe",
	PatchFile:    "patch\\client_patch.zip",
	ServerExe:    "server\\GameServer.exe",
	LauncherExe:  "client\\GameLauncher.exe",
	ViewerExe:    "tools\\物品查看器.exe",

	FootNote: "官网：http://www.yueyoue.cn",
	XianyuID: "闲鱼ID：悦游网单",
}

func configPath() string {
	dir, _ := filepath.Abs(filepath.Dir(os.Args[0]))
	return filepath.Join(dir, "config.json")
}

func loadConfig() Config {
	cfg := defaultConfig
	data, err := os.ReadFile(configPath())
	if err != nil {
		return cfg
	}
	_ = json.Unmarshal(data, &cfg)
	return cfg
}

func saveConfig(cfg Config) error {
	data, err := json.MarshalIndent(cfg, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(configPath(), data, 0644)
}

// resolvePath resolves a path relative to the launcher's directory
func resolvePath(p string) string {
	if filepath.IsAbs(p) {
		return p
	}
	dir, _ := filepath.Abs(filepath.Dir(os.Args[0]))
	return filepath.Join(dir, p)
}
