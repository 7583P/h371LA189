<#
.SYNOPSIS
    Interactive script to safely purge 'hilladventurer.keystore' from Git history.
.DESCRIPTION
    WARNING: This script rewrites Git commit history.
    DO NOT run without backing up your repository first.
    All commit hashes will change. Remote force push will be required.
#>

param(
    [switch]$Force
)

Write-Host "==========================================================" -ForegroundColor Red
Write-Host "   HILL ADVENTURER: GIT HISTORY KEYSTORE PURGE PROCEDURE   " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Red
Write-Host ""
Write-Host "This operation will:"
Write-Host " 1. Remove 'hilladventurer.keystore' from ALL past commits."
Write-Host " 2. Rewrite all commit SHA-1 hashes across the repository."
Write-Host " 3. Require a force-push ('git push --force --all') to GitHub."
Write-Host " 4. Invalidate existing forks/clones (collaborators must re-clone)."
Write-Host ""
Write-Host "CRITICAL NOTE:" -ForegroundColor Cyan
Write-Host "Purging Git history DOES NOT revoke the key on Google Play!"
Write-Host "You must also reset your upload key in Google Play Console:"
Write-Host " Play Console -> Release -> Setup -> App integrity -> Request upload key reset."
Write-Host ""

if (-not $Force) {
    $confirm = Read-Host "Are you absolutely sure you want to proceed? (Type 'YES-PURGE-HISTORY' to confirm)"
    if ($confirm -ne "YES-PURGE-HISTORY") {
        Write-Host "Operation cancelled. No changes were made." -ForegroundColor Green
        exit 0
    }
}

Write-Host "Checking for git-filter-repo..." -ForegroundColor Cyan
$hasFilterRepo = Get-Command "git-filter-repo" -ErrorAction SilentlyContinue

if ($hasFilterRepo) {
    Write-Host "Executing git-filter-repo..." -ForegroundColor Yellow
    git filter-repo --invert-paths --path hilladventurer.keystore --force
} else {
    Write-Host "git-filter-repo not found. Using git filter-branch fallback..." -ForegroundColor Yellow
    $env:FILTER_BRANCH_SQUELCH_WARNING = "1"
    git filter-branch --force --index-filter "git rm --cached --ignore-unmatch hilladventurer.keystore" --prune-empty --tag-name-filter cat -- --all
}

Write-Host ""
Write-Host "History purge completed locally!" -ForegroundColor Green
Write-Host "Next steps required for remote repository:" -ForegroundColor Yellow
Write-Host " 1. Verify log: git log --all -- hilladventurer.keystore (must be empty)"
Write-Host " 2. Push rewritten history: git push origin --force --all"
Write-Host " 3. Push rewritten tags: git push origin --force --tags"
