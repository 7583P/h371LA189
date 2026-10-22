#!/usr/bin/env bash
# ==============================================================================
# Script: git-purge-keystore.sh
# Purpose: Controlled Git history purge with cryptographic backup.
# ==============================================================================

set -euo pipefail

TARGET_BRANCH="${1:-oss-readiness}"
BACKUP_BUNDLE="backup-hilladventurer-$(date +%Y%m%d-%H%M%S).bundle"

echo "=========================================================="
echo "   HILL ADVENTURER: CONTROLLED GIT HISTORY PURGE SCRIPT   "
echo "=========================================================="
echo "Target branch: $TARGET_BRANCH"
echo ""

echo "STEP 1: CREATING VERIFIABLE CRYPTOGRAPHIC REPOSITORY BACKUP"
git bundle create "$BACKUP_BUNDLE" --all
git bundle verify "$BACKUP_BUNDLE"
echo "Backup verified successfully at: $BACKUP_BUNDLE"
echo "Rollback command: git clone $BACKUP_BUNDLE restored-repo"
echo ""

read -rp "Type 'YES-PURGE-HISTORY' to execute local history rewrite: " CONFIRM
if [ "$CONFIRM" != "YES-PURGE-HISTORY" ]; then
    echo "Operation cancelled by user. No changes made."
    exit 0
fi

if command -v git-filter-repo >/dev/null 2>&1; then
    git filter-repo --invert-paths --path hilladventurer.keystore --refs "$TARGET_BRANCH" --force
else
    FILTER_BRANCH_SQUELCH_WARNING=1 git filter-branch --force --index-filter \
        "git rm --cached --ignore-unmatch hilladventurer.keystore" \
        --prune-empty --tag-name-filter cat -- "$TARGET_BRANCH"
fi

echo "Local history purge completed!"
echo "To publish safely without --force --all, use:"
echo "  git push origin $TARGET_BRANCH --force-with-lease"
