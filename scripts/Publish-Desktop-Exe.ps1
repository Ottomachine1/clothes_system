# Publish-Desktop-Exe.ps1
# Publish Clothes Management System Desktop Application as Windows EXE
# Output: publish/desktop/

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$OutputDir = Join-Path $ProjectRoot "publish/desktop"

Write-Host "Starting publish ClothesSystem Desktop Application..." -ForegroundColor Cyan
Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan

# Stop running application
$processName = "ClothesSystem.Desktop"
$runningProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($runningProcess) {
    Write-Host "Stopping running application..." -ForegroundColor Yellow
    $runningProcess | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Clean and create output directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning old publish files..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Publish application
Write-Host "Executing dotnet publish..." -ForegroundColor Cyan
$publishArgs = @(
    "publish",
    (Join-Path $ProjectRoot "src/ClothesSystem.Desktop/ClothesSystem.Desktop.csproj"),
    "-c", $Configuration,
    "-r", "win-x64",
    "--self-contained",
    "-p:PublishSingleFile=true",
    "-p:PublishReadyToRun=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-o", $OutputDir
)

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Copy templates folder
$templateSource = Join-Path $ProjectRoot "src/ClothesSystem.Desktop/Templates"
$templateDest = Join-Path $OutputDir "Templates"

if (Test-Path $templateSource) {
    Write-Host "Copying template files..." -ForegroundColor Cyan
    Copy-Item -Path $templateSource -Destination $templateDest -Recurse -Force
}

# Show output files
Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Publish completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nOutput: $OutputDir" -ForegroundColor Cyan
Write-Host "`nGenerated files:" -ForegroundColor Cyan
Get-ChildItem $OutputDir -File | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  - $($_.Name) ($size MB)"
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "IMPORTANT: Database file location" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Database: %APPDATA%\ClothesSystem\clothes-system.db" -ForegroundColor White
Write-Host "Logs: %APPDATA%\ClothesSystem\logs\" -ForegroundColor White
Write-Host "`nVersion upgrades will preserve your data automatically!" -ForegroundColor Green
