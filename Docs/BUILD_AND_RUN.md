# Build and Execution Guide

This guide provides step-by-step instructions for setting up, compiling, and running **Hill Adventurer** on desktop and mobile platforms.

---

## 1. Prerequisites

### Required Software
1. **Unity Hub & Unity Editor `6000.3.13f1`**
   - Universal Render Pipeline (URP) support (bundled).
   - *For Android builds:* Install the **Android Build Support** module, along with **Android SDK & NDK Tools** and **OpenJDK**.
2. **Git & Git Large File Storage (Git LFS)**
   - Required to download large binary scene assets (e.g., `MainMenu.unity`).
3. **.NET SDK `10.0` (or `8.0`/`9.0`)**
   - Required for building and running the offline simulation and QA CLI tools.

---

## 2. Initial Setup

### A. Clone the Repository
```bash
# Ensure Git LFS is enabled
git lfs install

# Clone the repository
git clone https://github.com/7583P/h37iLA189.git
cd h37iLA189

# Verify all LFS assets are pulled
git lfs pull
```

### B. Configure Local AdMob Test Settings
To ensure development builds never trigger invalid ad requests or require private credentials, copy the example test configuration:
```powershell
Copy-Item "Assets\AnimeHillRacing\Resources\AdMobConfiguration.asset.example" "Assets\AnimeHillRacing\Resources\AdMobConfiguration.asset" -Force
```
This configuration uses Google's standard public test IDs (`ca-app-pub-3940256099942544~3347511713`) with `useTestAds: 1`.

---

## 3. Running in Unity Editor

1. Open **Unity Hub** and select **Add > Add project from disk**.
2. Select the repository root folder.
3. Open the project with Unity `6000.3.13f1`.
4. In the Project window, navigate to `Assets/AnimeHillRacing/Scenes/` and double-click **`MainMenu.unity`**.
5. Click the **Play** button in the Unity Editor toolbar.

### Controls:
- **Accelerate / Nose-Up in Air**: `D` / `Right Arrow` / Gamepad Right Trigger
- **Brake / Reverse / Nose-Down in Air**: `A` / `Left Arrow` / Gamepad Left Trigger
- **Pause Menu**: `Escape` / Gamepad Start
- **Mobile Touch**: On-screen pedal buttons are enabled by default on touch displays and mobile viewports.

---

## 4. Building Windows Standalone

1. In Unity Editor, open **File > Build Profiles** (or **Build Settings**).
2. Ensure the following scenes are included in order:
   - `Assets/AnimeHillRacing/Scenes/MainMenu.unity`
   - `Assets/AnimeHillRacing/Scenes/Garage.unity`
   - `Assets/AnimeHillRacing/Scenes/PreStart.unity`
   - `Assets/AnimeHillRacing/Scenes/Adventure2_5D.unity`
   - `Assets/AnimeHillRacing/Scenes/Adventure3D.unity`
   - `Assets/AnimeHillRacing/Scenes/TimeTrial3D.unity`
3. Target Platform: **Windows, Mac, Linux** -> Architecture: **Intel 64-bit**.
4. Click **Build**, select your output directory (e.g. `Builds/Windows/`).
5. Run the generated executable (`Hill Adventurer.exe`).

---

## 5. Building Android APK / App Bundle (AAB)

### A. Safe Keystore Setup (Local Only)
> [!IMPORTANT]
> **Never commit signing keys to git.** The `.gitignore` file is pre-configured to ignore all `*.keystore` and `*.jks` files.

To generate your own local release or debug key:
```bash
keytool -genkey -v -keystore local-signing.keystore -alias local-alias -keyalg RSA -keysize 2048 -validity 10000
```
Store this keystore in a private folder outside the repository or in the root (where `.gitignore` will keep it untracked).

### B. Configure Signing in Unity
1. In Unity, go to **Edit > Project Settings > Player > Android tab > Publishing Settings**.
2. Check **Custom Keystore**.
3. Select your local keystore file and enter the alias and password.
4. If testing development builds, you may leave Custom Keystore unchecked to use Unity's default debug keystore.

### C. Build the Package
1. Open **File > Build Settings**.
2. Select **Android** and click **Switch Platform**.
3. For Google Play release: Check **Build App Bundle (Google Play)**.
4. For direct testing: Leave unchecked to output a standalone `.apk`.
5. Click **Build**.

---

## 6. Running Headless CLI & Test Lab

The project contains a standalone headless testing suite in `Tools/CompetitiveQA/` that executes without launching the Unity Editor:

```bash
# Build the tool
dotnet build Tools/CompetitiveQA/CompetitiveQA.csproj --configuration Release

# Run the test lab
dotnet run --project Tools/CompetitiveQA/CompetitiveQA.csproj
```
This tests economy balance, MMR tier migration, and numerical safety across vehicle physics configurations.
