using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Core;
using AnimeHillRacing.Pickups;
using AnimeHillRacing.Terrain;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public static class CityMapContentInstaller
    {
        private const string CityRoot = "Assets/Art/Backgrounds/City";
        private const string ProfilePath = "Assets/AnimeHillRacing/MapProfiles/City.asset";
        private const string EnvironmentPath = "Assets/AnimeHillRacing/MapProfiles/CityEnvironment.asset";
        private const string TerrainMaterialPath = "Assets/AnimeHillRacing/Materials/CityTerrain.mat";
        private const string GroundMaterialPath = "Assets/AnimeHillRacing/Materials/CityGround.mat";
        private const string CatalogPath = "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";
        private const string CoinRoutePath = "Assets/AnimeHillRacing/MapProfiles/CountrysideCoinRoute.asset";
        private const string GroundPhysicsPath = "Assets/AnimeHillRacing/Materials/TerrainStable.physicMaterial";

        [InitializeOnLoadMethod]
        private static void InstallWhenFirstAdded()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode ||
                    AssetDatabase.LoadAssetAtPath<MapProfile>(ProfilePath) != null ||
                    !File.Exists(CityRoot + "/Sky.png")) return;
                InstallOrRepairCity();
            };
        }

        [MenuItem("Anime Hill Racing/City/Install or Repair City Map")]
        public static void InstallOrRepairCity()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before installing City.");
                return;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSprite(CityRoot + "/Sky.png", 2048, TextureWrapMode.Clamp);
            ConfigureSprite(CityRoot + "/Background.png", 4096, TextureWrapMode.Repeat);
            ConfigureSprite(CityRoot + "/Terrain.png", 4096, TextureWrapMode.Repeat);
            ConfigureSprite(CityRoot + "/Ground.png", 4096, TextureWrapMode.Repeat);
            ConfigureSprite(CityRoot + "/Bridge.png", 4096, TextureWrapMode.Clamp);
            ConfigureSprite(CityRoot + "/Water.png", 4096, TextureWrapMode.Clamp);

            var clouds = new List<Sprite>(15);
            for (int i = 1; i <= 15; i++)
            {
                string path = $"{CityRoot}/Clouds/Cloud_{i:00}.png";
                ConfigureSprite(path, 2048, TextureWrapMode.Clamp);
                Sprite cloud = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (cloud != null) clouds.Add(cloud);
            }

            Sprite sky = AssetDatabase.LoadAssetAtPath<Sprite>(CityRoot + "/Sky.png");
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(CityRoot + "/Background.png");
            Sprite terrain = AssetDatabase.LoadAssetAtPath<Sprite>(CityRoot + "/Terrain.png");
            Sprite ground = AssetDatabase.LoadAssetAtPath<Sprite>(CityRoot + "/Ground.png");
            Sprite bridge = AssetDatabase.LoadAssetAtPath<Sprite>(CityRoot + "/Bridge.png");
            Sprite water = AssetDatabase.LoadAssetAtPath<Sprite>(CityRoot + "/Water.png");
            if (sky == null || background == null || terrain == null || ground == null || bridge == null ||
                water == null || clouds.Count != 15)
                throw new System.InvalidOperationException(
                    $"City source import is incomplete: sky={sky != null}, background={background != null}, " +
                    $"terrain={terrain != null}, ground={ground != null}, bridge={bridge != null}, " +
                    $"water={water != null}, clouds={clouds.Count}/15.");

            Material terrainMaterial = LoadOrCreateMaterial(TerrainMaterialPath, terrain.texture);
            Material groundMaterial = LoadOrCreateMaterial(GroundMaterialPath, ground.texture);
            EnvironmentDefinition environment = LoadOrCreate<EnvironmentDefinition>(EnvironmentPath);
            environment.environmentId = "city";
            environment.sky = sky;
            environment.mountains = background;
            environment.clouds = clouds.ToArray();
            environment.cloudParallaxX = .09f;
            environment.cloudSpacing = 19f;
            environment.cloudSpacingJitter = 4.5f;
            environment.cloudHeightRange = new Vector2(3.4f, 8.8f);
            environment.cloudVisualWidthRange = new Vector2(3.8f, 7.5f);
            environment.cloudDriftSpeedRange = new Vector2(.08f, .24f);
            environment.cloudOpacityRange = new Vector2(.56f, .9f);
            environment.cloudBobAmplitude = .22f;
            environment.cloudBobPeriodRange = new Vector2(18f, 34f);
            environment.pooledClouds = 8;
            environment.mountainParallaxX = .18f;
            environment.mountainParallaxY = .04f;
            environment.lockVerticalToCamera = true;
            environment.mountainVisualHeight = 15.5f;
            environment.mountainBaseY = 4.2f;
            environment.pooledMountainSegments = 5;
            environment.seamOverlap = .016f;
            environment.mirrorAlternatingMountainTiles = false;
            EditorUtility.SetDirty(environment);

            MapProfile profile = LoadOrCreate<MapProfile>(ProfilePath);
            profile.mapId = "city";
            profile.displayName = "CITY";
            profile.supportedGameMode = GameModeType.Adventure2_5D;
            profile.seed = 4152026;
            profile.unlockedByDefault = false;
            profile.coinPrice = 10000;
            profile.coinRoute = AssetDatabase.LoadAssetAtPath<CoinRouteDefinition>(CoinRoutePath);
            profile.elevationProfile = null;
            profile.baseHeight = 0f;
            profile.amplitude = 18f;
            profile.frequency = .012f;
            profile.smoothing = .72f;
            profile.hillSize = 1.15f;
            profile.useCityHighwayTerrain = true;
            profile.cityFeatureLength = 48f;
            profile.cityMiniRhythmStrength = .16f;
            profile.useCityBridges = true;
            profile.cityBridges ??= new CityBridgeSettings();
            profile.cityBridges.cycleLength = CityBridgeSettings.RequiredCycleLength;
            profile.cityBridges.minimumZonesPerCycle = CityBridgeSettings.RequiredMinimumZones;
            profile.cityBridges.maximumZonesPerCycle = CityBridgeSettings.RequiredMaximumZones;
            profile.cityBridges.minimumProgressiveZones = CityBridgeSettings.RequiredMinimumProgressiveZones;
            profile.cityBridges.maximumProgressiveZones = CityBridgeSettings.RequiredMaximumProgressiveZones;
            profile.cityBridges.cycleStartMargin = 320f;
            profile.cityBridges.cycleEndMargin = 320f;
            profile.cityBridges.minimumZoneSpacing = 300f;
            profile.cityBridges.approachBlendLength = 50f;
            profile.cityBridges.recoveryBlendLength = 50f;
            profile.cityBridges.bridgeLength = 14f;
            profile.cityBridges.normalWaterLength = 22f;
            profile.cityBridges.progressiveBaseWaterLength = 22f;
            profile.cityBridges.progressiveGrowthPerCycle = CityBridgeSettings.RequiredProgressiveGrowth;
            profile.cityBridges.waterVisualDepth = 10f;
            profile.cityBridges.waterSurfaceOffset = -.45f;
            profile.cityBridges.waterVisualAlpha = .5f;
            profile.cityBridges.waterBedThickness = 22f;
            profile.cityBridges.waterEndBankWidth = 3f;
            profile.cityBridges.waterSpeedRetainedPerSecond = .4f;
            profile.cityBridges.waterAngularSpeedRetainedPerSecond = .3f;
            profile.cityBridges.waterBuoyancySpring = 4f;
            profile.cityBridges.waterVerticalDamping = 3f;
            profile.cityBridges.waterMaximumSinkAcceleration = 8f;
            profile.cityBridges.waterDriverDownClearance = 5f;
            profile.cityBridges.bridgeColliderThickness = .45f;
            profile.cityBridges.bridgeGroundOverlap = 1f;
            profile.cityBridges.closedAngleDegrees = 0f;
            profile.cityBridges.openAngleDegrees = CityBridgeSettings.RequiredBridgeAngle;
            profile.cityBridges.transitionSeconds = CityBridgeSettings.RequiredTransitionSeconds;
            profile.cityBridges.closedHoldSeconds = 4f;
            profile.cityBridges.openHoldSeconds = 4f;
            profile.cityBridges.bridgeSprite = bridge;
            profile.cityBridges.waterSprite = water;
            profile.cityBridges.bridgeArtSlopeDegrees = 16.1f;
            profile.cityBridges.bridgeArtSurfaceOffset = -.61f;
            profile.cityBridges.waterSortingOrder = 1;
            profile.cityBridges.waterBedSortingOrder = 0;
            profile.cityBridges.bridgeSortingOrder = 5;
            profile.introductoryAmplitude = .38f;
            profile.introEndDistance = 525f;
            profile.easyEndDistance = 2250f;
            profile.normalEndDistance = 4950f;
            profile.hardEndDistance = 7500f;
            profile.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(.25f, .22f),
                new Keyframe(.60f, .62f), new Keyframe(1f, 1f));
            profile.maximumDifficulty = 1f;
            profile.sectionLength = 72f;
            profile.recoveryEverySections = 5;
            profile.technicalSectionChance = .42f;
            profile.technicalDetailStrength = .72f;
            profile.flatFeatureWeight = .02f;
            profile.hillFeatureWeight = .54f;
            profile.valleyFeatureWeight = .27f;
            profile.technicalFeatureWeight = .17f;
            profile.maxSlopeAngle = 34f;
            profile.maxHeightDeltaPerSample = Mathf.Tan(34f * Mathf.Deg2Rad) * (80f / 48f);
            profile.minimumValleyWidth = 18f;
            profile.minimumLandingLength = 16f;
            profile.maximumCrestSharpness = 7f;
            profile.smoothingPasses = 2;
            profile.initialFuelGap = 5f;
            profile.fuelGapIncrease = 5f;
            profile.fuelPlacementSearchRadius = 24f;
            profile.fuelPlacementSearchStep = 2f;
            profile.fuelMaximumPlacementSlope = 16f;
            profile.environment = environment;
            profile.terrainMaterial = terrainMaterial;
            profile.terrainEdgeMaterial = groundMaterial;
            profile.useSeparateTerrainMaterials = true;
            profile.fitAuthoredTerrainTexture = true;
            profile.terrainTextureRepeatWidth = TerrainGenerator.CompactGroundModuleWidth;
            profile.terrainWallUvRange = new Vector2(.171f, .82f);
            // Complete visible bounds of the long road tile in Ground.png. This removes only the
            // transparent canvas and alternate short tiles; no visible road pixel is cropped.
            profile.terrainSurfaceUvRange = new Vector2(.313f, .701f);
            profile.terrainSurfaceContactV = .579f;
            profile.terrainFillStartV = .579f;
            profile.groundPhysicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GroundPhysicsPath);
            profile.terrainEdgeWidth = .18f;
            profile.terrainMaximumVisualExtent = TerrainGenerator.CompactGroundMaximumVisualExtent;
            profile.terrainVisualDepth = 22f;
            profile.supportsLiquidExposure = false;
            EditorUtility.SetDirty(profile);

            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null) throw new System.InvalidOperationException("GameCatalog.asset is missing.");
            var maps = new List<MapDefinition>();
            if (catalog.maps != null)
                for (int i = 0; i < catalog.maps.Length; i++)
                    if (catalog.maps[i] != null && catalog.maps[i] != profile) maps.Add(catalog.maps[i]);
            int insertAt = 0;
            while (insertAt < maps.Count && maps[insertAt].supportedGameMode == GameModeType.Adventure2_5D)
                insertAt++;
            maps.Insert(insertAt, profile);
            catalog.maps = maps.ToArray();
            catalog.SortPurchasableContent();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateInstalledCity(profile, environment, catalog);
            Debug.Log("CITY_MAP_INSTALL_PASS: City, Highway terrain, 15 individual clouds, materials and selector catalog installed.");
        }

        private static void ValidateInstalledCity(MapProfile profile, EnvironmentDefinition environment,
            GameCatalog catalog)
        {
            float d0 = profile.EvaluateDifficulty(0f);
            float d3750 = profile.EvaluateDifficulty(3750f);
            float d7500 = profile.EvaluateDifficulty(7500f);
            float d12000 = profile.EvaluateDifficulty(12000f);
            if (!profile.useCityHighwayTerrain || profile.supportsLiquidExposure ||
                Mathf.Abs(profile.terrainFillStartV - profile.terrainSurfaceContactV) > .0001f ||
                Mathf.Abs(profile.terrainMaximumVisualExtent -
                          TerrainGenerator.CompactGroundMaximumVisualExtent) > .001f ||
                profile.terrainEdgeMaterial == null ||
                !profile.terrainEdgeMaterial.HasProperty("_DepthWrite") ||
                profile.terrainEdgeMaterial.GetFloat("_DepthWrite") < .5f ||
                !profile.terrainEdgeMaterial.HasProperty("_HorizontalInset") ||
                Mathf.Abs(profile.terrainEdgeMaterial.GetFloat("_HorizontalInset") - .008f) > .0001f ||
                !(d0 < d3750 && d3750 < d7500) || !Mathf.Approximately(d7500, d12000) ||
                environment.clouds == null || environment.clouds.Length != 15 ||
                !profile.useCityBridges || profile.cityBridges == null ||
                profile.cityBridges.bridgeSprite == null || profile.cityBridges.waterSprite == null ||
                catalog.FindMap("city", GameModeType.Adventure2_5D) != profile)
                throw new System.InvalidOperationException("City integration validation failed.");
            ValidateBridgeLayout(profile);
        }

        [MenuItem("Anime Hill Racing/City/Verify Bridge Layout (0-30 km)")]
        public static void VerifyBridgeLayout()
        {
            MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(ProfilePath);
            if (profile == null)
                throw new System.InvalidOperationException($"City profile is missing at {ProfilePath}.");
            ValidateBridgeLayout(profile);

            var cycleSummary = new List<string>(4);
            for (int cycle = 0; cycle <= 3; cycle++)
                cycleSummary.Add($"{cycle * 10} km: {TerrainGenerator.GetCityBridgeZoneCount(profile, cycle)} " +
                                 $"zones / {TerrainGenerator.GetCityProgressiveBridgeZoneCount(profile, cycle)} " +
                                 "progressive");
            Debug.Log("City bridge verification passed — " + string.Join(", ", cycleSummary));
        }

        private static void ValidateBridgeLayout(MapProfile profile)
        {
            if (profile == null || !profile.useCityBridges || profile.cityBridges == null ||
                profile.cityBridges.bridgeSprite == null || profile.cityBridges.waterSprite == null)
                throw new System.InvalidOperationException("City bridge assets or settings are missing.");
            CityBridgeSettings settings = profile.cityBridges;
            if (AssetDatabase.GetAssetPath(settings.bridgeSprite) != CityRoot + "/Bridge.png" ||
                AssetDatabase.GetAssetPath(settings.waterSprite) != CityRoot + "/Water.png" ||
                Mathf.Abs(settings.waterVisualDepth - 10f) > .001f ||
                Mathf.Abs(settings.waterVisualAlpha - .5f) > .001f ||
                settings.waterBedThickness + .01f < profile.terrainVisualDepth ||
                settings.waterEndBankWidth < 1f ||
                settings.waterSpeedRetainedPerSecond <= 0f ||
                settings.waterSpeedRetainedPerSecond >= 1f ||
                settings.waterAngularSpeedRetainedPerSecond <= 0f ||
                settings.waterAngularSpeedRetainedPerSecond >= 1f ||
                settings.waterBuoyancySpring <= 0f ||
                settings.waterVerticalDamping <= 0f ||
                settings.waterMaximumSinkAcceleration <= 0f ||
                Mathf.Abs(settings.waterDriverDownClearance - 5f) > .001f ||
                Vector2.Distance(profile.terrainSurfaceUvRange, new Vector2(.313f, .701f)) > .001f ||
                Mathf.Abs(profile.terrainSurfaceContactV - .579f) > .001f)
                throw new System.InvalidOperationException(
                    "City bridge visual references or water-bed geometry are invalid.");
            float normalLength = -1f;
            for (int cycle = 0; cycle <= 3; cycle++)
            {
                int count = TerrainGenerator.GetCityBridgeZoneCount(profile, cycle);
                int progressive = TerrainGenerator.GetCityProgressiveBridgeZoneCount(profile, cycle);
                if (count < 7 || count > 10 || progressive < 5 || progressive > 7)
                    throw new System.InvalidOperationException(
                        $"City bridge distribution failed in cycle {cycle}: {count} total, {progressive} progressive.");

                float previousEnd = cycle * CityBridgeSettings.RequiredCycleLength;
                int countedProgressive = 0;
                for (int slot = 0; slot < count; slot++)
                {
                    CityBridgeZoneLayout zone = TerrainGenerator.GetCityBridgeZone(profile, cycle, slot);
                    float separation = zone.approachStart - previousEnd;
                    if (separation < (slot == 0 ? 0f : settings.minimumZoneSpacing) - .01f)
                        throw new System.InvalidOperationException(
                            $"City bridge spacing failed in cycle {cycle}, slot {slot}: {separation:F2} m.");
                    if (!(zone.approachStart < zone.gapStart && zone.gapStart < zone.waterStart &&
                          zone.waterStart < zone.waterEnd && zone.waterEnd <= zone.gapEnd &&
                          zone.gapEnd < zone.recoveryEnd))
                        throw new System.InvalidOperationException(
                            $"City bridge geometry order failed in cycle {cycle}, slot {slot}.");
                    previousEnd = zone.recoveryEnd;
                    if (zone.isProgressive)
                    {
                        countedProgressive++;
                        float expected = profile.cityBridges.progressiveBaseWaterLength + cycle * 10f;
                        if (!Mathf.Approximately(zone.waterLength, expected) || zone.HasExitBridge)
                            throw new System.InvalidOperationException(
                                $"Progressive City bridge is invalid in cycle {cycle}, slot {slot}.");
                    }
                    else
                    {
                        if (normalLength < 0f) normalLength = zone.waterLength;
                        if (!Mathf.Approximately(zone.waterLength, normalLength) || !zone.HasExitBridge)
                            throw new System.InvalidOperationException(
                                $"Normal City bridge is invalid in cycle {cycle}, slot {slot}.");
                    }
                }
                if (countedProgressive != progressive)
                    throw new System.InvalidOperationException(
                        $"City progressive count mismatch in cycle {cycle}.");
                float cycleLimit = (cycle + 1) * CityBridgeSettings.RequiredCycleLength -
                                   settings.cycleEndMargin;
                if (previousEnd > cycleLimit + .01f)
                    throw new System.InvalidOperationException(
                        $"City bridge layout exceeds cycle {cycle}: {previousEnd:F2} > {cycleLimit:F2}.");
            }
            if (!Mathf.Approximately(profile.cityBridges.transitionSeconds, 3f) ||
                !Mathf.Approximately(profile.cityBridges.openAngleDegrees, 45f) ||
                !Mathf.Approximately(profile.cityBridges.closedAngleDegrees, 0f))
                throw new System.InvalidOperationException("City bridge animation contract is invalid.");
        }

        private static void ConfigureSprite(string path, int maximumSize, TextureWrapMode wrapMode)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = wrapMode;
            importer.maxTextureSize = maximumSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material LoadOrCreateMaterial(string path, Texture texture)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("AnimeHillRacing/Transparent Texture") ?? Shader.Find("Sprites/Default");
            if (material == null)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            material.mainTexture = texture;
            material.color = Color.white;
            if (material.HasProperty("_SeamlessHorizontalTiling"))
                material.SetFloat("_SeamlessHorizontalTiling", 1f);
            if (material.HasProperty("_HorizontalInset"))
                // Ground.png has a transparent antialiased gutter at both ends of the long road tile.
                // Sampling eleven pixels inward removes the repeating hairline without cropping road art.
                material.SetFloat("_HorizontalInset", path == GroundMaterialPath ? .008f : .012f);
            if (material.HasProperty("_HorizontalUvRange"))
                material.SetVector("_HorizontalUvRange", path == GroundMaterialPath
                    ? new Vector4(.004f, .688f, 0f, 0f) : new Vector4(0f, 1f, 0f, 0f));
            if (material.HasProperty("_DepthWrite"))
                // Ground contains opaque artwork above the physical yellow contact line. It must
                // write depth so the later transparent skyline cannot paint over that upper band.
                material.SetFloat("_DepthWrite", 1f);
            material.renderQueue = path == GroundMaterialPath ? 2451 : 2450;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
