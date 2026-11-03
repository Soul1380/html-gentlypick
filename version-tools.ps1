function Get-ProjectVersion {
 param([string]$Root, [string]$Requested = '')
 $file = Join-Path $Root 'version.json'
 if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw '源码主目录缺少 version.json。' }
 try { $config = Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json }
 catch { throw 'version.json 格式无效，请检查 JSON 内容。' }
 if ($null -eq $config -or $config.version -isnot [string]) { throw 'version.json 的 version 必须是三段版本号字符串。' }
 $value = $config.version
 if ($value -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') { throw 'version.json 的 version 必须是三段数字，例如 1.0.1。' }
 foreach ($part in $value.Split('.')) { $number = 0; if (-not [int]::TryParse($part,[ref]$number) -or $number -gt 65534) { throw '版本号每一段必须在 0 到 65534 之间。' } }
 if ($Requested) {
  $requestedValue = if ($Requested.StartsWith('v')) { $Requested.Substring(1) } else { $Requested }
  if ($requestedValue -cne $value) { throw "指定版本 $Requested 与 version.json 中的 $value 不一致。请先修改 version.json。" }
 }
 return $value
}
