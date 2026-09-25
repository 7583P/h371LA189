# Google Play icon for Hill Adventurer

Google Play rejected the release because the Play listing icon and the installed Android icon differed. The existing `Hill Adventurer 1.0.2.aab` still contains Unity's default cube launcher icon.

The single artwork source is `Assets/Branding/HillAdventurerIcon.png` (512 × 512, 32-bit PNG, under 1 MB). Android adaptive, round, and legacy icon slots all use this file. The build guard checks the icon, package ID, and installed app name before an Android build.

Validation: a non-development Android APK built from a temporary copy of this project passed the icon guard. Its extracted legacy and adaptive launcher resources show the red racing car. Android package metadata reports `com.matias.hilladventurer`, app label `Hill Adventurer`, and version code 19. This validation APK uses debug signing and is not the release AAB for Play Console.

To submit the correction:

1. In Unity, run **Anime Hill Racing > Publishing > Verify Google Play Icon**.
2. Build a new signed AAB with a version code higher than the last uploaded release. The current project setting is 19; increase it if Play Console has already received version 19. Do not upload an older AAB, since its embedded icon is still the Unity default.
3. In Play Console, upload `Assets/Branding/HillAdventurerIcon.png` as the high-resolution icon in the default store listing and every custom or translated listing that has its own icon, including en-US.
4. Install the new build on a device and confirm the launcher shows the same red racing car artwork and the name **Hill Adventurer**. Then send the changes for review.

If Unity resets an icon slot, run **Anime Hill Racing > Publishing > Apply Google Play Icon** and verify again.
