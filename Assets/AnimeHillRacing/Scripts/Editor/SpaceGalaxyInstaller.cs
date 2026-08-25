using System;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Terrain;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class SpaceGalaxyInstaller
    {
        private const string SessionKey = "AHR.SpaceGalaxyInstaller.v8";
        private const string GalaxyPath = "Assets/Art/Backgrounds/Space/Space_Cosmic_Panorama_v4.png";
        private const string StarfieldPath = "Assets/Art/Backgrounds/Space/Space_Stars.png";
        private const string PanoramaShaderPath =
            "Assets/AnimeHillRacing/Shaders/CinematicSpacePanorama.shader";
        private const string EnvironmentPath = "Assets/AnimeHillRacing/MapProfiles/SpaceEnvironment.asset";
        private const string ProfilePath = "Assets/AnimeHillRacing/MapProfiles/Space.asset";

        [InitializeOnLoadMethod]
        private static void QueueInstall()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Anime Hill Racing/Content/Install Cinematic Space Universe")]
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
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            Sprite galaxy = AssetDatabase.LoadAssetAtPath<Sprite>(GalaxyPath);
            Sprite starfield = AssetDatabase.LoadAssetAtPath<Sprite>(StarfieldPath);
            Shader panoramaShader = AssetDatabase.LoadAssetAtPath<Shader>(PanoramaShaderPath);
            EnvironmentDefinition environment = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(EnvironmentPath);
            MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(ProfilePath);
            if (galaxy == null || starfield == null || panoramaShader == null || environment == null || profile == null ||
                profile.spaceChallenges == null)
                throw new InvalidOperationException("Space galaxy configuration assets are incomplete.");

            profile.unlockedByDefault = false;
            profile.coinPrice = 40000;

            environment.sky = starfield;
            environment.mountains = galaxy;
            environment.sun = null;
            environment.earth = null;
            environment.overrideCameraBackground = true;
            environment.cameraBackgroundColor = new Color(.001f, .002f, .012f, 1f);
            environment.useRelativeParallaxSpeeds = true;
            environment.coverViewportVertically = true;
            environment.lockVerticalToCamera = true;
            environment.pooledMountainSegments = 3;
            environment.mirrorAlternatingMountainTiles = true;
            environment.useCinematicSpacePanorama = true;
            environment.cinematicSpacePanoramaShader = panoramaShader;
            environment.spacePanoramaOverscan = 1.04f;
            environment.spacePanoramaScrollSpeed = .075f;
            environment.mirrorSpacePanoramaTiles = true;
            environment.spaceNebulaFlowStrength = .0018f;
            environment.spaceNebulaFlowSpeed = .075f;
            environment.spaceNebulaPulse = .055f;
            environment.starTwinkleSpeed = 1.65f;
            environment.starTwinkleAmount = .34f;
            environment.enableShootingStars = true;
            environment.shootingStarIntervalRange = new Vector2(3.4f, 7.6f);
            environment.shootingStarSpeedRange = new Vector2(20f, 34f);
            environment.shootingStarTrailSeconds = .72f;
            environment.shootingStarPoolSize = 4;

            profile.spaceChallenges.baseDiameter = profile.spaceChallenges.initialGapLength;
            profile.spaceChallenges.diameterGrowthPerGapMetre = 1f;
            profile.spaceChallenges.maximumDiameter = 500f;

            EditorUtility.SetDirty(environment);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            SpaceChallengeLayout first = TerrainGenerator.GetSpaceChallenge(profile, 1);
            SpaceChallengeLayout mercury = TerrainGenerator.GetSpaceChallenge(profile, 2);
            SpaceChallengeLayout venus = TerrainGenerator.GetSpaceChallenge(profile, 3);
            SpaceChallengeLayout tenth = TerrainGenerator.GetSpaceChallenge(profile, 10);
            float firstTop = SpaceGapEvent.CalculateBlackHoleCenterY(
                                 profile.spaceChallenges.verticalOffset, first.gapLength) +
                             SpaceGapEvent.CalculateBlackHoleVisualHeight(first.gapLength) * .5f;
            if (environment.earth != null || environment.sun != null ||
                !environment.useCinematicSpacePanorama || environment.cinematicSpacePanoramaShader == null ||
                !environment.enableShootingStars ||
                environment.starTwinkleAmount < .25f ||
                AssetDatabase.GetAssetPath(environment.sky) != StarfieldPath ||
                AssetDatabase.GetAssetPath(environment.mountains) != GalaxyPath ||
                !first.IsVoid || !mercury.IsPlanet || mercury.planetType != SpacePlanetType.Mercury ||
                !venus.IsPlanet || venus.planetType != SpacePlanetType.Venus ||
                Mathf.Abs(first.gapLength - 15f) > .01f || Mathf.Abs(tenth.gapLength - 75f) > .01f ||
                Mathf.Abs(first.blackHoleDiameter - first.gapLength) > .01f ||
                Mathf.Abs(tenth.blackHoleDiameter - tenth.gapLength) > .01f || firstTop > -.19f)
                throw new InvalidOperationException("Space galaxy or black-hole coverage validation failed.");

            Debug.Log($"SPACE_UNIVERSE_INSTALL_OK|asset={GalaxyPath}|twinklingStars=56|shootingStars=cinematic|" +
                      $"sun=removed|earth=removed|panorama=seamless-scroll|void1={first.gapLength:0}/{first.blackHoleDiameter:0}|" +
                      $"void10={tenth.gapLength:0}/{tenth.blackHoleDiameter:0}|planets=Mercury-Venus|" +
                      $"top={firstTop:0.00}");
        }
    }
}
