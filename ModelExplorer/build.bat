@echo off
setlocal
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
