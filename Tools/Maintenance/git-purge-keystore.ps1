<#
.SYNOPSIS
    Interactive script to safely purge 'hilladventurer.keystore' from Git history with verifiable backup.
.DESCRIPTION
    WARNING: This script rewrites Git commit history locally.
    It creates a verifiable git bundle backup before proceeding.
    It DOES NOT execute force-push automatically.
#>

param(
    [string]$TargetBranch = "oss-readiness",
    [switch]$Force
)

Write-Host "==========================================================" -ForegroundColor Red
Write-Host "   HILL ADVENTURER: CONTROLLED GIT HISTORY PURGE SCRIPT   " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Red
Write-Host ""
Write-Host "Target branch to cleanse: $TargetBranch" -ForegroundColor Cyan
Write-Host ""
Write-Host "STEP 1: CREATING VERIFIABLE CRYPTOGRAPHIC REPOSITORY BACKUP" -ForegroundColor Yellow
$backupDir = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "..\Backups-HillAdventurer"
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
}
$backupBundle = Join-Path (Resolve-Path $backupDir) "backup-hilladventurer-$(Get-Date -Format 'yyyyMMdd-HHmmss').bundle"
Write-Host "Writing complete repository backup to $backupBundle..."
git bundle create $backupBundle --all

if ($LASTEXITCODE -ne 0) {
    Write-Error "CRITICAL: Backup bundle creation failed! Aborting."
    exit 1
}

Write-Host "Verifying backup bundle..."
git bundle verify $backupBundle
if ($LASTEXITCODE -ne 0) {
    Write-Error "CRITICAL: Backup bundle verification failed! Aborting."
    exit 1
}
Write-Host "Backup verified successfully at: $backupBundle" -ForegroundColor Green
Write-Host "ROLLBACK INSTRUCTION: To restore from this backup at any point, run:"
Write-Host "  git clone $backupBundle restored-repo" -ForegroundColor Gray
Write-Host ""

if (-not $Force) {
    $confirm = Read-Host "Type 'YES-PURGE-HISTORY' to execute local history rewrite"
    if ($confirm -ne "YES-PURGE-HISTORY") {
        Write-Host "Operation cancelled by user. No history was modified." -ForegroundColor Green
        exit 0
    }
}

Write-Host "Executing history rewrite for target branch..." -ForegroundColor Yellow
if (Get-Command "git-filter-repo" -ErrorAction SilentlyContinue) {
    git filter-repo --invert-paths --path hilladventurer.keystore --refs $TargetBranch --force
} else {
    $env:FILTER_BRANCH_SQUELCH_WARNING = "1"
    git filter-branch --force --index-filter "git rm --cached --ignore-unmatch hilladventurer.keystore" --prune-empty --tag-name-filter cat -- $TargetBranch
}

Write-Host ""
Write-Host "Local history purge completed!" -ForegroundColor Green
Write-Host ""
Write-Host "SAFE REMOTE PUBLISHING INSTRUCTIONS:" -ForegroundColor Yellow
Write-Host " DO NOT use 'git push --force --all'."
Write-Host " Instead, push only the validated target branch with lease protection:" -ForegroundColor Cyan
Write-Host "   git push origin $TargetBranch --force-with-lease"
Write-Host ""
