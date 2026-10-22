#!/usr/bin/env bash
# ==============================================================================
# Script: git-purge-keystore.sh
# Purpose: Purge 'hilladventurer.keystore' from all commits in Git history.
# WARNING: This rewrites commit hashes. Requires force-push.
# ==============================================================================

set -euo pipefail

echo "=========================================================="
echo "   HILL ADVENTURER: GIT HISTORY KEYSTORE PURGE PROCEDURE   "
echo "=========================================================="
echo ""
echo "WARNING: This will rewrite all Git commit hashes."
echo "Collaborators and forks will need to re-clone the repository."
echo "Google Play Console upload key reset is also required."
echo ""

read -rp "Type 'YES-PURGE-HISTORY' to confirm and proceed: " CONFIRM
if [ "$CONFIRM" != "YES-PURGE-HISTORY" ]; then
    echo "Operation cancelled. No changes made."
    exit 0
fi

if command -v git-filter-repo >/dev/null 2>&1; then
    echo "Running git-filter-repo..."
    git filter-repo --invert-paths --path hilladventurer.keystore --force
else
    echo "git-filter-repo not found. Using git filter-branch fallback..."
    FILTER_BRANCH_SQUELCH_WARNING=1 git filter-branch --force --index-filter \
        "git rm --cached --ignore-unmatch hilladventurer.keystore" \
        --prune-empty --tag-name-filter cat -- --all
fi

echo "Purge complete locally."
echo "To publish the purged history to GitHub:"
echo "  git push origin --force --all"
echo "  git push origin --force --tags"
