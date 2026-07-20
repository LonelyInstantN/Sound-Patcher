# <img src="app.png" width="48" align="absmiddle"> Sound Patcher

一个为**触摸屏**设计的 Windows 11 音频输出设备切换工具。大按钮列表，点一下即可把系统声音切换到对应设备。

![平台](https://img.shields.io/badge/platform-Windows%2011-blue)
![技术栈](https://img.shields.io/badge/built%20with-WinUI%203%20%2B%20.NET%208-512bd4)

## 功能

- 🎚️ 列出所有可用的音频输出设备，纵向大按钮排列，适合手指点击
- 👆 点击按钮立即切换默认音频输出
- ✏️ 编辑模式：可隐藏不常用的设备，只保留几个常用设备来回切换
- 📍 记忆窗口位置和尺寸，重启后自动恢复到摆放好的位置
- 🔄 每 2 秒自动刷新设备状态，热插拔也能及时响应
- 🎨 Win11 原生 Fluent 风格界面

## 界面

- 主名称 + 详细信息双行显示，一眼看清是哪个输出
- 隐藏的设备在编辑模式下半透明显示，点击可恢复

## 下载与运行

发布版是**单文件 exe**（约 62MB），已包含全部运行时，无需安装任何依赖，双击即用。

### 开机自启

1. `Win + R` 输入 `shell:startup` 回车
2. 把 `Sound Patcher.exe` 的快捷方式放进该文件夹

每次开机会自动启动并恢复到上次摆放的位置。

## 自行构建

需要 .NET 8 SDK：

```powershell
winget install Microsoft.DotNet.SDK.8
```

然后运行发布脚本：

```powershell
.\publish.ps1
```

产物输出到 `publish\Sound Patcher.exe`。

## 技术栈

- WinUI 3 (Windows App SDK) + .NET 8，unpackaged + self-contained 单文件发布
- 音频设备枚举 / 切换：Windows Core Audio API（COM interop，无第三方依赖）
- 设置持久化：`%LocalAppData%\SoundSwitcher\settings.json`

## 图标

Icon by [Icons8](https://icons8.com)
