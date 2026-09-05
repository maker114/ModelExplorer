$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src'
$out = Join-Path $root 'bin'
$keyFile = Join-Path $root 'ModelExplorerAddin.snk'

New-Item -ItemType Directory -Force -Path $out | Out-Null

$frameworkDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$snExe = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\sn.exe'
$cscExe = Join-Path $frameworkDir 'csc.exe'

if (-not (Test-Path $cscExe)) {
    throw "csc.exe not found: $cscExe"
}

if (-not (Test-Path $snExe)) {
    throw "sn.exe not found: $snExe"
}

if (-not (Test-Path $keyFile)) {
    & $snExe -k $keyFile
}

$references = @(
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.sldworks.dll',
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swconst.dll',
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swpublished.dll'
)

$referenceArgs = @()
foreach ($reference in $references) {
    $referenceArgs += '/reference:' + $reference
}

$sourceFiles = @()
foreach ($source in (Get-ChildItem -Path $src -Filter '*.cs' -Recurse)) {
    $sourceFiles += $source.FullName
}

$dllPath = Join-Path $out 'ModelExplorerAddin.dll'

& $cscExe `
    /nologo `
    /target:library `
    /platform:x64 `
    /out:$dllPath `
    /keyfile:$keyFile `
    $referenceArgs `
    $sourceFiles

if ($LASTEXITCODE -ne 0) {
    throw "csc.exe failed with exit code $LASTEXITCODE"
}

$interopFiles = @(
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.sldworks.dll',
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swconst.dll',
    'D:\SW2022\SOLIDWORKS\SolidWorks.Interop.swpublished.dll'
)

foreach ($interopFile in $interopFiles) {
    Copy-Item -LiteralPath $interopFile -Destination $out -Force
}

Write-Host "Built: $dllPath"
