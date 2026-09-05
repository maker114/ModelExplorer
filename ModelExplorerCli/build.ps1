$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'
$out = Join-Path $root 'bin'
$addinSrc = Join-Path (Split-Path $root -Parent) 'ModelExplorerAddin\src\AddinSettings.cs'

New-Item -ItemType Directory -Force -Path $out | Out-Null

$frameworkDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$cscExe = Join-Path $frameworkDir 'csc.exe'

if (-not (Test-Path $cscExe)) {
    throw "csc.exe not found: $cscExe"
}

$references = @(
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.sldworks.dll',
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swconst.dll'
)

$referenceArgs = @()
foreach ($reference in $references) {
    $referenceArgs += '/reference:' + $reference
}

$sourceFiles = @(
    (Join-Path $src 'Program.cs'),
    $addinSrc
)

$exePath = Join-Path $out 'ModelExplorerCli.exe'

& $cscExe `
    /nologo `
    /target:exe `
    /platform:x64 `
    /out:$exePath `
    $referenceArgs `
    $sourceFiles

if ($LASTEXITCODE -ne 0) {
    throw "csc.exe failed with exit code $LASTEXITCODE"
}

$interopFiles = @(
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.sldworks.dll',
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swconst.dll'
)

foreach ($interopFile in $interopFiles) {
    Copy-Item -LiteralPath $interopFile -Destination $out -Force
}

Write-Host "Built: $exePath"
