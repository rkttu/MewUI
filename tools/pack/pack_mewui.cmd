@echo off
setlocal EnableExtensions

REM Packs Aprillz.MewUI into a NuGet package under .artifacts\nuget.

set ROOT=%~dp0..\..
set OUT=%ROOT%\.artifacts\nuget
set VERSION=%~1

if not exist "%OUT%" mkdir "%OUT%" >nul 2>nul

if "%VERSION%"=="" (
  dotnet pack "%ROOT%\src\MewUI\MewUI.csproj" -c Release -o "%OUT%" /p:ContinuousIntegrationBuild=true
) else (
  dotnet pack "%ROOT%\src\MewUI\MewUI.csproj" -c Release -o "%OUT%" /p:ContinuousIntegrationBuild=true /p:PackageVersion=%VERSION%
)
exit /b %ERRORLEVEL%
