using System;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Terrain;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class SpaceGalaxyInstaller
    {
        private const string SessionKey = "AHR.SpaceGalaxyInstaller.v10";
        private const string GalaxyPath = "Assets/Art/Backgrounds/Space/Space_Cosmic_Panorama_v4.png";
        private const string StarfieldPath = "Assets/Art/Backgrounds/Space/Space_Stars.png";
        private const string GroundPath = "Assets/Art/Backgrounds/Space/Ground.png";
        private const string TerrainPath = "Assets/Art/Backgrounds/Space/Terrain.png";
        private const string GroundMaterialPath = "Assets/AnimeHillRacing/Materials/SpaceGround.mat";
        private const string TerrainMaterialPath = "Assets/AnimeHillRacing/Materials/SpaceTerrain.mat";
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

            TextureImporter groundImporter = AssetImporter.GetAtPath(GroundPath) as TextureImporter;
            if (groundImporter == null) throw new InvalidOperationException("The authored Space Ground.png is missing.");
            groundImporter.textureType = TextureImporterType.Default;
            groundImporter.alphaIsTransparency = true;
            groundImporter.mipmapEnabled = false;
            groundImporter.wrapMode = TextureWrapMode.Repeat;
            groundImporter.filterMode = FilterMode.Bilinear;
            groundImporter.maxTextureSize = 4096;
            groundImporter.SaveAndReimport();

            TextureImporter terrainImporter = AssetImporter.GetAtPath(TerrainPath) as TextureImporter;
            if (terrainImporter == null) throw new InvalidOperationException("The Space Terrain.png is missing.");
            terrainImporter.textureType = TextureImporterType.Default;
            terrainImporter.alphaIsTransparency = false;
            terrainImporter.mipmapEnabled = true;
            terrainImporter.wrapMode = TextureWrapMode.Mirror;
            terrainImporter.filterMode = FilterMode.Bilinear;
            terrainImporter.maxTextureSize = 2048;
            terrainImporter.SaveAndReimport();

            Sprite galaxy = AssetDatabase.LoadAssetAtPath<Sprite>(GalaxyPath);
            Sprite starfield = AssetDatabase.LoadAssetAtPath<Sprite>(StarfieldPath);
            Shader panoramaShader = AssetDatabase.LoadAssetAtPath<Shader>(PanoramaShaderPath);
            EnvironmentDefinition environment = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(EnvironmentPath);
            MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(ProfilePath);
            Texture2D ground = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundPath);
            Texture2D terrain = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainPath);
            Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            if (galaxy == null || starfield == null || panoramaShader == null || environment == null || profile == null ||
                profile.spaceChallenges == null || ground == null || terrain == null || groundMaterial == null ||
                terrainMaterial == null)
                throw new InvalidOperationException("Space galaxy configuration assets are incomplete.");

            Shader groundShader = Shader.Find("AnimeHillRacing/Transparent Texture") ?? Shader.Find("Sprites/Default");
            if (groundShader == null) throw new InvalidOperationException("The transparent terrain shader is missing.");
            groundMaterial.shader = groundShader;
            groundMaterial.mainTexture = ground;
            groundMaterial.color = Color.white;
            if (groundMaterial.HasProperty("_SeamlessHorizontalTiling"))
                groundMaterial.SetFloat("_SeamlessHorizontalTiling", 1f);
            if (groundMaterial.HasProperty("_HorizontalInset")) groundMaterial.SetFloat("_HorizontalInset", .004f);
            if (groundMaterial.HasProperty("_HorizontalUvRange"))
                groundMaterial.SetVector("_HorizontalUvRange", new Vector4(.05f, .95f, 0f, 0f));
            if (groundMaterial.HasProperty("_DepthWrite")) groundMaterial.SetFloat("_DepthWrite", 1f);
            groundMaterial.renderQueue = 2451;

            terrainMaterial.shader = groundShader;
            terrainMaterial.mainTexture = terrain;
            terrainMaterial.color = Color.white;
            if (terrainMaterial.HasProperty("_SeamlessHorizontalTiling"))
                terrainMaterial.SetFloat("_SeamlessHorizontalTiling", 1f);
            if (terrainMaterial.HasProperty("_HorizontalInset"))
                terrainMaterial.SetFloat("_HorizontalInset", .004f);
            if (terrainMaterial.HasProperty("_HorizontalUvRange"))
                terrainMaterial.SetVector("_HorizontalUvRange", new Vector4(0f, 1f, 0f, 0f));
            if (terrainMaterial.HasProperty("_DepthWrite")) terrainMaterial.SetFloat("_DepthWrite", 1f);
            terrainMaterial.renderQueue = 2450;

            profile.terrainMaterial = terrainMaterial;
            profile.terrainEdgeMaterial = groundMaterial;
            profile.useSeparateTerrainMaterials = true;
            profile.fitAuthoredTerrainTexture = true;
            profile.terrainTextureRepeatWidth = TerrainGenerator.SpaceGroundModuleWidth;
            profile.terrainWallUvRange = new Vector2(0f, 1f);
            profile.terrainSurfaceUvRange = new Vector2(.177f, .714f);
            profile.terrainSurfaceContactV = .56f;
            profile.terrainSurfaceContactVSamples = new float[0];
            // The Terrain wall overlaps behind Ground from the painted driving line downward,
            // following the proven City/North Pole composition contract.
            profile.terrainFillStartV = profile.terrainSurfaceContactV;
            profile.terrainMaximumVisualExtent = TerrainGenerator.SpaceGroundMaximumVisualExtent;
            profile.terrainMaximumVisualRiseAboveSurface = TerrainGenerator.SpaceGroundMaximumVisualRise;

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
            profile.spaceChallenges.gapIncreasePerCycle = 5f;
            profile.spaceChallenges.diameterGrowthPerGapMetre = 1f;
            profile.spaceChallenges.maximumDiameter = 500f;

            EditorUtility.SetDirty(environment);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(groundMaterial);
            EditorUtility.SetDirty(terrainMaterial);
            AssetDatabase.SaveAssets();

            SpaceChallengeLayout first = TerrainGenerator.GetSpaceChallenge(profile, 1);
            SpaceChallengeLayout second = TerrainGenerator.GetSpaceChallenge(profile, 2);
            SpaceChallengeLayout third = TerrainGenerator.GetSpaceChallenge(profile, 3);
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
                profile.terrainMaterial != terrainMaterial || profile.terrainEdgeMaterial != groundMaterial ||
                terrainMaterial.mainTexture != terrain || groundMaterial.mainTexture != ground ||
                !profile.fitAuthoredTerrainTexture ||
                profile.terrainSurfaceContactVSamples == null ||
                profile.terrainSurfaceContactVSamples.Length != 0 ||
                Mathf.Abs(profile.terrainFillStartV - profile.terrainSurfaceContactV) > .0001f ||
                Mathf.Abs(profile.terrainTextureRepeatWidth - TerrainGenerator.SpaceGroundModuleWidth) > .001f ||
                Mathf.Abs(profile.terrainMaximumVisualExtent -
                          TerrainGenerator.SpaceGroundMaximumVisualExtent) > .001f ||
                Mathf.Abs(profile.terrainMaximumVisualRiseAboveSurface -
                          TerrainGenerator.SpaceGroundMaximumVisualRise) > .001f ||
                Vector2.Distance(profile.terrainSurfaceUvRange, new Vector2(.177f, .714f)) > .001f ||
                Mathf.Abs(profile.terrainSurfaceContactV - .56f) > .001f ||
                ground.width != 2172 || ground.height != 724 ||
                !first.IsVoid || !second.IsVoid || !third.IsVoid || !tenth.IsVoid ||
                Mathf.Abs(first.eventDistance - 2000f) > .01f ||
                Mathf.Abs(second.eventDistance - 4000f) > .01f ||
                Mathf.Abs(first.gapLength - 15f) > .01f || Mathf.Abs(second.gapLength - 20f) > .01f ||
                Mathf.Abs(third.gapLength - 25f) > .01f || Mathf.Abs(tenth.gapLength - 60f) > .01f ||
                Mathf.Abs(first.blackHoleDiameter - first.gapLength) > .01f ||
                Mathf.Abs(tenth.blackHoleDiameter - tenth.gapLength) > .01f || firstTop > -.19f)
                throw new InvalidOperationException("Space galaxy or black-hole coverage validation failed.");

            Debug.Log($"SPACE_UNIVERSE_INSTALL_OK|asset={GalaxyPath}|twinklingStars=56|shootingStars=cinematic|" +
                      $"sun=removed|earth=removed|panorama=seamless-scroll|void1={first.gapLength:0}/{first.blackHoleDiameter:0}|" +
                      $"void10={tenth.gapLength:0}/{tenth.blackHoleDiameter:0}|interval=2000|growth=5|" +
                      $"top={firstTop:0.00}");
        }
    }
}
