param([string]$Version='',[string]$ToolsDirectory='')
$ErrorActionPreference='Stop'
$sourceRoot=$PSScriptRoot
. (Join-Path $sourceRoot 'version-tools.ps1')
$Version=Get-ProjectVersion -Root $sourceRoot -Requested $Version
function Download-VerifiedTLS([string]$Url,[string]$Destination){
 & node (Join-Path $sourceRoot 'app/update-download.cjs') --url $Url --out $Destination --limit 2147483648 | Out-Null
 if($LASTEXITCODE -ne 0){throw "下载失败：$Url"}
}
$deps=Get-Content -LiteralPath (Join-Path $sourceRoot 'dependencies.json') -Raw|ConvertFrom-Json
$workRoot=Join-Path $sourceRoot '.build'
$distRoot=Join-Path $sourceRoot 'dist'
$outDir=Join-Path $distRoot ('轻摘-v'+$Version)
if(Test-Path -LiteralPath (Join-Path $outDir 'version.json')){throw '目标目录已存在完整构建，请先改名保留旧构建或使用新版本号'}
New-Item -ItemType Directory -Force -Path $workRoot,$outDir,(Join-Path $outDir 'runtime')|Out-Null
$wvZip=Join-Path $workRoot 'webview2.zip'
$wvVersion=$deps.webview2.version
if(!(Test-Path -LiteralPath $wvZip)){Download-VerifiedTLS "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/$wvVersion/microsoft.web.webview2.$wvVersion.nupkg" $wvZip}
if((Get-FileHash -LiteralPath $wvZip -Algorithm SHA256).Hash -ne $deps.webview2.sha256){throw 'WebView2 SDK校验失败'}
$wvDir=Join-Path $workRoot 'webview2'
Expand-Archive -LiteralPath $wvZip -DestinationPath $wvDir -Force
$nodeVersion=$deps.node.version
$nodeZipName="node-v$nodeVersion-win-x64.zip"
$nodeZip=Join-Path $workRoot $nodeZipName
if(!(Test-Path -LiteralPath $nodeZip)){Download-VerifiedTLS "https://nodejs.org/dist/v$nodeVersion/$nodeZipName" $nodeZip}
$sumsFile=Join-Path $workRoot 'SHASUMS256.txt'
if(!(Test-Path -LiteralPath $sumsFile)){Download-VerifiedTLS "https://nodejs.org/dist/v$nodeVersion/SHASUMS256.txt" $sumsFile}
$sums=Get-Content -LiteralPath $sumsFile -Raw
$match=[regex]::Match($sums,('(?m)^([a-f0-9]{64})\s+'+[regex]::Escape($nodeZipName)+'\s*$'))
if(!$match.Success -or (Get-FileHash -LiteralPath $nodeZip -Algorithm SHA256).Hash -ne $match.Groups[1].Value){throw 'Node运行时SHA-256校验失败'}
Expand-Archive -LiteralPath $nodeZip -DestinationPath $workRoot -Force
$nodeDir=Join-Path $workRoot "node-v$nodeVersion-win-x64"
Copy-Item -LiteralPath (Join-Path $nodeDir 'node.exe') -Destination (Join-Path $outDir 'runtime/node.exe')
Copy-Item -LiteralPath (Join-Path $nodeDir 'LICENSE') -Destination (Join-Path $outDir 'runtime/NODE-LICENSE.txt')
$packageJson=@{private=$true;dependencies=@{playwright=$deps.playwright.version}}|ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $outDir 'runtime/package.json'),$packageJson,[Text.UTF8Encoding]::new($false))
& (Join-Path $nodeDir 'npm.cmd') install --prefix (Join-Path $outDir 'runtime') --ignore-scripts --omit=dev --no-audit --no-fund --cache (Join-Path $workRoot 'npm-cache')
if($LASTEXITCODE -ne 0){throw 'Playwright安装失败'}
Copy-Item -LiteralPath (Join-Path $sourceRoot 'app') -Destination $outDir -Recurse
Get-ChildItem -LiteralPath (Join-Path $outDir 'app') -File | Where-Object {$_.Extension -in @('.cjs','.js','.html')} | ForEach-Object {
 $content=[IO.File]::ReadAllText($_.FullName).Replace('1.0.0',$Version)
 [IO.File]::WriteAllText($_.FullName,$content,[Text.UTF8Encoding]::new($false))
}
$toolsOut=Join-Path $outDir 'app/tools'
if($ToolsDirectory){
 foreach($name in @('yt-dlp.exe','ffmpeg.exe','ffprobe.exe')){Copy-Item -LiteralPath (Join-Path $ToolsDirectory $name) -Destination $toolsOut -Force}
}else{
 $yt=Join-Path $workRoot 'yt-dlp.exe'
 if(!(Test-Path -LiteralPath $yt)){Download-VerifiedTLS $deps.videoTools.ytDlpUrl $yt}
 if((Get-FileHash -LiteralPath $yt -Algorithm SHA256).Hash -ne $deps.videoTools.ytDlpSha256){throw 'yt-dlp SHA-256 校验失败。'}
 $ffzip=Join-Path $workRoot 'ffmpeg-win64.zip'
 if(!(Test-Path -LiteralPath $ffzip)){Download-VerifiedTLS $deps.videoTools.ffmpegUrl $ffzip}
 if((Get-FileHash -LiteralPath $ffzip -Algorithm SHA256).Hash -ne $deps.videoTools.ffmpegZipSha256){throw 'FFmpeg 压缩包 SHA-256 校验失败。'}
 $ffdir=Join-Path $workRoot 'ffmpeg-win64'
 Expand-Archive -LiteralPath $ffzip -DestinationPath $ffdir -Force
 foreach($name in @('ffmpeg.exe','ffprobe.exe')){
  $matches=@(Get-ChildItem -LiteralPath $ffdir -Recurse -File -Filter $name)
  if($matches.Count -ne 1){throw "FFmpeg 包中无法唯一找到 $name"}
  Copy-Item -LiteralPath $matches[0].FullName -Destination $toolsOut -Force
 }
 Copy-Item -LiteralPath $yt -Destination $toolsOut -Force
}
foreach($item in @(@('yt-dlp.exe',$deps.videoTools.ytDlpSha256),@('ffmpeg.exe',$deps.videoTools.ffmpegSha256),@('ffprobe.exe',$deps.videoTools.ffprobeSha256))){
 if((Get-FileHash -LiteralPath (Join-Path $toolsOut $item[0]) -Algorithm SHA256).Hash -ne $item[1]){throw "视频工具校验失败：$($item[0])"}
}
$package=Get-Content (Join-Path $outDir 'app/package.json') -Raw|ConvertFrom-Json
$package | Add-Member -NotePropertyName version -NotePropertyValue $Version -Force
[IO.File]::WriteAllText((Join-Path $outDir 'app/package.json'),($package|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
$wvLib=Join-Path $wvDir 'lib/net462'
foreach($name in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll')){Copy-Item -LiteralPath (Join-Path $wvLib $name) -Destination $outDir}
Copy-Item -LiteralPath (Join-Path $wvDir 'runtimes/win-x64/native/WebView2Loader.dll') -Destination $outDir
$csc=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
foreach($type in @('Desktop','Updater')){
 $cs=(Get-Content -LiteralPath (Join-Path $sourceRoot "desktop/$type.cs") -Raw).Replace('1.0.0',$Version)
 $csFile=Join-Path $workRoot ($type+'.cs')
 [IO.File]::WriteAllText($csFile,$cs,[Text.UTF8Encoding]::new($true))
 $exe=if($type -eq 'Desktop'){'轻摘.exe'}else{'Updater.exe'}
 $refs=@('System.Windows.Forms.dll','System.Drawing.dll','System.Net.Http.dll','System.Web.Extensions.dll')
 if($type -eq 'Desktop'){$refs+=@((Join-Path $wvLib 'Microsoft.Web.WebView2.Core.dll'),(Join-Path $wvLib 'Microsoft.Web.WebView2.WinForms.dll'))}else{$refs+=@('System.IO.Compression.dll','System.IO.Compression.FileSystem.dll')}
 $argsList=@('/nologo','/codepage:65001','/target:winexe','/platform:x64','/optimize+',('/out:"'+(Join-Path $outDir $exe)+'"'))
 foreach($r in $refs){$argsList+=('/reference:"'+$r+'"')}
 $argsList+=('/win32manifest:"'+(Join-Path $sourceRoot 'desktop/app.manifest')+'"')
 $argsList+=('"'+$csFile+'"')
 $extra=if($type -eq 'Desktop'){@('ReleaseFeed.cs','ReleaseClient.cs','UpdateGate.cs')}else{@('ReleaseFeed.cs','ReleaseClient.cs','PackageValidator.cs','PackageDownload.cs','UpdateLog.cs','UpdateGate.cs','ProcessIdentity.cs','ProcessTree.cs','FileTransaction.cs','UpdateCoordinator.cs')}
 foreach($f in $extra){$argsList+=('"'+(Join-Path $sourceRoot ('desktop/updater/'+$f))+'"')}
 $rsp=Join-Path $workRoot ($type+'.rsp')
 [IO.File]::WriteAllLines($rsp,$argsList,[Text.UTF8Encoding]::new($true))
 & $csc ('@'+$rsp)
 if($LASTEXITCODE -ne 0){throw "编译$type失败"}
}
Copy-Item -LiteralPath (Join-Path $outDir 'Updater.exe') -Destination (Join-Path $outDir '轻摘更新.exe')
$versionInfo=@{name='轻摘';version=$Version;platform='win-x64';builtAt=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $outDir 'version.json'),$versionInfo,[Text.UTF8Encoding]::new($false))
foreach($f in @('README.md','THIRD-PARTY-NOTICES.md','导入旧资料.ps1','导入旧资料.cmd','启动轻摘.cmd')){Copy-Item -LiteralPath (Join-Path $sourceRoot "distribution/$f") -Destination $outDir}
Write-Host "构建成功：$outDir"
& (Join-Path $sourceRoot 'publish-package.ps1') -InstallDirectory $outDir -Version $Version
