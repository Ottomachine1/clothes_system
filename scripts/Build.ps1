Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
dotnet build (Join-Path $repoRoot "ClothesSystem.sln") -m:1
