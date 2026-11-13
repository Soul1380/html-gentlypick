param([string]$RepositoryUrl='https://github.com/Soul1380/html-gentlypick.git')
& (Join-Path $PSScriptRoot 'publish-release.ps1') -RepositoryUrl $RepositoryUrl
exit $LASTEXITCODE
