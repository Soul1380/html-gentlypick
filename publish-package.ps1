param([Parameter(Mandatory=$true)][string]$InstallDirectory,[string]$Version='1.0.0',[string]$Repository='OWNER/REPOSITORY',[string]$ReleaseNotes='')
$ErrorActionPreference='Stop'
$Version=$Version.TrimStart('v')
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw '版本号无效'}
$root=(Resolve-Path -LiteralPath $InstallDirectory).Path
$outDir=Split-Path $root -Parent
$zipName="LightExcerpt-v$Version-win-x64.zip"
$zip=Join-Path $outDir $zipName
$allow=@('轻摘.exe','轻摘更新.exe','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','version.json','README.md','THIRD-PARTY-NOTICES.md','启动轻摘.cmd','导入旧资料.cmd','导入旧资料.ps1','app','runtime')
$files=@();foreach($name in $allow){$candidate=Join-Path $root $name;if(Test-Path -LiteralPath $candidate){$files+=$candidate}}
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
$manifest=@{version=$Version;packageUrl="https://github.com/$Repository/releases/download/v$Version/$zipName";sha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant();size=(Get-Item -LiteralPath $zip).Length;releaseNotes=$ReleaseNotes}
[IO.File]::WriteAllText((Join-Path $outDir 'latest.json'),($manifest|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
Write-Host "发布文件：$zip"
Write-Host '另需将latest.json放进同一GitHub Release；发布前把OWNER/REPOSITORY改为实际仓库。'
