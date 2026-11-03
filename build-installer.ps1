param([Parameter(Mandatory=$true)][string]$Package,[string]$OutputDirectory=(Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'version-tools.ps1')
$version=Get-ProjectVersion -Root $PSScriptRoot
$packagePath=(Resolve-Path -LiteralPath $Package).Path
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$temp=Join-Path $env:TEMP ('gentlypick-installer-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
 $uninstall=Join-Path $temp 'Uninstall.exe'
 & $compiler /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll "/out:$uninstall" (Join-Path $PSScriptRoot 'packaging\Uninstall.cs')
 if($LASTEXITCODE -ne 0){throw '卸载程序编译失败'}
 $assembly=Join-Path $temp 'version.cs'
 [IO.File]::WriteAllText($assembly,"[assembly:System.Reflection.AssemblyVersion(`"$version.0`")]",[Text.UTF8Encoding]::new($true))
 $setup=Join-Path $OutputDirectory "GentlyPickSetup-v$version.exe"
 & $compiler /nologo /codepage:65001 /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:Microsoft.CSharp.dll "/resource:$packagePath,payload.zip" "/resource:$uninstall,Uninstall.exe" "/out:$setup" (Join-Path $PSScriptRoot 'packaging\Installer.cs') $assembly
 if($LASTEXITCODE -ne 0){throw '安装程序编译失败'}
 Write-Host "安装程序：$setup"
} finally { Remove-Item -LiteralPath $temp -Recurse -Force }
