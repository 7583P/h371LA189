# Asset & Dependency Inventory

This document provides a comprehensive inventory of all software code, assets, packages, and dependencies in the **Hill Adventurer** repository, detailing provenance, file count, and applicable licensing terms.

---

## 1. Summary of Licensing Categories

```
┌─────────────────────────────────┬──────────────────────────────┬─────────────────────────────────────┐
│ Category                        │ Scope / Path                 │ Applied License                     │
├─────────────────────────────────┼──────────────────────────────┼─────────────────────────────────────┤
│ 1. Engine & Game Source Code    │ Assets/AnimeHillRacing/      │ MIT License                         │
│ 2. Offline Simulation Tooling   │ Tools/CompetitiveQA/         │ MIT License                         │
│ 3. Automated CI / Workflows     │ .github/workflows/           │ MIT License                         │
│ 4. Original 2D Artwork & UI     │ Assets/Art/                  │ Proprietary (All Rights Reserved)   │
│ 5. Trademarks & Brand Livery    │ "Hill Adventurer" / Studio   │ All Rights Reserved                 │
│ 6. Google Play Games Plugin     │ Assets/GooglePlayGames/      │ Apache License 2.0 (Google Inc.)    │
│ 7. Unity Engine Packages (UPM)  │ Packages/manifest.json       │ Unity Companion License / BSD-3     │
│ 8. Google Mobile Ads Plugin     │ OpenUPM / Packages/          │ Google Mobile Ads SDK License       │
└─────────────────────────────────┴──────────────────────────────┴─────────────────────────────────────┘
```

---

## 2. Detailed Breakdown by Directory

### A. Source Code (MIT License)
- **`Assets/AnimeHillRacing/Scripts/`** (452 C# files):
  - `Core/`: State persistence (`SaveManager`), upgrade progression, player prefs.
  - `Vehicle/`: 2D WheelJoint2D physics, engine torque, transmission, air-pitch dynamics (`VehicleController`, `VehicleRuntimeBase`).
  - `Racing3D/`: 3D raycast suspension physics, track telemetry, lap checkpoints, MMR tier classification (`RoadVehicleController`, `MmrTier`).
  - `Terrain/`: Procedural terrain generation and 5-segment parallax scrolling coordinators.
  - `UI/`: 3-layer live garage preview compositor and gameplay HUD controllers.
  - `Editor/`: Foundation auditor test runners (`RacingSystemAuditor`, `RacingClosureTests`).
- **`Tools/CompetitiveQA/`** (17 C# files):
  - Standalone .NET 10.0 CLI headless simulation suite for matchmaking balance, race telemetry, and economy validation.

### B. Original Artistic Media (Proprietary — All Rights Reserved)
- **`Assets/Art/`** (273 PNG files):
  - `Vehicles/`: 2D sprites and body textures for Jeep, Formula, Rally, Buggy, and Sport Bike.
  - `Characters/Stickman_Black/`: Modular body parts (head, torso, arms, legs) for custom pose construction.
  - `Backgrounds/`: Countryside, Mountains, Neon City, Rainyer, North Pole, Space layers.
  - `Interface/` & `Buttons/`: Custom UI buttons, progress bars, coin icons, distance bonus art.
  - `Obstacles/` & `Terrain/`: Ground tiles, road textures, hazard indicators.
- *Notice:* These assets are proprietary to Matías Games Dev. Open-source contributors may modify the code and run local builds, but may not commercialize or redistribute these assets in competing titles without written authorization.

### C. Third-Party Dependencies & Plugins
1. **Google Play Games Plugin for Unity (`Assets/GooglePlayGames/`)**:
   - Origin: Google Play Services Open Source Project
   - License: **Apache License 2.0**
   - Compliance: Apache 2.0 notice files and headers retained.
2. **Google Mobile Ads (`Packages/manifest.json` -> `com.google.ads.mobile: 11.4.0`)**:
   - Origin: Google LLC via OpenUPM registry
   - License: Google Mobile Ads Terms of Service & SDK License.
3. **Unity Built-in & Verified Packages (`Packages/manifest.json`)**:
   - `com.unity.inputsystem`: Unity Companion License
   - `com.unity.ugui`: BSD 3-Clause License
   - `com.unity.purchasing`: Unity Companion License
   - `com.unity.render-pipelines.universal` (URP): Unity EULA

---

## 3. Brand & Trademark Guidelines

The name **"Hill Adventurer"**, the logo, and the developer identity **"Matías Games Dev"** are trademarks of the project owner. Community forks, derivative tools, and academic studies must use distinct names and must not present themselves as official releases of the original game on digital storefronts (such as Google Play, App Store, or Steam).
