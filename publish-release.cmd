@echo off
chcp 65001 >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-release.ps1"
set "result=%errorlevel%"
echo.
if not "%result%"=="0" echo Publish failed. See the message above.
pause
exit /b %result%
