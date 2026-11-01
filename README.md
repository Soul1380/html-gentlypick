# 轻摘桌面版源码 v1.0.0

Windows WinForms + Microsoft WebView2 + 本机 Node 服务。桌面端与独立更新器的源码在 desktop/；原有采集和AI功能在 app/。

## 本地构建
构建需 Windows x64、Node.js 22+、PowerShell；最终用户无需安装 Node。用 PowerShell 运行：
```powershell
./build-desktop.ps1 -Version 1.0.0 -ToolsDirectory '旧版完整软件/app/tools'
```
脚本从微软 NuGet 下载固定版本的 WebView2 SDK并核对已记录SHA-256；下载固定版本Node并核对官方SHASUMS；安装固定版本Playwright；用系统.NET Framework C#编译器构建EXE，然后生成dist目录与更新包。

app/tools 的大型第三方 EXE 没有放进源码，避免超出GitHub单文件限制。完整的初始桌面发布包已另行提供。构建初次完整便携版时请传ToolsDirectory；未来更新包即使不带工具，也会保留已安装版本中的工具文件。若从GitHub Actions构建的包作为全新安装使用，需另行放入视频工具，或传入工具目录重新打完整包。

## 准备GitHub
1. 将本源码目录内容上传到自己的GitHub仓库，不要上传UserData或本机报告。
2. 首次可直接在Release v1.0.0上传另行提供的完整包 `LightExcerpt-v1.0.0-win-x64.zip`。
3. 运行 `./publish-package.ps1 -InstallDirectory '完整软件目录' -Version 1.0.0 -Repository '用户名/仓库名'`，将生成的latest.json一并上传同一Release。
4. 在软件更新器填：`https://github.com/用户名/仓库名/releases/latest/download/latest.json`。
5. 下次修改源码后打tag，例如v1.0.1。`.github/workflows/windows-release.yml` 会构建并上传新的ZIP与latest.json。该工作流只有在你上传源码并触发后才执行，目前没有操作你的GitHub仓库。

更新包ZIP根目录必须直接包含轻摘.exe、version.json、app/server.cjs、runtime/node.exe，不要再外套一层目录。版本信息、清单版本与发布tag应一致。SHA-256和size必须针对最终上传的ZIP计算，不要修改ZIP后继续使用旧清单。

## 独立更新器
下载 → 大小/SHA-256 → 路径与包内版本检查 → 关闭目标目录主进程 → 替换（保留备份）→ 失败回滚或成功询问重启。更新器启动时复制自身到临时目录，避免更换自身EXE时被占用；运行时不自动提权。可写目录部署，不建议放Program Files。

## 测试记录
已验证本机带会话网页采集、匿名401与桌面会话成功、桥接未授权403；更新器测试覆盖正常下载替换、错误哈希、路径穿越、截断下载、用户数据保留和替换失败回滚。真实GitHub在线升级需发布实际仓库后再验证。当前没有代码签名。
