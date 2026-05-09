$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "..\src\ClothesSystem.Web\ClothesSystem.Web.csproj"
$templateSource = Join-Path $PSScriptRoot "..\src\ClothesSystem.Web\Templates"
$publishRoot = Join-Path $PSScriptRoot "..\publish"
$output = Join-Path $PSScriptRoot "..\publish\windows-exe"
$staging = Join-Path $PSScriptRoot "..\publish\windows-exe.staging"
$targetExe = Join-Path $output "ClothesSystem.Web.exe"
$legacyPublishFolders = @(
    (Join-Path $PSScriptRoot "..\publish\windows-exe-optimized"),
    (Join-Path $PSScriptRoot "..\publish\windows-exe-fix-0315"),
    (Join-Path $PSScriptRoot "..\publish\windows-exe-fix-edit-save")
)

function Remove-DirectoryIfExists {
    param([string] $Path)

    if (Test-Path $Path) {
        try {
            Remove-Item $Path -Recurse -Force
        }
        catch {
            Write-Warning "Failed to remove directory: $Path"
        }
    }
}

function Sync-Directory {
    param(
        [string] $Source,
        [string] $Destination
    )

    if (!(Test-Path $Destination)) {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }

    robocopy $Source $Destination /MIR /R:3 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    $exitCode = $LASTEXITCODE

    if ($exitCode -gt 7) {
        throw "Robocopy failed with exit code $exitCode while syncing $Source to $Destination."
    }
}

$resolvedPublishRoot = (Resolve-Path $publishRoot).Path
$runningProcesses = Get-Process ClothesSystem.Web -ErrorAction SilentlyContinue | Where-Object {
    try {
        $_.Path -and $_.Path.StartsWith($resolvedPublishRoot, [System.StringComparison]::OrdinalIgnoreCase)
    }
    catch {
        $false
    }
}

if ($runningProcesses) {
    Write-Host "Stopping running published ClothesSystem.Web process..." -ForegroundColor Yellow
    $runningProcesses | Stop-Process -Force
    Start-Sleep -Seconds 1
}

Remove-DirectoryIfExists -Path $staging

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishTrimmed=false `
    -o $staging

if (Test-Path $templateSource) {
    $templateTarget = Join-Path $staging "Templates"
    New-Item -ItemType Directory -Path $templateTarget -Force | Out-Null
    Copy-Item -Path (Join-Path $templateSource "*") -Destination $templateTarget -Recurse -Force
}

Sync-Directory -Source $staging -Destination $output
Remove-DirectoryIfExists -Path $staging

foreach ($legacyFolder in $legacyPublishFolders) {
    if ((Test-Path $legacyFolder) -and ((Resolve-Path $legacyFolder).Path -ne (Resolve-Path $output).Path)) {
        Remove-DirectoryIfExists -Path $legacyFolder
    }
}

Write-Host ""
Write-Host "Windows EXE publish completed:" -ForegroundColor Green
Write-Host $output
