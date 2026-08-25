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
            if (sky == null || background == null || terrain == null || ground == null || clouds.Count != 15)
                throw new System.InvalidOperationException(
                    $"City source import is incomplete: sky={sky != null}, background={background != null}, " +
                    $"terrain={terrain != null}, ground={ground != null}, clouds={clouds.Count}/15.");

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
            profile.terrainTextureRepeatWidth = 45f;
            profile.terrainWallUvRange = new Vector2(.171f, .82f);
            profile.terrainSurfaceUvRange = new Vector2(.293f, .45f);
            profile.terrainSurfaceContactV = .45f;
            profile.terrainFillStartV = .45f;
            profile.groundPhysicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(GroundPhysicsPath);
            profile.terrainEdgeWidth = .18f;
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
                !(d0 < d3750 && d3750 < d7500) || !Mathf.Approximately(d7500, d12000) ||
                environment.clouds == null || environment.clouds.Length != 15 ||
                catalog.FindMap("city", GameModeType.Adventure2_5D) != profile)
                throw new System.InvalidOperationException("City integration validation failed.");
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
                material.SetFloat("_HorizontalInset", .012f);
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
