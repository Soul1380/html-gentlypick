# 轻摘 Windows 桌面版源码

当前版本由源码根目录的 [version.json](version.json) 决定。桌面程序使用 WinForms、WebView2 和本机 Node 服务；`desktop/` 包含主程序和独立更新器，`app/` 包含工作台与采集服务。

## 文件夹与常用入口

- `app/`：网页工作台、采集、分析和报告功能的源码。
- `desktop/`：Windows 主程序及 `updater/` 更新器源码。
- `tests/`：摘录与更新器测试。
- `docs/`：版本发布说明，见 [1.0.1 发布说明](docs/RELEASE_NOTES_1.0.1.md)。
- `distribution/`：构建时放入安装包的说明和启动脚本。
- `.github/workflows/`：GitHub 自动构建与发布配置。
- `.build/`：构建缓存，已经排除在 Git 提交之外；删除后下次构建需重新下载依赖。
- `dist/`：下次构建时自动生成的输出目录，不是源码。
- `发布版本到GitHub.cmd`：提交并上传源码，再推送版本标签；实际发布步骤由 `publish-release.ps1` 执行。
- `version.json`：手动维护的版本号；`version-tools.ps1`：版本校验；构建脚本和依赖配置也在根目录。

## 构建

在 Windows x64 的 PowerShell 中运行：

```powershell
./build-desktop.ps1
```

构建前修改根目录 `version.json` 的 `version`，例如 `"1.0.2"`。构建脚本读取该文件，并将相同版本写入构建后的软件版本文件和安装包名称；如使用 `-Version`，其值必须与文件一致。构建需要 Node.js、PowerShell、.NET Framework 4.8 和网络。脚本固定并校验 WebView2、Node、Playwright 和视频组件；可选 `-ToolsDirectory` 指向本地视频工具目录，本地副本同样会校验哈希。

## 发布与更新

应用从 `Soul1380/html-gentlypick` 的正式 GitHub Release 检查更新。新版本 Release 需要上传 `LightExcerpt-win-x64-update.zip`，更新器校验 GitHub 返回的附件大小和 SHA-256。`v*` 标签可以触发 `.github/workflows/windows-release.yml` 构建并上传发布附件。已有同名附件不会自动覆盖。

发布时双击 `发布版本到GitHub.cmd`：脚本读取 `version.json`，先上传源码，再推送对应标签，例如 `v1.0.2`，随后到 GitHub Actions 查看构建。旧的 `发布1.0.1到GitHub.cmd` 会转到同一个入口。标签、手动输入的版本与版本文件不一致时会停止构建；若本地标签已指向其他提交，发布脚本也会停止。

1.0.0 的旧更新器使用另一种更新清单，首次迁移至 1.0.1 需要关闭旧版并备份 `UserData`，使用完整安装包；之后的更新由 1.0.1 更新器处理。程序数据和登录资料位于 `UserData`，不得上传到仓库或包含在发布包中。

## 测试

在 PowerShell 中运行 `./tests/version-tests.ps1` 检查版本配置，运行 `./tests/updater/run-tests.ps1` 测试更新器，运行 `node ./tests/formatted-extract.cjs` 测试表格和代码块摘录。完整轻摘程序另在独立安装目录完成过旧进程退出、替换和重启测试。真实 GitHub Release 在线升级需要发布后验证；软件目前未进行代码签名。
