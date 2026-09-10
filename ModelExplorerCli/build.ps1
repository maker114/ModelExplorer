# 构建 Model Explorer CLI
#
# v3.0.0 起改用标准 dotnet build：原先用 csc.exe 手工拼命令行，
# 并把 Interop 路径硬编码为 D:\SW2022\SOLIDWORKS\，换机器即无法构建。

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'ModelExplorerCli.csproj'

# dotnet 不在 PATH 时回退到默认安装位置（部分受限会话不继承 PATH）
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

& $dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

Write-Host "Built: $(Join-Path $root 'bin\Release\ModelExplorerCli.exe')"
