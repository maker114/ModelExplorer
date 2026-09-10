# 构建 Model Explorer 插件
#
# v3.0.0 起改用标准 dotnet build（原先用 csc.exe 手工拼命令行，
# 并把 Interop 路径硬编码为 D:\SW2022\SOLIDWORKS\）。
# 强名称密钥位于仓库根 ModelExplorer.snk。

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'ModelExplorerAddin.csproj'

# dotnet 不在 PATH 时回退到默认安装位置（部分受限会话不继承 PATH）
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

& $dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

$dllPath = Join-Path $root 'bin\Release\ModelExplorerAddin.dll'
Write-Host "Built: $dllPath"
Write-Host "接下来运行 install.ps1 注册插件（升级后必须重新注册，因为程序集版本已变化）。"
