@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "STATE_DIR=%ROOT%\.autosync"
set "PID_FILE=%STATE_DIR%\watcher.pid"
set "LOG_FILE=%STATE_DIR%\autosync.log"
set "SCRIPT=%ROOT%\tools\github_autosync.py"

set "PY_EXE=C:\Users\Miltan\AppData\Local\Python\bin\python.exe"
set "PYW_EXE=C:\Users\Miltan\AppData\Local\Python\bin\pythonw.exe"
if not exist "%PY_EXE%" set "PY_EXE=python"
if not exist "%PYW_EXE%" set "PYW_EXE=pythonw"

set "ACTION=%~1"
if "%ACTION%"=="" set "ACTION=status"

if /I "%ACTION%"=="start" goto :DO_START
if /I "%ACTION%"=="stop" goto :DO_STOP
if /I "%ACTION%"=="status" goto :DO_STATUS
if /I "%ACTION%"=="sync" goto :DO_SYNC
if /I "%ACTION%"=="init" goto :DO_INIT

echo Использование:
echo   autosync.cmd start              - Запустить фоновую синхронизацию с GitHub (debounce 20 сек)
echo   autosync.cmd stop               - Остановить фонового наблюдателя
echo   autosync.cmd status             - Проверить статус наблюдателя и последние записи лога
echo   autosync.cmd sync               - Принудительно выполнить разовый коммит и push прямо сейчас
echo   autosync.cmd init ^<GITHUB_URL^>  - Инициализировать Git-репозиторий и привязать ветку main к GitHub
exit /b 0

:DO_START
if not exist "%STATE_DIR%" mkdir "%STATE_DIR%"
if exist "%PID_FILE%" (
    set /p OLD_PID=<"%PID_FILE%"
    tasklist /FI "PID eq !OLD_PID!" 2>nul | findstr /R /C:" !OLD_PID! " >nul
    if !ERRORLEVEL! EQU 0 (
        echo [AutoSync] Наблюдатель уже запущен в фоне ^(PID=!OLD_PID!^).
        exit /b 0
    )
)
powershell -NoProfile -Command "$p = Start-Process -FilePath '%PYW_EXE%' -ArgumentList '\"%SCRIPT%\" --debounce 20' -WorkingDirectory '%ROOT%' -WindowStyle Hidden -PassThru; Write-Host ('[AutoSync] Фоновый наблюдатель запущен! PID=' + $p.Id + ' | Лог: .autosync\autosync.log')"
exit /b 0

:DO_STOP
if not exist "%PID_FILE%" (
    echo [AutoSync] Активный PID-файл не найден ^(наблюдатель не запущен^).
    exit /b 0
)
set /p CUR_PID=<"%PID_FILE%"
taskkill /PID %CUR_PID% /F >nul 2>&1
if exist "%PID_FILE%" del /F /Q "%PID_FILE%" >nul 2>&1
echo [AutoSync] Фоновый наблюдатель остановлен ^(PID=%CUR_PID%^).
exit /b 0

:DO_SYNC
"%PY_EXE%" "%SCRIPT%" --once
echo [AutoSync] Разовая проверка завершена. См. лог .autosync\autosync.log
exit /b 0

:DO_INIT
set "REPO_URL=%~2"
if "%REPO_URL%"=="" (
    echo [Ошибка] Укажите URL вашего репозитория: autosync.cmd init https://github.com/USER/REPO.git
    exit /b 1
)
where git >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    if exist "C:\Program Files\Git\cmd\git.exe" (
        set "PATH=C:\Program Files\Git\cmd;%PATH%"
    ) else (
        echo [AutoSync] Git не установлен в системе. Установите Git командой: winget install --id Git.Git -e
        exit /b 1
    )
)
pushd "%ROOT%"
if not exist ".git" git init -b main
git remote remove origin >nul 2>&1
git remote add origin "%REPO_URL%"
popd
"%PY_EXE%" "%SCRIPT%" --once
echo [AutoSync] Репозиторий привязан к %REPO_URL%. Запускаю фоновый наблюдатель...
call "%~f0" start
exit /b 0

:DO_STATUS
if exist "%PID_FILE%" (
    set /p CUR_PID=<"%PID_FILE%"
    tasklist /FI "PID eq !CUR_PID!" 2>nul | findstr /R /C:" !CUR_PID! " >nul
    if !ERRORLEVEL! EQU 0 (
        echo [AutoSync] Статус: РАБОТАЕТ В ФОНЕ ^(PID=!CUR_PID!, задержка debounce: 20 сек^)
    ) else (
        echo [AutoSync] Статус: ОСТАНОВЛЕН ^(устаревший PID=!CUR_PID!^)
    )
) else (
    echo [AutoSync] Статус: ОСТАНОВЛЕН
)
if exist "%LOG_FILE%" (
    echo --- Последние записи .autosync\autosync.log ---
    powershell -NoProfile -Command "Get-Content -LiteralPath '%LOG_FILE%' -Tail 8 -Encoding UTF8"
)
exit /b 0