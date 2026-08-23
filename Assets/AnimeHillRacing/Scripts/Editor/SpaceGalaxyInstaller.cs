using System;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Terrain;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class SpaceGalaxyInstaller
    {
        private const string SessionKey = "AHR.SpaceGalaxyInstaller.v1";
        private const string GalaxyPath = "Assets/Art/Backgrounds/Space/Space_Galaxy_Blue_v2.png";
        private const string EnvironmentPath = "Assets/AnimeHillRacing/MapProfiles/SpaceEnvironment.asset";
        private const string ProfilePath = "Assets/AnimeHillRacing/MapProfiles/Space.asset";

        [InitializeOnLoadMethod]
        private static void QueueInstall()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Anime Hill Racing/Content/Install Space Blue Galaxy")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(SessionKey, false);
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(GalaxyPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("The generated Space galaxy is missing.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            Sprite galaxy = AssetDatabase.LoadAssetAtPath<Sprite>(GalaxyPath);
            EnvironmentDefinition environment = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(EnvironmentPath);
            MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(ProfilePath);
            if (galaxy == null || environment == null || profile == null || profile.spaceChallenges == null)
                throw new InvalidOperationException("Space galaxy configuration assets are incomplete.");

            environment.sky = galaxy;
            environment.mountains = galaxy;
            environment.sun = null;
            environment.overrideCameraBackground = true;
            environment.cameraBackgroundColor = new Color(.002f, .012f, .07f, 1f);
            environment.useRelativeParallaxSpeeds = true;
            environment.coverViewportVertically = true;
            environment.starTwinkleAmount = 0f;

            profile.spaceChallenges.baseDiameter = 16f;
            profile.spaceChallenges.diameterGrowthPerGapMetre = .55f;
            profile.spaceChallenges.maximumDiameter = 80f;

            EditorUtility.SetDirty(environment);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            SpaceChallengeLayout first = TerrainGenerator.GetSpaceChallenge(profile, 1);
            SpaceChallengeLayout tenth = TerrainGenerator.GetSpaceChallenge(profile, 10);
            if (environment.earth == null || environment.sun != null ||
                first.blackHoleDiameter < first.gapLength + 5.99f ||
                tenth.blackHoleDiameter < tenth.gapLength + 5.99f)
                throw new InvalidOperationException("Space galaxy or black-hole coverage validation failed.");

            Debug.Log($"SPACE_GALAXY_INSTALL_OK|asset={GalaxyPath}|sun=removed|earth=kept|" +
                      $"void1={first.gapLength:0}/{first.blackHoleDiameter:0}|" +
                      $"void10={tenth.gapLength:0}/{tenth.blackHoleDiameter:0}");
        }
    }
}
