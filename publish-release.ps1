$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
Set-Location -LiteralPath $PSScriptRoot
. (Join-Path $PSScriptRoot 'version-tools.ps1')
$version=Get-ProjectVersion -Root $PSScriptRoot
$tag='v'+$version
$git=Get-Command git -ErrorAction SilentlyContinue
if(-not $git){throw '找不到 Git，请先安装 Git for Windows。'}
if(-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot '.git'))){throw '请在轻摘源码目录运行发布程序。'}
function Run-Git([string[]]$GitArgs){
 & $git.Source -c 'safe.directory=*' @GitArgs
 if($LASTEXITCODE -ne 0){throw "Git 操作失败，退出码：$LASTEXITCODE。请查看上方提示。"}
}
Write-Host "准备发布版本 $version。"
$proxyPort=Read-Host '代理 HTTP/Mixed 端口（不使用代理直接回车）'
if($proxyPort){
 $port=0
 if(-not [int]::TryParse($proxyPort,[ref]$port) -or $port -lt 1 -or $port -gt 65535){throw '代理端口必须为 1 至 65535 的数字。'}
 $env:HTTP_PROXY="http://127.0.0.1:$port"
 $env:HTTPS_PROXY="http://127.0.0.1:$port"
}
Run-Git @('add','.')
& $git.Source -c 'safe.directory=*' diff --cached --quiet
$diffCode=$LASTEXITCODE
if($diffCode -eq 1){Run-Git @('commit','-m','更新')}
elseif($diffCode -ne 0){throw "检查待提交文件失败，退出码：$diffCode。"}
Run-Git @('pull','--no-rebase','--no-edit','origin','main')
if((Get-ProjectVersion -Root $PSScriptRoot) -cne $version){throw '拉取后 version.json 已变化，停止发布，请检查文件。'}
Run-Git @('push','origin','HEAD:main')
& $git.Source -c 'safe.directory=*' rev-parse --verify --quiet ('refs/tags/'+$tag) | Out-Null
$tagCode=$LASTEXITCODE
if($tagCode -eq 1){Run-Git @('tag',$tag)}
elseif($tagCode -ne 0){throw "检查版本标签失败，退出码：$tagCode。"}
$tagCommit=& $git.Source -c 'safe.directory=*' rev-list -n 1 $tag
if($LASTEXITCODE -ne 0){throw '读取版本标签失败。'}
$headCommit=& $git.Source -c 'safe.directory=*' rev-parse HEAD
if($LASTEXITCODE -ne 0){throw '读取当前提交失败。'}
if(-not $tagCommit -or -not $headCommit -or $tagCommit -cne $headCommit){throw "$tag 标签已指向其他提交，已停止推送标签。"}
Run-Git @('push','origin',('refs/tags/'+$tag))
Write-Host "源码与 $tag 标签已推送，请到 GitHub Actions 查看构建结果。"
Write-Host '如果标签之前已推送且没有新构建，请到 Actions 手动运行工作流。'
