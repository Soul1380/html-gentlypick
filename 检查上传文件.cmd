@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-release.ps1" -CheckOnly
set "result=%errorlevel%"
echo.
pause
exit /b %result%