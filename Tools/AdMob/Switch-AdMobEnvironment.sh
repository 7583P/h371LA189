#!/usr/bin/env bash
# ==============================================================================
# Script: Switch-AdMobEnvironment.sh
# Purpose: Toggle between Public Test AdMob IDs and Private Production IDs.
# ==============================================================================

set -euo pipefail

MODE="${1:-Test}"
TARGET="Assets/AnimeHillRacing/Resources/AdMobConfiguration.asset"
EXAMPLE="Assets/AnimeHillRacing/Resources/AdMobConfiguration.asset.example"
PRIVATE="Assets/AnimeHillRacing/Resources/AdMobConfiguration.private.asset"

if [ "$MODE" = "Test" ]; then
    echo "[AdMob] Switching to PUBLIC TEST mode..."
    if [ -f "$EXAMPLE" ]; then
        cp -f "$EXAMPLE" "$TARGET"
        echo "[AdMob] Successfully applied public test configuration (useTestAds=1)."
    else
        echo "Error: $EXAMPLE not found."
        exit 1
    fi
elif [ "$MODE" = "Production" ]; then
    echo "[AdMob] Switching to PRIVATE PRODUCTION mode..."
    if [ ! -f "$PRIVATE" ]; then
        echo "Creating untracked template $PRIVATE..."
        cp -f "$TARGET" "$PRIVATE"
        echo "Please edit $PRIVATE with your real production AdMob IDs."
        echo "NOTE: $PRIVATE is ignored by .gitignore."
        exit 0
    fi
    cp -f "$PRIVATE" "$TARGET"
    echo "[AdMob] Successfully applied private production configuration."
    echo "WARNING: Switch back to Test mode before committing!"
else
    echo "Usage: ./Switch-AdMobEnvironment.sh [Test|Production]"
    exit 1
fi
