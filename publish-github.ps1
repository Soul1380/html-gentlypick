param([string]$RepositoryUrl = 'https://github.com/Soul1380/html-gentlypick.git')
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$env:GIT_TERMINAL_PROMPT = '1'
$proxyPort = Read-Host '代理 HTTP/Mixed 端口（不使用代理直接回车）'
if ($proxyPort) {
    $parsed = 0
    if (-not [int]::TryParse($proxyPort, [ref]$parsed) -or $parsed -lt 1 -or $parsed -gt 65535) { throw '代理端口必须是 1 到 65535 之间的数字。' }
    $env:HTTP_PROXY = "http://127.0.0.1:$parsed"
    $env:HTTPS_PROXY = "http://127.0.0.1:$parsed"
}
$source = $PSScriptRoot
$defaultSource = 'C:\Users\a1801\Documents\ChatGPT\Web scraping and analysis\outputs\轻摘-v1.0.0-源码'
if (-not (Test-Path -LiteralPath (Join-Path $source '.git')) -and (Test-Path -LiteralPath (Join-Path $defaultSource '.git'))) {
    $source = $defaultSource
    Write-Host "找到轻摘源码目录：$source"
}
$git = Get-Command git -ErrorAction SilentlyContinue
if (-not $git) { throw '找不到 Git。请安装 Git for Windows 后重试。' }
function Run-Git([string[]]$GitArgs) {
    & $git.Source @GitArgs
    if ($LASTEXITCODE -ne 0) { throw "Git 操作失败（退出码 $LASTEXITCODE）。请查看上方提示。" }
}
$gitDir = Join-Path $source '.git'
if (-not (Test-Path -LiteralPath $gitDir)) { throw '请把此发布程序放在轻摘源码根目录后运行。' }
$tmp = Join-Path ([IO.Path]::GetTempPath()) ('gentlypick-publish-' + [guid]::NewGuid().ToString('N'))
$base = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
if (-not [IO.Path]::GetFullPath($tmp).StartsWith($base, [StringComparison]::OrdinalIgnoreCase)) { throw '临时目录路径错误。' }
try {
    Write-Host '正在读取 GitHub 仓库；需要时请在弹出的窗口登录。'
    Run-Git @('clone','--branch','main','--single-branch','--',$RepositoryUrl,$tmp)
    $tracked = & $git.Source -c 'safe.directory=*' -C $source -c core.quotepath=false ls-files
    if ($LASTEXITCODE -ne 0) { throw '无法读取源码文件列表。' }
    $tracked = @($tracked) + @('publish-github.ps1','一键发布GitHub.cmd') | Select-Object -Unique
    foreach ($relative in $tracked) {
        if ([string]::IsNullOrWhiteSpace($relative)) { continue }
        $relative = $relative.Trim('"')
        $src = [IO.Path]::GetFullPath((Join-Path $source $relative))
        $dst = [IO.Path]::GetFullPath((Join-Path $tmp $relative))
        if (-not $src.StartsWith($source.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or -not $dst.StartsWith($tmp.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)) { throw '检测到不安全的项目路径。' }
        if (-not (Test-Path -LiteralPath $src -PathType Leaf)) { throw "源码文件不存在：$relative" }
        New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null
        Copy-Item -LiteralPath $src -Destination $dst -Force
    }
    Run-Git @('-C',$tmp,'add','--all')
    & $git.Source -C $tmp diff --cached --quiet
    if ($LASTEXITCODE -eq 0) { Write-Host 'GitHub 仓库已经是最新内容。'; return }
    if ($LASTEXITCODE -ne 1) { throw '无法检查待发布的文件。' }
    Run-Git @('-C',$tmp,'-c','user.name=Soul1380','-c','user.email=142634717+Soul1380@users.noreply.github.com','commit','-m','Update GentlyPick source')
    Run-Git @('-C',$tmp,'push','origin','HEAD:main')
    Write-Host '源码已上传到 https://github.com/Soul1380/html-gentlypick'
    Write-Host '此操作只发布源码，不创建 Release 或上传安装包。'
} finally {
    if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Recurse -Force }
}
