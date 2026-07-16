[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $LibIMobileRuntime,

    [Parameter(Mandatory)]
    [string] $DokanMsi,

    [string] $DokanDll = 'C:\Program Files\Dokan\Dokan Library-2.3.1\dokan2.dll',
    [switch] $BuildInstaller
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$payload = Join-Path $repo 'payload'
$release = Join-Path $repo 'release'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

foreach ($required in @($LibIMobileRuntime, $DokanMsi, $DokanDll, $csc)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required build input was not found: $required"
    }
}

$runtime = (Resolve-Path -LiteralPath $LibIMobileRuntime).Path
$dokanMsiPath = (Resolve-Path -LiteralPath $DokanMsi).Path
$dokanDllPath = (Resolve-Path -LiteralPath $DokanDll).Path

foreach ($tool in @('idevice_id.exe', 'idevicepair.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $runtime $tool))) {
        throw "The libimobiledevice runtime is missing $tool"
    }
}

Remove-Item -LiteralPath $payload -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $release -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $payload, $release | Out-Null

dotnet publish (Join-Path $repo 'src\aggregate-proxy\AggregateProxy.csproj') `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o (Join-Path $payload 'aggregate')
if ($LASTEXITCODE -ne 0) { throw 'aggregate-proxy publish failed.' }

Move-Item -LiteralPath (Join-Path $payload 'aggregate\aggregate-proxy.exe') -Destination (Join-Path $payload 'aggregate-proxy.exe')
Remove-Item -LiteralPath (Join-Path $payload 'aggregate') -Recurse -Force

& $csc /nologo /platform:x64 /optimize+ /target:exe `
    "/out:$payload\ifuse-win.exe" (Join-Path $repo 'src\ifuse-win\ifuse-win.cs')
if ($LASTEXITCODE -ne 0) { throw 'ifuse-win compilation failed.' }

& $csc /nologo /platform:x64 /optimize+ /target:winexe `
    "/win32icon:$repo\assets\iphone-drive.ico" `
    /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
    "/out:$payload\iPhoneDrive.exe" (Join-Path $repo 'src\tray\iphone-drive-tray.cs')
if ($LASTEXITCODE -ne 0) { throw 'tray application compilation failed.' }

Copy-Item -Path (Join-Path $runtime '*.dll') -Destination $payload -Force
Copy-Item -LiteralPath (Join-Path $runtime 'idevice_id.exe'), (Join-Path $runtime 'idevicepair.exe') -Destination $payload -Force
Copy-Item -LiteralPath $dokanDllPath -Destination (Join-Path $payload 'dokan2.dll') -Force
Copy-Item -LiteralPath $dokanMsiPath -Destination (Join-Path $payload 'Dokan_x64.msi') -Force
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE'), (Join-Path $repo 'THIRD_PARTY_NOTICES.md') -Destination $payload -Force
Copy-Item -Path (Join-Path $repo 'third-party-licenses\*') -Destination $payload -Force

if ($BuildInstaller) {
    $iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
    if (-not $iscc) {
        $defaultIscc = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
        if (Test-Path -LiteralPath $defaultIscc) { $iscc = $defaultIscc }
    }
    if (-not $iscc) {
        $userIscc = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
        if (Test-Path -LiteralPath $userIscc) { $iscc = $userIscc }
    }
    if (-not $iscc) { throw 'Inno Setup 6 compiler (ISCC.exe) was not found.' }
    & $iscc (Join-Path $repo 'installer\iPhoneDrive.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
}

Write-Host "Build completed. Output: $release"
