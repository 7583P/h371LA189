# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased] - Open Source Hardening & Preparation

### Added
- Standardized open-source repository documentation: `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CHANGELOG.md`.
- Technical architecture documentation (`Docs/ARCHITECTURE.md`) and comprehensive build guides (`Docs/BUILD_AND_RUN.md`).
- GitHub Actions CI workflow for headless tool compilation, file hygiene, and JSON schema validation (`.github/workflows/ci.yml`).
- Automated secret scanning workflow using Gitleaks (`.github/workflows/gitleaks.yml`).
- Dependabot configuration for GitHub Actions and NuGet dependencies (`.github/dependabot.yml`).
- Issue templates for bug reports and feature requests, and standard pull request template.
- Test template for AdMob configuration using Google public test ad unit IDs (`AdMobConfiguration.asset.example`).
- Comprehensive asset and intellectual property audit document (`Docs/ASSET_LICENSING.md`).

### Changed
- Hardened `.gitignore` to explicitly ignore Android signing keystores (`*.keystore`, `*.jks`), private certificates, environment files (`.env`), and local IDE artifacts.
- Untracked local release keystore from git index to prevent future exposure.
- Removed legacy git conflict artifacts (`.orig`, `.rej`, `.backup` files).

---

## [1.2.0] - 2026-10-20

### Added
- 3D Racing and Time Trial gameplay modes with dedicated 3D physics pipelines (`RoadVehicleController`).
- Procedural circuit builder modules: `TestCircuit3DBuilder`, `CanyonRushBuilder`, and `VerdantCircuitBuilder`.
- Competitive Matchmaking Rating (MMR) tier calculations and telemetry recording (`MmrTier.cs`).
- Headless simulation lab tool in .NET (`Tools/CompetitiveQA/CompetitiveQA.csproj`) for economy and rating calibration.
- In-editor automated test harness (`RacingSystemAuditor.cs`) for circuit geometry and physics invariants.

### Changed
- Refactored vehicle runtime architecture into shared base contracts (`VehicleRuntimeBase.cs`).
- Upgraded project to Unity 6000.3.13f1 and Universal Render Pipeline (URP).

---

## [1.1.0] - 2026-08-15

### Added
- Multi-layer Garage customization: dynamic vehicle skins, wheel variants, and modular driver pose system (`Stickman_Black`).
- Countryside procedural terrain generation with multi-layer recycled parallax background rendering (`CountrysideEnvironment`).
- Formula vehicle baseline with dedicated aerodynamic downforce and air-pitch torque control.

### Changed
- Separated physics bodies from visual renderers in vehicle prefabs.
- Optimized camera follow damping for high-speed hill navigation.

---

## [1.0.0] - 2026-06-01

### Added
- Core 2.5D physics hill climbing engine built on Unity 2D physics (`WheelJoint2D`).
- Engine torque curve, manual/automatic transmission modeling, and dynamic fuel consumption system.
- Save state management (`SaveManager.cs`) for coins, gems, and vehicle upgrade trees.
- Initial vehicle definitions: Jeep (`car-01`) and initial track obstacles.
