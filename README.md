# Stratum 桌面版

基于 Avalonia 的跨平台双因素认证 (2FA) 桌面客户端,由 DeepSeek 开发。
核心逻辑复用 [Stratum](https://github.com/stratumauth/app) 项目的 `Stratum.Core`
库(原项目遵循 GPL-3.0)。

## 功能

- TOTP / HOTP / Steam / mOTP / Yandex 验证码生成,实时刷新与倒计时
- 账户与分类管理,内置 774 个服务图标,支持图标包与自定义图标
- 加密备份(Argon2id + AES-GCM),与 Android 版备份格式互通
- 导入 16 种第三方验证器备份(Aegis、Bitwarden、2FAS、KeePass、Google Authenticator 等)
- 从二维码图片识别并添加账户
- 中文 / English 双语,首次启动跟随系统语言;深色 / 浅色 / 自动主题
- 无边框窗口,自制标题栏与边缘缩放;单实例运行

## 平台支持

| 平台 | 架构 | 安装包 | 系统要求 |
| --- | --- | --- | --- |
| Windows | x64 / arm64 | NSIS 安装器 | Windows 10 1809 及以上 |
| Linux | x86_64 / aarch64 | deb / rpm / AppImage | glibc 2.35 及以上,X11 或 Wayland |

各平台均为自包含发布,无需安装 .NET 运行时。数据保存在 `~/.local/share/Stratum/`。

## 下载与安装

前往 [GitHub Releases](https://github.com/cyf112233/stratum_desktop/releases) 下载对应平台的安装包。

- **Windows**:运行安装包,自动创建开始菜单与桌面快捷方式
- **Linux**:
  - deb(Debian / Ubuntu):`sudo dpkg -i stratum-*.deb`
  - rpm(Fedora / openSUSE):`sudo rpm -i stratum-*.rpm`
  - AppImage:`chmod +x Stratum-*.AppImage && ./Stratum-*.AppImage`

## 从源码构建

需要 .NET 10 SDK。

```bash
# 单平台自包含二进制
./build-linux.sh      # Linux x64
./build-windows.sh    # Windows x64

# Linux 安装包(在 publish 产物目录上执行)
bash packaging/linux/build-deb.sh <publish-dir> <rid> <out-dir>
bash packaging/linux/build-rpm.sh <publish-dir> <rpm-arch> <out-dir>
bash packaging/linux/build-appimage.sh <publish-dir> <arch> <out-dir>
```

GitHub Actions 工作流 `.github/workflows/desktop.yml` 会在推送 `v*` 标签时自动构建
Windows 与 Linux 各架构的安装包,并发布到 Releases。

## 字体

内置 [Noto Sans SC](https://github.com/notofonts/noto-cjk)(思源黑体简体中文子集,
Regular + Bold),遵循 SIL Open Font License 1.1,允许自由使用、嵌入与再分发。

## 许可

本项目基于 GPL-3.0 发布。`Stratum.Core` 等核心库版权归原 Stratum 项目所有。
内置服务图标来自各服务官网,仅供识别用途。
