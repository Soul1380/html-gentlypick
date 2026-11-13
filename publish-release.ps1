param([string]$RepositoryUrl='https://github.com/Soul1380/html-gentlypick.git',[switch]$NonInteractive)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$source=$PSScriptRoot
. (Join-Path $source 'version-tools.ps1')
$version=Get-ProjectVersion -Root $source
foreach($required in @('app/server.cjs','desktop/Desktop.cs','build-desktop.ps1','.github/workflows/windows-release.yml')){if(!(Test-Path -LiteralPath (Join-Path $source $required))){throw "源码不完整：$required"}}
if(!(Get-Command git -ErrorAction SilentlyContinue)){throw '请先安装 Git for Windows，再双击发布程序。'}
function Run-Git([string[]]$Arguments){
 & git @gitNetwork @Arguments
 if($LASTEXITCODE -ne 0){throw "Git 操作失败（$LASTEXITCODE）。没有继续执行后续发布步骤。"}
}
$stage=Join-Path ([IO.Path]::GetTempPath()) ('gentlypick-release-'+[Guid]::NewGuid().ToString('N'))
Write-Host "准备上传轻摘 v$version"
Write-Host "源码位置：$source"

try{
 $gitNetwork=@('-c','http.proxy=')
 if(-not $NonInteractive){
  $proxy=Read-Host '代理 HTTP/Mixed 端口（不使用代理直接回车）'
  if(-not [string]::IsNullOrWhiteSpace($proxy)){
   $port=0
   if(-not [int]::TryParse($proxy.Trim(),[ref]$port) -or $port -lt 1 -or $port -gt 65535){throw '端口必须是 1 至 65535 的数字，请重新运行发布程序。'}
   $gitNetwork=@('-c',"http.proxy=http://127.0.0.1:$port")
  }
 }
 Write-Host '正在连接 GitHub 并上传…'
 Run-Git @('clone','--branch','main','--single-branch','--',$RepositoryUrl,$stage)
 $remoteVersion=Get-ProjectVersion -Root $stage
 if([version]$remoteVersion -gt [version]$version){throw "GitHub 已有更高源码版本 $remoteVersion。为避免覆盖，请先同步最新版源码。"}
 $tag=& git -C $stage tag --list "v$version"
 if($LASTEXITCODE -ne 0){throw '无法检查已发布版本。'}
 if($tag){throw "v$version 已有发布标签，请修改主目录 version.json 后再发布，不覆盖已有 Release。"}
 $rootFiles=@('version.json','version-tools.ps1','dependencies.json','README.md','.gitignore','build-desktop.ps1','build-installer.ps1','publish-package.ps1','publish-github.ps1','publish-release.ps1','publish-release.cmd','publish-v1.0.7.cmd')
 $files=@($rootFiles | ForEach-Object {Get-Item -LiteralPath (Join-Path $source $_) -ErrorAction Stop})
 foreach($folder in @('app','desktop','packaging','distribution','docs','tests','.github')){
  $dir=Join-Path $source $folder
  if(!(Test-Path -LiteralPath $dir)){continue}
  $files+=Get-ChildItem -LiteralPath $dir -Recurse -File | Where-Object {
   $_.FullName -notmatch '[\\/](UserData|node_modules|tools|media|videos|\.git|\.build|dist)[\\/]' -and
   $_.Name -notin @('data.json','adapter-service-data.json','feed-request.json') -and
   ($_.Extension -in @('.js','.cjs','.css','.html','.cs','.manifest','.ps1','.cmd','.md','.yml','.yaml') -or $_.Name -eq 'package.json' -or ($_.Extension -eq '.png' -and $_.Directory.Name -eq 'report-samples'))
  }
 }
 foreach($file in $files){
  if(($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw '源码中含有链接文件，请检查后再发布。'}
  $relative=$file.FullName.Substring($source.TrimEnd('\').Length+1)
  $target=Join-Path $stage $relative
  New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
  Copy-Item -LiteralPath $file.FullName -Destination $target -Force
 }
 Run-Git @('-C',$stage,'add','--all')
 & git -C $stage diff --cached --quiet
 $changed=$LASTEXITCODE
 if($changed -eq 0){Write-Host '源码已上传，无需重复提交。请查看 GitHub Actions / Releases。';exit 0}
 if($changed -ne 1){throw '无法检查本次提交。'}
 Run-Git @('-C',$stage,'-c','user.name=Soul1380','-c','user.email=142634717+Soul1380@users.noreply.github.com','commit','-m',"Release v$version")
 Run-Git @('-C',$stage,'push','origin','HEAD:main')
 Write-Host "源码上传成功：v$version" -ForegroundColor Green
 Write-Host 'GitHub 将自动构建并发布安装程序、更新包和校验文件。上传成功不代表构建已经完成。'
 Write-Host '构建进度：https://github.com/Soul1380/html-gentlypick/actions'
 Write-Host "下载页面：https://github.com/Soul1380/html-gentlypick/releases/tag/v$version"
}catch{
 Write-Host $_.Exception.Message -ForegroundColor Red
 Write-Host "本次临时工作目录：$stage"
 exit 1
}
