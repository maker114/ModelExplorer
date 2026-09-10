@echo off
setlocal
rem Build the main application.
rem v3.0.0: the GUI depends on ModelExplorer.Core, which is built too.
rem To build every project and run the test suite, use build.ps1 in the repo root.
set "DOTNET=C:\Program Files\dotnet\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"

"%DOTNET%" build "%~dp0ModelExplorer.csproj" -c Release --nologo
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo Built: %~dp0bin\Release\ModelExplorer.exe
pause
