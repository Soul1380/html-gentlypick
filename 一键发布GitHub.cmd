@echo off
chcp 65001 >nul
cd /d "%~dp0"

git config --global --add safe.directory "%cd:\=/%"
if errorlevel 1 goto failed

set "proxyPort="
set /p "proxyPort=代理 HTTP/Mixed 端口（不使用代理直接回车）："
if defined proxyPort (
    set "HTTP_PROXY=http://127.0.0.1:%proxyPort%"
    set "HTTPS_PROXY=http://127.0.0.1:%proxyPort%"
)

git add .
if errorlevel 1 goto failed

git diff --cached --quiet
if errorlevel 1 (
    git commit -m "更新"
    if errorlevel 1 goto failed
)

git pull --no-rebase --no-edit origin main
if errorlevel 1 goto failed

git push origin master:main
if errorlevel 1 goto failed

echo.
echo 上传成功！请到 GitHub Actions 查看构建结果。
pause
exit /b 0

:failed
echo.
echo 操作失败，请查看上面的报错。未继续执行后续步骤。
pause
exit /b 1