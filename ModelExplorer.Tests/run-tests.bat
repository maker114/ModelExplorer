@echo off
setlocal
rem Run the Model Explorer test suite (naming rules, scan rules, organize rules).
"%~dp0bin\Release\ModelExplorer.Tests.exe"
if errorlevel 1 (
  echo Tests failed.
  pause
  exit /b 1
)
pause
