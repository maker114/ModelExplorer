$ErrorActionPreference = 'Continue'

$clsid = '8A5C3F2B-6D7E-4B9A-9C1D-2E4F60718293'
$progId = 'ModelExplorerAddin.ModelExplorerAddin'
$addinKeyName = '{' + $clsid + '}'

$paths = @(
    'HKCU:\Software\Classes\CLSID\{' + $clsid + '}',
    'HKCU:\Software\Classes\' + $progId,
    'HKCU:\SOFTWARE\SolidWorks\AddIns\' + $addinKeyName,
    'HKCU:\SOFTWARE\SolidWorks\SOLIDWORKS 2022\AddIns\' + $addinKeyName,
    'HKCU:\SOFTWARE\SolidWorks\SolidWorks 2022\AddInsStartup\' + $addinKeyName,
    'HKLM:\SOFTWARE\SolidWorks\AddIns\' + $addinKeyName,
    'HKLM:\SOFTWARE\SolidWorks\SOLIDWORKS 2022\Addins\' + $addinKeyName,
    'HKLM:\SOFTWARE\WOW6432Node\SolidWorks\AddIns\' + $addinKeyName,
    'HKLM:\SOFTWARE\WOW6432Node\SolidWorks\SOLIDWORKS 2022\Addins\' + $addinKeyName,
    'HKLM:\SOFTWARE\SolidWorks\SolidWorks 2022\AddInsStartup\' + $addinKeyName,
    'HKLM:\Software\Classes\CLSID\{' + $clsid + '}',
    'HKLM:\Software\Classes\' + $progId
)

foreach ($path in $paths) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

Write-Host 'ModelExplorerAddin registry entries removed. Restart SolidWorks.'
