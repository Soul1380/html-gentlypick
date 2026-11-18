param([string]$RepositoryUrl='https://github.com/Soul1380/html-gentlypick.git',[switch]$NonInteractive,[switch]$CheckOnly)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
$source=$PSScriptRoot
try{
 $requiredFiles=@('version-tools.ps1','version.json','dependencies.json','app/server.cjs','app/task-control.cjs','app/page-read-policy.cjs','app/report-clear.cjs','app/workspace-actions.js','app/ui-selection.js','app/device-profile.cjs','app/report-writing.cjs','app/report-layout.cjs','app/report-data.cjs','app/report-xlsx.cjs','app/report-narrative.cjs','app/ui-components.js','app/ui-components.css','app/ui-theme.css','app/embedded-ui.css','app/ui-scrollbars.js','app/ui-scrollbars.css','app/ui-workflows.js','app/tutorial.html','app/tutorial.css','app/tutorial.js','app/tutorial-assets/workspace.png','app/tutorial-assets/task.png','app/tutorial-assets/projects.png','app/tutorial-assets/models.png','app/tutorial-assets/video.png','desktop/Desktop.cs','desktop/SharedControls.cs','build-desktop.ps1','build-installer.ps1','publish-package.ps1','.github/workflows/windows-release.yml','AGENTS.md')
 $missing=@($requiredFiles|Where-Object {-not(Test-Path -LiteralPath (Join-Path $source $_) -PathType Leaf)})
 if($missing.Count){throw ('当前不是完整上传目录，缺少：'+($missing -join '、')+'。请解压并保留整个 GitHub 上传文件夹，不要只复制发布脚本，也不要放到安装程序目录运行。')}
 . (Join-Path $source 'version-tools.ps1')
 $version=Get-ProjectVersion -Root $source
 if($CheckOnly){Write-Host ('检查通过：轻摘 v'+$version+'，上传依赖完整；没有连接 GitHub，也没有上传。') -ForegroundColor Green;return}
 if(!(Get-Command git -ErrorAction SilentlyContinue)){throw '请先安装 Git for Windows，再双击发布程序。'}
}catch{Write-Host $_.Exception.Message -ForegroundColor Red;exit 1}
function Run-Git([string[]]$Arguments){
 & git @gitNetwork @Arguments
 if($LASTEXITCODE -ne 0){throw "Git 操作失败（$LASTEXITCODE）。没有继续执行后续发布步骤。"}
}
$stage=Join-Path (Join-Path $source '.publish-work') ('gentlypick-release-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path (Split-Path $stage -Parent) | Out-Null
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
 $rootFiles=@('检查上传文件.cmd','AGENTS.md','version.json','version-tools.ps1','dependencies.json','README.md','.gitignore','build-desktop.ps1','build-installer.ps1','publish-package.ps1','publish-github.ps1','publish-release.ps1','publish-release.cmd','publish-v1.0.7.cmd')
 $files=@($rootFiles | ForEach-Object {Get-Item -LiteralPath (Join-Path $source $_) -ErrorAction Stop})
 foreach($folder in @('app','desktop','packaging','distribution','docs','tests','.github')){
  $dir=Join-Path $source $folder
  if(!(Test-Path -LiteralPath $dir)){continue}
  $files+=Get-ChildItem -LiteralPath $dir -Recurse -File -Force | Where-Object {
   $_.FullName -notmatch '[\\/](UserData|node_modules|media|videos|\.git|\.build|dist)[\\/]' -and
   $_.Name -notin @('data.json','adapter-service-data.json','feed-request.json') -and
   ($_.Extension -in @('.js','.cjs','.css','.html','.cs','.manifest','.ps1','.cmd','.md','.yml','.yaml','.py','.ico','.txt') -or $_.Name -eq 'package.json' -or ($_.Extension -eq '.png' -and $_.Directory.Name -in @('report-samples','video-analysis','tutorial-assets')))
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
 if($changed -notin @(0,1)){throw '无法检查本次提交。'}
 $commitArgs=@('-C',$stage,'-c','user.name=Soul1380','-c','user.email=142634717+Soul1380@users.noreply.github.com','commit')
 if($changed -eq 0){$commitArgs+= '--allow-empty';Write-Host '源码没有变化，将重新启动此版本的构建。'}
 $commitArgs+= @('-m',"Release v$version")
 Run-Git $commitArgs
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
