# Hill Adventurer

[![CI](https://github.com/7583P/h37iLA189/actions/workflows/ci.yml/badge.svg)](https://github.com/7583P/h37iLA189/actions/workflows/ci.yml)
[![Security Policy](https://img.shields.io/badge/security-policy-blue.svg)](SECURITY.md)
[![Unity Version](https://img.shields.io/badge/Unity-6000.3.13f1%20URP-black.svg?logo=unity)](https://unity.com/)
[![Target Platforms](https://img.shields.io/badge/Platforms-Android%20%7C%20Windows-green.svg)]()

**Hill Adventurer** is a physics-driven racing and hill-climbing game developed in **Unity 6 (URP)** by **Matías Games Dev**. It combines 2.5D suspension-based terrain climbing with full 3D competitive circuit racing and time trials, backed by a modular garage compositor and headless .NET simulation tooling.

---

## Key Features

### 🏎️ Dual Physics Engine Architecture
- **2.5D Adventure Hill Climbing**:
  - Realistic multi-link suspension powered by Unity 2D physics (`WheelJoint2D`).
  - Dynamic in-air pitch control (accelerator pitches nose-up, brake pitches nose-down).
  - Real-time engine torque curves, automatic transmission modeling, and dynamic fuel consumption.
  - Stunt and flip detection with landing stabilization.
- **3D Circuit Racing & Time Trials**:
  - Full 3D vehicle physics (`RoadVehicleController`) featuring raycast spring-damper suspension.
  - Lateral tire slip and aerodynamic downforce calculations.
  - Lap timing, sector checkpoints, and ghost telemetry recording.

### 🚗 Vehicle Catalog & Customization
- **Vehicles**: Jeep (`car-01`), Formula (`formula-01`), Rally, and Sport Bike configurations.
- **Modular 3-Layer Garage Compositor**:
  - **Skins**: Swappable liveries per vehicle.
  - **Wheels**: Front/rear rim and tire variants with dedicated friction properties.
  - **Driver Model**: Modular humanoid stickman (`Stickman_Black`) with per-vehicle seating and compact postures, head-strike triggers, and damped angular reactions.
- **Upgrade System**: Engine power, suspension travel, tire grip, and downforce progression.

### 🌄 Procedural Environments & Parallax
- **Countryside**: Procedural terrain mesh generation paired with a 5-segment recycled parallax background system (X parallax `0.20`, Y parallax `0.04`) to prevent visual gaps.
- **3D Tracks**: Procedural circuit builders for Grand Circuit, Canyon Rush, and Verdant Circuit.

### 🧪 Automated QA & Simulation Lab
- **`Tools/CompetitiveQA`**: Standalone .NET CLI suite (`net10.0`) that simulates race outcomes, rating migrations (MMR), and vehicle balance offline without requiring the Unity Editor.
- **In-Editor Test Auditors**: Automated validation suites verifying scene connections, prefab integrity, and physics contracts.

---

## Repository Structure

```
├── .github/                     # CI workflows, secret scanning, issue/PR templates
├── Assets/
│   ├── AnimeHillRacing/
│   │   ├── Resources/           # Configurations (AdMob test template, game settings)
│   │   ├── Scenes/              # Production scenes (MainMenu, Garage, Gameplay)
│   │   ├── Scripts/             # Game logic (Core, Vehicle, Terrain, Racing3D, UI)
│   │   └── VehicleData/         # ScriptableObject vehicle definitions
│   ├── Art/                     # 2D Sprites, UI atlases, character parts, textures
│   └── Plugins/Android/         # Android library manifests and build templates
├── Docs/                        # Architecture, Build guides, Licensing audit
├── Packages/                    # Unity Package Manager manifest and dependencies
├── ProjectSettings/             # Unity project, physics, and input settings
└── Tools/
    └── CompetitiveQA/           # Standalone .NET CLI simulation laboratory
```

---

## Quick Start

### 1. Requirements
- **Unity Editor**: `6000.3.13f1` with Universal Render Pipeline (URP).
- **Git LFS**: Required to fetch binary scene assets.
- **.NET SDK**: `10.0` (or `8.0`/`9.0`) for CLI tools.

### 2. Clone and Setup
```bash
# Clone with Git LFS
git lfs install
git clone https://github.com/7583P/h37iLA189.git
cd h37iLA189
git lfs pull
```

### 3. Setup Test Configuration
Copy the sample AdMob configuration using Google public test ad IDs:
```powershell
Copy-Item "Assets\AnimeHillRacing\Resources\AdMobConfiguration.asset.example" "Assets\AnimeHillRacing\Resources\AdMobConfiguration.asset" -Force
```

### 4. Open in Unity
1. Open **Unity Hub** and click **Add > Add project from disk**.
2. Select this project root folder and open with Unity `6000.3.13f1`.
3. Open `Assets/AnimeHillRacing/Scenes/MainMenu.unity` and click **Play**.

For detailed platform-specific compilation instructions, see **[Docs/BUILD_AND_RUN.md](Docs/BUILD_AND_RUN.md)**.

---

## Controls

| Action | Keyboard / Mouse | Gamepad | Mobile Touch |
|:---|:---|:---|:---|
| **Accelerate / Nose-Up** | `D` or `Right Arrow` | Right Trigger / `A` | Right Pedal Button |
| **Brake / Reverse / Nose-Down** | `A` or `Left Arrow` | Left Trigger / `X` | Left Pedal Button |
| **Pause Menu** | `Escape` | `Start` Button | Pause UI Icon |

---

## Verification & Automated Testing

### Headless CLI Lab
You can verify the economy and MMR simulation without launching Unity:
```bash
dotnet build Tools/CompetitiveQA/CompetitiveQA.csproj --configuration Release
dotnet run --project Tools/CompetitiveQA/CompetitiveQA.csproj
```

### In-Editor Test Harnesses
- **Anime Hill Racing > Validation > Run Racing 3D Foundation Tests**: Validates circuit builders and physics assertions.
- **Anime Hill Racing > Build All Game Scenes**: Verifies all scene connections and vehicle catalog assets.

---

## Documentation

- **[Architecture Guide](Docs/ARCHITECTURE.md)**: Deep dive into systems, physics separation, and scene graph.
- **[Build & Execution Guide](Docs/BUILD_AND_RUN.md)**: Compiling for Windows and Android, local signing setup.
- **[Asset Licensing & IP Audit](Docs/ASSET_LICENSING.md)**: Details on third-party libraries and proposed open-source licensing.
- **[Contributing Guide](CONTRIBUTING.md)**: Guidelines for opening PRs, code standards, and branching.
- **[Security Policy](SECURITY.md)**: Secret handling and vulnerability disclosure.
- **[Changelog](CHANGELOG.md)**: Historical release notes and progress.

---

## Security Notice

- **Android Signing Keys**: Release signing keys (`*.keystore`, `*.jks`) and sensitive environment variables are strictly excluded via `.gitignore`. Never commit signing credentials.
- **Secret Scanning**: All pull requests are verified via automated **Gitleaks** and CI hygiene jobs.

---

## Community & Contributing

Contributions, issues, and discussions are welcome! Please review our **[Contributing Guide](CONTRIBUTING.md)** and **[Code of Conduct](CODE_OF_CONDUCT.md)** before submitting pull requests.

---

## License & Legal Notices

This project utilizes a clear separation between open-source code and proprietary artistic assets:
- **Source Code**: Licensed under the canonical [MIT License](LICENSE).
- **Artistic Media & Trademarks**: All artwork under `Assets/Art/`, scene files, the title "Hill Adventurer", and "Matías Games Dev" branding are **Proprietary (All Rights Reserved)** as set forth in **[NOTICE.md](NOTICE.md)**.
- **Third-Party Packages**: Retain their respective original licenses (Apache 2.0, Unity Companion, etc.) as itemized in **[Docs/ASSET_INVENTORY.md](Docs/ASSET_INVENTORY.md)**.
