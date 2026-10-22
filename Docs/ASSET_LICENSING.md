# Intellectual Property & Asset Licensing Audit

This document provides a thorough audit of all software components, packages, artistic assets, and third-party dependencies in **Hill Adventurer**, accompanied by a dual-licensing recommendation for open-source transition.

---

## 1. Inventory of Repository Components

| Category | Component / Location | Count / Format | Current Origin / Status |
|:---|:---|:---:|:---|
| **C# Source Code** | `Assets/AnimeHillRacing/Scripts/` | 452 `.cs` files | Original codebase developed by Matías Games Dev |
| **CLI QA Tooling** | `Tools/CompetitiveQA/` | 17 `.cs` files | Original .NET simulation and verification lab |
| **2D Art & Sprites** | `Assets/Art/` | 273 `.png` files | Original sprites (Vehicles, Characters, UI, Backgrounds) |
| **3D Shaders & Materials** | `Assets/AnimeHillRacing/` | 25 `.shader`, 52 `.mat` | URP shaders and materials |
| **Google Play Games** | `Assets/GooglePlayGames/` | Plugin package | Apache License 2.0 (Google Inc.) |
| **Google Mobile Ads** | `Packages/manifest.json` (`com.google.ads.mobile`) | UPM Package | Google Mobile Ads SDK License |
| **Unity Packages** | `com.unity.inputsystem`, `com.unity.ugui`, etc. | UPM Packages | Unity Companion License / BSD-3 |
| **Audio & Music** | `Assets/` | 0 raw files | Built-in sound synthesis or dynamic audio triggers |

---

## 2. Identification of Third-Party Dependencies

1. **Google Play Games Plugin for Unity (`Assets/GooglePlayGames`)**:
   - Upstream: [playgameservices/play-games-plugin-for-unity](https://github.com/playgameservices/play-games-plugin-for-unity)
   - License: **Apache License 2.0**.
   - Redistribution: Permitted under Apache 2.0 terms with copyright notices retained.

2. **Google Mobile Ads Unity Plugin (`com.google.ads.mobile`)**:
   - Managed via OpenUPM / Unity Package Manager.
   - Redistributed via package manifest reference, not bundled source tree.

3. **Unity Engine Modules & URP**:
   - Standard Unity packages governed by the Unity Software End User License Agreement (EULA) and Unity Companion License.
   - Permitted for distribution in source form as standard Unity projects.

---

## 3. Brand Assets & Proprietary Elements

The following items are proprietary trademarks and creative assets belonging to **Matías Games Dev**:
- The project title **"Hill Adventurer"**.
- The studio branding and developer identity **"Matías Games Dev"**.
- Original vehicle livery designs, character sprites (`Stickman_Black`), and UI atlas graphics.

---

## 4. Proposed Open Source Licensing Strategy

To protect the commercial integrity of the game on mobile app stores while enabling community collaboration and eligibility for open-source grants (such as OpenAI Codex for Open Source), we propose a **Separated (Dual) Licensing Model**, identical to the industry standard used by Godot projects and open-source games (e.g., Blender open games, OpenRA):

### A. Source Code License: **MIT License** (Recommended)
- **Scope**: All C# code under `Assets/AnimeHillRacing/Scripts/`, `Tools/CompetitiveQA/`, and automation scripts.
- **Why MIT?**
  - Universally recognized by OSI (Open Source Initiative) and the Free Software Foundation (FSF).
  - Unambiguously eligible for the OpenAI Codex for Open Source program.
  - Highly permissive, allowing developers to study, fork, and build educational tools with minimal friction.

### B. Game Assets & Media License: **CC-BY-NC 4.0** or **Proprietary All Rights Reserved**
- **Scope**: All `.png` sprites, textures, materials, and scene arrangements in `Assets/Art/` and `Assets/AnimeHillRacing/Scenes/`.
- **Option 1 (Recommended for maximum protection): Proprietary / All Rights Reserved for Media**.
  - Anyone can clone the repo and compile/modify the engine for personal learning, development, and PR contributions.
  - No third party may redistribute or re-publish the game under the same visual branding on Google Play or Steam.
- **Option 2 (Creative Commons CC-BY-NC-SA 4.0)**:
  - Community members may share and remix assets non-commercially with attribution, but commercial cloning remains strictly prohibited.

---

## 5. Status: Pending Maintainer Approval

> [!NOTE]
> Per the project instructions, no open-source license file (`LICENSE`) has been applied yet. The maintainer can select between the proposals above before committing a final `LICENSE` file.
