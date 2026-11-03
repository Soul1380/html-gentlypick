param([Parameter(Mandatory=$true)][string]$InstallDirectory,[string]$Version='',[string]$Repository='Soul1380/html-gentlypick',[string]$ReleaseNotes='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'version-tools.ps1')
$Version=Get-ProjectVersion -Root $PSScriptRoot -Requested $Version
$root=(Resolve-Path -LiteralPath $InstallDirectory).Path
$versionFile=Join-Path $root 'version.json'
$embedded=Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8|ConvertFrom-Json
if($embedded.version -ne $Version){throw '软件目录版本与目标版本不一致。'}
$out=Split-Path $root -Parent
$zip=Join-Path $out 'LightExcerpt-win-x64-update.zip'
$installer=Join-Path $out ("GentlyPick-v$Version-win-x64.zip")
if((Test-Path -LiteralPath $zip) -or (Test-Path -LiteralPath $installer)){throw '输出包已存在，请先移走旧包。'}
$names=@('轻摘.exe','Updater.exe','轻摘更新.exe','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','version.json','README.md','THIRD-PARTY-NOTICES.md','启动轻摘.cmd','导入旧资料.cmd','导入旧资料.ps1','app','runtime')
$files=@();foreach($name in $names){$path=Join-Path $root $name;if(-not (Test-Path -LiteralPath $path)){throw "缺少发布文件：$name"};$files+=$path}
Compress-Archive -LiteralPath $files -DestinationPath $zip
Copy-Item -LiteralPath $zip -Destination $installer
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $out 'SHA256.txt'),"$hash  LightExcerpt-win-x64-update.zip`r`n",[Text.UTF8Encoding]::new($false))
Write-Host "更新包：$zip"
Write-Host "安装包：$installer"
Write-Host "SHA-256：$hash"
Write-Host '将更新包上传至正式 GitHub Release；应用读取 GitHub 提供的附件摘要。'
