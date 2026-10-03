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
    public static class RainyerMapContentInstaller
    {
        private const string ArtRoot = "Assets/Art/Backgrounds/Rainyer";
        private const string ProfilePath = "Assets/AnimeHillRacing/MapProfiles/Rainyer.asset";
        private const string EnvironmentPath = "Assets/AnimeHillRacing/MapProfiles/RainyerEnvironment.asset";
        private const string TerrainMaterialPath = "Assets/AnimeHillRacing/Materials/RainyerTerrain.mat";
        private const string GroundMaterialPath = "Assets/AnimeHillRacing/Materials/RainyerGround.mat";
        private const string WetPhysicsPath = "Assets/AnimeHillRacing/Materials/RainyerWetGround.physicMaterial";
        private const string CatalogPath = "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";
        private const string CoinRoutePath = "Assets/AnimeHillRacing/MapProfiles/CountrysideCoinRoute.asset";

        [InitializeOnLoadMethod]
        private static void InstallWhenAssetsArrive()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode ||
                    AssetDatabase.LoadAssetAtPath<MapProfile>(ProfilePath) != null ||
                    !File.Exists(ArtRoot + "/Sky.png") || !File.Exists(ArtRoot + "/Background.png") ||
                    !File.Exists(ArtRoot + "/Ground.png") || !File.Exists(ArtRoot + "/Terrain.png")) return;
                InstallOrRepairRainyer();
            };
        }

        [MenuItem("Anime Hill Racing/Rainyer/Install or Repair Rainyer Map")]
        public static void InstallOrRepairRainyer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before installing Rainyer.");
                return;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSprite(ArtRoot + "/Sky.png", 2048, TextureWrapMode.Clamp);
            ConfigureSprite(ArtRoot + "/Background.png", 4096, TextureWrapMode.Repeat);
            ConfigureSprite(ArtRoot + "/Ground.png", 4096, TextureWrapMode.Repeat);
            ConfigureSprite(ArtRoot + "/Terrain.png", 2048, TextureWrapMode.Repeat);

            Sprite sky = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Sky.png");
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Background.png");
            Sprite ground = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Ground.png");
            Sprite terrain = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Terrain.png");
            if (sky == null || background == null || ground == null || terrain == null)
                throw new System.InvalidOperationException("Rainyer sprite import is incomplete.");

            Material terrainMaterial = LoadOrCreateMaterial(TerrainMaterialPath, terrain.texture, 2450);
            Material groundMaterial = LoadOrCreateMaterial(GroundMaterialPath, ground.texture, 2451);
            PhysicsMaterial wetGround = LoadOrCreateWetPhysics();

            EnvironmentDefinition environment = LoadOrCreate<EnvironmentDefinition>(EnvironmentPath);
            environment.environmentId = "rainyer";
            environment.sky = sky;
            environment.mountains = background;
            environment.clouds = null;
            environment.mountainParallaxX = .16f;
            environment.mountainParallaxY = .035f;
            environment.lockVerticalToCamera = true;
            environment.coverViewportVertically = true;
            environment.mountainVisualHeight = 18.5f;
            environment.mountainBaseY = 5.1f;
            environment.pooledMountainSegments = 5;
            environment.seamOverlap = .008f;
            environment.mirrorAlternatingMountainTiles = true;
            EditorUtility.SetDirty(environment);

            MapProfile profile = LoadOrCreate<MapProfile>(ProfilePath);
            profile.mapId = "rainyer";
            profile.displayName = "RAINYER";
            profile.unlockedByDefault = false;
            profile.coinPrice = 30000;
            profile.supportedGameMode = GameModeType.Adventure2_5D;
            profile.seed = 8152026;
            profile.coinRoute = AssetDatabase.LoadAssetAtPath<CoinRouteDefinition>(CoinRoutePath);
            profile.elevationProfile = null;
            profile.baseHeight = 0f;
            profile.amplitude = 27f;
            profile.frequency = .022f;
            profile.smoothing = .54f;
            profile.hillSize = .82f;
            profile.useCityHighwayTerrain = false;
            profile.introductoryAmplitude = .34f;
            profile.introEndDistance = 390f;
            profile.easyEndDistance = 1575f;
            profile.normalEndDistance = 3600f;
            profile.hardEndDistance = 6300f;
            profile.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(.25f, .34f),
                new Keyframe(.60f, .78f), new Keyframe(1f, 1.2f));
            profile.maximumDifficulty = 1.12f;
            profile.sectionLength = 48f;
            profile.recoveryEverySections = 6;
            profile.technicalSectionChance = .68f;
            profile.technicalDetailStrength = .94f;
            profile.flatFeatureWeight = .01f;
            profile.hillFeatureWeight = .45f;
            profile.valleyFeatureWeight = .25f;
            profile.technicalFeatureWeight = .29f;
            profile.maxSlopeAngle = 40f;
            profile.maxHeightDeltaPerSample = Mathf.Tan(40f * Mathf.Deg2Rad) * (80f / 48f);
            profile.minimumValleyWidth = 16f;
            profile.minimumLandingLength = 14f;
            profile.maximumCrestSharpness = 8f;
            profile.smoothingPasses = 1;
            profile.coinDensityPer100m = 9f;
            profile.initialFuelGap = 5f;
            profile.fuelGapIncrease = 5f;
            profile.fuelPlacementSearchRadius = 26f;
            profile.fuelPlacementSearchStep = 2f;
            profile.fuelMaximumPlacementSlope = 18f;
            profile.environment = environment;
            profile.terrainMaterial = terrainMaterial;
            profile.terrainEdgeMaterial = groundMaterial;
            profile.useSeparateTerrainMaterials = true;
            profile.fitAuthoredTerrainTexture = true;
            profile.terrainTextureRepeatWidth = TerrainGenerator.CompactGroundModuleWidth;
            profile.terrainWallUvRange = new Vector2(0f, 1f);
            profile.terrainSurfaceUvRange = new Vector2(.313f, .701f);
            profile.terrainSurfaceContactV = .579f;
            profile.terrainSurfaceContactVSamples = new float[0];
            // Match City/North Pole: the deep Terrain wall starts at the physical contact line
            // behind the complete Ground ribbon, so transparent lower contours cannot expose a gap.
            profile.terrainFillStartV = profile.terrainSurfaceContactV;
            profile.groundPhysicsMaterial = wetGround;
            profile.terrainEdgeWidth = .18f;
            profile.terrainMaximumVisualExtent = TerrainGenerator.CompactGroundMaximumVisualExtent;
            profile.terrainVisualDepth = 27f;
            profile.supportsLiquidExposure = false;
            profile.rainstorm ??= new RainstormSettings();
            profile.rainstorm.permanentRain = true;
            profile.rainstorm.maximumParticles = 520;
            profile.rainstorm.emissionRate = 145f;
            profile.rainstorm.fallVelocity = new Vector2(-4.8f, -21f);
            profile.rainstorm.streakLength = .72f;
            profile.rainstorm.streakWidth = .035f;
            profile.rainstorm.surfaceGripMultiplier = .58f;
            profile.rainstorm.lightningEventsPerMinute = 4f;
            profile.rainstorm.lightningFlashDuration = .38f;
            profile.rainstorm.lightningFlashAlpha = .42f;
            EditorUtility.SetDirty(profile);

            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null) throw new System.InvalidOperationException("GameCatalog.asset is missing.");
            var maps = new List<MapDefinition>();
            if (catalog.maps != null)
                for (int i = 0; i < catalog.maps.Length; i++)
                    if (catalog.maps[i] != null && catalog.maps[i] != profile) maps.Add(catalog.maps[i]);
            int cityIndex = maps.FindIndex(map => map != null && map.mapId == "city");
            maps.Insert(cityIndex >= 0 ? cityIndex + 1 : Mathf.Min(3, maps.Count), profile);
            catalog.maps = maps.ToArray();
            catalog.SortPurchasableContent();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Validate(profile, environment, catalog);
            Debug.Log("RAINYER_MAP_INSTALL_PASS: map, assets, wet grip, permanent rain and 4/min lightning installed.");
        }

        private static void Validate(MapProfile profile, EnvironmentDefinition environment, GameCatalog catalog)
        {
            MapProfile city = catalog.FindMap("city", GameModeType.Adventure2_5D) as MapProfile;
            if (catalog.FindMap("rainyer", GameModeType.Adventure2_5D) != profile ||
                profile.environment != environment || !profile.useSeparateTerrainMaterials ||
                profile.terrainMaterial == profile.terrainEdgeMaterial || !profile.fitAuthoredTerrainTexture ||
                profile.terrainSurfaceContactVSamples == null ||
                profile.terrainSurfaceContactVSamples.Length != 0 ||
                Mathf.Abs(profile.terrainFillStartV - profile.terrainSurfaceContactV) > .0001f ||
                Mathf.Abs(profile.terrainTextureRepeatWidth - TerrainGenerator.CompactGroundModuleWidth) > .001f ||
                Mathf.Abs(profile.terrainMaximumVisualExtent -
                          TerrainGenerator.CompactGroundMaximumVisualExtent) > .001f ||
                Vector2.Distance(profile.terrainSurfaceUvRange, new Vector2(.313f, .701f)) > .001f ||
                Mathf.Abs(profile.terrainSurfaceContactV - .579f) > .001f ||
                profile.rainstorm == null || !profile.rainstorm.permanentRain ||
                Mathf.Abs(profile.rainstorm.lightningEventsPerMinute - 4f) > .001f ||
                profile.rainstorm.surfaceGripMultiplier >= 1f || city == null ||
                profile.maxSlopeAngle <= city.maxSlopeAngle || profile.sectionLength >= city.sectionLength ||
                profile.technicalSectionChance <= city.technicalSectionChance)
                throw new System.InvalidOperationException("Rainyer integration validation failed.");
        }

        private static PhysicsMaterial LoadOrCreateWetPhysics()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(WetPhysicsPath);
            if (material == null)
            {
                material = new PhysicsMaterial("Rainyer Wet Ground");
                AssetDatabase.CreateAsset(material, WetPhysicsPath);
            }
            material.dynamicFriction = .24f;
            material.staticFriction = .31f;
            material.bounciness = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Multiply;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureSprite(string path, int maximumSize, TextureWrapMode wrapMode)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = wrapMode;
            importer.maxTextureSize = maximumSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Material LoadOrCreateMaterial(string path, Texture texture, int renderQueue)
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
                material.SetFloat("_HorizontalInset", path == GroundMaterialPath ? .008f : .012f);
            if (material.HasProperty("_HorizontalUvRange"))
                material.SetVector("_HorizontalUvRange", new Vector4(0f, 1f, 0f, 0f));
            if (material.HasProperty("_DepthWrite")) material.SetFloat("_DepthWrite", 1f);
            material.renderQueue = renderQueue;
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
