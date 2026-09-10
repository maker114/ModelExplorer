# Model Explorer 便携版发布脚本
#
# v3.0.0 新增。修复 v2.4.1 的发布缺陷：
#   旧流程靠人工把 exe 覆盖进 dist\ModelExplorer-2.0.1-portable\，
#   目录名停留在 2.0.1 而内容已是 2.4.1（Git 因忽略 dist\ 也无法发现）。
#   现在目录名由程序集版本号推导，版本与产物必然一致。

param(
    [string]$Configuration = 'Release',
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# dotnet 不在 PATH 时回退到默认安装位置（部分受限会话不继承 PATH）
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

# --- 版本号取自主程序 AssemblyInfo，保证与 exe 一致 ---
$assemblyInfo = Get-Content (Join-Path $root 'ModelExplorer\AssemblyInfo.cs') -Raw
if ($assemblyInfo -notmatch 'AssemblyVersion\("(\d+\.\d+\.\d+)') {
    throw '未能从 ModelExplorer\AssemblyInfo.cs 解析出版本号'
}
$version = $Matches[1]

Write-Host "版本号：$version"

# --- 构建（-m:1 串行，理由见 build.ps1） ---
& $dotnet build (Join-Path $root 'ModelExplorer.sln') -c $Configuration --nologo -m:1
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build 失败，退出码 $LASTEXITCODE"
}

# --- 测试 ---
if (-not $SkipTests) {
    & (Join-Path $root "ModelExplorer.Tests\bin\$Configuration\ModelExplorer.Tests.exe")
    if ($LASTEXITCODE -ne 0) {
        throw "测试失败，退出码 $LASTEXITCODE"
    }
}

# --- 打包 ---
$bin = Join-Path $root "ModelExplorer\bin\$Configuration"
$distRoot = Join-Path $root 'dist'
$packageName = "ModelExplorer-$version-portable"
$package = Join-Path $distRoot $packageName

if (Test-Path $package) {
    Remove-Item $package -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $package | Out-Null

# 运行必需：主程序、共享库、SolidWorks Interop（exe 启动时需要它们在同一目录）
$payload = @(
    'ModelExplorer.exe',
    'ModelExplorer.exe.config',
    'ModelExplorer.pdb',
    'ModelExplorer.Core.dll',
    'SolidWorks.Interop.sldworks.dll',
    'SolidWorks.Interop.swconst.dll'
)
foreach ($file in $payload) {
    $source = Join-Path $bin $file
    if (-not (Test-Path $source)) {
        throw "缺少发布文件：$source"
    }
    Copy-Item $source $package -Force
}

# --- 启动脚本（保持 ASCII，避免 cmd 代码页问题） ---
$launcher = @(
    '@echo off',
    'start "" "%~dp0ModelExplorer.exe"'
) -join "`r`n"
[System.IO.File]::WriteAllText(
    (Join-Path $package '启动ModelExplorer.bat'),
    $launcher + "`r`n",
    (New-Object System.Text.ASCIIEncoding))

# --- 使用说明 ---
$readme = @"
Model Explorer $version 便携版
================================

运行：
  双击“启动ModelExplorer.bat”，或直接运行“ModelExplorer.exe”。

依赖：
  - Windows 10/11 自带 .NET Framework 4.8 即可运行。
  - 导出 STL 需要本机安装 SolidWorks 2022，并已注册 SolidWorks COM；
    也可在软件设置里配置 SLDWORKS.exe 路径，便于 API 调用。
  - 打开或保存 3MF 时，可在软件设置里配置 Bambu Studio 路径。

文件：
  - ModelExplorer.exe
  - ModelExplorer.Core.dll
  - SolidWorks.Interop.sldworks.dll
  - SolidWorks.Interop.swconst.dll

说明：
  本目录可直接复制到其他电脑使用，请保持上述文件放在同一目录。
  配置保存在 %APPDATA%\ModelExplorer\config.json，与 SolidWorks 插件、
  命令行工具共用同一份配置。
"@

# 显式写 UTF-8 + BOM：保证在记事本等工具中中文说明不乱码
[System.IO.File]::WriteAllText(
    (Join-Path $package '使用说明.txt'),
    $readme,
    (New-Object System.Text.UTF8Encoding $true))

Write-Host ''
Write-Host "已发布：$package"
Get-ChildItem $package | Select-Object Name, @{Name = 'MB'; Expression = { [math]::Round($_.Length / 1MB, 2) } } | Format-Table -AutoSize
