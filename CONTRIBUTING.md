# Contributing to Hill Adventurer

Thank you for your interest in contributing to **Hill Adventurer**! This guide outlines how to contribute effectively to our codebase, game mechanics, and test suites.

---

## Code of Conduct

All contributors and maintainers are expected to follow our [Code of Conduct](CODE_OF_CONDUCT.md). Please read it before participating.

---

## Getting Started

### Prerequisites
1. **Unity Editor**: Version `6000.3.13f1` (with Android Build Support, OpenJDK, and Android SDK/NDK if building for mobile).
2. **Git & Git LFS**: Ensure `git-lfs` is installed prior to cloning to ensure large binary assets (such as `MainMenu.unity`) are properly fetched:
   ```bash
   git lfs install
   git clone https://github.com/7583P/h37iLA189.git
   cd h37iLA189
   git lfs pull
   ```
3. **.NET SDK**: Version `10.0` (or `8.0`/`9.0`) to build CLI QA harnesses located under `Tools/CompetitiveQA/`.

---

## Branching & Workflow

1. Fork the repository on GitHub.
2. Clone your fork locally and create a descriptive branch:
   ```bash
   git checkout -b feature/improved-suspension-damping
   # or
   git checkout -b fix/time-trial-checkpoint-detection
   ```
3. Make your modifications cleanly, testing them thoroughly in Unity and via automated test runners.
4. Ensure no unwanted files, sensitive files, or large temporary caches are included:
   ```bash
   git status
   ```
5. Commit with concise, descriptive commit messages:
   ```bash
   git commit -m "Vehicle: Calibrate Formula air-control pitch damping"
   ```
6. Push to your fork and open a Pull Request against the `main` branch.

---

## Development & Architecture Rules

### 1. Gameplay & Mechanics Preservation
- Hill Adventurer features distinct physics engines:
  - **Adventure 2.5D**: Uses 2D Rigidbodies (`Rigidbody2D`) and WheelJoint2D components.
  - **Adventure 3D & TimeTrial 3D**: Uses 3D Rigidbodies (`Rigidbody`) and custom raycast/collider suspension calculations (`RoadVehicleController`).
- When tuning physics parameters or adding vehicles, do not compromise the responsive feel of existing vehicles (Jeep, Formula, Rally, Sport Bike).

### 2. Performance & Memory Discipline
- **Zero Allocations in Update Loops**: Avoid `new`, LINQ, or string concatenation inside `Update()` and `FixedUpdate()`.
- **Component Caching**: Cache `GetComponent<T>()` references in `Awake()` or `Start()`.
- **Particle & Mesh Recycling**: Parallax segments and track geometry must be recycled or pooled rather than instantiated dynamically during gameplay.

### 3. Separation of Configuration & Secrets
- Never commit release keystores (`*.keystore`), credentials, or production advertising tokens.
- All AdMob configurations in git must reference Google test unit IDs (see `Assets/AnimeHillRacing/Resources/AdMobConfiguration.asset.example`).
- Keep private configuration overrides in `.gitignore`.

---

## Automated QA & Test Suites

Before opening a pull request, run the automated validation suites:

### 1. Build and Run Competitive QA CLI
The headless laboratory simulates MMR progression, vehicle economy, and physics invariants:
```bash
dotnet build Tools/CompetitiveQA/CompetitiveQA.csproj --configuration Release
```

### 2. Unity In-Editor Validation
Inside Unity Editor 6000.3.13f1:
- Execute **Anime Hill Racing > Validation > Run Racing 3D Foundation Tests** to verify physics contracts and circuit builders.
- Run **Anime Hill Racing > Build All Game Scenes** to ensure scenes, prefabs, and vehicle definitions are connected without missing references.

---

## Pull Request Checklist

When submitting your PR, confirm the following:
- [ ] Code adheres to C# naming conventions and Unity conventions.
- [ ] No binary secrets, keystores, or production AdMob IDs are committed.
- [ ] `dotnet build Tools/CompetitiveQA/CompetitiveQA.csproj` compiles with 0 errors.
- [ ] Game scenes load cleanly without null-reference exceptions in console.
- [ ] Added or updated tests/fixtures if adding new mechanics or telemetry fields.
