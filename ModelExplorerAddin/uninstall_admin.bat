@echo off
setlocal
set "SCRIPT_DIR=%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$installer = '%SCRIPT_DIR%uninstall.ps1'; Start-Process -FilePath 'powershell.exe' -Verb RunAs -Wait -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',$installer); Write-Host 'Uninstall finished. Restart SolidWorks.'"

pause
