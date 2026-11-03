@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
call "%~dp0一键发布GitHub.cmd"
if errorlevel 1 goto failed
git -c safe.directory=* rev-parse --verify refs/tags/v1.0.1 >nul 2>&1
if errorlevel 1 (
    git -c safe.directory=* tag v1.0.1
    if errorlevel 1 goto failed
)
for /f %%T in ('git -c safe.directory^=* rev-list -n 1 v1.0.1') do set "tagCommit=%%T"
for /f %%H in ('git -c safe.directory^=* rev-parse HEAD') do set "headCommit=%%H"
if not defined tagCommit goto failed
if not defined headCommit goto failed
if /i not "%tagCommit%"=="%headCommit%" (
    echo v1.0.1 标签已指向其他提交，停止发布，请检查标签。
    goto failed
)
git -c safe.directory=* push origin refs/tags/v1.0.1
if errorlevel 1 goto failed
echo.
echo 版本标签已推送，请在 GitHub Actions 查看构建结果。
echo 如果标签早已存在，需要在 Actions 手动运行构建。
pause
exit /b 0
:failed
echo.
echo 发布未完成，请查看上面的提示。
pause
exit /b 1
