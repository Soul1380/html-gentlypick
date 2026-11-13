@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-release.ps1"
set "result=%errorlevel%"
echo.
if not "%result%"=="0" echo Publish failed. Read the message above.
pause
exit /b %result%
