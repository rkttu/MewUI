@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM Runs the already-published NativeAOT+Trimmed sample EXE repeatedly
REM and writes a small Markdown report. No build/publish is performed here.

set RUNS=50
if not "%~1"=="" set RUNS=%~1
set ROOT=%~dp0..\..
set PUBLISH_DIR=%ROOT%\.artifacts\publish\MewUI.Sample\win-x64-trimmed
for %%D in ("%PUBLISH_DIR%") do set PUBLISH_DIR=%%~fD

if not exist "%PUBLISH_DIR%\" (
  echo Publish folder not found:
  echo   %PUBLISH_DIR%
  echo Run a publish first:
  echo   dotnet publish .\samples\MewUI.Sample\MewUI.Sample.csproj -c Release -p:PublishProfile=win-x64-trimmed
  exit /b 1
)

set EXE=%PUBLISH_DIR%\Aprillz.MewUI.Sample.exe
if not exist "%EXE%" (
  REM fallback: first exe in folder
  for %%F in ("%PUBLISH_DIR%\*.exe") do (
    set EXE=%%~fF
    goto :exe_found
  )
)
:exe_found
if not exist "%EXE%" (
  echo EXE not found in:
  echo   %PUBLISH_DIR%
  exit /b 1
)

echo EXE: %EXE%
echo Runs per backend: %RUNS%

REM Direct2D
del /q "%PUBLISH_DIR%\metrics.log" >nul 2>nul
for /L %%I in (1,1,%RUNS%) do (
  echo [d2d] %%I/%RUNS%
  "%EXE%" --bench
  REM Sleep ~1s without stdin usage (timeout fails under redirected input).
  ping 127.0.0.1 -n 2 >nul
)
if exist "%PUBLISH_DIR%\metrics.log" (
  copy /y "%PUBLISH_DIR%\metrics.log" "%PUBLISH_DIR%\metrics_d2d.log" >nul
)

REM GDI
del /q "%PUBLISH_DIR%\metrics.log" >nul 2>nul
for /L %%I in (1,1,%RUNS%) do (
  echo [gdi] %%I/%RUNS%
  "%EXE%" --bench --gdi
  REM Sleep ~1s without stdin usage (timeout fails under redirected input).
  ping 127.0.0.1 -n 2 >nul
)
if exist "%PUBLISH_DIR%\metrics.log" (
  copy /y "%PUBLISH_DIR%\metrics.log" "%PUBLISH_DIR%\metrics_gdi.log" >nul
)

REM Generate markdown summary.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0bench_trimmed_analyze.ps1" -Dir "%PUBLISH_DIR%" -Runs %RUNS%
if errorlevel 1 (
  echo Failed to generate report.
  exit /b 1
)

echo Done.
echo Report: %PUBLISH_DIR%\bench_report.md
exit /b 0
