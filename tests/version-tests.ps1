$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $root 'version-tools.ps1')
$temp=Join-Path ([IO.Path]::GetTempPath()) ('gentlypick-version-test-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
 [IO.File]::WriteAllText((Join-Path $temp 'version.json'),'{"version":"1.0.2"}',[Text.UTF8Encoding]::new($false))
 if((Get-ProjectVersion -Root $temp) -ne '1.0.2'){throw '版本文件未生效。'}
 if((Get-ProjectVersion -Root $temp -Requested 'v1.0.2') -ne '1.0.2'){throw '版本标签解析失败。'}
 $rejected=$false;try{Get-ProjectVersion -Root $temp -Requested 'v1.0.3'}catch{$rejected=$true};if(-not $rejected){throw '不匹配的标签未被拒绝。'}
 foreach($value in @('1.0','v1.0.2','1.02.3','1.2.65535','1.2.-1')){
  [IO.File]::WriteAllText((Join-Path $temp 'version.json'),('{"version":"'+$value+'"}'),[Text.UTF8Encoding]::new($false))
  $rejected=$false;try{Get-ProjectVersion -Root $temp}catch{$rejected=$true};if(-not $rejected){throw "无效版本未被拒绝：$value"}
 }
 Write-Host 'PASS 版本文件、标签一致性和无效版本检查'
} finally {
 $prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
 if([IO.Path]::GetFullPath($temp).StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $temp)){Remove-Item -LiteralPath $temp -Recurse -Force}
}
