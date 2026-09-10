# Model Explorer —— 一键构建全部工程并跑测试
#
# v3.0.0 新增。仓库原先只有 ModelExplorer\build.bat 能构建主程序，
# CLI 与插件靠各自独立的 csc.exe 脚本，且都没有测试。

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root 'ModelExplorer.sln'

# dotnet 不在 PATH 时回退到默认安装位置（部分受限会话不继承 PATH）
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

Write-Host '=== 构建 ==='
# -m:1：串行构建。本解决方案只有 5 个工程，串行与并行的耗时差异可忽略，
# 但可以避免在禁止命名管道的受限环境（容器、沙箱、部分受限企业机器）下
# MSBuild 多节点通信失败而导致构建返回 1。
& $dotnet build $solution -c Release --nologo -m:1
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build 失败，退出码 $LASTEXITCODE"
}

Write-Host ''
Write-Host '=== 测试 ==='
& (Join-Path $root 'ModelExplorer.Tests\bin\Release\ModelExplorer.Tests.exe')
if ($LASTEXITCODE -ne 0) {
    throw "测试失败，退出码 $LASTEXITCODE"
}

Write-Host ''
Write-Host '构建与测试全部通过。'
Write-Host '主程序：ModelExplorer\bin\Release\ModelExplorer.exe'
Write-Host '发布打包：powershell -ExecutionPolicy Bypass -File .\publish.ps1'
