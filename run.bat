@echo off
chcp 65001 >nul
title WordFormatter Launcher

:: Fix: ensure working directory is the script's location
cd /d "%~dp0"
set PYTHONPATH=%~dp0;%PYTHONPATH%

echo ========================================
echo   WordFormatter - 一键启动
echo ========================================
echo.
echo   工作目录: %~dp0
echo.

:: 启动后端（新窗口）
echo [1/2] 启动后端服务器 (FastAPI)...
start "WordFormatter Backend" cmd /k "cd /d %~dp0 && set PYTHONPATH=%~dp0;%%PYTHONPATH%% && echo 正在启动后端... && python -m uvicorn backend.server:app --host 127.0.0.1 --port 8765 --reload"

:: 等待后端启动
echo [2/2] 等待 5 秒后启动前端 (WinUI 3)...
timeout /t 5 /nobreak >nul

:: 启动前端（新窗口）
start "WordFormatter Frontend" cmd /k "cd /d %~dp0frontend && echo 正在编译并启动前端... && dotnet run"

echo.
echo 两个窗口已启动：
echo   - 后端: http://127.0.0.1:8765  (修改 .py 文件自动重启)
echo   - 前端: WinUI 3 应用窗口        (修改 .cs/.xaml 后重启生效)
echo.
echo 关闭两个窗口即可停止程序。
echo ========================================
pause