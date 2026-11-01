$ErrorActionPreference='Stop'
$installRoot=$PSScriptRoot
$targetData=Join-Path $installRoot 'UserData'
$pending=Join-Path $targetData 'import-next-start.txt'
if(!(Test-Path -LiteralPath $pending)){Write-Host '请先在软件“版本与更新”中选择旧版资料目录。';exit 1}
$taskSource=(Get-Content -LiteralPath $pending -Raw).Trim([char]0xFEFF).Trim()
$sourceResolved=(Resolve-Path -LiteralPath $taskSource).Path
$destResolved=[IO.Path]::GetFullPath($targetData)
if($sourceResolved.TrimEnd('\') -eq $destResolved.TrimEnd('\')){throw '来源目录与目标目录相同，无需导入'}
$exePath=Join-Path $installRoot '轻摘.exe'
foreach($p in Get-Process -Name '轻摘' -ErrorAction SilentlyContinue){if($p.Path -eq $exePath){throw '请先关闭本目录的轻摘软件再导入'}}
$backup=Join-Path $targetData ('import-backup-'+(Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Force -Path $backup | Out-Null
foreach($file in @('data.json','adapter-service-data.json')){if(Test-Path -LiteralPath (Join-Path $targetData $file)){Copy-Item -LiteralPath (Join-Path $targetData $file) -Destination $backup};if(Test-Path -LiteralPath (Join-Path $sourceResolved $file)){Copy-Item -LiteralPath (Join-Path $sourceResolved $file) -Destination (Join-Path $targetData $file) -Force}}
foreach($dir in @('media','videos')){if(Test-Path -LiteralPath (Join-Path $sourceResolved $dir)){New-Item -ItemType Directory -Force -Path (Join-Path $targetData $dir)|Out-Null;Get-ChildItem -LiteralPath (Join-Path $sourceResolved $dir) -File | Copy-Item -Destination (Join-Path $targetData $dir) -Force}}
Remove-Item -LiteralPath $pending
Write-Host '导入完成。可以重新启动轻摘。'
