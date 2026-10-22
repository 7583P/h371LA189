<#
.SYNOPSIS
    Safely switches AdMob configuration between Public Test mode and Private Production mode.
.DESCRIPTION
    Public Test mode: Uses Google's official test ad unit IDs (safe for Git and open-source).
    Production mode: Restores the maintainer's real ad units from an untracked private configuration file.
#>

param(
    [ValidateSet("Test", "Production")]
    [string]$Mode = "Test"
)

$targetAsset = "Assets/AnimeHillRacing/Resources/AdMobConfiguration.asset"
$exampleAsset = "Assets/AnimeHillRacing/Resources/AdMobConfiguration.asset.example"
$privateAsset = "Assets/AnimeHillRacing/Resources/AdMobConfiguration.private.asset"

if ($Mode -eq "Test") {
    Write-Host "[AdMob] Switching to PUBLIC TEST mode..." -ForegroundColor Cyan
    if (Test-Path $exampleAsset) {
        Copy-Item $exampleAsset $targetAsset -Force
        Write-Host "[AdMob] Successfully applied public test configuration." -ForegroundColor Green
        Write-Host "       useTestAds = 1, Google sample test IDs active."
    } else {
        Write-Error "Example asset not found at $exampleAsset"
    }
} elseif ($Mode -eq "Production") {
    Write-Host "[AdMob] Switching to PRIVATE PRODUCTION mode..." -ForegroundColor Yellow
    if (-not (Test-Path $privateAsset)) {
        Write-Host "No private asset found at $privateAsset." -ForegroundColor Red
        Write-Host "Creating an untracked template for you..." -ForegroundColor Cyan
        Copy-Item $targetAsset $privateAsset -Force
        Write-Host "Please edit $privateAsset with your real production AdMob IDs." -ForegroundColor Yellow
        Write-Host "NOTE: $privateAsset is strictly excluded by .gitignore and will never be committed." -ForegroundColor Green
        exit 0
    }

    Copy-Item $privateAsset $targetAsset -Force
    Write-Host "[AdMob] Successfully applied private production configuration." -ForegroundColor Green
    Write-Host "WARNING: Remember to run 'Switch-AdMobEnvironment.ps1 -Mode Test' before committing to git!" -ForegroundColor Yellow
}
