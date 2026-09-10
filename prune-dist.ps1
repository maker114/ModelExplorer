# 清理 dist 里的旧版本目录
#
# V3.0.3 新增。规则：只保留最新的 -KeepVersions 个版本目录
# （默认 3 = 当前版本 + 2 个历史版本），更旧的自动删除，避免目录无限堆积。
#
# 排序依据是目录名里的版本号而不是修改时间，所以即使手工重排过目录，
# 也不会把较新的版本当成旧的删掉。
#
# 可以单独运行：
#   powershell -ExecutionPolicy Bypass -File .\prune-dist.ps1
#   powershell -ExecutionPolicy Bypass -File .\prune-dist.ps1 -KeepVersions 2
# publish.ps1 在发布完成后会自动调用它。

param(
    [string]$DistRoot = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'dist'),
    [int]$KeepVersions = 3
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $DistRoot)) {
    Write-Host "dist 目录不存在，无需清理：$DistRoot"
    exit 0
}

$keep = [Math]::Max(1, $KeepVersions)

$packages = Get-ChildItem -LiteralPath $DistRoot -Directory -Filter 'ModelExplorer-*-portable' |
    ForEach-Object {
        $parsedVersion = $null
        if ($_.Name -match '^ModelExplorer-(\d+(?:\.\d+)+)-portable$') {
            $parsedVersion = [version]$Matches[1]
        }
        New-Object PSObject -Property @{ Directory = $_; Version = $parsedVersion }
    } |
    Where-Object { $_.Version -ne $null } |
    Sort-Object -Property Version -Descending

$obsolete = @($packages | Select-Object -Skip $keep)
if ($obsolete.Count -eq 0) {
    Write-Host "dist 中共有 $($packages.Count) 个版本目录，无需清理（保留最新 $keep 个）。"
    exit 0
}

$removed = @()
$failed = @()
foreach ($item in $obsolete) {
    try {
        Remove-Item -LiteralPath $item.Directory.FullName -Recurse -Force -ErrorAction Stop
        $removed += $item.Directory.Name
    }
    catch {
        # 最常见的原因：该版本的程序正在运行，目录里的 dll / exe 被占用
        $failed += ('{0}：{1}' -f $item.Directory.Name, $_.Exception.Message)
    }
}

if ($removed.Count -gt 0) {
    Write-Host "已清理旧版本（保留最新 $keep 个）：$($removed -join '，')"
}
if ($failed.Count -gt 0) {
    Write-Warning ("以下旧版本目录未能删除，请关闭对应程序后重新运行本脚本：`n  " + ($failed -join "`n  "))
    exit 1
}

exit 0
