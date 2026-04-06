$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "Cleaning and publishing release output..."
dotnet clean | Out-Null
dotnet publish -c Release -r win-x64 --self-contained true

$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    $iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $iscc) {
    throw "ISCC.exe not found. Install Inno Setup 6."
}

Write-Host "Compiling installer with: $iscc"
& $iscc "Installer.iss"

Write-Host ""
Write-Host "Installer ready in installer_output folder." -ForegroundColor Green
