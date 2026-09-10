param(
    [string]$DllPath = (Join-Path $PSScriptRoot 'bin\Release\ModelExplorerAddin.dll'),
    [switch]$AllUsers
)

$ErrorActionPreference = 'Stop'

$dll = (Resolve-Path -LiteralPath $DllPath).Path
$assemblyName = [Reflection.AssemblyName]::GetAssemblyName($dll)
$publicKeyToken = ($assemblyName.GetPublicKeyToken() | ForEach-Object { $_.ToString('x2') }) -join ''

if ([string]::IsNullOrEmpty($publicKeyToken)) {
    throw "ModelExplorerAddin.dll is not strong-name signed. Run build.ps1 first."
}

$clsid = '8A5C3F2B-6D7E-4B9A-9C1D-2E4F60718293'
$progId = 'ModelExplorerAddin.ModelExplorerAddin'
$codeBase = 'file:///' + $dll.Replace('\', '/')
$assemblyNameValue = '{0}, Version={1}, Culture=neutral, PublicKeyToken={2}' -f $assemblyName.Name, $assemblyName.Version, $publicKeyToken

$classesRoot = if ($AllUsers) { 'HKLM:\Software\Classes' } else { 'HKCU:\Software\Classes' }
$classesClsid = Join-Path $classesRoot ('CLSID\{' + $clsid + '}')
$inprocKey = Join-Path $classesClsid 'InprocServer32'
$progKey = Join-Path $classesRoot $progId
$progClsidKey = Join-Path $progKey 'CLSID'
$progCurVerKey = Join-Path $progKey 'CurVer'

New-Item -Path $classesClsid -Force | Out-Null
New-Item -Path $inprocKey -Force | Out-Null
Set-ItemProperty -Path $classesClsid -Name '(default)' -Value $progId
Set-ItemProperty -Path $inprocKey -Name '(default)' -Value 'mscoree.dll'
Set-ItemProperty -Path $inprocKey -Name 'ThreadingModel' -Value 'Both'
Set-ItemProperty -Path $inprocKey -Name 'Class' -Value $progId
Set-ItemProperty -Path $inprocKey -Name 'Assembly' -Value $assemblyNameValue
Set-ItemProperty -Path $inprocKey -Name 'RuntimeVersion' -Value 'v4.0.30319'
Set-ItemProperty -Path $inprocKey -Name 'CodeBase' -Value $codeBase

New-Item -Path $progKey -Force | Out-Null
New-Item -Path $progClsidKey -Force | Out-Null
New-Item -Path $progCurVerKey -Force | Out-Null
Set-ItemProperty -Path $progKey -Name '(default)' -Value 'ModelExplorerAddin class'
Set-ItemProperty -Path $progClsidKey -Name '(default)' -Value ('{' + $clsid + '}')
Set-ItemProperty -Path $progCurVerKey -Name '(default)' -Value $progId

$addinKeyName = '{' + $clsid + '}'
$swRoot = if ($AllUsers) { 'HKLM:\SOFTWARE\SolidWorks' } else { 'HKCU:\SOFTWARE\SolidWorks' }
$swAddIns = Join-Path $swRoot 'AddIns'
$sw2022AddIns = Join-Path $swRoot 'SOLIDWORKS 2022\AddIns'
$sw2022Startup = Join-Path $swRoot 'SolidWorks 2022\AddInsStartup'

$key1 = Join-Path $swAddIns $addinKeyName
$key2 = Join-Path $sw2022AddIns $addinKeyName
$key3 = Join-Path $sw2022Startup $addinKeyName

New-Item -Path $key1 -Force | Out-Null
New-Item -Path $key2 -Force | Out-Null
New-Item -Path $key3 -Force | Out-Null

Set-ItemProperty -Path $key1 -Name '(default)' -Value 0
Set-ItemProperty -Path $key1 -Name 'Title' -Value 'Model Explorer Add-in'
Set-ItemProperty -Path $key1 -Name 'Description' -Value 'One-click STL export to Bambu Studio'

Set-ItemProperty -Path $key2 -Name '(default)' -Value 0
Set-ItemProperty -Path $key2 -Name 'Title' -Value 'Model Explorer Add-in'
Set-ItemProperty -Path $key2 -Name 'Description' -Value 'One-click STL export to Bambu Studio'

Set-ItemProperty -Path $key3 -Name '(default)' -Value 0

$scope = if ($AllUsers) { 'all users (HKLM)' } else { 'current user (HKCU)' }
Write-Host "Installed for $scope. Restart SolidWorks and enable Model Explorer Add-in in Tools > Add-Ins."
