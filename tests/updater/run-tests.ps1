$ErrorActionPreference='Stop'
$src=(Resolve-Path (Join-Path $PSScriptRoot '../../desktop/updater')).Path
$csc=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$tmp=Join-Path ([IO.Path]::GetTempPath()) ('gentlypick-tests-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null
function Compile([string]$name,[string[]]$files,[string[]]$refs,[string]$target='exe') {
 $args=@('/nologo','/codepage:65001',('/target:'+$target),'/platform:x64',('/out:'+(Join-Path $tmp ($name+'.exe'))))
 foreach($r in $refs){$args+=('/reference:'+$r)}
 foreach($file in $files){$p=Join-Path $src $file;if(-not (Test-Path -LiteralPath $p)){$p=Join-Path $PSScriptRoot $file};$args+=$p}
 & $csc @args
 if($LASTEXITCODE -ne 0){throw "编译测试失败：$name"}
}
function TestExe([string]$name,[string[]]$testArgs=@()) {& (Join-Path $tmp ($name+'.exe')) @testArgs;if($LASTEXITCODE -ne 0){throw "测试失败：$name"}}
$refs=@('System.Web.Extensions.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll')
try {
 Compile 'ReleaseFeedTests' @('ReleaseFeed.cs','ReleaseFeedTests.cs') @('System.Web.Extensions.dll');TestExe 'ReleaseFeedTests'
 Compile 'PackageValidatorTests' @('ReleaseFeed.cs','PackageValidator.cs','PackageValidatorTests.cs') $refs;TestExe 'PackageValidatorTests'
 Compile 'PackageDownloadTests' @('ReleaseFeed.cs','PackageValidator.cs','PackageDownload.cs','PackageDownloadTests.cs') $refs;TestExe 'PackageDownloadTests'
 Compile 'UpdateLogTests' @('UpdateLog.cs','UpdateLogTests.cs') @();TestExe 'UpdateLogTests'
 Compile 'UpdateGateTests' @('UpdateGate.cs','UpdateGateTests.cs') @();TestExe 'UpdateGateTests'
 Compile 'FileTransactionTests' @('ReleaseFeed.cs','PackageValidator.cs','FileTransaction.cs','FileTransactionTests.cs') $refs;TestExe 'FileTransactionTests'
 Compile 'ProcessIdentityTests' @('ProcessIdentity.cs','ProcessIdentityTests.cs') @();TestExe 'ProcessIdentityTests'
 Compile 'ProcessTreeTests' @('ProcessIdentity.cs','ProcessTree.cs','ProcessTreeTests.cs') @();TestExe 'ProcessTreeTests'
 Compile 'FixtureApp' @('FixtureApp.cs','UpdateGate.cs') @('System.Windows.Forms.dll','System.Drawing.dll') 'winexe'
 Compile 'ShutdownAndGateTests' @('UpdateGate.cs','ProcessIdentity.cs','ShutdownAndGateTests.cs') @();TestExe 'ShutdownAndGateTests'
 $all=@('ReleaseFeed.cs','PackageValidator.cs','PackageDownload.cs','UpdateLog.cs','UpdateGate.cs','ProcessIdentity.cs','ProcessTree.cs','FileTransaction.cs','UpdateCoordinator.cs','EndToEndTests.cs')
 Compile 'EndToEndTests' $all ($refs+@('System.Net.Http.dll'));TestExe 'EndToEndTests';TestExe 'EndToEndTests' @('--restart-failure');TestExe 'EndToEndTests' @('--startup-failure')
 Write-Host '更新器组件及隔离程序测试完成。'
} finally {
 $prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if([IO.Path]::GetFullPath($tmp).StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $tmp)){Remove-Item -LiteralPath $tmp -Recurse -Force}
}
