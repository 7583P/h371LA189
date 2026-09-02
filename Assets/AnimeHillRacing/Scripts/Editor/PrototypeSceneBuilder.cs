#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AnimeHillRacing.CameraSystem;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using AnimeHillRacing.EditorTools;
using AnimeHillRacing.Pickups;
using AnimeHillRacing.Road;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.TimeTrial;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private const string Root = "Assets/AnimeHillRacing";
        private const string Scenes = Root + "/Scenes";
        private const string MainMenuPath = Scenes + "/MainMenu.unity";
        private const string PreStartPath = Scenes + "/PreStart.unity";
        private const string GaragePath = Scenes + "/Garage.unity";
        private const string Adventure2DPath = Scenes + "/Adventure2_5D.unity";
        private const string Adventure3DPath = Scenes + "/Adventure3D.unity";
        private const string TimeTrialPath = Scenes + "/TimeTrial3D.unity";
        private const string Prefabs = Root + "/Prefabs/Vehicles";
        private const string JeepBodyPath = "Assets/Art/Vehicles/Jeep/Skins/Jeep_Skin_Default.png";
        private const string JeepRetroSkinPath = "Assets/Art/Vehicles/Jeep/Skins/Retro_Skin.png";
        private const string JeepWheel01Path = "Assets/Art/Vehicles/Jeep/Wheels/Jeep_Wheel_01.png";
        private const string JeepWheel02Path = "Assets/Art/Vehicles/Jeep/Wheels/Jeep_Wheel_02.png";
        private const string JeepStarryWheelPath = "Assets/Art/Vehicles/Jeep/Wheels/Starry_Wheel.png";
        private const string FormulaBodyPath = "Assets/Art/Vehicles/Formula/Skins/Formula_skin_Default.png";
        private const string FormulaNeonSkinPath = "Assets/Art/Vehicles/Formula/Skins/Neon_Skin.png";
        private const string FormulaElectricitySkinPath = "Assets/Art/Vehicles/Formula/Skins/Electricity.png";
        private const string FormulaHillAdventurerSkinPath =
            "Assets/Art/Vehicles/Formula/Skins/HillAdventurer_WhiteGold.png";
        private const string FormulaWheel01Path = "Assets/Art/Vehicles/Formula/Wheels/Formula_Wheel_01.png";
        private const string FormulaWheel02Path = "Assets/Art/Vehicles/Formula/Wheels/Formula_Wheel_02.png";
        private const string FormulaLuxuryWheelPath = "Assets/Art/Vehicles/Formula/Wheels/Luxury.png";
        private const string FormulaReferencePath = "Assets/Art/Vehicles/Formula/Formula_Reference.png";
        private const string HillAdventurerPurpleSkinPath =
            "Assets/Art/Vehicles/Hill Adventurer 4x4/Skins/Purple_4x4.png";
        private const string RallyBodyPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_Skin_Default.png";
        private const string RallyHillAdventurerPath = "Assets/Art/Vehicles/Rally/Vehicle/Hill Adventurer.png";
        private const string RallyNeoPath = "Assets/Art/Vehicles/Rally/Vehicle/Neo_Rally.png";
        private const string RallyNoRoofPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_No_Roof_01.png";
        private const string RallyRoofPath = "Assets/Art/Vehicles/Rally/Vehicle/Rally_Roof_01.png";
        private const string RallyWheelPath = "Assets/Art/Vehicles/Rally/Wheels/Rally_Wheel_01.png";
        private const string RallyGhostyWheelPath = "Assets/Art/Vehicles/Rally/Wheels/Rally_Wheel_Ghosty.png";
        private const string RallySkyBlueWheelPath = "Assets/Art/Vehicles/Rally/Wheels/SkyBlue_Wheel.png";
        private const string SportBikeBodyPath = "Assets/Art/Vehicles/Sport Bike/Vehicle/Bike_Skin_Default.png";
        private const string SportBikeSunRayPath = "Assets/Art/Vehicles/Sport Bike/Vehicle/Sun_Ray.png";
        private const string SportBikeRetroLuxePath =
            "Assets/Art/Vehicles/Sport Bike/Vehicle/Bike_Retro_Luxe.png";
        private const string SportBikeNeonFlowSkinPath =
            "Assets/Art/Vehicles/Sport Bike/Vehicle/Bike_Neon_Flow.png";
        private const string SportBikeWheel01Path = "Assets/Art/Vehicles/Sport Bike/Wheels/Bike_Wheel_01.png";
        private const string SportBikeWheel02Path = "Assets/Art/Vehicles/Sport Bike/Wheels/Bike_Wheel_02.png";
        private const string SportBikeGoldWheelPath = "Assets/Art/Vehicles/Sport Bike/Wheels/Gold_Wheel.png";
        private const string SportBikeGhostyWheelPath =
            "Assets/Art/Vehicles/Sport Bike/Wheels/SportBike_Wheel_Ghosty.png.png";
        private const string SportBikeNeonFlowWheelPath =
            "Assets/Art/Vehicles/Sport Bike/Wheels/Neon_Flow_Wheel.png";
        private const string NeonRoot = "Assets/Art/Backgrounds/Neon_CIty";
        private const string NeonSkyPath = NeonRoot + "/Sky.png";
        private const string NeonBackgroundPath = NeonRoot + "/Background.png";
        private const string NeonTerrainPath = NeonRoot + "/Terrain.png";
        private const string NeonGroundPath = NeonRoot + "/Ground.png";
        private const string NeonLiquidPath = NeonRoot + "/Liquid.png";
        private const string NeonCautionPath = NeonRoot + "/Caution.png";
        private const string ButtonRoot = "Assets/Art/Buttons";
        private const string MainMenuBackgroundPath = ButtonRoot + "/mainmenu.png";
        private const string HomeButtonPath = ButtonRoot + "/home.png";
        private const string RaceButtonPath = ButtonRoot + "/race.png";
        private const string SettingsButtonPath = ButtonRoot + "/settings.png";
        private const string SkinsButtonPath = ButtonRoot + "/Skins.png";
        private const string StoreButtonPath = ButtonRoot + "/store.png";
        private const string TuningButtonPath = ButtonRoot + "/tuning.png";
        private const string UpgradeButtonPath = ButtonRoot + "/upgrade.png";
        private const string VehicleButtonPath = ButtonRoot + "/vehicle_button.png";
        private const string GameUiRoot = "Assets/Art/Game";
        private const string BrakePedalPath = GameUiRoot + "/brake.png";
        private const string BrakePedalPressedPath = GameUiRoot + "/brake_push.png";
        private const string GasPedalPath = GameUiRoot + "/gas.png";
        private const string GasPedalPressedPath = GameUiRoot + "/gas_push.png";
        private const string RpmGaugePath = GameUiRoot + "/rpm.png";
        private const string RpmNeedlePath = GameUiRoot + "/rpm_arrow.png";
        private const string SpeedGaugePath = GameUiRoot + "/kmh.png";
        private const string SpeedNeedlePath = GameUiRoot + "/kmh_arrow.png";
        private const string FuelCanPath = "Assets/Art/Collectibles/Fuel/Fuel_Can_Default.png";
        private const string Coin01Path = "Assets/Art/Collectibles/Coins/Coin_01.png";
        private const string Coin05Path = "Assets/Art/Collectibles/Coins/Coin_05.png";
        private const string ChestRoot = "Assets/Art/Chests";
        private const string BronzeChestClosedPath = ChestRoot + "/Bronze_Chest_Closed.png";
        private const string BronzeChestOpenPath = ChestRoot + "/Bronze_Chest_Open.png";
        private const string SilverChestClosedPath = ChestRoot + "/Silver_Chest_Closed.png";
        private const string SilverChestOpenPath = ChestRoot + "/Silver_Chest_Open.png";
        private const string GoldChestClosedPath = ChestRoot + "/Gold_Chest_Closed.png";
        private const string GoldChestOpenPath = ChestRoot + "/Gold_Chest_Open.png";
        private const string MasterChestClosedPath = ChestRoot + "/Master_Chest_Closed.png";
        private const string MasterChestOpenPath = ChestRoot + "/Master_Chest_Open.png";
        private const string LegendaryChestClosedPath = ChestRoot + "/Legendary_Chest_Closed.png";
        private const string LegendaryChestOpenPath = ChestRoot + "/Legendary_Chest_Open.png";
        private const string CoinsChestPath = ChestRoot + "/Coins_Chest.png";
        private const string ChestShelfPath = ChestRoot + "/Shelf.png";
        private const string DriverRoot = "Assets/Art/Characters/Stickman_Black";
        private const string DriverPreviewPath = DriverRoot + "/Stickman_Black_Preview.png";
        private const string DriverHeadPath = DriverRoot + "/Head/Stickman_Black_Head.png";
        private const string DriverTorsoPath = DriverRoot + "/Torso/Stickman_Black_Torso.png";
        private const string DriverUpperArmPath = DriverRoot + "/Arms/Stickman_Black_UpperArm.png";
        private const string DriverForearmPath = DriverRoot + "/Arms/Stickman_Black_Forearm.png";
        private const string DriverThighPath = DriverRoot + "/Legs/Stickman_Black_Thigh.png";
        private const string DriverLowerLegPath = DriverRoot + "/Legs/Stickman_Black_LowerLeg.png";
        private const string DriverAccessoryRoot = "Assets/Art/Characters/Accesory";
        private const string DriverGlassesPath = DriverAccessoryRoot + "/Glasses.png";
        private const string DriverFireGlassesPath = DriverAccessoryRoot + "/Fire Glasses.png";
        private const string DriverScarfPath = DriverAccessoryRoot + "/Scarf.png";
        private const string DriverSantaHatPath = DriverAccessoryRoot + "/SantaHat.png";
        private const string DriverNeonGlassesPath = DriverAccessoryRoot + "/NeonGlasses.png";
        private const string DriverTireAccessoryPath = DriverAccessoryRoot + "/TireAccessory.png";
        private const string DriverBaseballCapPath = DriverAccessoryRoot + "/BaseballCap.png";
        private const string CountrysideRoot = "Assets/Art/Backgrounds/Countryside";
        private const string CountrysideSkyPath = CountrysideRoot + "/Countryside_Sky.png";
        private const string CountrysideMountainsPath = CountrysideRoot + "/Countryside_Mountains.png";
        private const string CountrysideSunPath = CountrysideRoot + "/Countryside_Sun.png";
        private const string CountrysideTerrainPath = CountrysideRoot + "/Terrain.png";
        private const string CountrysideGroundPath = CountrysideRoot + "/Ground.png";
        private const string NorthPoleRoot = "Assets/Art/Backgrounds/NorthPole";
        private const string NorthPoleSkyPath = NorthPoleRoot + "/NorthPole_Sky.png";
        private const string NorthPolePanoramaPath = NorthPoleRoot + "/NorthPole_Panorama.png";
        private const string NorthPoleNearPinesPath = NorthPoleRoot + "/NorthPole_NearPines.png";
        private const string NorthPoleTerrainPath = NorthPoleRoot + "/Terrain.png";
        private const string NorthPoleGroundPath = NorthPoleRoot + "/Ground.png";
        private const string SpaceMapPath = Root + "/MapProfiles/Space.asset";
        private const string SpaceGroundPath = "Assets/Art/Backgrounds/Space/Ground.png";
        private const string SpaceTerrainPath = "Assets/Art/Backgrounds/Space/Terrain.png";
        private const string SpaceGalaxyPath = "Assets/Art/Backgrounds/Space/Space_Cosmic_Panorama_v4.png";
        private const string SpaceStarfieldPath = "Assets/Art/Backgrounds/Space/Space_Stars.png";
        private const string ArenaMapPath = Root + "/MapProfiles/Arena.asset";
        private const string CityRoot = "Assets/Art/Backgrounds/City";
        private const string CitySkyPath = CityRoot + "/SkyWithClouds.png";
        private const string CityBackgroundPath = CityRoot + "/Background.png";
        private const string CityTerrainPath = CityRoot + "/Terrain.png";
        private const string CityGroundPath = CityRoot + "/Ground.png";
        private const string CityBridgePath = CityRoot + "/Bridge.png";
        private const string CityWaterPath = CityRoot + "/Water.png";
        private const string CityBoatSmallPath = CityRoot + "/Boats/CityBoat_Small.png";
        private const string CityBoatMediumPath = CityRoot + "/Boats/CityBoat_Medium.png";
        private const string CityBoatLargePath = CityRoot + "/Boats/CityBoat_Large.png";
        private static Font font;

        private sealed class BuiltAssets
        {
            public VehicleDefinition jeep;
            public VehicleDefinition formula;
            public VehicleDefinition rally;
            public VehicleDefinition sportBike;
            public VehicleDefinition hillAdventurer;
            public Sprite hillAdventurerSkin;
            public Sprite hillAdventurerPurpleSkin;
            public Sprite hillAdventurerWheel;
            public Sprite formulaBody;
            public Sprite formulaNeonSkin;
            public Sprite formulaElectricitySkin;
            public Sprite formulaHillAdventurerSkin;
            public Sprite formulaWheel01;
            public Sprite formulaWheel02;
            public Sprite luxuryWheel;
            public Sprite jeepBody;
            public Sprite jeepRetroSkin;
            public Sprite jeepWheel01;
            public Sprite jeepWheel02;
            public Sprite jeepStarryWheel;
            public Sprite rallyBody;
            public Sprite rallyHillAdventurer;
            public Sprite rallyNeo;
            public Sprite rallyNoRoof;
            public Sprite rallyRoof;
            public Sprite rallyWheel;
            public Sprite rallyGhostyWheel;
            public Sprite rallySkyBlueWheel;
            public Sprite sportBikeBody;
            public Sprite sportBikeSunRay;
            public Sprite sportBikeRetroLuxe;
            public Sprite sportBikeNeonFlowSkin;
            public Sprite sportBikeWheel01;
            public Sprite sportBikeWheel02;
            public Sprite sportBikeGoldWheel;
            public Sprite sportBikeGhostyWheel;
            public Sprite sportBikeNeonFlowWheel;
            public Sprite driverGlasses;
            public Sprite driverFireGlasses;
            public Sprite driverScarf;
            public Sprite driverSantaHat;
            public Sprite driverNeonGlasses;
            public Sprite driverTireAccessory;
            public Sprite driverBaseballCap;
            public Sprite neonSky;
            public Sprite neonBackground;
            public Sprite neonTerrain;
            public Sprite neonGround;
            public Sprite neonLiquid;
            public Sprite neonCaution;
            public Sprite countrysideTerrain;
            public Sprite countrysideGround;
            public Sprite northPoleSky;
            public Sprite northPolePanorama;
            public Sprite northPoleNearPines;
            public Sprite northPoleTerrain;
            public Sprite northPoleGround;
            public Sprite citySky;
            public Sprite cityBackground;
            public Sprite cityTerrain;
            public Sprite cityGround;
            public Sprite cityBridge;
            public Sprite cityWater;
            public Sprite[] cityBoats;
            public Sprite mainMenuBackground;
            public Sprite homeButton;
            public Sprite raceButton;
            public Sprite settingsButton;
            public Sprite skinsButton;
            public Sprite storeButton;
            public Sprite tuningButton;
            public Sprite upgradeButton;
            public Sprite vehicleButton;
            public Sprite brakePedal;
            public Sprite brakePedalPressed;
            public Sprite gasPedal;
            public Sprite gasPedalPressed;
            public Sprite rpmGauge;
            public Sprite rpmNeedle;
            public Sprite speedGauge;
            public Sprite speedNeedle;
            public Sprite fuelCan;
            public Sprite coin01;
            public Sprite coin05;
            public Sprite bronzeChestClosed, bronzeChestOpen, silverChestClosed, silverChestOpen;
            public Sprite goldChestClosed, goldChestOpen, masterChestClosed, masterChestOpen;
            public Sprite legendaryChestClosed, legendaryChestOpen, coinsChest, chestShelf;
            public DriverDefinition stickmanBlack;
            public DriverDefinition stickmanWhite;
            public EnvironmentDefinition countrysideEnvironment;
            public EnvironmentDefinition northPoleEnvironment;
            public EnvironmentDefinition neonCityEnvironment;
            public EnvironmentDefinition cityEnvironment;
            public MapProfile countryside;
            public MapProfile northPole;
            public MapProfile space;
            public MapProfile arena;
            public MapProfile neonCity;
            public MapProfile city;
            public CoinRouteDefinition countrysideCoinRoute;
            public TerrainElevationProfile elevation;
            public TerrainElevationProfile northPoleElevation;
            public RoadCurvatureProfile curvature;
            public RoadMapProfile countrysideRoad;
            public CircuitDefinition testCircuit;
            public UpgradeEconomyDefinition economy;
            public GameCatalog catalog;
            public ChestCatalog chestCatalog;
            public Material countrysideMaterial;
            public Material countrysideEdgeMaterial;
            public Material northPoleMaterial;
            public Material northPoleEdgeMaterial;
            public Material neonCityMaterial;
            public Material neonCityEdgeMaterial;
            public Material cityTerrainMaterial;
            public Material cityGroundMaterial;
            public Material nearMountains;
            public Material farMountains;
            public Material road;
            public Material grass;
            public Material shortcut;
            public PhysicsMaterial chassisPhysics;
            public PhysicsMaterial groundPhysics;
            public PhysicsMaterial tirePhysics;
        }

        /// <summary>
        /// CreateAssets intentionally rebuilds the authored base customization arrays. Keep the
        /// data-only Glitch entries (including their hand-tuned visual poses) outside that rewrite
        /// and merge them back before any scene or asset is saved.
        /// </summary>
        private sealed class GlitchCustomizationSnapshot
        {
            private sealed class VehicleSnapshot
            {
                public string assetPath;
                public VehicleDefinition.CustomizationOption[] skins;
                public VehicleDefinition.CustomizationOption[] wheels;
            }

            private readonly List<VehicleSnapshot> vehicles = new List<VehicleSnapshot>();

            public static GlitchCustomizationSnapshot Capture()
            {
                var snapshot = new GlitchCustomizationSnapshot();
                string[] guids = AssetDatabase.FindAssets("t:VehicleDefinition",
                    new[] { Root + "/VehicleData" });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    VehicleDefinition vehicle =
                        AssetDatabase.LoadAssetAtPath<VehicleDefinition>(path);
                    if (vehicle == null) continue;
                    VehicleDefinition.CustomizationOption[] skins = CaptureOptions(
                        vehicle.skins, GlitchCrystalTideEffect.SkinPrefix);
                    VehicleDefinition.CustomizationOption[] wheels = CaptureOptions(
                        vehicle.wheels, GlitchCrystalTideEffect.WheelPrefix);
                    if (skins.Length == 0 && wheels.Length == 0) continue;
                    snapshot.vehicles.Add(new VehicleSnapshot
                    {
                        assetPath = path,
                        skins = skins,
                        wheels = wheels
                    });
                }
                return snapshot;
            }

            public void Restore()
            {
                for (int i = 0; i < vehicles.Count; i++)
                {
                    VehicleSnapshot saved = vehicles[i];
                    VehicleDefinition vehicle =
                        AssetDatabase.LoadAssetAtPath<VehicleDefinition>(saved.assetPath);
                    if (vehicle == null)
                        throw new System.InvalidOperationException(
                            $"Glitch pose vehicle asset disappeared during rebuild: {saved.assetPath}");
                    vehicle.skins = MergeOptions(vehicle.skins, saved.skins,
                        GlitchCrystalTideEffect.SkinPrefix);
                    vehicle.wheels = MergeOptions(vehicle.wheels, saved.wheels,
                        GlitchCrystalTideEffect.WheelPrefix);
                    EditorUtility.SetDirty(vehicle);
                }
            }

            public void ValidateOrThrow()
            {
                for (int i = 0; i < vehicles.Count; i++)
                {
                    VehicleSnapshot saved = vehicles[i];
                    VehicleDefinition vehicle =
                        AssetDatabase.LoadAssetAtPath<VehicleDefinition>(saved.assetPath);
                    if (vehicle == null)
                        throw new System.InvalidOperationException(
                            $"Glitch pose vehicle asset is missing after rebuild: {saved.assetPath}");
                    ValidateOptions(vehicle.skins, saved.skins, saved.assetPath, "skin");
                    ValidateOptions(vehicle.wheels, saved.wheels, saved.assetPath, "wheel");
                }
            }

            private static VehicleDefinition.CustomizationOption[] CaptureOptions(
                VehicleDefinition.CustomizationOption[] source, string prefix)
            {
                var result = new List<VehicleDefinition.CustomizationOption>();
                if (source == null) return result.ToArray();
                for (int i = 0; i < source.Length; i++)
                    if (HasPrefix(source[i], prefix)) result.Add(Clone(source[i]));
                return result.ToArray();
            }

            private static VehicleDefinition.CustomizationOption[] MergeOptions(
                VehicleDefinition.CustomizationOption[] current,
                VehicleDefinition.CustomizationOption[] saved, string prefix)
            {
                var merged = new List<VehicleDefinition.CustomizationOption>();
                if (current != null)
                    for (int i = 0; i < current.Length; i++)
                        if (!HasPrefix(current[i], prefix)) merged.Add(current[i]);
                if (saved != null)
                    for (int i = 0; i < saved.Length; i++) merged.Add(Clone(saved[i]));
                return merged.ToArray();
            }

            private static void ValidateOptions(
                VehicleDefinition.CustomizationOption[] current,
                VehicleDefinition.CustomizationOption[] saved, string assetPath, string kind)
            {
                for (int i = 0; i < saved.Length; i++)
                {
                    VehicleDefinition.CustomizationOption expected = saved[i];
                    VehicleDefinition.CustomizationOption actual = FindExact(current,
                        expected.optionId);
                    if (actual == null || !SameVisualPose(actual, expected))
                        throw new System.InvalidOperationException(
                            $"Build All Game Scenes changed the saved Glitch {kind} pose " +
                            $"'{expected.optionId}' in {assetPath}.");
                }
            }

            private static VehicleDefinition.CustomizationOption FindExact(
                VehicleDefinition.CustomizationOption[] options, string optionId)
            {
                if (options == null) return null;
                for (int i = 0; i < options.Length; i++)
                    if (options[i] != null && options[i].optionId == optionId) return options[i];
                return null;
            }

            private static bool SameVisualPose(VehicleDefinition.CustomizationOption left,
                VehicleDefinition.CustomizationOption right) =>
                left.sprite == right.sprite &&
                Mathf.Approximately(left.visibleDiameter, right.visibleDiameter) &&
                Approximately(left.visibleSize, right.visibleSize) &&
                Approximately(left.visualOffset, right.visualOffset) &&
                left.overrideGlitchTransform == right.overrideGlitchTransform &&
                Approximately(left.glitchManualSize, right.glitchManualSize) &&
                Approximately(left.glitchManualOffset, right.glitchManualOffset) &&
                left.overrideGlitchWheelTransform == right.overrideGlitchWheelTransform &&
                Mathf.Approximately(left.glitchRearWheelScale, right.glitchRearWheelScale) &&
                Mathf.Approximately(left.glitchFrontWheelScale, right.glitchFrontWheelScale) &&
                Approximately(left.glitchRearWheelOffset, right.glitchRearWheelOffset) &&
                Approximately(left.glitchFrontWheelOffset, right.glitchFrontWheelOffset);

            private static bool Approximately(Vector2 left, Vector2 right) =>
                Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);

            private static bool HasPrefix(VehicleDefinition.CustomizationOption option,
                string prefix) => option != null && !string.IsNullOrEmpty(option.optionId) &&
                                  option.optionId.StartsWith(prefix, System.StringComparison.Ordinal);

            private static VehicleDefinition.CustomizationOption Clone(
                VehicleDefinition.CustomizationOption source) =>
                new VehicleDefinition.CustomizationOption
                {
                    optionId = source.optionId,
                    displayName = source.displayName,
                    sprite = source.sprite,
                    unlockedByDefault = source.unlockedByDefault,
                    requiredBestDistance = source.requiredBestDistance,
                    gemPrice = source.gemPrice,
                    purchaseCurrency = source.purchaseCurrency,
                    googlePlayProductId = source.googlePlayProductId,
                    bundleType = source.bundleType,
                    allowsBlueprintProgression = source.allowsBlueprintProgression,
                    hasExplicitRarity = source.hasExplicitRarity,
                    rarity = source.rarity,
                    visibleDiameter = source.visibleDiameter,
                    visibleSize = source.visibleSize,
                    visualOffset = source.visualOffset,
                    overrideGlitchTransform = source.overrideGlitchTransform,
                    glitchManualSize = source.glitchManualSize,
                    glitchManualOffset = source.glitchManualOffset,
                    overrideGlitchWheelTransform = source.overrideGlitchWheelTransform,
                    glitchRearWheelScale = source.glitchRearWheelScale,
                    glitchFrontWheelScale = source.glitchFrontWheelScale,
                    glitchRearWheelOffset = source.glitchRearWheelOffset,
                    glitchFrontWheelOffset = source.glitchFrontWheelOffset
                };
        }

        [MenuItem("Anime Hill Racing/Build All Game Scenes _F8")]
        public static void BuildAllGameScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit Play Mode before rebuilding scenes.");
                return;
            }
            string previousScenePath = SceneManager.GetActiveScene().path;
            GlitchCustomizationSnapshot glitchSnapshot = GlitchCustomizationSnapshot.Capture();
            bool completed = false;
            try
            {
                BuildAllGameScenesInternal(glitchSnapshot);
                completed = true;
            }
            catch
            {
                string recoveryPath = !string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath)
                    ? previousScenePath
                    : File.Exists(MainMenuPath) ? MainMenuPath : string.Empty;
                if (!string.IsNullOrEmpty(recoveryPath))
                    EditorSceneManager.OpenScene(recoveryPath, OpenSceneMode.Single);
                throw;
            }
            finally
            {
                // A failed rebuild must not leave the in-memory VehicleDefinitions with their
                // Glitch arrays (and therefore their saved poses) removed.
                if (!completed)
                {
                    glitchSnapshot.Restore();
                    AssetDatabase.SaveAssets();
                }
            }
        }

        [MenuItem("Anime Hill Racing/Rebuild Neon Recovery Content")]
        public static void RebuildNeonRecoveryContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before rebuilding recovery content.");
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateCustomizationContract(assets);
            ValidateCountrysideTerrainContract(assets.countryside);
            ValidateSharedAuthoredTrackDimensions();
            ValidateNorthPoleContract(assets);
            ValidateSpaceContract(assets);
            ValidateNeonCityContract(assets);
            ValidateCityContract(assets);
            ValidateVehiclePhysicsContract(assets);
            ValidateTuningContract(assets);
            ValidateStuntRotationContract();
            ValidateVehiclePerformance(assets);
            ValidateSuspensionContract(assets);
            ValidateFuelSequenceContract(assets);
            ValidateCoinRouteContract(assets);
            BuildGarageModern(assets);
            BuildMainMenu(assets);
            BuildPreStartModern(assets);
            BuildAdventure2_5D(assets);
            BuildGarageHillAdventurer(assets);
            BuildMainMenuHillAdventurer(assets);
            BuildTrackSelectionHillAdventurer(assets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Debug.Log("NEON_RECOVERY_BUILD_OK: four required scenes rebuilt and validated.");
        }

        [MenuItem("Anime Hill Racing/Rebuild Driver + Physics + Characters + Shop")]
        public static void RebuildDriverPhysicsCharactersShop()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before rebuilding driver/shop content.");
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateCustomizationContract(assets);
            ValidateVehiclePhysicsContract(assets);
            ValidateSuspensionContract(assets);
            BuildGarageModern(assets);
            BuildGarageHillAdventurer(assets);
            ValidateCharacterAccessoryAndShopContract(assets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(GaragePath, OpenSceneMode.Single);
            Debug.Log("DRIVER_PHYSICS_CHARACTERS_SHOP_BUILD_OK: authoritative poses/prefabs, ride model, " +
                      "persistent accessories and vertical shop rebuilt and validated.");
        }

        [MenuItem("Anime Hill Racing/Capture Driver Pose Audit")]
        public static void CaptureDriverPoseAudit()
        {
            Scene scene = EditorSceneManager.OpenScene(GaragePath, OpenSceneMode.Single);
            GaragePreviewRenderer preview = SceneComponents<GaragePreviewRenderer>(scene).FirstOrDefault();
            if (preview == null || preview.PreviewCamera == null)
                throw new System.InvalidOperationException("Garage driver-pose audit camera is missing.");

            DriverDefinition driver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(
                Root + "/VehicleData/StickmanBlack.asset");
            VehicleDefinition[] vehicles =
            {
                AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/MainVehicle.asset"),
                AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/FormulaVehicle.asset"),
                AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/RallyVehicle.asset"),
                AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/SportBikeVehicle.asset")
            };
            // Unity clears the project Temp folder when a batch editor exits, so keep the
            // evidence beside the editor logs where it survives long enough to inspect.
            string outputDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Logs", "DriverPoseAudit");
            Directory.CreateDirectory(outputDirectory);

            const string selectionKey = "AHR.Customize.CharacterAccessory";
            bool hadSelection = PlayerPrefs.HasKey(selectionKey);
            string previousSelection = PlayerPrefs.GetString(selectionKey, string.Empty);
            string[] accessoryIds = { "default", "glasses" };
            Camera camera = preview.PreviewCamera;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            CameraClearFlags previousClearFlags = camera.clearFlags;
            Color previousBackgroundColor = camera.backgroundColor;
            try
            {
                // The character is nearly black; a neutral audit background keeps every
                // limb and accessory readable without altering camera framing or scale.
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.78f, .78f, .78f, 1f);
                for (int accessoryIndex = 0; accessoryIndex < accessoryIds.Length; accessoryIndex++)
                {
                    SaveManager.SetSelectedCharacterAccessoryId(accessoryIds[accessoryIndex]);
                    for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
                    {
                        VehicleDefinition vehicle = vehicles[vehicleIndex];
                        if (vehicle == null) continue;
                        VehicleDefinition.CustomizationOption skin = vehicle.skins != null && vehicle.skins.Length > 0
                            ? vehicle.skins[0] : null;
                        VehicleDefinition.CustomizationOption wheel = vehicle.wheels != null && vehicle.wheels.Length > 0
                            ? vehicle.wheels[0] : null;
                        preview.Apply(vehicle, skin, wheel, driver);

                        RenderTexture target = new RenderTexture(1280, 640, 24, RenderTextureFormat.ARGB32)
                        {
                            antiAliasing = 2
                        };
                        Texture2D capture = new Texture2D(1280, 640, TextureFormat.RGBA32, false);
                        try
                        {
                            camera.targetTexture = target;
                            target.Create();
                            camera.Render();
                            RenderTexture.active = target;
                            capture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                            capture.Apply();
                            string safeVehicleName = vehicle.displayName.Replace(" ", "-").ToLowerInvariant();
                            string outputPath = Path.Combine(outputDirectory,
                                safeVehicleName + "-" + accessoryIds[accessoryIndex] + ".png");
                            File.WriteAllBytes(outputPath, capture.EncodeToPNG());
                        }
                        finally
                        {
                            camera.targetTexture = previousTarget;
                            RenderTexture.active = previousActive;
                            target.Release();
                            Object.DestroyImmediate(target);
                            Object.DestroyImmediate(capture);
                        }
                    }
                }
            }
            finally
            {
                if (hadSelection) PlayerPrefs.SetString(selectionKey, previousSelection);
                else PlayerPrefs.DeleteKey(selectionKey);
                PlayerPrefs.Save();
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                camera.clearFlags = previousClearFlags;
                camera.backgroundColor = previousBackgroundColor;
            }
            Debug.Log($"DRIVER_POSE_AUDIT_CAPTURE_OK: {vehicles.Length * accessoryIds.Length} renders at " +
                      outputDirectory);
        }

        private static void BuildAllGameScenesInternal(
            GlitchCustomizationSnapshot glitchSnapshot)
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateCustomizationContract(assets);
            // Validate the freshly generated base catalog first, then restore the data-only Glitch
            // entries before any scene consumes or saves the vehicle definitions.
            glitchSnapshot.Restore();
            GlitchPhaseCatalogInstaller.EnsureConfigured();
            glitchSnapshot.ValidateOrThrow();
            GlitchPhaseCosmeticsValidation.ValidateOrThrow();
            AssetDatabase.SaveAssets();
            ValidateCountrysideTerrainContract(assets.countryside);
            ValidateSharedAuthoredTrackDimensions();
            ValidateNorthPoleContract(assets);
            ValidateSpaceContract(assets);
            ValidateNeonCityContract(assets);
            ValidateCityContract(assets);
            ValidateVehiclePhysicsContract(assets);
            ValidateStuntRotationContract();
            ValidateFuelSequenceContract(assets);
            ValidateCoinRouteContract(assets);
            BuildGarageModern(assets);
            BuildMainMenu(assets);
            BuildPreStartModern(assets);
            BuildAdventure2_5D(assets);
            BuildAdventure3D(assets);
            BuildTimeTrial3D(assets);
            BuildGarageHillAdventurer(assets);
            BuildMainMenuHillAdventurer(assets);
            BuildTrackSelectionHillAdventurer(assets);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(PreStartPath, true),
                new EditorBuildSettingsScene(GaragePath, true),
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(Adventure2DPath, true),
                new EditorBuildSettingsScene(Adventure3DPath, true),
                new EditorBuildSettingsScene(TimeTrialPath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AdventureBox2DGoldenInstaller.InstallGoldenBox2D();
            ValidateAllGameScenes();
            EditorSceneManager.OpenScene(PreStartPath, OpenSceneMode.Single);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(PreStartPath);
            Debug.Log("Anime Hill Racing: all six definitive game scenes built successfully.");
        }

        [MenuItem("Anime Hill Racing/Build Prototype Scene")]
        public static void BuildPrototypeScene() => BuildAllGameScenes();

        [MenuItem("Anime Hill Racing/Validate Suspension Audit")]
        public static void ValidateSuspensionAudit()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateVehiclePhysicsContract(assets);
            ValidateSuspensionContract(assets);
            LogResolvedPhysicsProfiles(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("COMPLETE VEHICLE PHYSICS AUDIT PASS");
        }

        [MenuItem("Anime Hill Racing/Validate Vehicle Performance Audit")]
        public static void ValidateVehiclePerformanceAudit()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateVehiclePerformance(assets);
            ValidateAirControlContract(assets);
            Debug.Log("COMPLETE VEHICLE PERFORMANCE AUDIT PASS");
        }

        [MenuItem("Anime Hill Racing/Synchronize Upgrade + Neon Authoritative Data")]
        public static void SynchronizeUpgradeAndNeonAuthoritativeData()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            VehicleDefinition jeep = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/MainVehicle.asset");
            VehicleDefinition formula = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/FormulaVehicle.asset");
            VehicleDefinition rally = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/RallyVehicle.asset");
            VehicleDefinition sportBike = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/SportBikeVehicle.asset");
            MapProfile neon = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/NeonCity.asset");
            if (jeep == null || formula == null || rally == null || sportBike == null || neon == null)
                throw new System.InvalidOperationException("Authoritative vehicle or Neon City asset is missing.");

            jeep.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = CreateProgressionTargets(14, 60f, 100f),
                accelerationZeroToHundredSeconds = CreateProgressionTargets(15, 12f, 7f),
                gripRatings = CreateProgressionTargets(12, 50f, 300f),
                airControlTorque = new[] { 3200f },
                airborneDownforceAcceleration = new[] { 0f },
                fuelRunDurationSeconds = CreateProgressionTargets(15, 15f, 30f)
            };
            formula.tuneTargets = CreateFormulaTuneTargets();
            rally.tuneTargets = CreateRallyTuneTargets();
            sportBike.tuneTargets = CreateSportBikeTuneTargets();
            neon.liquidChallenges.structuralRunUpPositions =
                LiquidChallengeSettings.DefaultStructuralRunUpPositions();
            EditorUtility.SetDirty(jeep);
            EditorUtility.SetDirty(formula);
            EditorUtility.SetDirty(rally);
            EditorUtility.SetDirty(sportBike);
            EditorUtility.SetDirty(neon);
            AssetDatabase.SaveAssets();
            ValidateImplementedUpgradeAndNeonAudit();
            Debug.Log("AUTHORITATIVE_UPGRADE_NEON_SYNC_OK: exactly four vehicle assets and Neon City data synchronized.");
        }

        [MenuItem("Anime Hill Racing/Validate Implemented Upgrade + Neon Audit")]
        public static void ValidateImplementedUpgradeAndNeonAudit()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = LoadImplementedAuditAssets();
            ValidateTuningContract(assets);
            ValidateNeonCityContract(assets);
            ValidatePedalPriorityContract();
            ValidateVehiclePerformance(assets);
            Debug.Log("IMPLEMENTED_UPGRADE_NEON_AUDIT_PASS: curves, physical stats, terrain, seams and liquids validated.");
        }

        [MenuItem("Anime Hill Racing/Validate Neon Difficulty + Pedal Priority")]
        public static void ValidateNeonDifficultyAndPedalPriority()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = LoadImplementedAuditAssets();
            ValidateNeonCityContract(assets);
            ValidatePedalPriorityContract();
            ValidateAirbornePedalPriorityContract(assets.jeep);
            Debug.Log("NEON_DIFFICULTY_PEDAL_PRIORITY_AUDIT_PASS: harder than Countryside, " +
                      "6000m/8 liquids/ramps/warnings and ground/air last-pressed pedals validated.");
        }

        private static void ValidatePedalPriorityContract()
        {
            GameObject inputObject = new GameObject("Pedal Priority Audit");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            MethodInfo refreshInput = typeof(VehicleInputController).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (refreshInput == null)
            {
                Object.DestroyImmediate(inputObject);
                throw new System.InvalidOperationException("Vehicle input refresh method is unavailable.");
            }

            input.SetTouchBrake(true);
            refreshInput.Invoke(input, null);
            bool brakeStarts = input.State.BrakeRaw > .5f && input.State.Gas < .5f;
            input.SetTouchGas(true);
            refreshInput.Invoke(input, null);
            bool gasOverrides = input.State.Gas > .5f && input.State.BrakeRaw < .5f;
            input.SetTouchGas(false);
            refreshInput.Invoke(input, null);
            bool brakeResumes = input.State.BrakeRaw > .5f && input.State.Gas < .5f;

            input.SetTouchBrake(false);
            input.SetTouchGas(true);
            refreshInput.Invoke(input, null);
            bool gasStarts = input.State.Gas > .5f && input.State.BrakeRaw < .5f;
            input.SetTouchBrake(true);
            refreshInput.Invoke(input, null);
            bool brakeOverrides = input.State.BrakeRaw > .5f && input.State.Gas < .5f;
            input.SetTouchBrake(false);
            refreshInput.Invoke(input, null);
            bool gasResumes = input.State.Gas > .5f && input.State.BrakeRaw < .5f;
            Object.DestroyImmediate(inputObject);

            if (!brakeStarts || !gasOverrides || !brakeResumes ||
                !gasStarts || !brakeOverrides || !gasResumes)
                throw new System.InvalidOperationException(
                    "Brake/Gas must give exclusive control to the last pedal pressed and resume the held pedal on release.");
            Debug.Log("PEDAL_LAST_PRESSED_PRIORITY: Brake->Gas and Gas->Brake override/resume PASS.");
        }

        private static void ValidateAirbornePedalPriorityContract(VehicleDefinition source)
        {
            if (source == null || source.gameplayPrefab2D == null)
                throw new System.InvalidOperationException("Airborne pedal audit requires a 2D vehicle prefab.");
            string sourcePath = AssetDatabase.GetAssetPath(source);
            NewScene();
            source = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(sourcePath);
            if (source == null || source.gameplayPrefab2D == null)
                throw new System.InvalidOperationException("Airborne pedal audit could not reload its vehicle prefab.");
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateMaximumTuneClone(source);
            GameObject inputObject = new GameObject("Airborne Pedal Priority Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D,
                new Vector3(0f, 10000f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            controller.AssignRuntimeReferences(testDefinition, input,
                instance.GetComponentsInChildren<SuspensionController>(true));
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");

            input.SetTouchGas(true);
            InvokePrivate(input, "Update");
            for (int step = 0; step < 4; step++)
            {
                InvokePrivate(controller, "FixedUpdate");
                Physics.Simulate(.02f);
            }
            float gasRotation = controller.Body.angularVelocity.z;

            input.SetTouchBrake(true);
            InvokePrivate(input, "Update");
            InvokePrivate(controller, "FixedUpdate");
            Physics.Simulate(.02f);
            float brakeOverrideRotation = controller.Body.angularVelocity.z;

            for (int step = 0; step < 8; step++)
            {
                InvokePrivate(controller, "FixedUpdate");
                Physics.Simulate(.02f);
            }
            float brakeHeldRotation = controller.Body.angularVelocity.z;

            input.SetTouchBrake(false);
            InvokePrivate(input, "Update");
            InvokePrivate(controller, "FixedUpdate");
            Physics.Simulate(.02f);
            float gasResumeRotation = controller.Body.angularVelocity.z;

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            if (gasRotation <= .1f || brakeOverrideRotation >= gasRotation - .01f ||
                brakeOverrideRotation <= 0f || brakeHeldRotation >= -.05f ||
                gasResumeRotation <= brakeHeldRotation + .01f)
                throw new System.InvalidOperationException(
                    $"Airborne last-pressed pedal priority failed: gas={gasRotation:0.000}, " +
                    $"brake first frame={brakeOverrideRotation:0.000}, brake held={brakeHeldRotation:0.000}, " +
                    $"gas resume={gasResumeRotation:0.000}.");
            Debug.Log($"AIR_PEDAL_LAST_PRESSED_PRIORITY: gas={gasRotation:0.000}, " +
                      $"brake first frame={brakeOverrideRotation:0.000} (inertia preserved), " +
                      $"brake held={brakeHeldRotation:0.000}, gas resume={gasResumeRotation:0.000} PASS.");
        }

        private static BuiltAssets LoadImplementedAuditAssets()
        {
            return new BuiltAssets
            {
                jeep = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/MainVehicle.asset"),
                formula = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/FormulaVehicle.asset"),
                rally = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/RallyVehicle.asset"),
                sportBike = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root + "/VehicleData/SportBikeVehicle.asset"),
                economy = AssetDatabase.LoadAssetAtPath<UpgradeEconomyDefinition>(
                    Root + "/VehicleData/UpgradeEconomy.asset"),
                catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(Root + "/VehicleData/GameCatalog.asset"),
                countryside = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/Countryside.asset"),
                northPole = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/NorthPole.asset"),
                space = AssetDatabase.LoadAssetAtPath<MapProfile>(SpaceMapPath),
                arena = AssetDatabase.LoadAssetAtPath<MapProfile>(ArenaMapPath),
                neonCity = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/NeonCity.asset"),
                neonLiquid = AssetDatabase.LoadAssetAtPath<Sprite>(NeonLiquidPath),
                neonCaution = AssetDatabase.LoadAssetAtPath<Sprite>(NeonCautionPath),
                groundPhysics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                    Root + "/Materials/TerrainStable.physicMaterial"),
                tirePhysics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                    Root + "/Materials/VehicleTireArcade.physicMaterial")
            };
        }

        [MenuItem("Anime Hill Racing/Validate Countryside Geometry")]
        public static void ValidateCountrysideGeometry()
        {
            MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/Countryside.asset");
            if (profile == null) { Debug.LogError("Run Build All Game Scenes first."); return; }
            Mesh mesh = new Mesh();
            float endHeight = 0f;
            float endSlope = 0f;
            float maxSlope = 0f;
            float maxDelta = 0f;
            for (int chunk = 0; chunk <= 125; chunk++)
            {
                TerrainGenerator.BuildMesh(mesh, profile, chunk * 80f, 80f, 48, 10f, chunk > 0,
                    endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                for (int i = 1; i < heights.Length; i++)
                {
                    float delta = Mathf.Abs(heights[i] - heights[i - 1]);
                    maxDelta = Mathf.Max(maxDelta, delta);
                    maxSlope = Mathf.Max(maxSlope, Mathf.Atan2(delta, 80f / 48f) * Mathf.Rad2Deg);
                }
            }
            Object.DestroyImmediate(mesh);
            float d150 = profile.EvaluateDifficulty(150f);
            float d3000 = profile.EvaluateDifficulty(3000f);
            float d7500 = profile.EvaluateDifficulty(7500f);
            float d13500 = profile.EvaluateDifficulty(13500f);
            float d15000 = profile.EvaluateDifficulty(15000f);
            float d16500 = profile.EvaluateDifficulty(16500f);
            bool progressive = d150 < d3000 && d3000 < d7500 && d7500 < d13500 && d13500 <= d15000;
            bool capped = Mathf.Approximately(d15000, d16500);
            Debug.Log($"Countryside 0-15km: maxSlope={maxSlope:0.0}° maxDelta={maxDelta:0.00}m; " +
                      $"difficulty 150/3000/7500/13500/15000/16500m=" +
                      $"{d150:0.00}/{d3000:0.00}/{d7500:0.00}/{d13500:0.00}/{d15000:0.00}/{d16500:0.00}; " +
                      $"progression={(progressive ? "OK" : "FAILED")}; cap={(capped ? "OK" : "FAILED")}.");
        }

        [MenuItem("Anime Hill Racing/Validate Game Scenes")]
        public static void ValidateAllGameScenes()
        {
            string[] paths = { MainMenuPath, GaragePath, PreStartPath, Adventure2DPath, Adventure3DPath, TimeTrialPath };
            bool valid = true;
            for (int i = 0; i < paths.Length; i++)
            {
                if (!File.Exists(paths[i]))
                {
                    Debug.LogError($"Missing required scene: {paths[i]}");
                    valid = false;
                    continue;
                }
                Scene scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                bool rebuiltMenu = scene.name == "MainMenu" || scene.name == "Garage" ||
                                   scene.name == "PreStart";
                valid &= rebuiltMenu ? ValidateHillAdventurerMenuScene(scene) : ValidateScene(scene);
            }
            if (valid)
                Debug.Log("Anime Hill Racing validation: all six scenes and runtime references are connected.");
            else
                throw new System.InvalidOperationException(
                    "Anime Hill Racing validation failed. Review the errors above.");
        }

        [MenuItem("Anime Hill Racing/Validate Gameplay Scenes")]
        public static void ValidateGameplayScenes()
        {
            string[] paths = { Adventure2DPath, Adventure3DPath, TimeTrialPath };
            bool valid = true;
            for (int i = 0; i < paths.Length; i++)
            {
                Scene scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                valid &= ValidateScene(scene);
            }
            if (!valid)
                throw new System.InvalidOperationException("Gameplay scene validation failed.");
            Debug.Log("GAMEPLAY SCENE VALIDATION PASS: HUD calibration, authoritative references, " +
                      "half-screen multitouch and automatic-only transmission UI.");
        }

        private static bool ValidateScene(Scene scene)
        {
            bool valid = true;
            valid &= RequireCount(scene, SceneComponents<Canvas>(scene), 1, "Canvas");
            valid &= RequireCount(scene, SceneComponents<SafeArea>(scene), 1, "SafeArea");
            MainMenuUI[] mainMenus = SceneComponents<MainMenuUI>(scene);
            PreStartMenuUI[] preStarts = SceneComponents<PreStartMenuUI>(scene);
            GarageUI[] garages = SceneComponents<GarageUI>(scene);
            GameManager[] managers = SceneComponents<GameManager>(scene);
            GameplayHUD[] huds = SceneComponents<GameplayHUD>(scene);
            ResultsUI[] results = SceneComponents<ResultsUI>(scene);
            if (scene.name == "MainMenu")
            {
                valid &= RequireCount(scene, mainMenus, 1, "MainMenuUI");
                if (mainMenus.Length == 1)
                {
                    string[] fields = { "catalog", "preview", "coinsText", "vehicleNameText" };
                    for (int i = 0; i < fields.Length; i++) valid &= RequireReference(scene, mainMenus[0], fields[i]);
                }
                valid &= RequireCount(scene, SceneComponents<GaragePreviewRenderer>(scene), 1, "selected vehicle preview");
                Image[] images = SceneComponents<Image>(scene);
                bool correctBackground = false;
                Sprite expectedBackground = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuBackgroundPath);
                for (int i = 0; i < images.Length; i++)
                    correctBackground |= images[i] != null && images[i].name == "Garage Background" &&
                                         images[i].sprite == expectedBackground;
                if (!correctBackground)
                {
                    Debug.LogError("MainMenu must use the recovered mainmenu.png background.");
                    valid = false;
                }
                valid &= ValidateDisplayCamera(scene, false);
            }
            else if (scene.name == "Garage")
            {
                valid &= RequireCount(scene, garages, 1, "GarageUI");
                valid &= RequireCount(scene, SceneComponents<GaragePreviewRenderer>(scene), 1, "GaragePreviewRenderer");
                if (garages.Length == 1)
                {
                    string[] fields = { "catalog", "preview", "vehicleName", "categoryName", "skinSummary",
                        "wheelSummary", "driverSummary", "categoryButtons", "optionButtons", "optionImages",
                        "optionLabels", "optionBlueprintFills", "optionBlueprintTexts", "tuningPanel",
                        "tuningNames", "tuningLevels", "tuningValues",
                        "tuningCosts", "tuningButtons", "tuningCoins", "sectionTitle", "upgradeSummary",
                        "vehiclePanel", "customizationPanel", "upgradePanel", "storePanel", "settingsPanel",
                        "confirmationModal", "confirmationTitle", "confirmationValues", "confirmationCost",
                        "confirmationStatus", "confirmationYes", "blueprintButtons", "blueprintImages",
                        "blueprintLabels", "blueprintPageText", "blueprintCoinsText", "blueprintPackButtons" };
                    for (int i = 0; i < fields.Length; i++) valid &= RequireReference(scene, garages[0], fields[i]);
                }
                valid &= ValidateDisplayCamera(scene, false);
            }
            else if (scene.name == "PreStart")
            {
                valid &= RequireCount(scene, preStarts, 1, "PreStartMenuUI");
                if (preStarts.Length == 1)
                {
                    valid &= RequireReference(scene, preStarts[0], "catalog");
                    valid &= RequireReference(scene, preStarts[0], "modeText");
                    valid &= RequireReference(scene, preStarts[0], "mapText");
                    valid &= RequireReference(scene, preStarts[0], "vehicleText");
                    valid &= RequireReference(scene, preStarts[0], "vehiclePreviewRenderer");
                    SerializedObject serialized = new SerializedObject(preStarts[0]);
                    SerializedProperty cards = serialized.FindProperty("modeCards");
                    SerializedProperty tuningPanel = serialized.FindProperty("tuningPanel");
                    SerializedProperty panels = serialized.FindProperty("stepPanels");
                    if (cards == null || cards.arraySize != 3 || panels == null || panels.arraySize != 2 ||
                        (tuningPanel != null && tuningPanel.objectReferenceValue != null))
                    {
                        Debug.LogError("PreStart must contain MODE -> VEHICLE, all three existing modes, and no tuning/transmission selector.");
                        valid = false;
                    }
                    InvokePrivate(preStarts[0], "Start");
                    int activePanels = 0;
                    if (panels != null)
                        for (int i = 0; i < panels.arraySize; i++)
                        {
                            GameObject panel = panels.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                            if (panel != null && panel.activeSelf) activePanels++;
                        }
                    if (activePanels != 1)
                    {
                        Debug.LogError($"PreStart runtime initialization must leave exactly one step visible, found {activePanels}.");
                        valid = false;
                    }
                }
                valid &= RequireCount(scene, SceneComponents<GaragePreviewRenderer>(scene), 1, "selected vehicle preview");
                valid &= RequireCount(scene, SceneComponents<ShifterUI>(scene), 0, "transmission selector");
                valid &= ValidateDisplayCamera(scene, false);
            }
            else
            {
                if (scene.name == "TimeTrial3D")
                {
                    valid &= RequireCount(scene, SceneComponents<ResultsUI>(scene), 1, "ResultsUI");
                    SelectedVehicleFactory[] timeTrialFactories = SceneComponents<SelectedVehicleFactory>(scene);
                    valid &= RequireCount(scene, timeTrialFactories, 1, "SelectedVehicleFactory");
                    if (timeTrialFactories.Length == 1)
                    {
                        string[] fields = { "catalog", "input", "timeTrialManager", "circuitGenerator", "circuit",
                            "timeTrialHud", "results", "capture", "controls", "roadCamera" };
                        for (int i = 0; i < fields.Length; i++)
                            valid &= RequireReference(scene, timeTrialFactories[0], fields[i]);
                    }
                    valid &= RequireCount(scene, SceneComponents<TimeTrialManager>(scene), 1, "TimeTrialManager");
                    TimeTrialHUD[] timeTrialHuds = SceneComponents<TimeTrialHUD>(scene);
                    valid &= RequireCount(scene, timeTrialHuds, 1, "TimeTrialHUD");
                    if (timeTrialHuds.Length == 1)
                    {
                        valid &= RequireReference(scene, timeTrialHuds[0], "speedNeedle");
                        valid &= RequireReference(scene, timeTrialHuds[0], "rpmNeedle");
                        valid &= RequireReference(scene, timeTrialHuds[0], "lowFuelWarning");
                        valid &= ValidateVehicleGauge(scene, timeTrialHuds[0], "rpmNeedle", "RPM Gauge",
                            new Vector2(-150f, 118f), new Vector2(.499694f, .357008f),
                            new Vector2(.136906f, .202061f), 104f, 159f, -49f);
                        valid &= ValidateVehicleGauge(scene, timeTrialHuds[0], "speedNeedle", "KMH Gauge",
                            new Vector2(150f, 118f), new Vector2(.499704f, .316062f),
                            new Vector2(.138627f, .166769f), 116f, 172f, -63f);
                    }
                    valid &= RequireCount(scene, SceneComponents<TimeTrialCircuitGenerator>(scene), 1, "TimeTrialCircuitGenerator");
                    valid &= RequireCount(scene, SceneComponents<ShifterUI>(scene), 0, "manual transmission selector");
                    Text[] timeTrialLabels = SceneComponents<Text>(scene);
                    bool hasJump = false;
                    for (int i = 0; i < timeTrialLabels.Length; i++)
                    {
                        hasJump |= timeTrialLabels[i] != null &&
                                   timeTrialLabels[i].text.Contains("HORIZONTAL JUMP");
                        string objectName = timeTrialLabels[i] != null
                            ? timeTrialLabels[i].name.ToUpperInvariant() : string.Empty;
                        if (objectName == "GEAR" || objectName == "TRANSMISSION")
                        {
                            Debug.LogError($"{scene.name}: legacy {objectName} HUD text still exists.");
                            valid = false;
                        }
                    }
                    CircuitDefinition builtCircuit = AssetDatabase.LoadAssetAtPath<CircuitDefinition>(
                        Root + "/MapProfiles/ProceduralTestCircuit.asset");
                    if (!hasJump || builtCircuit == null || builtCircuit.laps != 1)
                    {
                        Debug.LogError("TimeTrial requires one lap and the +2m Horizontal Jump control.");
                        valid = false;
                    }
                    return valid;
                }
                valid &= RequireCount(scene, managers, 1, "GameManager");
                valid &= RequireCount(scene, huds, 1, "GameplayHUD");
                valid &= RequireCount(scene, results, 1, "ResultsUI");
                SelectedVehicleFactory[] factories = SceneComponents<SelectedVehicleFactory>(scene);
                valid &= RequireCount(scene, factories, 1, "SelectedVehicleFactory");
                if (factories.Length == 1)
                {
                    string[] factoryFields = { "catalog", "input", "gameManager", "gameplayHud", "results",
                        "capture", "controls", "pickupPool" };
                    for (int i = 0; i < factoryFields.Length; i++)
                        valid &= RequireReference(scene, factories[0], factoryFields[i]);
                    string[] modeFields = scene.name == "Adventure2_5D"
                        ? new[] { "terrain", "terrainProfile", "sideCamera" }
                        : new[] { "road", "roadProfile", "roadCamera" };
                    for (int i = 0; i < modeFields.Length; i++)
                        valid &= RequireReference(scene, factories[0], modeFields[i]);
                }
                if (huds.Length == 1)
                {
                    string[] fields = { "distanceText", "bestText", "coinsText", "coinIcon", "nextFuelText",
                        "nextFuelIcon", "fuelText", "fuelIcon", "fuelFill", "lowFuelWarning",
                        "speedNeedle", "rpmNeedle", "debugPanel", "debugText" };
                    for (int i = 0; i < fields.Length; i++) valid &= RequireReference(scene, huds[0], fields[i]);
                    Image fuelFill = new SerializedObject(huds[0]).FindProperty("fuelFill")?.objectReferenceValue as Image;
                    if (fuelFill == null || fuelFill.type != Image.Type.Filled || fuelFill.sprite == null)
                    {
                        Debug.LogError($"{scene.name}: Fuel Fill requires a sprite-backed Filled Image.");
                        valid = false;
                    }
                    Text[] hudLabels = huds[0].GetComponentsInChildren<Text>(true);
                    for (int i = 0; i < hudLabels.Length; i++)
                    {
                        string objectName = hudLabels[i].name.ToUpperInvariant();
                        if (objectName != "RPM" && objectName != "SPEED" && objectName != "GEAR" &&
                            objectName != "TRANSMISSION") continue;
                        Debug.LogError($"{scene.name}: legacy {objectName} HUD text still exists.");
                        valid = false;
                    }
                    valid &= ValidateVehicleGauge(scene, huds[0], "rpmNeedle", "RPM Gauge",
                        new Vector2(-150f, 118f), new Vector2(.499694f, .357008f),
                        new Vector2(.136906f, .202061f), 104f, 159f, -49f);
                    valid &= ValidateVehicleGauge(scene, huds[0], "speedNeedle", "KMH Gauge",
                        new Vector2(150f, 118f), new Vector2(.499704f, .316062f),
                        new Vector2(.138627f, .166769f), 116f, 172f, -63f);
                }
                if (scene.name == "Adventure2_5D")
                {
                    valid &= RequireCount(scene, SceneComponents<VehicleController>(scene), 0, "scene VehicleController");
                    valid &= RequireCount(scene, SceneComponents<VehicleVisual2D>(scene), 0, "scene VehicleVisual2D");
                    valid &= RequireCount(scene, SceneComponents<VehicleVisual3D>(scene), 0, "VehicleVisual3D");
                    valid &= RequireCount(scene, SceneComponents<TerrainManager>(scene), 1, "TerrainManager");
                    valid &= RequireCount(scene, SceneComponents<MountainBackground>(scene), 1, "MountainBackground");
                    MountainBackground[] backgrounds = SceneComponents<MountainBackground>(scene);
                    if (backgrounds.Length == 1) valid &= ValidateCountrysideBackground(scene, backgrounds[0]);
                    valid &= ValidateDisplayCamera(scene, true);
                    ResultCaptureService[] captures = SceneComponents<ResultCaptureService>(scene);
                    valid &= RequireCount(scene, captures, 1, "ResultCaptureService");
                    if (captures.Length == 1)
                    {
                        Camera captureCamera = captures[0].CaptureCamera;
                        RawImage resultImage = captures[0].ResultImage;
                        if (captureCamera == null || !captureCamera.orthographic ||
                            captures[0].SideViewOrthographicSize < 12f)
                        {
                            Debug.LogError($"{scene.name}: ResultCaptureCamera must use the wide 2D orthographic setup.");
                            valid = false;
                        }
                        Canvas.ForceUpdateCanvases();
                        RectTransform photoFrame = resultImage != null
                            ? resultImage.transform.parent as RectTransform : null;
                        RectTransform statsRect = results.Length == 1 && results[0].StatsText != null
                            ? results[0].StatsText.rectTransform : null;
                        Rect photoRect = resultImage != null ? resultImage.rectTransform.rect : default;
                        bool squarePhoto = photoRect.width >= 560f && photoRect.height >= 560f &&
                                           Mathf.Abs(photoRect.width - photoRect.height) <= 1f;
                        if (resultImage == null || !squarePhoto ||
                            photoFrame == null || photoFrame.anchoredPosition.x > -250f ||
                            Mathf.Abs(photoFrame.rect.width - photoFrame.rect.height) > 1f ||
                            statsRect == null || statsRect.anchoredPosition.x < 250f)
                        {
                            Debug.LogError($"{scene.name}: Results must keep an aspect-safe photo on the left " +
                                           "and race stats on the right.");
                            valid = false;
                        }
                    }
                    HoldButton[] touchZones = SceneComponents<HoldButton>(scene);
                    if (touchZones.Length != 2)
                    {
                        Debug.LogError($"{scene.name}: expected exactly two independent Brake/Gas touch zones, found {touchZones.Length}.");
                        valid = false;
                    }
                    for (int i = 0; i < touchZones.Length; i++)
                    {
                        valid &= RequireReference(scene, touchZones[i], "visual");
                        valid &= RequireReference(scene, touchZones[i], "normalSprite");
                        valid &= RequireReference(scene, touchZones[i], "pressedSprite");
                        RectTransform zone = touchZones[i].transform as RectTransform;
                        bool brakeZone = touchZones[i].name.StartsWith("Brake");
                        Vector2 expectedMin = brakeZone ? Vector2.zero : new Vector2(.5f, 0f);
                        Vector2 expectedMax = brakeZone ? new Vector2(.5f, 1f) : Vector2.one;
                        if (zone == null || Vector2.Distance(zone.anchorMin, expectedMin) > .001f ||
                            Vector2.Distance(zone.anchorMax, expectedMax) > .001f)
                        {
                            Debug.LogError($"{scene.name}: {touchZones[i].name} must cover its exact half of the screen.");
                            valid = false;
                        }
                        SerializedProperty visualProperty = new SerializedObject(touchZones[i]).FindProperty("visual");
                        Image pedalVisual = visualProperty?.objectReferenceValue as Image;
                        if (pedalVisual == null || Vector2.Distance(pedalVisual.rectTransform.sizeDelta,
                                new Vector2(265f, 334f)) > .1f || !pedalVisual.preserveAspect)
                        {
                            Debug.LogError($"{scene.name}: {touchZones[i].name} visual must be 265x334 with preserved aspect ratio.");
                            valid = false;
                        }
                    }
                    VehicleInputController[] touchInputs = SceneComponents<VehicleInputController>(scene);
                    EventSystem[] eventSystems = SceneComponents<EventSystem>(scene);
                    if (touchZones.Length == 2 && touchInputs.Length == 1 && eventSystems.Length == 1)
                    {
                        HoldButton brake = touchZones[0].name.StartsWith("Brake") ? touchZones[0] : touchZones[1];
                        HoldButton gas = brake == touchZones[0] ? touchZones[1] : touchZones[0];
                        var brakePointer = new PointerEventData(eventSystems[0]) { pointerId = 101 };
                        var gasPointer = new PointerEventData(eventSystems[0]) { pointerId = 202 };
                        MethodInfo refreshInput = typeof(VehicleInputController).GetMethod("Update",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        VehicleInputController touchInput = touchInputs[0];

                        brake.OnPointerDown(brakePointer);
                        refreshInput?.Invoke(touchInput, null);
                        bool brakeStarts = touchInput.State.BrakeRaw > .5f && touchInput.State.Gas < .5f;
                        gas.OnPointerDown(gasPointer);
                        refreshInput?.Invoke(touchInput, null);
                        bool gasOverridesBrake = touchInput.State.Gas > .5f && touchInput.State.BrakeRaw < .5f;
                        gas.OnPointerUp(gasPointer);
                        refreshInput?.Invoke(touchInput, null);
                        bool brakeResumes = touchInput.State.BrakeRaw > .5f && touchInput.State.Gas < .5f;
                        brake.OnPointerUp(brakePointer);

                        gas.OnPointerDown(gasPointer);
                        refreshInput?.Invoke(touchInput, null);
                        bool gasStarts = touchInput.State.Gas > .5f && touchInput.State.BrakeRaw < .5f;
                        brake.OnPointerDown(brakePointer);
                        refreshInput?.Invoke(touchInput, null);
                        bool brakeOverridesGas = touchInput.State.BrakeRaw > .5f && touchInput.State.Gas < .5f;
                        brake.OnPointerUp(brakePointer);
                        refreshInput?.Invoke(touchInput, null);
                        bool gasResumes = touchInput.State.Gas > .5f && touchInput.State.BrakeRaw < .5f;
                        gas.OnPointerUp(gasPointer);
                        touchInput.SetInputEnabled(false);
                        touchInput.SetInputEnabled(true);

                        if (refreshInput == null || !brakeStarts || !gasOverridesBrake || !brakeResumes ||
                            !gasStarts || !brakeOverridesGas || !gasResumes)
                        {
                            Debug.LogError($"{scene.name}: last-pressed Brake/Gas priority test failed.");
                            valid = false;
                        }
                    }
                    else
                    {
                        Debug.LogError($"{scene.name}: touch input test requires 2 zones, 1 input and 1 EventSystem.");
                        valid = false;
                    }
                }
                else if (scene.name == "Adventure3D")
                {
                    valid &= RequireCount(scene, SceneComponents<RoadVehicleController>(scene), 0, "scene RoadVehicleController");
                    valid &= RequireCount(scene, SceneComponents<VehicleVisual3D>(scene), 0, "scene VehicleVisual3D");
                    valid &= RequireCount(scene, SceneComponents<RoadTrackGenerator>(scene), 1, "RoadTrackGenerator");
                }

                FuelPickup[] fuelPickups = SceneComponents<FuelPickup>(scene);
                CoinPickup[] coinPickups = SceneComponents<CoinPickup>(scene);
                if (fuelPickups.Length != 1)
                {
                    Debug.LogError($"{scene.name}: expected exactly one pooled FuelPickup template, found {fuelPickups.Length}.");
                    valid = false;
                }
                else
                {
                    BoxCollider fuelTrigger = fuelPickups[0].GetComponent<BoxCollider>();
                    SpriteRenderer fuelSprite = fuelPickups[0].GetComponentInChildren<SpriteRenderer>(true);
                    Sprite expectedFuel = AssetDatabase.LoadAssetAtPath<Sprite>(FuelCanPath);
                    if (fuelTrigger == null || !fuelTrigger.isTrigger || fuelTrigger.size.x > 1.05f ||
                        fuelTrigger.size.y > 1.15f || fuelTrigger.size.z > .75f || fuelSprite == null ||
                        fuelSprite.sprite != expectedFuel)
                    {
                        Debug.LogError($"{scene.name}: FuelPickup requires the real Fuel sprite and a tight physical trigger.");
                        valid = false;
                    }
                }
                if (coinPickups.Length != 2)
                {
                    Debug.LogError($"{scene.name}: expected Coin 01 and Coin 05 pooled templates.");
                    valid = false;
                }
                else
                {
                    bool one = false, five = false;
                    for (int i = 0; i < coinPickups.Length; i++)
                    {
                        SphereCollider trigger = coinPickups[i].GetComponent<SphereCollider>();
                        SpriteRenderer sprite = coinPickups[i].GetComponentInChildren<SpriteRenderer>(true);
                        one |= coinPickups[i].Value == 1 && sprite != null &&
                               sprite.sprite == AssetDatabase.LoadAssetAtPath<Sprite>(Coin01Path);
                        five |= coinPickups[i].Value == 5 && sprite != null &&
                                sprite.sprite == AssetDatabase.LoadAssetAtPath<Sprite>(Coin05Path);
                        if (trigger == null || !trigger.isTrigger || trigger.radius > .42f)
                        {
                            Debug.LogError($"{scene.name}: Coin collider must tightly match its visible 0.82m sprite.");
                            valid = false;
                        }
                    }
                    if (!one || !five)
                    {
                        Debug.LogError($"{scene.name}: Coin template sprites/values must be exactly 01/+1 and 05/+5.");
                        valid = false;
                    }
                }
            }

            Button[] buttons = SceneComponents<Button>(scene);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].GetComponent<ButtonPressFeedback>() == null)
                {
                    Debug.LogError($"{scene.name}: button '{buttons[i].name}' is missing immediate mobile press feedback.");
                    valid = false;
                }
                if (buttons[i].GetComponent<HoldButton>() == null && buttons[i].onClick.GetPersistentEventCount() == 0)
                {
                    Debug.LogError($"{scene.name}: button '{buttons[i].name}' has no persistent action.");
                    valid = false;
                }
            }
            valid &= ValidateButtonSpacing(scene, buttons);
            return valid;
        }

        private static bool ValidateButtonSpacing(Scene scene, Button[] buttons)
        {
            Canvas.ForceUpdateCanvases();
            bool valid = true;
            var aCorners = new Vector3[4];
            var bCorners = new Vector3[4];
            for (int a = 0; a < buttons.Length; a++)
            {
                if (buttons[a] == null) continue;
                Transform aStep = FindNamedAncestor(buttons[a].transform, "Step ");
                for (int b = a + 1; b < buttons.Length; b++)
                {
                    if (buttons[b] == null || buttons[a].transform.IsChildOf(buttons[b].transform) ||
                        buttons[b].transform.IsChildOf(buttons[a].transform)) continue;
                    Transform bStep = FindNamedAncestor(buttons[b].transform, "Step ");
                    bool sameInactiveStep = aStep != null && aStep == bStep;
                    if (!sameInactiveStep && (!buttons[a].gameObject.activeInHierarchy ||
                                              !buttons[b].gameObject.activeInHierarchy)) continue;
                    if (aStep != bStep && (aStep != null || bStep != null)) continue;
                    RectTransform aRect = buttons[a].transform as RectTransform;
                    RectTransform bRect = buttons[b].transform as RectTransform;
                    if (aRect == null || bRect == null) continue;
                    aRect.GetWorldCorners(aCorners);
                    bRect.GetWorldCorners(bCorners);
                    float overlapX = Mathf.Min(aCorners[2].x, bCorners[2].x) -
                                     Mathf.Max(aCorners[0].x, bCorners[0].x);
                    float overlapY = Mathf.Min(aCorners[2].y, bCorners[2].y) -
                                     Mathf.Max(aCorners[0].y, bCorners[0].y);
                    if (overlapX <= 2f || overlapY <= 2f) continue;
                    Debug.LogError($"{scene.name}: UI buttons '{buttons[a].name}' and '{buttons[b].name}' overlap " +
                                   $"by {overlapX:0.#} x {overlapY:0.#} px.");
                    valid = false;
                }
            }
            return valid;
        }

        private static Transform FindNamedAncestor(Transform child, string prefix)
        {
            for (Transform current = child; current != null; current = current.parent)
                if (current.name.StartsWith(prefix)) return current;
            return null;
        }

        private static T[] SceneComponents<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) found.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return found.ToArray();
        }

        private static bool ValidateDisplayCamera(Scene scene, bool requireOrthographic)
        {
            Camera[] cameras = SceneComponents<Camera>(scene);
            int renderingCount = 0;
            Camera renderingCamera = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (!cameras[i].enabled || cameras[i].targetDisplay != 0 || cameras[i].targetTexture != null) continue;
                renderingCount++;
                renderingCamera = cameras[i];
            }
            bool valid = renderingCount == 1 && (!requireOrthographic || renderingCamera.orthographic);
            if (!valid)
                Debug.LogError($"{scene.name}: expected one valid Display 1 " +
                               $"{(requireOrthographic ? "orthographic " : string.Empty)}camera, found {renderingCount}.");
            return valid;
        }

        private static bool ValidateCountrysideBackground(Scene scene, MountainBackground background)
        {
            bool valid = true;
            SerializedObject serialized = new SerializedObject(background);
            SerializedProperty segments = serialized.FindProperty("mountainSegments");
            if (segments == null || segments.arraySize < 5)
            {
                Debug.LogError($"{scene.name}: Countryside requires at least five pooled mountain segments.");
                valid = false;
            }
            else
                for (int i = 0; i < segments.arraySize; i++)
                    if (segments.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        Debug.LogError($"{scene.name}: pooled mountain segment {i} is not connected.");
                        valid = false;
                    }
            if (background.GetComponentsInChildren<Collider>(true).Length != 0 ||
                background.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            {
                Debug.LogError($"{scene.name}: sky/mountains must have no Collider or Rigidbody.");
                valid = false;
            }
            return valid;
        }

        private static bool RequireCount<T>(Scene scene, T[] values, int expected, string label)
        {
            if (values.Length == expected) return true;
            Debug.LogError($"{scene.name}: expected {expected} {label}, found {values.Length}.");
            return false;
        }

        private static bool RequireReference(Scene scene, Component component, string field)
        {
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            if (property != null && property.isArray)
            {
                if (property.arraySize == 0)
                {
                    Debug.LogError($"{scene.name}: {component.GetType().Name}.{field} is empty.");
                    return false;
                }
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty element = property.GetArrayElementAtIndex(i);
                    if (element.propertyType == SerializedPropertyType.ObjectReference &&
                        element.objectReferenceValue != null) continue;
                    Debug.LogError($"{scene.name}: {component.GetType().Name}.{field}[{i}] is not connected.");
                    return false;
                }
                return true;
            }
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue != null) return true;
            Debug.LogError($"{scene.name}: {component.GetType().Name}.{field} is not connected.");
            return false;
        }

        [MenuItem("Anime Hill Racing/Validate Wheel Stability Diagnostics")]
        public static void ValidateWheelStabilityDiagnostics()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            ValidateVehiclePhysicsContract(assets);
            ValidateTerrainSeamContinuity(assets.countryside);
            VehicleDefinition[] vehicles = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            var failures = new List<string>();
            for (int v = 0; v < vehicles.Length; v++)
            {
                int maximum = Mathf.Max(0, vehicles[v].suspensionMaxTunes);
                int[] levels = { 0, maximum / 2, maximum };
                for (int i = 0; i < levels.Length; i++)
                {
                    if (i > 0 && levels[i] == levels[i - 1]) continue;
                    WheelStabilityResult result = SimulateWheelStability(vehicles[v], levels[i]);
                    Debug.Log($"Wheel stability {vehicles[v].displayName} L{levels[i]} | " +
                              $"axis={result.maximumAxisDrift:0.0000}deg, anchors={result.maximumAnchorDrift:0.000000}m, " +
                              $"settledSpan={result.settledCompressionSpan:P2}, relWheelV={result.maximumRelativeWheelVelocity:0.000}m/s, " +
                              $"phaseV={result.stationaryRelativeWheelVelocity:0.000}/{result.coastRelativeWheelVelocity:0.000}/" +
                              $"{result.seamRelativeWheelVelocity:0.000}/{result.motorRelativeWheelVelocity:0.000}/" +
                              $"{result.reverseRelativeWheelVelocity:0.000}m/s, " +
                              $"chassisV={result.maximumChassisVerticalVelocity:0.000}m/s, " +
                              $"travelExcess={result.maximumTravelExcess:0.0000}m, " +
                              "motor+reverse+50/100/150/200/top-kmh=RUN.");
                    if (result.maximumAxisDrift > .02f) failures.Add($"{vehicles[v].displayName} L{levels[i]} axis drift");
                    if (result.maximumAnchorDrift > .0001f) failures.Add($"{vehicles[v].displayName} L{levels[i]} anchor drift");
                    if (result.settledCompressionSpan > .10f) failures.Add($"{vehicles[v].displayName} L{levels[i]} stationary jitter");
                    if (result.maximumRelativeWheelVelocity > 2.5f) failures.Add($"{vehicles[v].displayName} L{levels[i]} wheel velocity spike");
                    if (result.maximumChassisVerticalVelocity > 1.5f) failures.Add($"{vehicles[v].displayName} L{levels[i]} chassis vertical spike");
                    if (result.maximumTravelExcess > .04f) failures.Add($"{vehicles[v].displayName} L{levels[i]} joint over-travel");
                }
            }
            if (failures.Count > 0)
                throw new System.InvalidOperationException("WHEEL STABILITY DIAGNOSTICS FAILED: " +
                                                           string.Join("; ", failures));
            Debug.Log("WHEEL STABILITY DIAGNOSTICS PASS: JEEP/FORMULA/RALLY/SPORT BIKE level 0/mid/max, " +
                      "stationary, motor, reverse, 50/100/150/200/top km/h and a collider seam.");
        }

        [MenuItem("Anime Hill Racing/Capture HUD Calibration Frames")]
        public static void CaptureHudCalibrationFrames()
        {
            Scene scene = EditorSceneManager.OpenScene(Adventure2DPath, OpenSceneMode.Single);
            Canvas canvas = SceneComponents<Canvas>(scene).FirstOrDefault();
            GameplayHUD hud = SceneComponents<GameplayHUD>(scene).FirstOrDefault();
            Camera camera = SceneComponents<Camera>(scene).FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (canvas == null || hud == null || camera == null)
                throw new System.InvalidOperationException("Adventure2_5D HUD capture dependencies are missing.");
            RectTransform rpm = new SerializedObject(hud).FindProperty("rpmNeedle")?.objectReferenceValue as RectTransform;
            RectTransform speed = new SerializedObject(hud).FindProperty("speedNeedle")?.objectReferenceValue as RectTransform;
            if (rpm == null || speed == null)
                throw new System.InvalidOperationException("HUD needles are not connected for calibration capture.");

            RenderMode oldRenderMode = canvas.renderMode;
            Camera oldCanvasCamera = canvas.worldCamera;
            RenderTexture oldTarget = camera.targetTexture;
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            camera.targetTexture = target;
            string directory = Path.Combine(Path.GetTempPath(), "AnimeHillRacing_HUD_Calibration");
            Directory.CreateDirectory(directory);
            for (int sample = 0; sample <= 4; sample++)
            {
                float normalized = sample * .25f;
                rpm.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(159f, -49f, normalized));
                speed.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(172f, -63f, normalized));
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(Path.Combine(directory, $"hud_{sample * 25:000}.png"), image.EncodeToPNG());
            }
            RenderTexture.active = null;
            camera.targetTexture = oldTarget;
            canvas.worldCamera = oldCanvasCamera;
            canvas.renderMode = oldRenderMode;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Debug.Log("HUD calibration frames captured at " + directory);
        }

        private static bool ValidateVehicleGauge(Scene scene, Component hud, string needleField, string gaugeName,
            Vector2 expectedPosition, Vector2 expectedFaceCenter, Vector2 expectedPivot,
            float expectedTipLength, float zeroAngle, float fullAngle)
        {
            RectTransform needle = new SerializedObject(hud).FindProperty(needleField)?.objectReferenceValue as RectTransform;
            RectTransform gauge = needle != null ? needle.parent as RectTransform : null;
            Image face = gauge != null ? gauge.GetComponent<Image>() : null;
            bool valid = needle != null && gauge != null && gauge.name == gaugeName && face != null &&
                         face.sprite != null && face.preserveAspect &&
                         Mathf.Abs(gauge.sizeDelta.y - 200f) <= .1f &&
                         Vector2.Distance(gauge.anchoredPosition, expectedPosition) <= .1f &&
                         Vector2.Distance(needle.anchorMin, expectedFaceCenter) <= .0005f &&
                         Vector2.Distance(needle.anchorMax, expectedFaceCenter) <= .0005f &&
                         Vector2.Distance(needle.pivot, expectedPivot) <= .0005f &&
                         Vector2.Distance(needle.anchoredPosition, Vector2.zero) <= .01f &&
                         Vector3.Distance(needle.localScale, Vector3.one) <= .001f;
            if (!valid)
            {
                Debug.LogError($"{scene.name}: {gaugeName} layout/pivot/scale calibration is invalid.");
                return false;
            }

            Vector2 sourceTip = new Vector2((1f - needle.pivot.x) * needle.rect.width,
                (1f - needle.pivot.y) * needle.rect.height);
            if (Mathf.Abs(sourceTip.magnitude - expectedTipLength) > .75f)
            {
                Debug.LogError($"{scene.name}: {gaugeName} needle tip length is {sourceTip.magnitude:0.00}, expected {expectedTipLength:0.00} px.");
                return false;
            }

            Vector2 hub = Vector2.Scale(expectedFaceCenter - new Vector2(.5f, .5f), gauge.rect.size);
            for (int sample = 0; sample <= 4; sample++)
            {
                float normalized = sample * .25f;
                float radians = Mathf.Lerp(zeroAngle, fullAngle, normalized) * Mathf.Deg2Rad;
                Vector2 rotatedTip = new Vector2(
                    sourceTip.x * Mathf.Cos(radians) - sourceTip.y * Mathf.Sin(radians),
                    sourceTip.x * Mathf.Sin(radians) + sourceTip.y * Mathf.Cos(radians));
                Vector2 tip = hub + rotatedTip;
                if (Mathf.Abs(tip.x) <= gauge.rect.width * .5f + .5f &&
                    Mathf.Abs(tip.y) <= gauge.rect.height * .5f + .5f) continue;
                Debug.LogError($"{scene.name}: {gaugeName} needle leaves the dial at {normalized:P0} ({tip}).");
                return false;
            }
            return true;
        }

        private static void ConfigureFormulaSprite(string path, int maximumSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null) throw new FileNotFoundException("Formula texture not found.", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.spritePixelsPerUnit = 100f;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            bool tiled = path == NeonLiquidPath || path == NeonTerrainPath || path == NeonGroundPath ||
                         path == CityTerrainPath || path == CityGroundPath ||
                         path == CountrysideTerrainPath || path == CountrysideGroundPath ||
                         path == NorthPoleTerrainPath || path == NorthPoleGroundPath;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = maximumSize;
            importer.SaveAndReimport();
        }

        private static VehicleDefinition.CustomizationOption Customization(string id, string name, Sprite sprite,
            bool unlockedByDefault = true, int gemPrice = 0, float visibleDiameter = 0f, Vector2 visibleSize = default,
            Vector2 visualOffset = default, bool allowsBlueprintProgression = true) =>
            new VehicleDefinition.CustomizationOption
            {
                optionId = id, displayName = name, sprite = sprite,
                unlockedByDefault = unlockedByDefault, gemPrice = gemPrice,
                bundleType = BundleType.Cosmetic, allowsBlueprintProgression = allowsBlueprintProgression,
                visibleDiameter = visibleDiameter,
                visibleSize = visibleSize,
                visualOffset = visualOffset
            };

        private static Vector2 VisibleSpriteSize(Sprite sprite)
        {
            if (sprite == null) return Vector2.one;
            string path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return sprite.bounds.size;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                Object.DestroyImmediate(texture);
                return sprite.bounds.size;
            }
            Color32[] pixels = texture.GetPixels32();
            int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    // Every non-transparent pixel belongs to the authored visible bounds. A threshold
                    // would incorrectly discard the Sport Bike's faint antialiased silhouette.
                    if (pixels[y * texture.width + x].a == 0) continue;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
            Object.DestroyImmediate(texture);
            if (maxX < minX || maxY < minY) return sprite.bounds.size;
            float pixelsPerUnit = Mathf.Max(.001f, sprite.pixelsPerUnit);
            return new Vector2((maxX - minX + 1) / pixelsPerUnit,
                (maxY - minY + 1) / pixelsPerUnit);
        }

        private static float VisibleSpriteDiameter(Sprite sprite)
        {
            Vector2 size = VisibleSpriteSize(sprite);
            return Mathf.Max(size.x, size.y);
        }

        private static void ValidateCustomizationContract(BuiltAssets assets)
        {
            if (assets.catalog == null || assets.catalog.vehicles == null || assets.catalog.vehicles.Length < 4)
                throw new System.InvalidOperationException("Garage requires Jeep, Formula, Rally and Sport Bike.");
            if (assets.jeepRetroSkin == null || assets.jeepStarryWheel == null ||
                assets.formulaNeonSkin == null || assets.formulaElectricitySkin == null ||
                assets.formulaHillAdventurerSkin == null || assets.hillAdventurerPurpleSkin == null ||
                assets.luxuryWheel == null ||
                assets.rallyGhostyWheel == null || assets.rallySkyBlueWheel == null ||
                assets.sportBikeRetroLuxe == null || assets.sportBikeNeonFlowSkin == null ||
                assets.sportBikeGhostyWheel == null || assets.sportBikeNeonFlowWheel == null)
                throw new System.InvalidOperationException("One or more premium garage sprites are missing.");
            if (assets.jeep.skins == null || assets.jeep.skins.Length != 2 || assets.jeep.wheels == null ||
                assets.jeep.wheels.Length != 3 || assets.jeep.skins[1].optionId != "jeep-skin-retro" ||
                assets.jeep.skins[1].sprite != assets.jeepRetroSkin || assets.jeep.skins[1].unlockedByDefault ||
                assets.jeep.skins[1].gemPrice != 1000 || assets.jeep.wheels[2].optionId != "jeep-wheel-starry" ||
                assets.jeep.wheels[2].sprite != assets.jeepStarryWheel || assets.jeep.wheels[2].unlockedByDefault ||
                assets.jeep.wheels[2].gemPrice != 500)
                throw new System.InvalidOperationException(
                    "Jeep requires Default/Retro skins and Wheel 01/Wheel 02/Starry Garage options.");
            if (assets.formula.skins == null || assets.formula.skins.Length != 4 || assets.formula.wheels == null ||
                assets.formula.wheels.Length != 3 || assets.formula.skins[1].sprite != assets.formulaNeonSkin ||
                assets.formula.skins[1].unlockedByDefault || assets.formula.skins[1].gemPrice != 1000 ||
                assets.formula.skins[2].optionId != FormulaElectricSkinEffect.ElectricSkinId ||
                assets.formula.skins[2].sprite != assets.formulaElectricitySkin ||
                assets.formula.skins[2].unlockedByDefault || assets.formula.skins[2].gemPrice != 1000 ||
                assets.formula.skins[3].optionId != "formula-skin-hill-adventurer-gold" ||
                assets.formula.skins[3].sprite != assets.formulaHillAdventurerSkin ||
                assets.formula.skins[3].unlockedByDefault || assets.formula.skins[3].gemPrice != 0 ||
                assets.formula.skins[3].allowsBlueprintProgression ||
                assets.formula.wheels[0].sprite != assets.formulaWheel02 ||
                assets.formula.wheels[1].sprite != assets.formulaWheel01 ||
                assets.formula.wheels[1].unlockedByDefault || assets.formula.wheels[1].gemPrice != 500 ||
                assets.formula.wheels[2].optionId != "formula-wheel-luxury" ||
                assets.formula.wheels[2].sprite != assets.luxuryWheel ||
                assets.formula.electricSkinOverlay != assets.formulaElectricitySkin ||
                assets.formula.wheels[2].unlockedByDefault || assets.formula.wheels[2].gemPrice != 0 ||
                assets.formula.wheels[2].allowsBlueprintProgression)
                throw new System.InvalidOperationException(
                    "Formula Default/Neon/Electricity/Hill Adventurer or Ghosty/Luxury Garage contract is invalid.");
            if (assets.hillAdventurer.skins == null || assets.hillAdventurer.skins.Length != 2 ||
                assets.hillAdventurer.skins[1].optionId != "hill-adventurer-skin-purple-4x4" ||
                assets.hillAdventurer.skins[1].sprite != assets.hillAdventurerPurpleSkin ||
                assets.hillAdventurer.skins[1].unlockedByDefault ||
                assets.hillAdventurer.skins[1].gemPrice != 1000)
                throw new System.InvalidOperationException(
                    "Hill Adventurer requires the Default and Purple 4x4 Garage skins.");
            if (assets.rally.skins == null || assets.rally.skins.Length < 1 || assets.rally.wheels == null ||
                assets.rally.wheels.Length != 4 || assets.rally.skins[0].sprite != assets.rallyBody ||
                assets.rally.wheels[1].sprite != assets.rallyGhostyWheel ||
                assets.rally.wheels[2].sprite != assets.rallySkyBlueWheel ||
                assets.rally.wheels[3].optionId != "rally-wheel-luxury" ||
                assets.rally.wheels[3].sprite != assets.luxuryWheel ||
                assets.rally.wheels.Skip(1).Any(wheel => wheel.unlockedByDefault || wheel.gemPrice != 500))
                throw new System.InvalidOperationException(
                    "Rally must expose its base wheel plus Ghosty, Sky Blue and Luxury at 500 Gems each.");
            if (assets.catalog.vehicles.SelectMany(vehicle => vehicle.wheels).Any(wheel =>
                    wheel.optionId.IndexOf("rainbow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    wheel.displayName.IndexOf("rainbow", System.StringComparison.OrdinalIgnoreCase) >= 0))
                throw new System.InvalidOperationException("Rainbow wheels must not remain selectable.");
            if (assets.sportBike == null || assets.sportBike.skins == null || assets.sportBike.skins.Length != 4 ||
                assets.sportBike.wheels == null || assets.sportBike.wheels.Length != 5 ||
                assets.sportBike.skins[0].optionId != "sport-bike-default" ||
                assets.sportBike.skins[0].sprite != assets.sportBikeBody ||
                assets.sportBike.skins[1].optionId != "sport-bike-sun-ray" ||
                assets.sportBike.skins[1].sprite != assets.sportBikeSunRay ||
                assets.sportBike.skins[1].unlockedByDefault || assets.sportBike.skins[1].gemPrice != 1000 ||
                assets.sportBike.skins[2].optionId != "sport-bike-retro-luxe" ||
                assets.sportBike.skins[2].sprite != assets.sportBikeRetroLuxe ||
                assets.sportBike.skins[2].unlockedByDefault || assets.sportBike.skins[2].gemPrice != 1000 ||
                assets.sportBike.skins[3].optionId != SportBikeNeonFlowEffect.SkinId ||
                assets.sportBike.skins[3].sprite != assets.sportBikeNeonFlowSkin ||
                assets.sportBike.skins[3].unlockedByDefault || assets.sportBike.skins[3].gemPrice != 1000 ||
                !SportBikeNeonFlowEffect.IsNeonSkin(assets.sportBike, assets.sportBike.skins[3]) ||
                assets.sportBike.wheels[2].unlockedByDefault || assets.sportBike.wheels[2].gemPrice != 500 ||
                assets.sportBike.wheels[3].sprite != assets.sportBikeGhostyWheel ||
                assets.sportBike.wheels[3].unlockedByDefault || assets.sportBike.wheels[3].gemPrice != 500 ||
                assets.sportBike.wheels[4].optionId != SportBikeNeonFlowEffect.WheelId ||
                assets.sportBike.wheels[4].sprite != assets.sportBikeNeonFlowWheel ||
                assets.sportBike.wheels[4].unlockedByDefault || assets.sportBike.wheels[4].gemPrice != 500 ||
                !SportBikeNeonFlowEffect.IsNeonWheel(assets.sportBike, assets.sportBike.wheels[4]))
                throw new System.InvalidOperationException(
                    "Sport Bike Default/Sun Ray/Retro Luxe/Neon Flow skin and wheel contract is invalid.");
            Vector2 defaultBikeBodySize = VehicleDefinition.VisibleSize(assets.sportBike.skins[0]);
            Vector3 defaultBikeBodyScale = assets.sportBike.BodyCustomizationScale(assets.sportBike.skins[0]);
            for (int skinIndex = 2; skinIndex <= 3; skinIndex++)
            {
                Vector2 candidateBodySize = VehicleDefinition.VisibleSize(assets.sportBike.skins[skinIndex]);
                Vector3 candidateBodyScale = assets.sportBike.BodyCustomizationScale(
                    assets.sportBike.skins[skinIndex]);
                if (Mathf.Abs(defaultBikeBodySize.x * defaultBikeBodyScale.x -
                              candidateBodySize.x * candidateBodyScale.x) > .0001f ||
                    Mathf.Abs(defaultBikeBodySize.y * defaultBikeBodyScale.y -
                              candidateBodySize.y * candidateBodyScale.y) > .0001f)
                    throw new System.InvalidOperationException(
                        $"Sport Bike {assets.sportBike.skins[skinIndex].displayName} must match the " +
                        "Default body's real visible width and height.");
            }
            if (assets.stickmanBlack == null || assets.stickmanBlack.head == null || assets.stickmanBlack.torso == null ||
                assets.stickmanBlack.upperArm == null || assets.stickmanBlack.forearm == null ||
                assets.stickmanBlack.thigh == null || assets.stickmanBlack.lowerLeg == null)
                throw new System.InvalidOperationException("Stickman Black articulated parts are incomplete.");
            if (assets.countrysideEnvironment == null || assets.countrysideEnvironment.sky == null ||
                assets.countrysideEnvironment.mountains == null ||
                assets.countrysideEnvironment.mountainParallaxX < .15f ||
                assets.countrysideEnvironment.mountainParallaxX > .25f ||
                assets.countrysideEnvironment.mountainParallaxY < .03f ||
                assets.countrysideEnvironment.mountainParallaxY > .06f ||
                !assets.countrysideEnvironment.lockVerticalToCamera ||
                !assets.countrysideEnvironment.mirrorAlternatingMountainTiles ||
                assets.countrysideEnvironment.mountainVisualHeight < 18f ||
                assets.countrysideEnvironment.mountainVisualHeight > 20f ||
                assets.countrysideEnvironment.mountainBaseY < .5f ||
                assets.countrysideEnvironment.mountainBaseY > 2f)
                throw new System.InvalidOperationException("Countryside environment assets or parallax bounds are invalid.");
            if (AssetDatabase.LoadAssetAtPath<Sprite>(FormulaReferencePath) == assets.formula.previewSprite)
                throw new System.InvalidOperationException("Formula_Reference must never be used as the selectable vehicle skin.");
            Debug.Log("Garage validation: real vehicle/driver/environment assets, independent options and poses are connected.");
        }

        private static void ValidateStuntRotationContract()
        {
            var tracker = new StuntRotationTracker();
            tracker.Reset(0f);
            for (int angle = 1; angle <= 359; angle++) tracker.SampleAngle(angle, true, true);
            if (tracker.Flips != 0 || tracker.Backflips != 0 || tracker.StuntBonus != 0)
                throw new System.InvalidOperationException("A 0-359 degree rotation was counted as a stunt.");
            tracker.SampleAngle(0f, true, true);
            if (tracker.Flips != 0 || tracker.Backflips != 1 || tracker.StuntBonus != 5)
                throw new System.InvalidOperationException("A positive 360 degree Backflip was not counted once.");
            for (int rotation = 0; rotation < 720; rotation++)
                tracker.SampleDelta(1f, true, true);
            if (tracker.Backflips != 3 || tracker.StuntBonus != 15)
                throw new System.InvalidOperationException("A 720 degree Backflip was not counted twice.");

            tracker.Reset(0f);
            for (int rotation = 0; rotation < 1080; rotation++)
                tracker.SampleDelta(-1f, true, true);
            if (tracker.Flips != 3 || tracker.Backflips != 0 || tracker.StuntBonus != 15)
                throw new System.InvalidOperationException("A 1080 degree Flip was not counted three times.");

            tracker.Reset(0f);
            for (int rotation = 0; rotation < 720; rotation++)
                tracker.SampleDelta(-1f, true, true);
            for (int rotation = 0; rotation < 1080; rotation++)
                tracker.SampleDelta(1f, true, true);
            if (tracker.Flips != 2 || tracker.Backflips != 3 || tracker.StuntBonus != 25)
                throw new System.InvalidOperationException(
                    "Two Flips plus three Backflips must produce exactly 25 Stunt Bonus.");

            tracker.Reset(0f);
            for (int rotation = 0; rotation < 270; rotation++)
                tracker.SampleDelta(-1f, true, true);
            tracker.SampleDelta(0f, false, true);
            for (int rotation = 0; rotation < 90; rotation++)
                tracker.SampleDelta(-1f, true, true);
            tracker.SampleDelta(360f, true, false);
            if (tracker.Flips != 0 || tracker.Backflips != 0 || tracker.StuntBonus != 0)
                throw new System.InvalidOperationException(
                    "Partial rotations crossed a landing/game-over boundary or a teleport was counted.");
            tracker.Reset();
            if (tracker.Flips != 0 || tracker.Backflips != 0 || tracker.StuntBonus != 0)
                throw new System.InvalidOperationException("Fresh-run stunt counters did not reset to zero.");
            Debug.Log("STUNT_ROTATION_TEST: 0-359=0, +360 Backflip=1, +720=2, " +
                      "-1080 Flip=3, 2 Flips + 3 Backflips = 25, landing/game-over reset PASS; " +
                      "bonus=(Flips+Backflips)*5 only.");
        }

        private static void ValidateNorthPoleContract(BuiltAssets assets)
        {
            MapProfile profile = assets.northPole;
            EnvironmentDefinition environment = profile != null ? profile.environment : null;
            if (profile == null || profile == assets.countryside || profile.mapId != "north-pole" ||
                profile.elevationProfile == null || profile.elevationProfile != assets.northPoleElevation ||
                profile.coinRoute != assets.countrysideCoinRoute || profile.supportsLiquidExposure ||
                profile.terrainMaterial != assets.northPoleMaterial ||
                profile.terrainEdgeMaterial != assets.northPoleEdgeMaterial)
                throw new System.InvalidOperationException(
                    "North Pole must be an independent 2D profile using the shared stable terrain and pickup systems.");
            if (environment == null || environment.sky != assets.northPoleSky ||
                environment.mountains != assets.northPolePanorama ||
                environment.nearMountains != assets.northPoleNearPines ||
                environment.mountainParallaxX <= 0f ||
                environment.nearMountainParallaxX <= environment.mountainParallaxX ||
                !environment.useRelativeParallaxSpeeds ||
                !environment.lockVerticalToCamera || !environment.lockNearMountainsVerticalToCamera)
                throw new System.InvalidOperationException(
                    "North Pole requires sky, distant panorama and near-pine layers with ordered parallax speeds.");
            if (profile.amplitude <= assets.countryside.amplitude * 1.5f ||
                profile.sectionLength <= assets.countryside.sectionLength ||
                profile.minimumValleyWidth <= assets.countryside.minimumValleyWidth ||
                profile.smoothingPasses < assets.countryside.smoothingPasses ||
                profile.maxSlopeAngle <= assets.countryside.maxSlopeAngle)
                throw new System.InvalidOperationException(
                    "North Pole terrain parameters must remain more aggressive, broad and smooth than Countryside.");

            TerrainDifficultyAudit countryside = AuditGeneratedTerrain(assets.countryside, 6000f);
            TerrainDifficultyAudit northPole = AuditGeneratedTerrain(profile, 6000f);
            // Countryside's pointed-terrain update raised its authored cap from 32 to 38 degrees.
            // North Pole remains broader and steeper at 40 degrees; requiring a 3-degree measured
            // lead is now impossible while still respecting North Pole's own safety cap.
            const float minimumNorthPoleSlopeLead = 1.5f;
            if (northPole.HeightRange < countryside.HeightRange * 1.35f ||
                northPole.maximumSlope < countryside.maximumSlope + minimumNorthPoleSlopeLead ||
                northPole.maximumSlope > profile.maxSlopeAngle + .1f)
                throw new System.InvalidOperationException(
                    $"North Pole terrain audit failed: range/slope={northPole.HeightRange:0.0}m/" +
                    $"{northPole.maximumSlope:0.0} degrees; Countryside={countryside.HeightRange:0.0}m/" +
                    $"{countryside.maximumSlope:0.0} degrees.");
            Debug.Log($"NORTH_POLE_TERRAIN_PASS: 0-6000m range {northPole.HeightRange:0.0}m " +
                      $"(Countryside {countryside.HeightRange:0.0}m), max slope " +
                      $"{northPole.maximumSlope:0.0} degrees, two-layer continuous parallax configured.");
        }

        private static void ValidateSpaceContract(BuiltAssets assets)
        {
            MapProfile profile = assets.space != null
                ? assets.space : AssetDatabase.LoadAssetAtPath<MapProfile>(SpaceMapPath);
            if (profile == null || profile.mapId != "space" || !profile.useSpaceTerrain ||
                profile.supportsLiquidExposure || profile.environment == null ||
                profile.spaceChallenges == null || profile.terrainMaterial == null ||
                profile.terrainEdgeMaterial == null || profile.spaceChallenges.rampMaterial == null ||
                profile.spaceChallenges.blackHoleSprite == null || profile.spaceChallenges.warningSprite == null)
                throw new System.InvalidOperationException(
                    "Space requires an independent profile, lunar materials and complete pooled void artwork.");

            SpaceChallengeSettings settings = profile.spaceChallenges;
            SpaceChallengeLayout first = TerrainGenerator.GetSpaceChallenge(profile, 1);
            SpaceChallengeLayout second = TerrainGenerator.GetSpaceChallenge(profile, 2);
            SpaceChallengeLayout third = TerrainGenerator.GetSpaceChallenge(profile, 3);
            SpaceChallengeLayout fourth = TerrainGenerator.GetSpaceChallenge(profile, 4);
            if (Mathf.Abs(first.eventDistance - 2000f) > .01f ||
                Mathf.Abs(second.eventDistance - 4000f) > .01f ||
                Mathf.Abs(third.eventDistance - 6000f) > .01f ||
                Mathf.Abs(fourth.eventDistance - 8000f) > .01f ||
                Mathf.Abs(first.warningX - 1900f) > .01f ||
                !first.IsVoid || !second.IsVoid || !third.IsVoid || !fourth.IsVoid ||
                Mathf.Abs(settings.eventInterval - 2000f) > .01f ||
                Mathf.Abs(settings.gapIncreasePerCycle - 5f) > .01f ||
                Mathf.Abs(settings.approachLength - 200f) > .01f ||
                Mathf.Abs(TerrainGenerator.SampleHeight(profile, first.rampStart - 199f) -
                          first.baselineHeight) > .001f ||
                Mathf.Abs(TerrainGenerator.SampleHeight(profile, first.rampStart - 1f) -
                          first.baselineHeight) > .001f ||
                TerrainGenerator.IsTerrainGap(profile, first.rampStart + 1f) ||
                TerrainGenerator.SampleHeight(profile, first.gapStart - 1f) <= first.baselineHeight + 1f ||
                Mathf.Abs(first.gapLength - 15f) > .01f || Mathf.Abs(second.gapLength - 20f) > .01f ||
                Mathf.Abs(third.gapLength - 25f) > .01f || Mathf.Abs(fourth.gapLength - 30f) > .01f ||
                !TerrainGenerator.IsTerrainGap(profile, (first.gapStart + first.gapEnd) * .5f) ||
                !TerrainGenerator.IsTerrainGap(profile, (second.gapStart + second.gapEnd) * .5f) ||
                TerrainGenerator.IsTerrainGap(profile, first.gapEnd - .25f) ||
                TerrainGenerator.IsTerrainGap(profile, first.gapEnd + 1f) ||
                Mathf.Abs(TerrainGenerator.GetSpaceChallenge(profile, 10).gapLength - 60f) > .01f ||
                Mathf.Abs(first.blackHoleDiameter - first.gapLength) > .01f ||
                Mathf.Abs(second.blackHoleDiameter - second.gapLength) > .01f ||
                Mathf.Abs(SpaceGapEvent.CalculateBlackHoleVisualHeight(first.gapLength) -
                          SpaceGapEvent.CalculateBlackHoleVisualHeight(fourth.gapLength)) > .01f ||
                profile.hardEndDistance != 10000f ||
                !profile.fitAuthoredTerrainTexture ||
                profile.terrainSurfaceContactVSamples == null ||
                profile.terrainSurfaceContactVSamples.Length != 0 ||
                Mathf.Abs(profile.terrainTextureRepeatWidth -
                          TerrainGenerator.SpaceGroundModuleWidth) > .001f ||
                Mathf.Abs(profile.terrainMaximumVisualExtent -
                          TerrainGenerator.SpaceGroundMaximumVisualExtent) > .001f ||
                Mathf.Abs(profile.terrainMaximumVisualRiseAboveSurface -
                          TerrainGenerator.SpaceGroundMaximumVisualRise) > .001f ||
                Vector2.Distance(profile.terrainSurfaceUvRange, new Vector2(.177f, .714f)) > .001f ||
                Mathf.Abs(profile.terrainSurfaceContactV - .56f) > .001f ||
                profile.terrainEdgeMaterial.mainTexture == null ||
                AssetDatabase.GetAssetPath(profile.terrainEdgeMaterial.mainTexture) !=
                SpaceGroundPath ||
                profile.terrainMaterial.mainTexture == null ||
                AssetDatabase.GetAssetPath(profile.terrainMaterial.mainTexture) != SpaceTerrainPath ||
                settings.rampRise < 5f ||
                settings.rampFlatFraction > .1f)
                throw new System.InvalidOperationException(
                    "Space requires a black-hole void every 2000m with 5m horizontal growth per event.");

            EnvironmentDefinition environment = profile.environment;
            if (!environment.overrideCameraBackground || environment.sky == null ||
                environment.mountains == null || environment.sun != null || environment.earth != null ||
                !environment.useRelativeParallaxSpeeds || !environment.coverViewportVertically ||
                !environment.useCinematicSpacePanorama || environment.cinematicSpacePanoramaShader == null ||
                environment.starTwinkleAmount < .25f || !environment.enableShootingStars ||
                AssetDatabase.GetAssetPath(environment.sky) != SpaceStarfieldPath ||
                AssetDatabase.GetAssetPath(environment.mountains) != SpaceGalaxyPath)
                throw new System.InvalidOperationException(
                    "Space needs its cinematic panorama, independent twinkling and shooting stars, with no Sun or Earth layer.");
            float introMinimum = float.MaxValue;
            float introMaximum = float.MinValue;
            for (int distance = 0; distance <= 100; distance += 5)
            {
                float height = TerrainGenerator.SampleHeight(profile, distance);
                introMinimum = Mathf.Min(introMinimum, height);
                introMaximum = Mathf.Max(introMaximum, height);
            }
            if (Mathf.Abs(TerrainGenerator.SampleHeight(profile, 0f) - profile.baseHeight) > .01f ||
                introMaximum - introMinimum > 2f)
                throw new System.InvalidOperationException(
                    "Space requires a low-relief, spawn-safe opening for its first 100 metres.");
            if (assets.northPole != null &&
                (profile.EvaluateDifficulty(0f) <= assets.northPole.EvaluateDifficulty(0f) ||
                 profile.amplitude <= assets.northPole.amplitude))
                throw new System.InvalidOperationException(
                    "Space must recover its high difficulty after the protected 100m opening.");
            Debug.Log("SPACE_TRACK_PASS: spawn-safe 100m opening, hard lunar terrain, pooled 2000m void events, " +
                      "width-fitted black holes, integrated ramps, 100m warnings, dark galaxies, auroras and animated shooting stars configured.");
        }

        private static void ValidateArenaContract(BuiltAssets assets)
        {
            MapProfile profile = assets.arena != null
                ? assets.arena : AssetDatabase.LoadAssetAtPath<MapProfile>(ArenaMapPath);
            if (profile == null || profile.mapId != "arena" || !profile.useArenaTerrain ||
                profile.useSpaceTerrain || profile.useCityHighwayTerrain || profile.supportsLiquidExposure ||
                profile.arena == null || profile.environment == null || profile.terrainMaterial == null ||
                profile.terrainEdgeMaterial != profile.terrainMaterial || profile.useSeparateTerrainMaterials ||
                profile.terrainMaterial.mainTexture == null)
                throw new System.InvalidOperationException(
                    "Arena requires its independent stunt generator and one unified textured dirt material.");

            ArenaSettings settings = profile.arena;
            if (settings.tireSprite == null || settings.platformSprite == null || settings.lavaSprite == null ||
                settings.recentSegmentHistory < 4 || settings.maximumTechnicalStreak > 3 ||
                settings.minimumBumpWavelength < 5f || settings.maximumBumpWavelength <= settings.minimumBumpWavelength ||
                settings.maximumPlatformTilt < 20f || settings.maximumPlatformTilt > 30f ||
                settings.easyPlatformWidth <= settings.hardPlatformWidth || settings.lavaDragStrength < 4f ||
                settings.lavaMaximumSpeed > 8f || settings.lavaRecoverySeconds <= 0f)
                throw new System.InvalidOperationException(
                    "Arena stunt assets, coarse bumps, platform limits or temporary lava physics are invalid.");

            EnvironmentDefinition environment = profile.environment;
            if (environment.environmentId != "arena" || !environment.overrideCameraBackground ||
                environment.sky == null || environment.mountains == null ||
                environment.clouds == null || environment.clouds.Length != 0 ||
                !environment.useRelativeParallaxSpeeds || !environment.lockVerticalToCamera ||
                environment.mountainParallaxX <= 0f || environment.mountainParallaxX > .08f ||
                environment.sun != null || environment.earth != null ||
                environment.cameraBackgroundColor.maxColorComponent < .12f ||
                environment.cameraBackgroundColor.maxColorComponent > .28f ||
                Mathf.Abs(environment.cameraBackgroundColor.r - environment.cameraBackgroundColor.g) > .01f ||
                Mathf.Abs(environment.cameraBackgroundColor.g - environment.cameraBackgroundColor.b) > .01f)
                throw new System.InvalidOperationException(
                    "Arena needs its neutral night sky, pooled stadium grandstand and stable slow parallax.");

            var history = new List<ArenaSegmentType>(96);
            var encountered = new HashSet<ArenaSegmentType>();
            int recoveryCount = 0;
            int technicalStreak = 0;
            bool foundTires = false, foundPlatform = false, foundLava = false, foundLongJump = false;
            ArenaSegmentLayout physicalLayout = default;
            bool foundCombinedPhysicalLayout = false;
            for (int segment = 0; segment < 96; segment++)
            {
                ArenaSegmentLayout layout = TerrainGenerator.GetArenaSegment(profile, segment);
                encountered.Add(layout.type);
                if (segment < 5 && (layout.HasGap || layout.HasLava))
                    throw new System.InvalidOperationException(
                        $"Arena generated an extreme obstacle before its 500m learning runway at segment {segment}.");
                for (int back = 1; back <= settings.recentSegmentHistory && history.Count - back >= 0; back++)
                    if (history[history.Count - back] == layout.type)
                        throw new System.InvalidOperationException(
                            $"Arena repeated {layout.type} inside its {settings.recentSegmentHistory}-segment history at {segment}.");
                history.Add(layout.type);

                bool technical = layout.type == ArenaSegmentType.FloatingPlatform ||
                                 layout.type == ArenaSegmentType.LongJump ||
                                 layout.type == ArenaSegmentType.MixedObstacle ||
                                 layout.type == ArenaSegmentType.TireObstacle ||
                                 layout.type == ArenaSegmentType.Lava;
                technicalStreak = technical ? technicalStreak + 1 : 0;
                if (technicalStreak > settings.maximumTechnicalStreak)
                    throw new System.InvalidOperationException("Arena generated too many technical segments without recovery.");
                if (layout.type == ArenaSegmentType.Recovery) recoveryCount++;

                float boundary = layout.start;
                if (segment > 0 && Mathf.Abs(TerrainGenerator.SampleHeight(profile, boundary - .02f) -
                                             TerrainGenerator.SampleHeight(profile, boundary + .02f)) > .18f)
                    throw new System.InvalidOperationException($"Arena terrain is discontinuous at segment {segment}.");

                if (layout.HasGap)
                {
                    float gapLength = layout.gapEnd - layout.gapStart;
                    if (gapLength < settings.minimumGapLength * .85f ||
                        gapLength > settings.maximumGapLength * 1.1f ||
                        !TerrainGenerator.IsTerrainGap(profile, (layout.gapStart + layout.gapEnd) * .5f) ||
                        TerrainGenerator.IsTerrainGap(profile, layout.gapStart - .02f) ||
                        TerrainGenerator.IsTerrainGap(profile, layout.gapEnd) ||
                        layout.end - layout.gapEnd < settings.landingSafetyLength)
                        throw new System.InvalidOperationException($"Arena gap/landing envelope is invalid at segment {segment}.");
                    if (layout.type == ArenaSegmentType.LongJump && gapLength > 20f)
                        throw new System.InvalidOperationException(
                            $"Arena long jump exceeds its conservative vehicle reach at segment {segment}.");
                }
                if (layout.HasPlatform)
                {
                    foundPlatform = true;
                    if (!layout.HasGap || layout.platformWidth < settings.hardPlatformWidth * .9f ||
                        layout.platformCenterX - layout.platformWidth * .5f < layout.gapStart + .9f ||
                        layout.platformCenterX + layout.platformWidth * .5f > layout.gapEnd - .9f)
                        throw new System.InvalidOperationException(
                            $"Arena platform is outside its reachable authored gap at segment {segment}.");
                }
                if (layout.HasGap && layout.HasLava &&
                    layout.lavaStart - layout.gapEnd < settings.landingSafetyLength - .01f)
                    throw new System.InvalidOperationException(
                        $"Arena lava overlaps the protected landing runway at segment {segment}.");
                if (layout.HasGap && layout.HasTires)
                {
                    float rampStart = layout.gapStart - Mathf.Lerp(17f, 13f, layout.difficulty);
                    float lastTire = layout.tireStart + Mathf.Max(0, layout.tireCount - 1) * layout.tireSpacing;
                    if (lastTire + settings.tireRadius + .5f > rampStart)
                        throw new System.InvalidOperationException(
                            $"Arena tires overlap the launch ramp at segment {segment}.");
                }
                foundTires |= layout.HasTires;
                foundLava |= layout.HasLava;
                foundLongJump |= layout.type == ArenaSegmentType.LongJump && layout.HasGap;
                if (!foundCombinedPhysicalLayout && layout.HasTires && layout.HasPlatform && layout.HasLava)
                {
                    physicalLayout = layout;
                    foundCombinedPhysicalLayout = true;
                }
            }
            if (encountered.Count < 9 || recoveryCount < 5 || !foundTires || !foundPlatform ||
                !foundLava || !foundLongJump)
                throw new System.InvalidOperationException(
                    "Arena did not expose enough distinct obstacle, jump and recovery categories in its deterministic audit.");

            if (!foundCombinedPhysicalLayout)
            {
                for (int segment = 96; segment < 320 && !foundCombinedPhysicalLayout; segment++)
                {
                    ArenaSegmentLayout layout = TerrainGenerator.GetArenaSegment(profile, segment);
                    if (!layout.HasTires || !layout.HasPlatform || !layout.HasLava) continue;
                    physicalLayout = layout;
                    foundCombinedPhysicalLayout = true;
                }
            }
            if (!foundCombinedPhysicalLayout)
                throw new System.InvalidOperationException("Arena never generated its advanced combined obstacle segment.");

            GameObject auditObject = new GameObject("Arena Physics Contract Audit");
            try
            {
                ArenaSegmentEvent arenaEvent = auditObject.AddComponent<ArenaSegmentEvent>();
                arenaEvent.Configure(profile, physicalLayout, 2f);
                Physics2D.SyncTransforms();
                if (!arenaEvent.AuditPhysicsComponents(physicalLayout, settings, out string problem))
                    throw new System.InvalidOperationException("Arena physics audit failed: " + problem + ".");
            }
            finally
            {
                Object.DestroyImmediate(auditObject);
            }
            Debug.Log($"ARENA_TRACK_PASS: {encountered.Count} segment categories, no repeats inside the last " +
                      $"{settings.recentSegmentHistory}, precise gaps/landings and physical tire/platform/lava " +
                      "collider contracts validated.");
        }

        private static void ValidateNeonCityContract(BuiltAssets assets)
        {
            MapProfile profile = assets.neonCity;
            MapProfile rainyer = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/Rainyer.asset");
            MapProfile mountains = AssetDatabase.LoadAssetAtPath<MapProfile>(Root + "/MapProfiles/Mountains.asset");
            EnvironmentDefinition neonEnvironment = profile != null ? profile.environment : null;
            float lockedYLow = MountainBackground.CalculateVerticalPosition(neonEnvironment, -80f, 5f);
            float lockedYHigh = MountainBackground.CalculateVerticalPosition(neonEnvironment, 120f, 5f);
            if (neonEnvironment == null || !neonEnvironment.lockVerticalToCamera ||
                neonEnvironment.mountainParallaxX <= 0f ||
                Mathf.Abs(neonEnvironment.mountainBaseY - 2f) > .001f ||
                Mathf.Abs((lockedYLow + 80f) - (lockedYHigh - 120f)) > .001f)
                throw new System.InvalidOperationException(
                    "Neon City background must remain vertically locked to the camera with horizontal parallax enabled.");
            if (assets.catalog.CountMaps(GameModeType.Adventure2_5D) != 7 ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 0)?.mapId != "countryside" ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 1)?.mapId != "city" ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 2)?.mapId != "neon-city" ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 3)?.mapId != "mountains" ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 4)?.mapId != "rainyer" ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 5)?.mapId != "space" ||
                assets.catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 6)?.mapId != "north-pole" ||
                rainyer == null || assets.catalog.FindMap("rainyer", GameModeType.Adventure2_5D) != rainyer ||
                mountains == null || assets.catalog.FindMap("mountains", GameModeType.Adventure2_5D) != mountains)
                throw new System.InvalidOperationException(
                    "Adventure track selector requires all seven profiles in ascending coin-price order: " +
                    "Countryside, City, Neon City, Mountains, Rainyer, Space and North Pole.");
            if (profile == null || !profile.supportsLiquidExposure || profile.liquidChallenges == null ||
                profile.liquidChallenges.cycleLength != 6000f ||
                profile.liquidChallenges.challengesPerCycle != 8 ||
                profile.liquidChallenges.liquidSprite != assets.neonLiquid ||
                profile.liquidChallenges.cautionSprite != assets.neonCaution)
                throw new System.InvalidOperationException("Neon City profile or required artwork is incomplete.");
            LiquidChallengeSettings settings = profile.liquidChallenges;
            if (settings.rampAngleDegrees < 20f || settings.rampHorizontalLength < 12f ||
                settings.cautionDistanceBeforeRamp <= settings.rampTransitionLength ||
                settings.cautionDistanceBeforeRamp >= settings.runUpLength + settings.rampTransitionLength)
                throw new System.InvalidOperationException(
                    "Every Neon liquid requires its authored ramp and a caution marker on the stable run-up before it.");
            float damping = -Mathf.Log(settings.entrySpeedRemaining) / settings.entrySlowdownSeconds;
            float measuredRemaining = Mathf.Exp(-damping * settings.entrySlowdownSeconds);
            float liquidTerminalSpeed = settings.liquidDriveAcceleration /
                                        Mathf.Max(.001f, settings.continuousViscosity);
            if (Mathf.Abs(measuredRemaining - .10f) > .005f || settings.normalAlpha != 1f ||
                Mathf.Abs(settings.submergedAlpha - .65f) > .001f ||
                liquidTerminalSpeed * (settings.exposureLimitSeconds - .7f) < settings.maximumPoolLength)
                throw new System.InvalidOperationException(
                    "Neon Liquid slowdown, alpha, or Level-1 escape envelope is invalid.");
            float[] repeatedRunUps = new float[LiquidChallengeSettings.RequiredChallengesPerCycle];
            NeonTemplateSection[] liquidSections =
            {
                NeonTemplateSection.Warmup, NeonTemplateSection.GradualDescent,
                NeonTemplateSection.BumpyFlat, NeonTemplateSection.Staircases,
                NeonTemplateSection.BrokenTerrain, NeonTemplateSection.RollingDescent,
                NeonTemplateSection.DoubleHill, NeonTemplateSection.TechBumps
            };
            if (settings.structuralRunUpPositions == null ||
                settings.structuralRunUpPositions.Length != LiquidChallengeSettings.RequiredChallengesPerCycle)
                throw new System.InvalidOperationException(
                    "Neon City requires eight authored structural liquid positions.");
            for (int cycle = 0; cycle < 4; cycle++)
            {
                int detected = 0;
                float previousRecoveryEnd = cycle * 6000f;
                for (int slot = 0; slot < 8; slot++)
                {
                    LiquidChallengeLayout layout = TerrainGenerator.GetLiquidChallenge(profile, cycle, slot);
                    LiquidChallengeLayout repeat = TerrainGenerator.GetLiquidChallenge(profile, cycle, slot);
                    if (Mathf.Abs(layout.poolStart - repeat.poolStart) > .0001f ||
                        Mathf.Abs(layout.poolLength - repeat.poolLength) > .0001f)
                        throw new System.InvalidOperationException("Neon City generation is not deterministic.");
                    float localRunUp = layout.runUpStart - cycle * LiquidChallengeSettings.RequiredCycleLength;
                    float authoredRunUp = TerrainGenerator.GetNeonLiquidLocalRunUp(profile, slot);
                    if (Mathf.Abs(localRunUp - authoredRunUp) > .001f ||
                        TerrainGenerator.GetNeonTemplateSection(localRunUp) != liquidSections[slot])
                        throw new System.InvalidOperationException(
                            $"Neon City challenge {slot + 1} is not anchored in its authored terrain section.");
                    if (cycle == 0) repeatedRunUps[slot] = localRunUp;
                    else if (Mathf.Abs(localRunUp - repeatedRunUps[slot]) > .001f)
                        throw new System.InvalidOperationException(
                            $"Neon City challenge {slot + 1} changed logical position in cycle {cycle}.");
                    if (layout.runUpStart < previousRecoveryEnd || layout.transitionStart <= layout.runUpStart ||
                        layout.rampStart <= layout.transitionStart || layout.poolStart <= layout.rampStart ||
                        layout.poolEnd <= layout.poolStart || layout.landingEnd <= layout.poolEnd ||
                        layout.recoveryEnd <= layout.landingEnd)
                        throw new System.InvalidOperationException(
                            $"Neon City cycle {cycle} challenge {slot + 1} grammar/spacing is invalid.");
                    float cautionX = layout.rampStart - settings.cautionDistanceBeforeRamp;
                    if (cautionX <= layout.runUpStart || cautionX >= layout.transitionStart)
                        throw new System.InvalidOperationException(
                            $"Neon City cycle {cycle} challenge {slot + 1} caution marker is not before its ramp.");
                    if (!TerrainGenerator.TryGetLiquidChallengeAtDistance(profile,
                            (layout.poolStart + layout.poolEnd) * .5f, out LiquidChallengeLayout found) ||
                        found.challengeIndex != slot)
                        throw new System.InvalidOperationException("Neon City pool lookup failed.");
                    if (settings.recoveryLength < 30f || settings.recoveryLength > 70f)
                        throw new System.InvalidOperationException(
                            "Every Neon liquid recovery must be between 30 and 70 metres.");
                    float previousRecoveryHeight = TerrainGenerator.SampleHeight(profile, layout.landingEnd);
                    for (float sample = layout.landingEnd + 1f; sample <= layout.recoveryEnd; sample += 1f)
                    {
                        float recoveryHeight = TerrainGenerator.SampleHeight(profile, sample);
                        if (recoveryHeight > previousRecoveryHeight + .002f)
                            throw new System.InvalidOperationException(
                                $"Neon cycle {cycle} challenge {slot + 1} has an uphill post-liquid recovery.");
                        previousRecoveryHeight = recoveryHeight;
                    }
                    previousRecoveryEnd = layout.recoveryEnd + settings.postRecoveryBlendLength;
                    detected++;
                }
                if (detected != 8)
                    throw new System.InvalidOperationException($"Neon City cycle {cycle}: {detected}/8 liquid challenges.");
                Debug.Log($"NEON_LIQUID_COUNT Cycle {cycle}: 8/8 pools + 8/8 Caution markers");
                Debug.Log($"NEON_POST_LIQUID Cycle {cycle}: 8/8 clean landings, " +
                          $"{settings.recoveryLength:0}m flat/gently descending recoveries before difficulty resumes.");
            }

            float minimumLiquidSpacing = float.PositiveInfinity;
            float maximumLiquidSpacing = 0f;
            for (int i = 1; i < repeatedRunUps.Length; i++)
            {
                float spacing = repeatedRunUps[i] - repeatedRunUps[i - 1];
                minimumLiquidSpacing = Mathf.Min(minimumLiquidSpacing, spacing);
                maximumLiquidSpacing = Mathf.Max(maximumLiquidSpacing, spacing);
            }
            if (maximumLiquidSpacing - minimumLiquidSpacing < 200f)
                throw new System.InvalidOperationException(
                    "Neon liquid challenges are effectively uniform instead of section-authored.");

            NeonSectionAudit[] sectionAudits = new NeonSectionAudit[TerrainGenerator.NeonTemplateSectionCount];
            for (int sectionIndex = 0; sectionIndex < sectionAudits.Length; sectionIndex++)
            {
                NeonTemplateSection section = (NeonTemplateSection)sectionIndex;
                sectionAudits[sectionIndex] = AuditNeonSection(profile, section, 2f);
                NeonSectionAudit audit = sectionAudits[sectionIndex];
                Debug.Log($"NEON_HEIGHT_PROFILE {sectionIndex + 1}/{TerrainGenerator.NeonTemplateSectionCount} {section} " +
                          $"{audit.startDistance:0}-{audit.endDistance:0}m | " +
                          $"height={audit.minimumHeight:0.00}..{audit.maximumHeight:0.00}m | " +
                          $"net={audit.endHeight - audit.startHeight:+0.00;-0.00;0.00}m | " +
                          $"turns={audit.directionChanges} | maxSlope={audit.maximumSlope:0.0}deg");
            }
            if (sectionAudits[(int)NeonTemplateSection.Warmup].HeightRange < 3f ||
                sectionAudits[(int)NeonTemplateSection.Warmup].HeightRange > 9f ||
                sectionAudits[(int)NeonTemplateSection.RollingHills].HeightRange < 18f ||
                sectionAudits[(int)NeonTemplateSection.RollingHills].directionChanges < 15 ||
                sectionAudits[(int)NeonTemplateSection.RollingHills].maximumSlope < 20f ||
                sectionAudits[(int)NeonTemplateSection.GradualDescent].NetHeight > -18f ||
                sectionAudits[(int)NeonTemplateSection.GradualDescent].directionChanges < 12 ||
                Mathf.Abs(sectionAudits[(int)NeonTemplateSection.BumpyFlat].NetHeight) > 4f ||
                sectionAudits[(int)NeonTemplateSection.BumpyFlat].HeightRange < 8f ||
                sectionAudits[(int)NeonTemplateSection.BumpyFlat].directionChanges < 12 ||
                sectionAudits[(int)NeonTemplateSection.Staircases].NetHeight < 20f ||
                sectionAudits[(int)NeonTemplateSection.Staircases].directionChanges < 10 ||
                sectionAudits[(int)NeonTemplateSection.Staircases].maximumSlope < 20f ||
                sectionAudits[(int)NeonTemplateSection.BrokenTerrain].HeightRange < 25f ||
                sectionAudits[(int)NeonTemplateSection.BrokenTerrain].directionChanges < 12 ||
                sectionAudits[(int)NeonTemplateSection.RollingDescent].NetHeight > -10f ||
                sectionAudits[(int)NeonTemplateSection.RollingDescent].directionChanges < 15 ||
                sectionAudits[(int)NeonTemplateSection.DoubleHill].HeightRange < 40f ||
                sectionAudits[(int)NeonTemplateSection.DoubleHill].directionChanges < 12 ||
                sectionAudits[(int)NeonTemplateSection.DoubleHill].maximumSlope < 30f ||
                sectionAudits[(int)NeonTemplateSection.TechBumps].HeightRange < 16f ||
                sectionAudits[(int)NeonTemplateSection.TechBumps].directionChanges < 12 ||
                sectionAudits[(int)NeonTemplateSection.TechBumps].maximumSlope < 30f ||
                sectionAudits[(int)NeonTemplateSection.Recovery].HeightRange > 8f ||
                Mathf.Abs(sectionAudits[(int)NeonTemplateSection.Recovery].endHeight - profile.baseHeight) > .01f)
                throw new System.InvalidOperationException(
                    "Neon City height profile does not expose the complete authored ten-section grammar.");

            float minimumHeight = TerrainGenerator.SampleNeonTemplateHeight(profile, 0f);
            float maximumHeight = minimumHeight;
            float maximumSlope = 0f;
            int terrainDirectionChanges = 0;
            int previousDirection = 0;
            float maximumSlopeChange = 0f;
            float maximumSlopeChangeAt = 0f;
            float previousSlope = 0f;
            bool hasPreviousSlope = false;
            const float terrainAuditStep = 2f;
            float previousHeight = TerrainGenerator.SampleNeonTemplateHeight(profile, 0f);
            for (float distance = terrainAuditStep; distance <= 24000f; distance += terrainAuditStep)
            {
                float height = TerrainGenerator.SampleNeonTemplateHeight(profile, distance);
                minimumHeight = Mathf.Min(minimumHeight, height);
                maximumHeight = Mathf.Max(maximumHeight, height);
                float delta = height - previousHeight;
                int direction = delta > .01f ? 1 : delta < -.01f ? -1 : 0;
                if (direction != 0)
                {
                    if (previousDirection != 0 && direction != previousDirection) terrainDirectionChanges++;
                    previousDirection = direction;
                }
                float signedSlope = Mathf.Atan2(delta, terrainAuditStep) * Mathf.Rad2Deg;
                maximumSlope = Mathf.Max(maximumSlope, Mathf.Abs(signedSlope));
                if (hasPreviousSlope)
                {
                    float slopeChange = Mathf.Abs(signedSlope - previousSlope);
                    if (slopeChange > maximumSlopeChange)
                    {
                        maximumSlopeChange = slopeChange;
                        maximumSlopeChangeAt = distance;
                    }
                }
                previousSlope = signedSlope;
                hasPreviousSlope = true;
                previousHeight = height;
            }
            for (float local = 0f; local < 6000f; local += 25f)
            {
                float cycle0 = (TerrainGenerator.SampleNeonTemplateHeight(profile, local) - profile.baseHeight) /
                               Mathf.Max(.001f, TerrainGenerator.EvaluateNeonAmplitude(profile, local));
                float cycle1 = (TerrainGenerator.SampleNeonTemplateHeight(profile, local + 6000f) - profile.baseHeight) /
                               Mathf.Max(.001f, TerrainGenerator.EvaluateNeonAmplitude(profile, local + 6000f));
                if (Mathf.Abs(cycle0 - cycle1) > .0001f)
                    throw new System.InvalidOperationException("Neon City cycle template does not repeat.");
            }
            var seamLeftMesh = new Mesh();
            var seamRightMesh = new Mesh();
            TerrainGenerator.BuildMesh(seamLeftMesh, profile, 5920f, 80f, 48, 10f, false, 0f, 0f,
                out _, out float seamLeftHeight, out float seamLeftSlope);
            TerrainGenerator.BuildMesh(seamRightMesh, profile, 6000f, 80f, 48, 10f, true,
                seamLeftHeight, seamLeftSlope, out float[] seamRightHeights, out _, out _);
            float analyticSeamDelta = Mathf.Max(
                Mathf.Abs(TerrainGenerator.SampleHeight(profile, 5999.9f) -
                          TerrainGenerator.SampleHeight(profile, 6000f)),
                Mathf.Abs(TerrainGenerator.SampleHeight(profile, 6000.1f) -
                          TerrainGenerator.SampleHeight(profile, 6000f)));
            float meshSeamDelta = Mathf.Abs(seamLeftHeight - seamRightHeights[0]);
            Object.DestroyImmediate(seamLeftMesh);
            Object.DestroyImmediate(seamRightMesh);

            if (TerrainGenerator.NeonTemplateSectionCount != 10 ||
                TerrainGenerator.NeonMesoFeatureCount != 66 ||
                TerrainGenerator.NeonSharpCrestCount != 10 ||
                TerrainGenerator.NeonPotholeCount != 18 ||
                TerrainGenerator.NeonWhoopsCount != 4 ||
                TerrainGenerator.NeonSoftStepCount != 3 ||
                TerrainGenerator.NeonTripleBumpCount != 4 ||
                TerrainGenerator.NeonPlateauCount != 3 ||
                TerrainGenerator.NeonBrokenRhythmCount != 3 ||
                TerrainGenerator.NeonMesoMinimumLength != 28f ||
                TerrainGenerator.NeonMesoMaximumLength != 70f ||
                maximumSlope > profile.maxSlopeAngle + .25f || maximumSlope < 38f ||
                maximumSlopeChange > 20f || maximumHeight - minimumHeight < 55f ||
                terrainDirectionChanges < 170 || analyticSeamDelta > .01f || meshSeamDelta > .001f ||
                TerrainGenerator.IsLiquidGap(profile, 5999f) || TerrainGenerator.IsLiquidGap(profile, 6001f))
                throw new System.InvalidOperationException(
                    $"Neon terrain grammar invalid: sections={TerrainGenerator.NeonTemplateSectionCount}, " +
                    $"meso={TerrainGenerator.NeonMesoFeatureCount}, slope={maximumSlope:0.0}, " +
                    $"slope-step={maximumSlopeChange:0.0}@{maximumSlopeChangeAt:0}m, " +
                    $"range={minimumHeight:0.0}..{maximumHeight:0.0}m, " +
                    $"direction-changes={terrainDirectionChanges}, seam={meshSeamDelta:0.000000}m.");

            TerrainDifficultyAudit neonCycle = AuditDriveableTerrain(profile, 6000f, terrainAuditStep);
            TerrainDifficultyAudit countrysideCycle = AuditGeneratedTerrain(assets.countryside, 6000f);
            if (assets.countryside == null || assets.countryside.supportsLiquidExposure ||
                profile.maxSlopeAngle < assets.countryside.maxSlopeAngle + 12f ||
                neonCycle.maximumSlope < countrysideCycle.maximumSlope + 8f ||
                neonCycle.HeightRange < countrysideCycle.HeightRange * 1.70f ||
                profile.initialFuelGap <= assets.countryside.initialFuelGap ||
                profile.fuelGapIncrease < assets.countryside.fuelGapIncrease)
                throw new System.InvalidOperationException(
                    $"Neon City must remain harder than Countryside over the same 6000m: " +
                    $"Neon range/slope/turns={neonCycle.HeightRange:0.0}m/{neonCycle.maximumSlope:0.0}deg/" +
                    $"{neonCycle.directionChanges}; Countryside={countrysideCycle.HeightRange:0.0}m/" +
                    $"{countrysideCycle.maximumSlope:0.0}deg/{countrysideCycle.directionChanges}.");

            NeonHandlingAudit cycle0Handling = AuditGeneratedNeonHandling(profile, 0f, 6000f);
            NeonHandlingAudit cycle1Handling = AuditGeneratedNeonHandling(profile, 6000f, 6000f);
            if (cycle0Handling.maximumSlope > profile.maxSlopeAngle + .25f ||
                cycle1Handling.maximumSlope > profile.maxSlopeAngle + .25f ||
                cycle0Handling.maximumSlopeChange > 20f || cycle1Handling.maximumSlopeChange > 20f ||
                cycle0Handling.maximumSeamGap > .001f || cycle1Handling.maximumSeamGap > .001f ||
                cycle0Handling.minimumCrestSpeed < 4f || cycle1Handling.minimumCrestSpeed < 4f ||
                cycle0Handling.extremaSpacingVariation < .35f || cycle1Handling.extremaSpacingVariation < .35f ||
                cycle0Handling.microExtremaIntervals > 2 || cycle1Handling.microExtremaIntervals > 2)
                throw new System.InvalidOperationException(
                    $"Neon smoothed-mesh handling invalid: C0 slope/change/seam/safe/spacingCV/micro=" +
                    $"{cycle0Handling.maximumSlope:0.0}/{cycle0Handling.maximumSlopeChange:0.0}" +
                    $"@{cycle0Handling.maximumSlopeChangeAt:0.0}m/" +
                    $"{cycle0Handling.maximumSeamGap:0.000000}/{cycle0Handling.minimumCrestSpeed:0.0}/" +
                    $"{cycle0Handling.extremaSpacingVariation:0.00}/{cycle0Handling.microExtremaIntervals}; C1=" +
                    $"{cycle1Handling.maximumSlope:0.0}/{cycle1Handling.maximumSlopeChange:0.0}" +
                    $"@{cycle1Handling.maximumSlopeChangeAt:0.0}m/" +
                    $"{cycle1Handling.maximumSeamGap:0.000000}/{cycle1Handling.minimumCrestSpeed:0.0}/" +
                    $"{cycle1Handling.extremaSpacingVariation:0.00}/{cycle1Handling.microExtremaIntervals}; " +
                    $"C0 micro positions={string.Join(",", cycle0Handling.microExtremaPositions.Select(x => x.ToString("0.0")))}.");

            VehicleDefinition[] levelOneVehicles = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            for (int i = 0; i < levelOneVehicles.Length; i++)
            {
                VehicleDefinition vehicle = levelOneVehicles[i];
                ResolvedVehicleStats levelOne = vehicle.Resolve(new VehicleUpgradeState());
                int cycle0OverspeedZones = CountOverspeedZones(cycle0Handling, levelOne.topSpeedMps);
                int cycle1OverspeedZones = CountOverspeedZones(cycle1Handling, levelOne.topSpeedMps);
                float minimumReadableLength = vehicle.halfWheelbase * 10f;
                if (cycle0OverspeedZones < 6 || cycle1OverspeedZones < cycle0OverspeedZones ||
                    TerrainGenerator.NeonMesoMinimumLength < minimumReadableLength)
                    throw new System.InvalidOperationException(
                        $"{vehicle.displayName} Level 1 terrain envelope failed: C0/C1 overspeed zones=" +
                        $"{cycle0OverspeedZones}/{cycle1OverspeedZones}, minimum feature=" +
                        $"{TerrainGenerator.NeonMesoMinimumLength:0}m, wheelbase envelope={minimumReadableLength:0.0}m.");
                Debug.Log($"NEON_LEVEL1 {vehicle.displayName}: top={levelOne.topSpeedMps * 3.6f:0}km/h, " +
                          $"controlled crest floor C0/C1={cycle0Handling.minimumCrestSpeed * 3.6f:0}/" +
                          $"{cycle1Handling.minimumCrestSpeed * 3.6f:0}km/h, full-Gas overspeed zones=" +
                          $"{cycle0OverspeedZones}/{cycle1OverspeedZones}; PASS with braking/momentum.");
            }
            Debug.Log($"NEON_WHEEL_JITTER_AUDIT: smoothing={profile.smoothingPasses} passes (1-2-1 kernel), " +
                      $"C0/C1 max slope={cycle0Handling.maximumSlope:0.0}/{cycle1Handling.maximumSlope:0.0}deg, " +
                      $"max slope change={cycle0Handling.maximumSlopeChange:0.0}/" +
                      $"{cycle1Handling.maximumSlopeChange:0.0}deg per 1.67m, seams=" +
                      $"{cycle0Handling.maximumSeamGap:0.000000}/{cycle1Handling.maximumSeamGap:0.000000}m, " +
                      $"extrema spacing={cycle0Handling.minimumExtremaSpacing:0.0}-" +
                      $"{cycle0Handling.maximumExtremaSpacing:0.0}m, CV={cycle0Handling.extremaSpacingVariation:0.00}; PASS.");
            Debug.Log($"NEON_TERRAIN_PATTERN: Warmup -> Rolling Hills -> Gradual Descent -> Bumpy Flat -> " +
                      $"Staircases -> Broken Terrain -> Rolling Descent -> Double Hill -> Tech Bumps -> Recovery; " +
                      $"{TerrainGenerator.NeonMesoFeatureCount} authored 28-70m medium-scale features; repeated " +
                      $"distinct obstacles={TerrainGenerator.NeonSharpCrestCount} sharp crests + " +
                      $"{TerrainGenerator.NeonPotholeCount} potholes + {TerrainGenerator.NeonWhoopsCount} whoops + " +
                      $"{TerrainGenerator.NeonSoftStepCount} soft steps + {TerrainGenerator.NeonTripleBumpCount} triple bumps + " +
                      $"{TerrainGenerator.NeonPlateauCount} short plateaus + " +
                      $"{TerrainGenerator.NeonBrokenRhythmCount} broken-rhythm groups; " +
                      $"obstacle height approx " +
                      $"{TerrainGenerator.NeonMesoMinimumNormalizedHeight * TerrainGenerator.EvaluateNeonAmplitude(profile, 0f):0.0}-" +
                      $"{TerrainGenerator.NeonMesoMaximumNormalizedHeight * TerrainGenerator.EvaluateNeonAmplitude(profile, 6000f):0.0}m; " +
                      $"6000m cycle; height {minimumHeight:0.0}..{maximumHeight:0.0}m; max slope " +
                      $"{maximumSlope:0.0}deg; max 2m slope change={maximumSlopeChange:0.0}deg; " +
                      $"5990-6010 seam analytic={analyticSeamDelta:0.000000}m, mesh={meshSeamDelta:0.000000}m; " +
                      $"direction changes={terrainDirectionChanges}.");
            Debug.Log($"NEON_HARDER_THAN_COUNTRYSIDE: equal 6000m comparison | " +
                      $"height range {neonCycle.HeightRange:0.0}>{countrysideCycle.HeightRange:0.0}m | " +
                      $"max slope {neonCycle.maximumSlope:0.0}>{countrysideCycle.maximumSlope:0.0}deg | " +
                      $"8 liquids with {settings.rampAngleDegrees:0}deg ramps and " +
                      $"warnings {settings.cautionDistanceBeforeRamp:0}m before each ramp.");

            var exposure = new LiquidExposureTracker();
            exposure.Reset(10f);
            exposure.SetContact(101, true); exposure.Tick(3f); exposure.SetContact(101, false); exposure.Tick(5f);
            if (Mathf.Abs(exposure.TotalSeconds - 3f) > .001f || exposure.LimitReached)
                throw new System.InvalidOperationException("Liquid exposure did not pause at 3 seconds after exit.");
            exposure.SetContact(202, true); exposure.Tick(4f); exposure.SetContact(202, false);
            exposure.SetContact(303, true); exposure.Tick(2.9f);
            if (exposure.LimitReached || Mathf.Abs(exposure.TotalSeconds - 9.9f) > .001f)
                throw new System.InvalidOperationException("Liquid exposure crashed before 10 seconds.");
            exposure.Tick(.1f);
            if (!exposure.LimitReached || Mathf.Abs(exposure.TotalSeconds - 10f) > .001f)
                throw new System.InvalidOperationException("Liquid exposure did not crash at 10 seconds.");

            var gemRoute = new RandomGemRouteSequence();
            var gemPoints = new List<GemRoutePoint>();
            gemRoute.Reset(0f, 73421);
            gemRoute.GetPoints(0f, 10000f, gemPoints);
            if (gemPoints.Count < 9 || !gemRoute.HasValidSpacing())
                throw new System.InvalidOperationException("Random Gem route left the 700-1000m interval.");
            bool hasVariableSpacing = false;
            float previousGem = 0f;
            float firstSpacing = gemPoints[0].Position;
            var gemIds = new HashSet<long>();
            for (int i = 0; i < gemPoints.Count; i++)
            {
                float spacing = gemPoints[i].Position - previousGem;
                hasVariableSpacing |= Mathf.Abs(spacing - firstSpacing) > .01f;
                previousGem = gemPoints[i].Position;
                if (!gemIds.Add(gemPoints[i].StableId))
                    throw new System.InvalidOperationException("Gem route generated a duplicate pickup id.");
            }
            if (!hasVariableSpacing)
                throw new System.InvalidOperationException("Gem route intervals did not vary.");
            if (SaveManager.CanAffordGems(999, 1000) || !SaveManager.CanAffordGems(1000, 1000) ||
                SaveManager.CanAffordGems(499, 500) || !SaveManager.CanAffordGems(500, 500))
                throw new System.InvalidOperationException("Premium cosmetic Gem thresholds are invalid.");
            Debug.Log("Neon validation: cycles 0-3=8/8, exposure 3+4+3=10s, random pooled Gems stay 700-1000m apart.");
        }

        private static void ValidateCityContract(BuiltAssets assets)
        {
            MapProfile profile = assets.city;
            EnvironmentDefinition environment = profile != null ? profile.environment : null;
            if (profile == null || profile.mapId != "city" || !profile.useCityHighwayTerrain ||
                profile.supportsLiquidExposure || !profile.useSeparateTerrainMaterials ||
                !profile.fitAuthoredTerrainTexture ||
                Mathf.Abs(profile.terrainFillStartV - profile.terrainSurfaceContactV) > .0001f ||
                profile.hardEndDistance != 10000f || environment == null || environment.sky != assets.citySky ||
                environment.mountains != assets.cityBackground || environment.clouds == null ||
                environment.clouds.Length != 0 || profile.terrainMaterial != assets.cityTerrainMaterial ||
                profile.terrainEdgeMaterial != assets.cityGroundMaterial ||
                profile.terrainMaterial.mainTexture != assets.cityTerrain.texture ||
                profile.terrainEdgeMaterial.mainTexture != assets.cityGround.texture ||
                !profile.useCityBridges || profile.cityBridges == null ||
                profile.cityBridges.boatSprites == null || profile.cityBridges.boatSprites.Length != 3)
                throw new System.InvalidOperationException("City profile, visuals, clouds or terrain materials are incomplete.");
            float d0 = profile.EvaluateDifficulty(0f);
            float d3750 = profile.EvaluateDifficulty(3750f);
            float d10000 = profile.EvaluateDifficulty(10000f);
            float d12000 = profile.EvaluateDifficulty(12000f);
            if (!(d0 < d3750 && d3750 < d10000) || !Mathf.Approximately(d10000, d12000))
                throw new System.InvalidOperationException("City difficulty must grow through 10000 m and remain capped.");
            for (int cycle = 0; cycle <= 3; cycle++)
            {
                int zones = TerrainGenerator.GetCityBridgeZoneCount(profile, cycle);
                int boatZones = TerrainGenerator.GetCityBoatZoneCount(profile, cycle);
                if (zones < 10 || zones > 15 || boatZones < 5 || boatZones > 10)
                    throw new System.InvalidOperationException(
                        $"City distribution failed in cycle {cycle}: {zones} water, {boatZones} with boats.");
            }
            float maximumRawSlope = 0f;
            float maximumRawSlopeDistance = 0f;
            for (float x = 0f; x <= 8000f; x += 1f)
            {
                float rise = TerrainGenerator.SampleCityHighwayHeight(profile, x + 1f) -
                             TerrainGenerator.SampleCityHighwayHeight(profile, x);
                float rawSlope = Mathf.Abs(Mathf.Atan(rise) * Mathf.Rad2Deg);
                if (float.IsNaN(rawSlope) || float.IsInfinity(rawSlope))
                    throw new System.InvalidOperationException($"City raw terrain is invalid near {x:0} m.");
                if (rawSlope > maximumRawSlope)
                {
                    maximumRawSlope = rawSlope;
                    maximumRawSlopeDistance = x;
                }
            }

            const float chunkLength = 80f;
            const int samples = 48;
            var visualMesh = new Mesh();
            var colliderMesh = new Mesh();
            float endHeight = 0f;
            float endSlope = 0f;
            float maximumAlignmentError = 0f;
            float maximumFinalSlope = 0f;
            float minimumGroundDepth = float.MaxValue;
            for (int chunk = 0; chunk <= 100; chunk++)
            {
                float start = chunk * chunkLength;
                TerrainGenerator.BuildMesh(visualMesh, profile, start, chunkLength, samples, 10f,
                    chunk > 0, endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                TerrainGenerator.BuildSurfaceColliderMesh(colliderMesh, profile, start, chunkLength, heights, 10f);
                Vector3[] visual = visualMesh.vertices;
                Vector3[] physical = colliderMesh.vertices;
                if (visual.Length != heights.Length * 6 || physical.Length != heights.Length * 2)
                    throw new System.InvalidOperationException("City visual/collider mesh topology is incomplete.");
                for (int i = 0; i < heights.Length; i++)
                {
                    float groundTop = visual[i * 6 + 2].y;
                    float groundBottom = visual[i * 6 + 3].y;
                    float textureU = (start + i * (chunkLength / samples)) /
                                     Mathf.Max(1f, profile.terrainTextureRepeatWidth);
                    float contactV = TerrainGenerator.SampleGroundSurfaceContactV(profile, textureU);
                    float contactT = Mathf.InverseLerp(profile.terrainSurfaceUvRange.x,
                        profile.terrainSurfaceUvRange.y, contactV);
                    float groundContact = Mathf.Lerp(groundBottom, groundTop, contactT);
                    float terrainTop = visual[i * 6].y;
                    float colliderTop = physical[i * 2].y;
                    maximumAlignmentError = Mathf.Max(maximumAlignmentError,
                        Mathf.Abs(groundContact - heights[i]), Mathf.Abs(colliderTop - heights[i]),
                        Mathf.Abs(terrainTop - (heights[i] - TerrainGenerator.CalculateTerrainFillDepth(profile))),
                        Mathf.Max(0f, groundBottom - terrainTop));
                    minimumGroundDepth = Mathf.Min(minimumGroundDepth, groundTop - groundBottom);
                    if (i > 0)
                    {
                        float dx = chunkLength / samples;
                        maximumFinalSlope = Mathf.Max(maximumFinalSlope,
                            Mathf.Abs(Mathf.Atan2(heights[i] - heights[i - 1], dx) * Mathf.Rad2Deg));
                    }
                }
            }
            Object.DestroyImmediate(visualMesh);
            Object.DestroyImmediate(colliderMesh);
            if (maximumAlignmentError > .0001f || minimumGroundDepth <= .05f || maximumFinalSlope > 36.05f)
                throw new System.InvalidOperationException(
                    $"City surface contract failed: alignment={maximumAlignmentError:0.000000}m, " +
                    $"Ground depth={minimumGroundDepth:0.000}m, slope={maximumFinalSlope:0.00} degrees.");
            Debug.Log($"CITY_SURFACE_ALIGNMENT_PASS: Ground yellow stripe = generated height = collider, " +
                       $"max error {maximumAlignmentError:0.000000}m over 0-8000m; " +
                       $"Ground depth {minimumGroundDepth:0.000}m; max final slope {maximumFinalSlope:0.00} degrees; " +
                       $"raw peak {maximumRawSlope:0.00} degrees near {maximumRawSlopeDistance:0}m is constrained " +
                       "by the authoritative mesh limiter.");
        }

        private struct NeonSectionAudit
        {
            public float startDistance;
            public float endDistance;
            public float startHeight;
            public float endHeight;
            public float minimumHeight;
            public float maximumHeight;
            public float maximumSlope;
            public int directionChanges;
            public float HeightRange => maximumHeight - minimumHeight;
            public float NetHeight => endHeight - startHeight;
        }

        private struct TerrainDifficultyAudit
        {
            public float minimumHeight;
            public float maximumHeight;
            public float maximumSlope;
            public int directionChanges;
            public float HeightRange => maximumHeight - minimumHeight;
        }

        private sealed class NeonHandlingAudit
        {
            public float maximumSlope;
            public float maximumSlopeChange;
            public float maximumSlopeChangeAt;
            public float maximumSeamGap;
            public float minimumCrestSpeed = float.PositiveInfinity;
            public float minimumExtremaSpacing = float.PositiveInfinity;
            public float maximumExtremaSpacing;
            public float extremaSpacingMean;
            public float extremaSpacingVariation;
            public int extremaCount;
            public int microExtremaIntervals;
            public readonly List<float> microExtremaPositions = new List<float>();
            public readonly List<Vector2> crestSpeedSamples = new List<Vector2>();
        }

        private static NeonHandlingAudit AuditGeneratedNeonHandling(MapProfile profile, float startDistance,
            float length)
        {
            const float chunkLength = 80f;
            const int samples = 48;
            float dx = chunkLength / samples;
            var audit = new NeonHandlingAudit();
            var extremaSpacings = new List<float>();
            var mesh = new Mesh();
            float endHeight = 0f;
            float endSlope = 0f;
            float previousChunkEnd = 0f;
            bool hasPreviousChunk = false;
            float previousHeight = 0f;
            float previousPreviousHeight = 0f;
            float previousSlope = 0f;
            bool hasPreviousHeight = false;
            bool hasPreviousPreviousHeight = false;
            bool hasPreviousSlope = false;
            int previousDirection = 0;
            float lastExtremum = float.NegativeInfinity;
            float lastExtremumHeight = 0f;
            int chunks = Mathf.CeilToInt(length / chunkLength);

            for (int chunk = 0; chunk < chunks; chunk++)
            {
                float chunkStart = startDistance + chunk * chunkLength;
                TerrainGenerator.BuildMesh(mesh, profile, chunkStart, chunkLength, samples, 10f,
                    chunk > 0, endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                if (hasPreviousChunk)
                    audit.maximumSeamGap = Mathf.Max(audit.maximumSeamGap,
                        Mathf.Abs(previousChunkEnd - heights[0]));
                previousChunkEnd = heights[heights.Length - 1];
                hasPreviousChunk = true;

                for (int i = chunk == 0 ? 0 : 1; i < heights.Length; i++)
                {
                    float x = chunkStart + i * dx;
                    if (x > startDistance + length + .001f) break;
                    if (IsNeonLiquidStructure(profile, x))
                    {
                        hasPreviousHeight = false;
                        hasPreviousPreviousHeight = false;
                        hasPreviousSlope = false;
                        previousDirection = 0;
                        continue;
                    }

                    float height = heights[i];
                    if (hasPreviousHeight)
                    {
                        float rise = height - previousHeight;
                        float slope = rise / dx;
                        float slopeAngle = Mathf.Atan(slope) * Mathf.Rad2Deg;
                        audit.maximumSlope = Mathf.Max(audit.maximumSlope, Mathf.Abs(slopeAngle));
                        if (hasPreviousSlope)
                        {
                            float slopeChange = Mathf.Abs(slopeAngle - previousSlope);
                            if (slopeChange > audit.maximumSlopeChange)
                            {
                                audit.maximumSlopeChange = slopeChange;
                                audit.maximumSlopeChangeAt = x;
                            }
                        }

                        int direction = rise > .015f ? 1 : rise < -.015f ? -1 : 0;
                        if (direction != 0 && previousDirection != 0 && direction != previousDirection)
                        {
                            float extremum = x - dx;
                            if (float.IsNegativeInfinity(lastExtremum))
                            {
                                lastExtremum = extremum;
                                lastExtremumHeight = previousHeight;
                                audit.extremaCount++;
                            }
                            else if (Mathf.Abs(previousHeight - lastExtremumHeight) >= .25f)
                            {
                                float spacing = extremum - lastExtremum;
                                extremaSpacings.Add(spacing);
                                audit.minimumExtremaSpacing = Mathf.Min(audit.minimumExtremaSpacing, spacing);
                                audit.maximumExtremaSpacing = Mathf.Max(audit.maximumExtremaSpacing, spacing);
                                if (spacing < 6f)
                                {
                                    audit.microExtremaIntervals++;
                                    audit.microExtremaPositions.Add(extremum);
                                }
                                lastExtremum = extremum;
                                lastExtremumHeight = previousHeight;
                                audit.extremaCount++;
                            }
                        }
                        if (direction != 0) previousDirection = direction;

                        if (hasPreviousPreviousHeight)
                        {
                            float secondDerivative = (height - 2f * previousHeight + previousPreviousHeight) /
                                                     (dx * dx);
                            if (secondDerivative < -.002f)
                            {
                                float curvature = -secondDerivative /
                                    Mathf.Pow(1f + slope * slope, 1.5f);
                                float safeSpeed = Mathf.Sqrt(Physics.gravity.magnitude /
                                    Mathf.Max(.0001f, curvature));
                                audit.minimumCrestSpeed = Mathf.Min(audit.minimumCrestSpeed, safeSpeed);
                                audit.crestSpeedSamples.Add(new Vector2(x - dx, safeSpeed));
                            }
                        }
                        previousSlope = slopeAngle;
                        hasPreviousSlope = true;
                    }
                    previousPreviousHeight = previousHeight;
                    previousHeight = height;
                    hasPreviousPreviousHeight = hasPreviousHeight;
                    hasPreviousHeight = true;
                }
            }
            Object.DestroyImmediate(mesh);

            if (extremaSpacings.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < extremaSpacings.Count; i++) sum += extremaSpacings[i];
                audit.extremaSpacingMean = sum / extremaSpacings.Count;
                float variance = 0f;
                for (int i = 0; i < extremaSpacings.Count; i++)
                {
                    float difference = extremaSpacings[i] - audit.extremaSpacingMean;
                    variance += difference * difference;
                }
                audit.extremaSpacingVariation = Mathf.Sqrt(variance / extremaSpacings.Count) /
                                                Mathf.Max(.001f, audit.extremaSpacingMean);
            }
            if (float.IsPositiveInfinity(audit.minimumCrestSpeed)) audit.minimumCrestSpeed = 0f;
            if (float.IsPositiveInfinity(audit.minimumExtremaSpacing)) audit.minimumExtremaSpacing = 0f;
            return audit;
        }

        private static bool IsNeonLiquidStructure(MapProfile profile, float distance)
        {
            int cycle = TerrainGenerator.GetCycleIndex(distance);
            LiquidChallengeSettings settings = profile.liquidChallenges;
            for (int i = 0; i < LiquidChallengeSettings.RequiredChallengesPerCycle; i++)
            {
                LiquidChallengeLayout layout = TerrainGenerator.GetLiquidChallenge(profile, cycle, i);
                if (distance >= layout.runUpStart - 18f &&
                    distance <= layout.recoveryEnd + settings.postRecoveryBlendLength) return true;
            }
            return false;
        }

        private static int CountOverspeedZones(NeonHandlingAudit audit, float fullGasSpeed)
        {
            float threshold = fullGasSpeed * .72f;
            int zones = 0;
            float lastQualifyingX = float.NegativeInfinity;
            bool insideZone = false;
            for (int i = 0; i < audit.crestSpeedSamples.Count; i++)
            {
                Vector2 sample = audit.crestSpeedSamples[i];
                if (sample.y >= threshold)
                {
                    insideZone = false;
                    continue;
                }
                if (!insideZone || sample.x - lastQualifyingX > 8f) zones++;
                insideZone = true;
                lastQualifyingX = sample.x;
            }
            return zones;
        }

        private static TerrainDifficultyAudit AuditNeonTemplate(MapProfile profile, float distance, float step)
        {
            float sampleStep = Mathf.Max(.25f, step);
            float previousHeight = TerrainGenerator.SampleNeonTemplateHeight(profile, 0f);
            var audit = new TerrainDifficultyAudit
            {
                minimumHeight = previousHeight,
                maximumHeight = previousHeight
            };
            int previousDirection = 0;
            for (float x = sampleStep; x <= distance + .001f; x += sampleStep)
            {
                float clampedX = Mathf.Min(x, distance);
                float height = TerrainGenerator.SampleNeonTemplateHeight(profile, clampedX);
                float delta = height - previousHeight;
                audit.minimumHeight = Mathf.Min(audit.minimumHeight, height);
                audit.maximumHeight = Mathf.Max(audit.maximumHeight, height);
                audit.maximumSlope = Mathf.Max(audit.maximumSlope,
                    Mathf.Abs(Mathf.Atan2(delta, sampleStep) * Mathf.Rad2Deg));
                int direction = delta > .01f ? 1 : delta < -.01f ? -1 : 0;
                if (direction != 0)
                {
                    if (previousDirection != 0 && direction != previousDirection) audit.directionChanges++;
                    previousDirection = direction;
                }
                previousHeight = height;
            }
            return audit;
        }

        private static TerrainDifficultyAudit AuditDriveableTerrain(MapProfile profile, float distance, float step)
        {
            float sampleStep = Mathf.Max(.25f, step);
            var audit = new TerrainDifficultyAudit
            {
                minimumHeight = float.PositiveInfinity,
                maximumHeight = float.NegativeInfinity
            };
            float previousHeight = 0f;
            bool hasPreviousDriveable = false;
            int previousDirection = 0;
            for (float x = 0f; x <= distance + .001f; x += sampleStep)
            {
                float clampedX = Mathf.Min(x, distance);
                if (TerrainGenerator.IsLiquidGap(profile, clampedX))
                {
                    hasPreviousDriveable = false;
                    previousDirection = 0;
                    continue;
                }
                float height = TerrainGenerator.SampleHeight(profile, clampedX);
                audit.minimumHeight = Mathf.Min(audit.minimumHeight, height);
                audit.maximumHeight = Mathf.Max(audit.maximumHeight, height);
                if (hasPreviousDriveable)
                {
                    float delta = height - previousHeight;
                    audit.maximumSlope = Mathf.Max(audit.maximumSlope,
                        Mathf.Abs(Mathf.Atan2(delta, sampleStep) * Mathf.Rad2Deg));
                    int direction = delta > .01f ? 1 : delta < -.01f ? -1 : 0;
                    if (direction != 0)
                    {
                        if (previousDirection != 0 && direction != previousDirection) audit.directionChanges++;
                        previousDirection = direction;
                    }
                }
                previousHeight = height;
                hasPreviousDriveable = true;
            }
            return audit;
        }

        private static TerrainDifficultyAudit AuditGeneratedTerrain(MapProfile profile, float distance)
        {
            if (profile == null) return default;
            const float chunkLength = 80f;
            const int samples = 48;
            float dx = chunkLength / samples;
            var audit = new TerrainDifficultyAudit
            {
                minimumHeight = float.PositiveInfinity,
                maximumHeight = float.NegativeInfinity
            };
            var mesh = new Mesh();
            float endHeight = 0f;
            float endSlope = 0f;
            int previousDirection = 0;
            int chunks = Mathf.CeilToInt(distance / chunkLength);
            for (int chunk = 0; chunk < chunks; chunk++)
            {
                TerrainGenerator.BuildMesh(mesh, profile, chunk * chunkLength, chunkLength, samples, 10f,
                    chunk > 0, endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                for (int i = 0; i < heights.Length; i++)
                {
                    audit.minimumHeight = Mathf.Min(audit.minimumHeight, heights[i]);
                    audit.maximumHeight = Mathf.Max(audit.maximumHeight, heights[i]);
                    if (i == 0) continue;
                    float delta = heights[i] - heights[i - 1];
                    audit.maximumSlope = Mathf.Max(audit.maximumSlope,
                        Mathf.Abs(Mathf.Atan2(delta, dx) * Mathf.Rad2Deg));
                    int direction = delta > .05f ? 1 : delta < -.05f ? -1 : 0;
                    if (direction != 0)
                    {
                        if (previousDirection != 0 && direction != previousDirection) audit.directionChanges++;
                        previousDirection = direction;
                    }
                }
            }
            Object.DestroyImmediate(mesh);
            return audit;
        }

        private static NeonSectionAudit AuditNeonSection(MapProfile profile, NeonTemplateSection section,
            float step)
        {
            Vector2 range = TerrainGenerator.GetNeonSectionRange(section);
            float sampleStep = Mathf.Max(.25f, step);
            float startHeight = TerrainGenerator.SampleNeonTemplateHeight(profile, range.x);
            var result = new NeonSectionAudit
            {
                startDistance = range.x,
                endDistance = range.y,
                startHeight = startHeight,
                endHeight = TerrainGenerator.SampleNeonTemplateHeight(profile, range.y),
                minimumHeight = startHeight,
                maximumHeight = startHeight
            };
            float previousHeight = startHeight;
            int previousDirection = 0;
            for (float distance = range.x + sampleStep; distance <= range.y + .001f;
                 distance += sampleStep)
            {
                float clampedDistance = Mathf.Min(distance, range.y);
                float height = TerrainGenerator.SampleNeonTemplateHeight(profile, clampedDistance);
                result.minimumHeight = Mathf.Min(result.minimumHeight, height);
                result.maximumHeight = Mathf.Max(result.maximumHeight, height);
                float delta = height - previousHeight;
                int direction = delta > .01f ? 1 : delta < -.01f ? -1 : 0;
                if (direction != 0)
                {
                    if (previousDirection != 0 && direction != previousDirection) result.directionChanges++;
                    previousDirection = direction;
                }
                result.maximumSlope = Mathf.Max(result.maximumSlope,
                    Mathf.Abs(Mathf.Atan2(delta, sampleStep) * Mathf.Rad2Deg));
                previousHeight = height;
            }
            return result;
        }

        private static void ValidateCountrysideTerrainContract(MapProfile profile)
        {
            if (profile == null || profile.mapId != "countryside" || profile.supportsLiquidExposure)
                throw new System.InvalidOperationException("Countryside terrain profile is invalid.");
            Texture2D authoredTerrain = AssetDatabase.LoadAssetAtPath<Texture2D>(CountrysideTerrainPath);
            Texture2D authoredGround = AssetDatabase.LoadAssetAtPath<Texture2D>(CountrysideGroundPath);
            TextureImporter terrainImporter = AssetImporter.GetAtPath(CountrysideTerrainPath) as TextureImporter;
            TextureImporter groundImporter = AssetImporter.GetAtPath(CountrysideGroundPath) as TextureImporter;
            Vector4 horizontalUvRange = profile != null && profile.terrainEdgeMaterial != null &&
                                        profile.terrainEdgeMaterial.HasProperty("_HorizontalUvRange")
                ? profile.terrainEdgeMaterial.GetVector("_HorizontalUvRange")
                : Vector4.zero;
            if (authoredTerrain == null || authoredGround == null || profile.terrainMaterial == null ||
                profile.terrainEdgeMaterial == null || profile.terrainMaterial.mainTexture != authoredTerrain ||
                profile.terrainEdgeMaterial.mainTexture != authoredGround || !profile.useSeparateTerrainMaterials ||
                !profile.fitAuthoredTerrainTexture || profile.terrainSurfaceContactVSamples == null ||
                profile.terrainSurfaceContactVSamples.Length != 0 ||
                Mathf.Abs(profile.terrainFillStartV - profile.terrainSurfaceContactV) > .0001f ||
                Mathf.Abs(profile.terrainTextureRepeatWidth -
                          TerrainGenerator.CountrysideGroundModuleWidth) > .001f ||
                Mathf.Abs(profile.terrainMaximumVisualExtent -
                          TerrainGenerator.CountrysideGroundMaximumVisualExtent) > .001f ||
                Mathf.Abs(profile.terrainMaximumVisualRiseAboveSurface -
                          TerrainGenerator.CountrysideGroundMaximumVisualRise) > .001f ||
                Vector2.Distance(profile.terrainSurfaceUvRange, new Vector2(.32f, .676f)) > .001f ||
                Mathf.Abs(profile.terrainSurfaceContactV - .555f) > .001f ||
                authoredGround.width != 1810 || authoredGround.height != 869 ||
                !profile.terrainEdgeMaterial.HasProperty("_HorizontalInset") ||
                Mathf.Abs(profile.terrainEdgeMaterial.GetFloat("_HorizontalInset") - .004f) > .001f ||
                Vector2.Distance(new Vector2(horizontalUvRange.x, horizontalUvRange.y),
                    new Vector2(.03f, .695f)) > .001f ||
                terrainImporter == null || groundImporter == null || terrainImporter.mipmapEnabled ||
                groundImporter.mipmapEnabled || terrainImporter.wrapMode != TextureWrapMode.Repeat ||
                groundImporter.wrapMode != TextureWrapMode.Repeat ||
                Mathf.Abs(terrainImporter.spritePixelsPerUnit - 100f) > .001f ||
                Mathf.Abs(groundImporter.spritePixelsPerUnit - 100f) > .001f)
                throw new System.InvalidOperationException(
                    "Countryside Ground/Terrain materials, native scale or import settings are invalid.");
            var mesh = new Mesh();
            var colliderMesh = new Mesh();
            float endHeight = 0f;
            float endSlope = 0f;
            float minimumHeight = float.PositiveInfinity;
            float maximumHeight = float.NegativeInfinity;
            float maximumSlope = 0f;
            float maximumAlignmentError = 0f;
            float minimumGroundDepth = float.PositiveInfinity;
            float minimumTerrainDepth = float.PositiveInfinity;
            int directionChanges = 0;
            int previousDirection = 0;
            const float chunkLength = 80f;
            const int samples = 48;
            float dx = chunkLength / samples;
            for (int chunk = 0; chunk <= 125; chunk++)
            {
                TerrainGenerator.BuildMesh(mesh, profile, chunk * chunkLength, chunkLength, samples, 10f,
                    chunk > 0, endHeight, endSlope, out float[] heights, out endHeight, out endSlope);
                TerrainGenerator.BuildSurfaceColliderMesh(colliderMesh, profile, chunk * chunkLength,
                    chunkLength, heights, 10f);
                Vector3[] visual = mesh.vertices;
                Vector3[] physical = colliderMesh.vertices;
                if (mesh.subMeshCount != 2 || visual.Length != heights.Length * 6 ||
                    physical.Length != heights.Length * 2)
                    throw new System.InvalidOperationException("Countryside visual/collider mesh topology is incomplete.");
                for (int i = 0; i < heights.Length; i++)
                {
                    minimumHeight = Mathf.Min(minimumHeight, heights[i]);
                    maximumHeight = Mathf.Max(maximumHeight, heights[i]);
                    float groundTop = visual[i * 6 + 2].y;
                    float groundBottom = visual[i * 6 + 3].y;
                    float terrainTop = visual[i * 6].y;
                    float terrainBottom = visual[i * 6 + 1].y;
                    float renderedSurface = visual[i * 6 + 4].y;
                    float colliderSurface = physical[i * 2].y;
                    float textureU = (chunk * chunkLength + i * dx) /
                                     Mathf.Max(1f, profile.terrainTextureRepeatWidth);
                    float contactV = TerrainGenerator.SampleGroundSurfaceContactV(profile, textureU);
                    float contactT = Mathf.InverseLerp(profile.terrainSurfaceUvRange.x,
                        profile.terrainSurfaceUvRange.y, contactV);
                    float groundContact = Mathf.Lerp(groundBottom, groundTop, contactT);
                    float expectedTerrainTop = heights[i] - TerrainGenerator.CalculateTerrainFillDepth(profile);
                    maximumAlignmentError = Mathf.Max(maximumAlignmentError,
                        Mathf.Abs(groundContact - heights[i]), Mathf.Abs(renderedSurface - heights[i]),
                        Mathf.Abs(colliderSurface - heights[i]), Mathf.Abs(terrainTop - expectedTerrainTop),
                        Mathf.Max(0f, groundBottom - terrainTop));
                    minimumGroundDepth = Mathf.Min(minimumGroundDepth, groundTop - groundBottom);
                    minimumTerrainDepth = Mathf.Min(minimumTerrainDepth, terrainTop - terrainBottom);
                    if (i == 0) continue;
                    float delta = heights[i] - heights[i - 1];
                    maximumSlope = Mathf.Max(maximumSlope,
                        Mathf.Atan2(Mathf.Abs(delta), dx) * Mathf.Rad2Deg);
                    if (Mathf.Abs(delta) < .05f) continue;
                    int direction = delta > 0f ? 1 : -1;
                    if (previousDirection != 0 && direction != previousDirection) directionChanges++;
                    previousDirection = direction;
                }
            }
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(colliderMesh);
            float difficultyAtPeak = profile.EvaluateDifficulty(15000f);
            float difficultyAfterPeak = profile.EvaluateDifficulty(16500f);
            if (maximumSlope > profile.maxSlopeAngle + .25f || maximumHeight - minimumHeight < 25f ||
                directionChanges < 60 || !Mathf.Approximately(difficultyAtPeak, 1f) ||
                !Mathf.Approximately(difficultyAtPeak, difficultyAfterPeak) ||
                maximumAlignmentError > .0005f || minimumGroundDepth <= .05f || minimumTerrainDepth <= 5f)
                throw new System.InvalidOperationException(
                    $"Countryside movement invalid: range={minimumHeight:0.0}..{maximumHeight:0.0}m, " +
                    $"slope={maximumSlope:0.0}, directionChanges={directionChanges}, " +
                    $"difficulty15km={difficultyAtPeak:0.00}, alignment={maximumAlignmentError:0.000000}m.");
            Debug.Log($"COUNTRYSIDE_TERRAIN_PATTERN: range {minimumHeight:0.0}..{maximumHeight:0.0}m, " +
                       $"max slope {maximumSlope:0.0} degrees, {directionChanges} climb/descent changes, " +
                       $"difficulty peaks at 15000m; Ground/collider error {maximumAlignmentError:0.000000}m, " +
                       $"Ground band {minimumGroundDepth:0.000}m, Terrain fill >= {minimumTerrainDepth:0.0}m.");
        }

        [MenuItem("Anime Hill Racing/Validation/Validate Authored Track Surface Contract")]
        public static void ValidateAuthoredTrackSurfaceContract()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            MapProfile countryside = AssetDatabase.LoadAssetAtPath<MapProfile>(
                Root + "/MapProfiles/Countryside.asset");
            ValidateCountrysideTerrainContract(countryside);
            ValidateSharedAuthoredTrackDimensions();
        }

        private static void ValidateSharedAuthoredTrackDimensions()
        {
            string[] profilePaths =
            {
                Root + "/MapProfiles/Rainyer.asset", Root + "/MapProfiles/NeonCity.asset",
                Root + "/MapProfiles/Mountains.asset", SpaceMapPath
            };
            string[] groundPaths =
            {
                "Assets/Art/Backgrounds/Rainyer/Ground.png", NeonGroundPath,
                "Assets/Art/Terrain/Mountains/Ground.png", "Assets/Art/Backgrounds/Space/Ground.png"
            };
            var mesh = new Mesh();
            float maximumAlignmentError = 0f;
            float maximumSubsoilTopError = 0f;
            for (int track = 0; track < profilePaths.Length; track++)
            {
                MapProfile profile = AssetDatabase.LoadAssetAtPath<MapProfile>(profilePaths[track]);
                Texture2D ground = AssetDatabase.LoadAssetAtPath<Texture2D>(groundPaths[track]);
                TextureImporter importer = AssetImporter.GetAtPath(groundPaths[track]) as TextureImporter;
                float expectedRepeatWidth = track switch
                {
                    0 => TerrainGenerator.CompactGroundModuleWidth,
                    1 => TerrainGenerator.CompactGroundModuleWidth,
                    2 => TerrainGenerator.MountainsGroundModuleWidth,
                    _ => TerrainGenerator.SpaceGroundModuleWidth
                };
                float expectedMaximumExtent = track switch
                {
                    0 => TerrainGenerator.CompactGroundMaximumVisualExtent,
                    1 => TerrainGenerator.NeonCityGroundMaximumVisualExtent,
                    2 => TerrainGenerator.MountainsGroundMaximumVisualExtent,
                    _ => TerrainGenerator.SpaceGroundMaximumVisualExtent
                };
                float expectedMaximumRise = track switch
                {
                    0 => 0f,
                    1 => TerrainGenerator.NeonCityGroundMaximumVisualRise,
                    2 => TerrainGenerator.MountainsGroundMaximumVisualRise,
                    _ => TerrainGenerator.SpaceGroundMaximumVisualRise
                };
                Vector2 expectedSurfaceRange = track switch
                {
                    0 => new Vector2(.313f, .701f),
                    1 => new Vector2(.313f, .83f),
                    2 => new Vector2(.2f, .677f),
                    _ => new Vector2(.177f, .714f)
                };
                float expectedContact = track < 2 ? .579f : track == 2 ? .54f : .56f;
                Vector4 horizontalUvRange = profile != null && profile.terrainEdgeMaterial != null &&
                                            profile.terrainEdgeMaterial.HasProperty("_HorizontalUvRange")
                    ? profile.terrainEdgeMaterial.GetVector("_HorizontalUvRange") : Vector4.zero;
                Vector2 horizontalRange = new Vector2(horizontalUvRange.x, horizontalUvRange.y);
                Vector2 expectedHorizontalRange = track == 2
                    ? new Vector2(.1f, .9f) : new Vector2(.05f, .95f);
                if (profile == null || ground == null || importer == null || profile.terrainEdgeMaterial == null ||
                    profile.terrainEdgeMaterial.mainTexture != ground || !profile.useSeparateTerrainMaterials ||
                    !profile.fitAuthoredTerrainTexture || profile.terrainSurfaceContactVSamples == null ||
                    profile.terrainSurfaceContactVSamples.Length != 0 || importer.mipmapEnabled ||
                    importer.wrapMode != TextureWrapMode.Repeat ||
                    Mathf.Abs(profile.terrainFillStartV - profile.terrainSurfaceContactV) > .0001f ||
                    Mathf.Abs(profile.terrainTextureRepeatWidth - expectedRepeatWidth) > .001f ||
                    ground.width != 2172 || ground.height != 724 || importer.maxTextureSize < 4096 ||
                    Mathf.Abs(profile.terrainMaximumVisualExtent - expectedMaximumExtent) > .001f ||
                    Mathf.Abs(profile.terrainMaximumVisualRiseAboveSurface - expectedMaximumRise) > .001f ||
                    Mathf.Abs(profile.terrainSurfaceContactV - expectedContact) > .001f ||
                    Vector2.Distance(profile.terrainSurfaceUvRange, expectedSurfaceRange) > .001f ||
                    track >= 2 &&
                    (!profile.terrainEdgeMaterial.HasProperty("_HorizontalInset") ||
                     Mathf.Abs(profile.terrainEdgeMaterial.GetFloat("_HorizontalInset") - .004f) > .001f ||
                     Vector2.Distance(horizontalRange, expectedHorizontalRange) > .001f))
                    throw new System.InvalidOperationException(
                        $"{profilePaths[track]} does not satisfy the shared authored-track dimensions.");

                TerrainGenerator.BuildMesh(mesh, profile, 0f, 80f, 96, 10f, false, 0f, 0f,
                    out float[] heights, out _, out _);
                Vector3[] vertices = mesh.vertices;
                float trackMaximumDepth = 0f;
                float trackMaximumExtent = 0f;
                float dx = 80f / 96f;
                for (int i = 0; i < heights.Length; i++)
                {
                    float groundTop = vertices[i * 6 + 2].y;
                    float groundBottom = vertices[i * 6 + 3].y;
                    float terrainTop = vertices[i * 6].y;
                    float textureU = i * dx / profile.terrainTextureRepeatWidth;
                    float contactV = TerrainGenerator.SampleGroundSurfaceContactV(profile, textureU);
                    float contactT = Mathf.InverseLerp(profile.terrainSurfaceUvRange.x,
                        profile.terrainSurfaceUvRange.y, contactV);
                    float visualContact = Mathf.Lerp(groundBottom, groundTop, contactT);
                    maximumAlignmentError = Mathf.Max(maximumAlignmentError,
                        Mathf.Abs(visualContact - heights[i]));
                    maximumSubsoilTopError = Mathf.Max(maximumSubsoilTopError,
                        Mathf.Abs(terrainTop - heights[i]));
                    float depth = heights[i] - groundBottom;
                    float rise = groundTop - heights[i];
                    trackMaximumDepth = Mathf.Max(trackMaximumDepth, depth);
                    trackMaximumExtent = Mathf.Max(trackMaximumExtent, depth, rise);
                }
                if (trackMaximumExtent > expectedMaximumExtent + .001f ||
                    trackMaximumDepth < expectedMaximumExtent - .05f)
                    throw new System.InvalidOperationException(
                        $"{profilePaths[track]} visual extent is {trackMaximumExtent:0.000}m and " +
                        $"depth is {trackMaximumDepth:0.000}m; expected {expectedMaximumExtent:0.000}m.");
            }
            Object.DestroyImmediate(mesh);
            if (maximumAlignmentError > .0001f || maximumSubsoilTopError > .0001f)
                throw new System.InvalidOperationException(
                    $"Shared authored-track geometry failed: alignment={maximumAlignmentError:0.000000}m, " +
                    $"subsoilTop={maximumSubsoilTopError:0.000000}m.");
            Debug.Log($"AUTHORED_TRACK_DIMENSIONS_PASS|tracks=rainyer,neon-city,mountains,space|" +
                      $"compactWidth={TerrainGenerator.CompactGroundModuleWidth:0.00}m|" +
                      $"compactDepths=rainyer:{TerrainGenerator.CompactGroundMaximumVisualExtent:0.00}m," +
                      $"neon-city:{TerrainGenerator.NeonCityGroundMaximumVisualExtent:0.00}m|" +
                      $"neonCityRise={TerrainGenerator.NeonCityGroundMaximumVisualRise:0.00}m|" +
                      $"mountains={TerrainGenerator.MountainsGroundModuleWidth:0.00}x" +
                      $"{TerrainGenerator.MountainsGroundMaximumVisualExtent:0.00}m|" +
                      $"space={TerrainGenerator.SpaceGroundModuleWidth:0.00}x" +
                      $"{TerrainGenerator.SpaceGroundMaximumVisualExtent:0.00}m|" +
                      $"alignment={maximumAlignmentError:0.000000}m|" +
                      $"subsoilTop={maximumSubsoilTopError:0.000000}m");
        }

        private static void ValidateFuelSequenceContract(BuiltAssets assets)
        {
            var sequence = new ProgressiveFuelSequence();
            sequence.Reset(0f, 5f, 5f);
            float[] expectedPositions = { 5f, 15f, 30f, 50f, 75f };
            float[] expectedSpacings = { 5f, 10f, 15f, 20f, 25f };
            for (int i = 0; i < expectedPositions.Length; i++)
            {
                if (Mathf.Abs(sequence.NextPosition - expectedPositions[i]) > .001f ||
                    Mathf.Abs(sequence.CurrentSpacing - expectedSpacings[i]) > .001f)
                    throw new System.InvalidOperationException("Fuel spacing accumulator is not 5/10/15/20/25 meters.");
                sequence.Advance(sequence.NextPosition);
            }
            sequence.Reset(0f, 5f, 5f);
            if (sequence.NextPosition != 5f || sequence.CurrentSpacing != 5f ||
                assets.countryside.initialFuelGap != 5f || assets.countrysideRoad.initialFuelGap != 5f)
                throw new System.InvalidOperationException("A new run must reset the Fuel sequence to exactly 5 meters.");
            Debug.Log("Fuel sequence validation: accumulator spacing=5/10/15/20/25m, positions=5/15/30/50/75m, reset=5m.");
        }

        private static void ValidateCoinRouteContract(BuiltAssets assets)
        {
            CoinRouteDefinition route = assets.countrysideCoinRoute;
            if (route == null || assets.countryside.coinRoute != route || assets.countrysideRoad.coinRoute != route)
                throw new System.InvalidOperationException("Countryside 2D and 3D must share one Coin Route asset.");
            int snapshotLastCell = route.LastCellForDistance(3200f);
            int deterministicHashA = RouteSnapshotHash(route, 0, snapshotLastCell);
            int deterministicHashB = RouteSnapshotHash(route, 0, snapshotLastCell);
            if (deterministicHashA != deterministicHashB)
                throw new System.InvalidOperationException("Countryside Coin Route changed between identical generations.");
            float earlyDensity = CountCoinsPer100(route, 0f, 300f);
            float middleDensity = CountCoinsPer100(route, 600f, 1200f);
            float lateDensity = CountCoinsPer100(route, 2000f, 3000f);
            if (earlyDensity <= 0f || middleDensity <= earlyDensity || lateDensity <= middleDensity ||
                lateDensity > route.MaximumGroupsPer100Meters * route.MaximumCoinsPerGroup + .01f)
                throw new System.InvalidOperationException($"Coin density progression invalid: " +
                    $"early={earlyDensity:0.0}, middle={middleDensity:0.0}, late={lateDensity:0.0} coins/100m.");
            bool foundSingleRow = false;
            bool foundGrowingSecondRow = false;
            bool foundFullSecondRow = false;
            int rowLastCell = route.LastCellForDistance(route.allFiveStartDistance + route.cellLength * 4f);
            for (int cell = 0; cell <= rowLastCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup rowGroup)) continue;
                if (rowGroup.lowerRowCoinCount + rowGroup.upperRowCoinCount != rowGroup.coinCount)
                    throw new System.InvalidOperationException("Coin Route row counts do not match the group total.");

                if (rowGroup.densityTier == 0)
                    foundSingleRow |= rowGroup.lowerRowCoinCount > 0 && rowGroup.upperRowCoinCount == 0;
                else if (rowGroup.densityTier == 1 || rowGroup.densityTier == 2)
                    foundGrowingSecondRow |= rowGroup.upperRowCoinCount > 0 &&
                                             rowGroup.upperRowCoinCount < rowGroup.lowerRowCoinCount;
                else
                    foundFullSecondRow |= rowGroup.upperRowCoinCount == rowGroup.lowerRowCoinCount;

                for (int point = 0; point < rowGroup.coinCount; point++)
                {
                    CoinRoutePoint routePoint = route.GetPoint(rowGroup, point);
                    float expectedHeight = point < rowGroup.lowerRowCoinCount ? 0f : route.rowVerticalSpacing;
                    if (Mathf.Abs(routePoint.heightOffset - expectedHeight) > .001f)
                        throw new System.InvalidOperationException("Coins must remain in two terrain-hugging rows.");
                }
            }
            if (!foundSingleRow || !foundGrowingSecondRow || !foundFullSecondRow)
                throw new System.InvalidOperationException(
                    "Coin Route must progress from one row to a partial second row and finally two full rows.");
            bool foundMixedGroup = false;
            int previousFiveCount = 0;
            int firstMixedCell = route.FirstCellForDistance(route.sparseEndDistance);
            int lastMixedCell = route.LastCellForDistance(route.allFiveStartDistance - 1f);
            for (int cell = firstMixedCell; cell <= lastMixedCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group) ||
                    group.anchorDistance < route.sparseEndDistance ||
                    group.anchorDistance >= route.allFiveStartDistance)
                    continue;
                foundMixedGroup = true;
                int fiveCount = 0;
                for (int point = 0; point < group.coinCount; point++)
                {
                    int value = route.GetPoint(group, point).value;
                    if (value == 5) fiveCount++;
                    else if (value != 1)
                        throw new System.InvalidOperationException("Coin progression can only contain Coin 01/Coin 05.");
                }
                if (fiveCount <= 0 || fiveCount >= group.coinCount || fiveCount < previousFiveCount)
                    throw new System.InvalidOperationException(
                        "Coin 05 count must grow progressively before the 10,000 m peak.");
                previousFiveCount = fiveCount;
            }
            if (!foundMixedGroup)
                throw new System.InvalidOperationException("Coin Route has no mixed Coin 01/Coin 05 phase.");
            int firstFinalCell = route.FirstCellForDistance(route.allFiveStartDistance);
            int lastFinalCell = route.LastCellForDistance(route.allFiveStartDistance + 1000f);
            bool foundFinalGroup = false;
            for (int cell = firstFinalCell; cell <= lastFinalCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group) || group.anchorDistance < route.allFiveStartDistance)
                    continue;
                foundFinalGroup = true;
                for (int point = 0; point < group.coinCount; point++)
                    if (route.GetPoint(group, point).value != 5)
                        throw new System.InvalidOperationException("All coins in the final route phase must be Coin 05.");
            }
            if (!foundFinalGroup)
                throw new System.InvalidOperationException("Coin Route has no all-Coin-05 final phase.");
            if (Mathf.Abs(route.allFiveStartDistance - 10000f) > .001f)
                throw new System.InvalidOperationException("Coin Route must reach its two-row Coin 05 peak at 10,000 m.");
            Debug.Log($"Countryside Coin Route validation | snapshot={deterministicHashA}, " +
                      $"density={earlyDensity:0.0}->{middleDensity:0.0}->{lateDensity:0.0} coins/100m, " +
                      $"rows=1->1+partial->2, maxGroups={route.MaximumGroupsPer100Meters:0.00}/100m, " +
                      $"Coin05=progressive, peak=10000m/all-05, deterministic=OK.");
        }

        private static int RouteSnapshotHash(CoinRouteDefinition route, int firstCell, int lastCell)
        {
            unchecked
            {
                int hash = 17;
                for (int cell = firstCell; cell <= lastCell; cell++)
                {
                    if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                    hash = hash * 31 + group.cellIndex;
                    hash = hash * 31 + Mathf.RoundToInt(group.anchorDistance * 1000f);
                    hash = hash * 31 + (int)group.pattern;
                    for (int point = 0; point < group.coinCount; point++)
                    {
                        CoinRoutePoint routePoint = route.GetPoint(group, point);
                        if (routePoint.value != 1 && routePoint.value != 5)
                            throw new System.InvalidOperationException("Coin Route contains a value other than 1 or 5.");
                        hash = hash * 31 + routePoint.value;
                        hash = hash * 31 + Mathf.RoundToInt(routePoint.distanceOffset * 100f);
                        hash = hash * 31 + Mathf.RoundToInt(routePoint.heightOffset * 100f);
                    }
                }
                return hash;
            }
        }

        private static float CountCoinsPer100(CoinRouteDefinition route, float start, float end)
        {
            int coins = 0;
            int firstCell = route.FirstCellForDistance(start);
            int lastCell = route.LastCellForDistance(end);
            for (int cell = firstCell; cell <= lastCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                for (int point = 0; point < group.coinCount; point++)
                {
                    float distance = group.anchorDistance + route.GetPoint(group, point).distanceOffset;
                    if (distance >= start && distance < end) coins++;
                }
            }
            return coins * 100f / Mathf.Max(1f, end - start);
        }

        private static void ValidateVehiclePhysicsContract(BuiltAssets assets)
        {
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            for (int d = 0; d < definitions.Length; d++)
            {
                VehicleDefinition definition = definitions[d];
                GameObject prefab = definition.gameplayPrefab2D;
                if (prefab == null) continue;

                VehiclePhysics2DController controller2D = prefab.GetComponent<VehiclePhysics2DController>();
                if (controller2D != null)
                {
                    Rigidbody2D rb2d = prefab.GetComponent<Rigidbody2D>();
                    Suspension2DController[] susp2d = prefab.GetComponentsInChildren<Suspension2DController>(true);
                    if (rb2d == null || susp2d.Length != 2)
                        throw new System.InvalidOperationException($"{definition.displayName} 2D Box2D prefab requires Rigidbody2D and 2 Suspension2DControllers.");
                }
                else
                {
                    CapsuleCollider chassis = prefab.GetComponent<CapsuleCollider>();
                    if (chassis == null || chassis.sharedMaterial != assets.chassisPhysics ||
                        chassis.sharedMaterial.bounciness > .001f ||
                        chassis.sharedMaterial.dynamicFriction < .08f ||
                        chassis.sharedMaterial.dynamicFriction > .24f ||
                        chassis.sharedMaterial.staticFriction < chassis.sharedMaterial.dynamicFriction ||
                        chassis.sharedMaterial.frictionCombine != PhysicsMaterialCombine.Multiply)
                        throw new System.InvalidOperationException($"{definition.displayName} requires a rounded, " +
                            "zero-bounce, low-friction sliding chassis collider.");
                    SuspensionController[] suspension = prefab.GetComponentsInChildren<SuspensionController>(true);
                    Rigidbody[] rigidbodies = prefab.GetComponentsInChildren<Rigidbody>(true);
                    ConfigurableJoint[] joints = prefab.GetComponentsInChildren<ConfigurableJoint>(true);
                    bool physicalWheels = suspension.Length == 2;
                    for (int wheel = 0; wheel < suspension.Length; wheel++)
                    {
                        ConfigurableJoint joint = suspension[wheel].SuspensionJoint;
                        physicalWheels &= suspension[wheel].GetComponent<Rigidbody>() != null &&
                                          suspension[wheel].GetComponent<SphereCollider>() != null &&
                                          joint != null && joint.gameObject == prefab &&
                                          joint.connectedBody == suspension[wheel].GetComponent<Rigidbody>() &&
                                          joint.secondaryAxis == Vector3.up &&
                                          !joint.configuredInWorldSpace;
                    }
                    if (rigidbodies.Length != 3 || joints.Length != 2 || !physicalWheels)
                        throw new System.InvalidOperationException($"{definition.displayName} requires one chassis and two independent physical wheel rigidbodies/colliders/joints.");
                }
                Transform rear = null;
                Transform front = null;
                if (controller2D != null)
                {
                    Suspension2DController[] susp2d = prefab.GetComponentsInChildren<Suspension2DController>(true);
                    for (int i = 0; i < susp2d.Length; i++)
                    {
                        if (susp2d[i].name.StartsWith("Rear")) rear = susp2d[i].transform;
                        if (susp2d[i].name.StartsWith("Front")) front = susp2d[i].transform;
                    }
                }
                else
                {
                    SuspensionController[] suspension = prefab.GetComponentsInChildren<SuspensionController>(true);
                    for (int i = 0; i < suspension.Length; i++)
                    {
                        if (suspension[i].name.StartsWith("Rear")) rear = suspension[i].transform;
                        if (suspension[i].name.StartsWith("Front")) front = suspension[i].transform;
                    }
                }
                if (rear == null || front == null ||
                    Mathf.Abs(rear.localPosition.x - definition.RearWheelAnchor) > .001f ||
                    Mathf.Abs(front.localPosition.x - definition.FrontWheelAnchor) > .001f)
                    throw new System.InvalidOperationException($"{definition.displayName} visual wheels and physical anchors are misaligned.");
                VehicleVisual2D visual = prefab.GetComponentInChildren<VehicleVisual2D>(true);
                SpriteRenderer bodyRenderer = visual != null ? visual.BodyRenderer : null;
                SpriteRenderer frontWheelRenderer = visual != null ? visual.FrontWheelRenderer : null;
                SpriteRenderer rearWheelRenderer = visual != null ? visual.RearWheelRenderer : null;
                if (bodyRenderer == null || frontWheelRenderer == null || rearWheelRenderer == null ||
                    frontWheelRenderer.sortingLayerID != bodyRenderer.sortingLayerID ||
                    rearWheelRenderer.sortingLayerID != bodyRenderer.sortingLayerID ||
                    frontWheelRenderer.sortingOrder <= bodyRenderer.sortingOrder ||
                    rearWheelRenderer.sortingOrder <= bodyRenderer.sortingOrder)
                    throw new System.InvalidOperationException($"{definition.displayName} wheels must render on the chassis layer with a greater sorting order.");
                for (int optionIndex = 0; optionIndex < definition.wheels.Length; optionIndex++)
                {
                    VehicleDefinition.CustomizationOption option = definition.wheels[optionIndex];
                    float sourceDiameter = VehicleDefinition.WheelSourceDiameter(option);
                    float effectiveRear = sourceDiameter *
                                          VehicleDefinition.WheelVisualScale(option, definition.RearWheelDiameter);
                    float effectiveFront = sourceDiameter *
                                           VehicleDefinition.WheelVisualScale(option, definition.FrontWheelDiameter);
                    if (Mathf.Abs(effectiveRear - definition.RearWheelDiameter) > .0001f ||
                        Mathf.Abs(effectiveFront - definition.FrontWheelDiameter) > .0001f)
                        throw new System.InvalidOperationException($"{definition.displayName} wheel skin " +
                            $"{option.optionId} is not normalized to its canonical axle diameters.");
                }
                for (int optionIndex = 0; optionIndex < definition.skins.Length; optionIndex++)
                {
                    Sprite sprite = definition.skins[optionIndex].sprite;
                    float effectiveWidth = (sprite != null ? sprite.bounds.size.x : 1f) *
                                           VehicleDefinition.SpriteWidthScale(sprite, definition.visualWidth);
                    if (Mathf.Abs(effectiveWidth - definition.visualWidth) > .0001f)
                        throw new System.InvalidOperationException($"{definition.displayName} body skin " +
                            $"{definition.skins[optionIndex].optionId} is not normalized to canonical width.");
                }
            }
            float jeepRearAnchor = assets.jeep.RearWheelAnchor;
            float jeepFrontAnchor = assets.jeep.FrontWheelAnchor;
            bool invalidJeepAnchors = float.IsNaN(jeepRearAnchor) || float.IsInfinity(jeepRearAnchor) ||
                                      float.IsNaN(jeepFrontAnchor) || float.IsInfinity(jeepFrontAnchor) ||
                                      jeepFrontAnchor - jeepRearAnchor < .5f;
            if (invalidJeepAnchors)
                throw new System.InvalidOperationException(
                    $"Jeep wheel anchors are invalid: rear={jeepRearAnchor}, front={jeepFrontAnchor}.");
            if (assets.groundPhysics == null || assets.countryside == null || assets.countrysideRoad == null ||
                assets.countryside.groundPhysicsMaterial != assets.groundPhysics ||
                assets.countrysideRoad.groundPhysicsMaterial != assets.groundPhysics)
                throw new System.InvalidOperationException(
                    "Countryside 2D and 3D must use the generated stable terrain physics material.");
            Debug.Log("Vehicle physics validation: low-friction zero-bounce sliding chassis, chassis-relative " +
                      "joint axes, bounded suspension, canonical skin sizes, visual/physical wheel anchors " +
                      "aligned and 2D wheels sorted above chassis.");
        }

        private struct SuspensionAuditResult
        {
            public int level;
            public float travel;
            public float maximumCompression;
            public float maximumExtension;
            public float spring;
            public float damping;
            public float staticCompression;
            public float predictedSag01;
            public float measuredSag01;
            public float settledOscillation;
            public float dampingRatio;
            public float minimumLength;
            public float progressiveFactor;
            public float compressionResistance;
            public float rideHeight;
        }

        private static void ValidateSuspensionContract(BuiltAssets assets)
        {
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            var failures = new List<string>();
            var summaries = new List<string>();
            int totalPassed = 0;
            int totalLevels = 0;
            for (int i = 0; i < definitions.Length; i++)
            {
                VehicleDefinition definition = definitions[i];
                int levelCount = Mathf.Max(1, definition.suspensionMaxTunes);
                int vehiclePassed = 0;
                SuspensionAuditResult previous = default;
                float minimumRideHeight = float.PositiveInfinity;
                float maximumRideHeight = float.NegativeInfinity;
                for (int level = 1; level <= levelCount; level++)
                {
                    SuspensionAuditResult result = AuditSuspension(definition, level);
                    var conditions = new List<string>();
                    CollectSuspensionFailures(definition, result, conditions);
                    if (level > 1)
                        CollectMonotonicFailures(previous, result, conditions);
                    minimumRideHeight = Mathf.Min(minimumRideHeight, result.rideHeight);
                    maximumRideHeight = Mathf.Max(maximumRideHeight, result.rideHeight);
                    if (conditions.Count == 0) vehiclePassed++;
                    else failures.Add($"{definition.displayName} LEVEL {level}: " + string.Join("; ", conditions) +
                                      $" | sag predicted/measured={result.predictedSag01:P1}/{result.measuredSag01:P1}, " +
                                      $"oscillation={result.settledOscillation:P1}, dampingRatio={result.dampingRatio:0.00}, " +
                                      $"minLength={result.minimumLength:0.000}m, progressive={result.progressiveFactor:0.00}x");
                    previous = result;
                }
                if (maximumRideHeight - minimumRideHeight > .04f)
                    failures.Add($"{definition.displayName} ALL LEVELS: ride-height drift " +
                                 $"{maximumRideHeight - minimumRideHeight:0.000}m > 0.040m");
                float visualRadius = Mathf.Max(definition.RearWheelDiameter,
                    definition.FrontWheelDiameter) * .5f;
                if (definition.archetype != VehicleArchetype.SportBike &&
                    Mathf.Abs(visualRadius - definition.wheelRadius) > .005f)
                    failures.Add($"{definition.displayName} ALL LEVELS: wheel sprite radius {visualRadius:0.000}m " +
                                 $"!= physical radius {definition.wheelRadius:0.000}m");
                if (definition.archetype == VehicleArchetype.SportBike &&
                    Mathf.Abs(definition.wheelRadius - .38f) > .001f)
                    failures.Add("SPORT BIKE: visual wheel enlargement changed the physical wheel radius");
                totalPassed += vehiclePassed;
                totalLevels += levelCount;
                summaries.Add($"{definition.displayName} {vehiclePassed}/{levelCount}");
            }
            CollectSuspensionDropFailures(definitions, failures);
            string summary = string.Join(" | ", summaries) + $" | TOTAL {totalPassed}/{totalLevels}";
            if (failures.Count > 0)
                throw new System.InvalidOperationException("SUSPENSION AUDIT FAILED\n" + summary +
                    "\nFAILED CONDITIONS:\n- " + string.Join("\n- ", failures));
            Debug.Log("Suspension Audit PASS: " + summary +
                      " | static 46-level matrix + min/mid/max drop/compression + max-speed landings PASS; " +
                      "predicted sag 25-35%, measured sag 24-36%, damping ratio 0.65-1.35 and monotonic curves valid.");
        }

        private static SuspensionAuditResult AuditSuspension(VehicleDefinition definition, int level)
        {
            var upgrades = new VehicleUpgradeState();
            upgrades.suspensionTune = level;
            ResolvedVehicleStats stats = definition.Resolve(upgrades);
            SuspensionPhysics.GetTravelLimits(definition.suspension, stats.suspensionTravel,
                out float maximumCompression, out float maximumExtension);
            float supportedMass = definition.mass * .5f;
            float staticCompression = supportedMass * Physics.gravity.magnitude / Mathf.Max(1f, stats.spring);
            float predictedSag = SuspensionPhysics.CalculateSag01(staticCompression,
                maximumCompression, maximumExtension);
            float dampingRatio = stats.damping /
                                 Mathf.Max(.001f, 2f * Mathf.Sqrt(stats.spring * supportedMass));
            SimulateStaticSag(definition, level, out float measuredSag, out float settledOscillation);
            float minimumLength = definition.suspension.restLength - maximumCompression;
            float linearForce = maximumCompression * .9f * stats.spring;
            float progressiveForce = SuspensionPhysics.CalculateNormalForce(maximumCompression * .9f, 0f,
                stats.spring, stats.damping, maximumCompression, maximumExtension,
                definition.suspension.bumpStopStart, stats.progressiveSpringMultiplier,
                definition.suspension.hardStopSpringMultiplier, float.MaxValue);
            return new SuspensionAuditResult
            {
                level = level,
                travel = stats.suspensionTravel,
                maximumCompression = maximumCompression,
                maximumExtension = maximumExtension,
                spring = stats.spring,
                damping = stats.damping,
                staticCompression = staticCompression,
                predictedSag01 = predictedSag,
                measuredSag01 = measuredSag,
                settledOscillation = settledOscillation,
                dampingRatio = dampingRatio,
                minimumLength = minimumLength,
                progressiveFactor = progressiveForce / Mathf.Max(1f, linearForce),
                compressionResistance = progressiveForce,
                rideHeight = definition.wheelRadius + definition.suspension.restLength - staticCompression
            };
        }

        private static void CollectSuspensionFailures(VehicleDefinition definition, SuspensionAuditResult result,
            List<string> failures)
        {
            if (result.predictedSag01 < .25f) failures.Add($"predictedSag {result.predictedSag01:P1} < 25.0%");
            if (result.predictedSag01 > .35f) failures.Add($"predictedSag {result.predictedSag01:P1} > 35.0%");
            if (result.measuredSag01 < .24f) failures.Add($"measuredSag {result.measuredSag01:P1} < 24.0%");
            if (result.measuredSag01 > .36f) failures.Add($"measuredSag {result.measuredSag01:P1} > 36.0%");
            float sagDifference = Mathf.Abs(result.predictedSag01 - result.measuredSag01);
            if (sagDifference > .04f) failures.Add($"predicted/measured sag difference {sagDifference:P1} > 4.0%");
            if (result.settledOscillation > .08f)
                failures.Add($"settled oscillation {result.settledOscillation:P1} > 8.0%");
            if (result.dampingRatio < .65f) failures.Add($"dampingRatio {result.dampingRatio:0.00} < 0.65");
            if (result.dampingRatio > 1.35f) failures.Add($"dampingRatio {result.dampingRatio:0.00} > 1.35");
            if (result.minimumLength < .08f) failures.Add($"minimumLength {result.minimumLength:0.000}m < 0.080m");
            if (result.progressiveFactor <= 1.05f)
                failures.Add($"progressive resistance {result.progressiveFactor:0.00}x <= 1.05x");
            if (definition.suspension.hardStopSpringMultiplier <= 0f)
                failures.Add("hard bump-stop multiplier is not positive");
            if (result.staticCompression > result.maximumCompression * .75f)
                failures.Add($"static compression uses {result.staticCompression / result.maximumCompression:P1} of max travel > 75.0%");
            if (definition.suspension.maximumAcceleration < 12f || definition.suspension.maximumAcceleration > 40f)
                failures.Add($"normal-force cap {definition.suspension.maximumAcceleration:0.0}g outside 12-40g");
            if (definition.suspension.maximumReboundAcceleration < .5f ||
                definition.suspension.maximumReboundAcceleration > 6f)
                failures.Add($"rebound acceleration cap {definition.suspension.maximumReboundAcceleration:0.0}g outside 0.5-6g");
        }

        private static void CollectMonotonicFailures(SuspensionAuditResult previous,
            SuspensionAuditResult current, List<string> failures)
        {
            if (current.travel + .0001f < previous.travel) failures.Add("travel decreased from previous level");
            if (current.spring + .1f < previous.spring) failures.Add("spring stiffness decreased from previous level");
            if (current.damping + .1f < previous.damping) failures.Add("damping decreased from previous level");
            if (current.compressionResistance + 1f < previous.compressionResistance)
                failures.Add("progressive compression resistance decreased from previous level");
            if (current.dampingRatio + .02f < previous.dampingRatio)
                failures.Add("damping stability decreased from previous level");
        }

        private static void SimulateStaticSag(VehicleDefinition source, int level,
            out float measuredSag, out float settledOscillation)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateSuspensionLevelClone(source, level);
            GameObject ground = new GameObject("Static Suspension Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -.5f, 0f);
            groundCollider.size = new Vector3(30f, 1f, 12f);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D,
                new Vector3(0f, 1.25f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            SuspensionController[] points = instance.GetComponentsInChildren<SuspensionController>(true);
            controller.AssignRuntimeReferences(testDefinition, null, points);
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");
            Physics.SyncTransforms();

            float total = 0f;
            int samples = 0;
            float minimum = 1f;
            float maximum = 0f;
            const float step = .02f;
            for (int frame = 0; frame < 350; frame++)
            {
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                if (frame < 250) continue;
                float frameSag = 0f;
                for (int p = 0; p < points.Length; p++) frameSag += points[p].Compression01;
                frameSag /= Mathf.Max(1, points.Length);
                total += frameSag;
                samples++;
                minimum = Mathf.Min(minimum, frameSag);
                maximum = Mathf.Max(maximum, frameSag);
            }
            measuredSag = samples > 0 ? total / samples : 0f;
            settledOscillation = maximum - minimum;
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
        }

        private static void ValidateGarageComposition(BuiltAssets assets)
        {
            GaragePreviewRenderer[] previews = Object.FindObjectsByType<GaragePreviewRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (previews.Length != 1)
                throw new System.InvalidOperationException("Garage requires exactly one composed preview renderer.");
            GaragePreviewRenderer preview = previews[0];
            SpriteRenderer body = preview.BodyRenderer;
            SpriteRenderer rear = preview.RearWheelRenderer;
            SpriteRenderer front = preview.FrontWheelRenderer;
            ArticulatedDriver2D driver = preview.Driver;
            preview.Apply(assets.jeep, assets.jeep.skins[0], assets.jeep.wheels[1], assets.stickmanBlack);
            if (body == null) throw new System.InvalidOperationException("Garage preview body renderer is missing.");
            if (rear == null || front == null) throw new System.InvalidOperationException("Garage preview wheel renderers are missing.");
            if (body.sprite != assets.jeepBody)
                throw new System.InvalidOperationException($"Garage Jeep body mismatch: actual={body.sprite?.name}, expected={assets.jeepBody?.name}.");
            if (rear.sprite != assets.jeepWheel02 || front.sprite != assets.jeepWheel02)
                throw new System.InvalidOperationException($"Garage Wheel 02 mismatch: rear={rear.sprite?.name}, front={front.sprite?.name}, " +
                    $"option={assets.jeep.wheels[1].sprite?.name}, expected={assets.jeepWheel02?.name}.");
            preview.Apply(assets.jeep, assets.jeep.skins[1], assets.jeep.wheels[2], assets.stickmanBlack);
            if (body.sprite != assets.jeepRetroSkin || rear.sprite != assets.jeepStarryWheel ||
                front.sprite != assets.jeepStarryWheel)
                throw new System.InvalidOperationException(
                    "Garage must preview and equip Jeep Retro Skin with Starry Wheel.");
            preview.Apply(assets.formula, assets.formula.skins[0], assets.formula.wheels[0], assets.stickmanBlack);
            if (body.sprite != assets.formulaBody || rear.sprite != assets.formulaWheel02 ||
                front.sprite != assets.formulaWheel02 || rear.sortingOrder <= body.sortingOrder ||
                front.sortingOrder <= body.sortingOrder)
                throw new System.InvalidOperationException("Garage Formula composition did not replace all Jeep pieces.");
            SerializedObject articulated = new SerializedObject(driver);
            SpriteRenderer formulaHead = articulated.FindProperty("head").objectReferenceValue as SpriteRenderer;
            SpriteRenderer formulaTorso = articulated.FindProperty("torso").objectReferenceValue as SpriteRenderer;
            SpriteRenderer formulaLeftThigh = articulated.FindProperty("leftThigh").objectReferenceValue as SpriteRenderer;
            SpriteRenderer formulaRightThigh = articulated.FindProperty("rightThigh").objectReferenceValue as SpriteRenderer;
            if (formulaHead == null || !formulaHead.enabled || formulaTorso == null || !formulaTorso.enabled ||
                formulaLeftThigh == null || formulaLeftThigh.enabled || formulaRightThigh == null || formulaRightThigh.enabled)
                throw new System.InvalidOperationException("Formula driver pose must show head/torso and hide both thighs.");
            float expectedFormulaHeadScale = Mathf.Abs(assets.formula.driverPose.head.scale.x) *
                                             ArticulatedDriver2D.CharacterVisualScaleMultiplier;
            if (Mathf.Abs(formulaHead.transform.lossyScale.x - expectedFormulaHeadScale) > .0001f)
                throw new System.InvalidOperationException(
                    "Character render pieces must keep the same effective scale across vehicle root poses.");
            SphereCollider formulaHeadTrigger = formulaHead.GetComponentInChildren<SphereCollider>(true);
            float expectedHeadTriggerRadius = Mathf.Min(formulaHead.sprite.bounds.extents.x,
                formulaHead.sprite.bounds.extents.y) * .82f;
            if (formulaHeadTrigger == null || !formulaHeadTrigger.isTrigger ||
                Mathf.Abs(formulaHeadTrigger.radius - expectedHeadTriggerRadius) > .0001f)
                throw new System.InvalidOperationException(
                    "Driver head trigger must cover the visible head instead of retaining the tiny legacy radius.");
            preview.Apply(assets.formula, assets.formula.skins[1], assets.formula.wheels[2], assets.stickmanBlack);
            Transform electricInternal = body.transform.Find("Formula Electricity - Internal");
            Transform electricExternal = body.transform.Find("Formula Electricity - External Glow");
            if (body.sprite != assets.formulaNeonSkin || rear.sprite != assets.luxuryWheel ||
                front.sprite != assets.luxuryWheel ||
                (electricInternal != null && electricInternal.gameObject.activeSelf) ||
                (electricExternal != null && electricExternal.gameObject.activeSelf))
                throw new System.InvalidOperationException(
                    "Formula Neon must preview unchanged with Luxury and without electricity animation.");
            preview.Apply(assets.formula, assets.formula.skins[2], assets.formula.wheels[2], assets.stickmanBlack);
            electricInternal = body.transform.Find("Formula Electricity - Internal");
            electricExternal = body.transform.Find("Formula Electricity - External Glow");
            if (body.sprite != assets.formulaElectricitySkin || electricInternal == null || electricExternal == null ||
                !electricInternal.gameObject.activeSelf || !electricExternal.gameObject.activeSelf ||
                rear.sortingOrder <= electricInternal.GetComponent<SpriteRenderer>().sortingOrder ||
                front.sortingOrder <= electricInternal.GetComponent<SpriteRenderer>().sortingOrder ||
                rear.sortingOrder <= electricExternal.GetComponent<SpriteRenderer>().sortingOrder ||
                front.sortingOrder <= electricExternal.GetComponent<SpriteRenderer>().sortingOrder)
                throw new System.InvalidOperationException(
                    "Formula Electricity must preview independently with both wheels above its electric layers.");
            preview.Apply(assets.formula, assets.formula.skins[0], assets.formula.wheels[0], assets.stickmanBlack);
            if (body.sprite != assets.formulaBody || electricInternal.gameObject.activeSelf ||
                electricExternal.gameObject.activeSelf)
                throw new System.InvalidOperationException(
                    "Formula Default must restore its own appearance and disable electricity animation.");
            preview.Apply(assets.rally, assets.rally.skins[0], assets.rally.wheels[0], assets.stickmanBlack);
            if (body.sprite != assets.rallyBody || rear.sprite != assets.rallyWheel || front.sprite != assets.rallyWheel ||
                !formulaHead.enabled || formulaTorso.enabled || formulaLeftThigh.enabled || formulaRightThigh.enabled)
                throw new System.InvalidOperationException("Rally garage must stay intact, use both Rally wheels, and show only the driver head.");
            preview.Apply(assets.sportBike, assets.sportBike.skins[0], assets.sportBike.wheels[0], assets.stickmanBlack);
            Vector2 bodyVisibleSize = VisibleSpriteSize(assets.sportBikeBody);
            float bodyVisibleHeight = bodyVisibleSize.y * body.transform.localScale.y;
            float frontPercent = assets.sportBike.FrontWheelDiameter / Mathf.Max(.001f, bodyVisibleHeight);
            float rearPercent = assets.sportBike.RearWheelDiameter / Mathf.Max(.001f, bodyVisibleHeight);
            bool sportCompositionValid = body.sprite == assets.sportBikeBody &&
                rear.sprite == assets.sportBikeWheel01 && front.sprite == assets.sportBikeWheel01 && !body.flipX;
            bool sportProportionsValid = frontPercent >= .40f && frontPercent <= .42f &&
                                         rearPercent >= .43f && rearPercent <= .45f;
            bool sportMountsValid = (Vector2)front.transform.localPosition ==
                                    assets.sportBike.frontWheelVisualPosition &&
                                    (Vector2)rear.transform.localPosition ==
                                    assets.sportBike.rearWheelVisualPosition;
            if (!sportCompositionValid || !sportProportionsValid || !sportMountsValid)
                throw new System.InvalidOperationException(
                    $"Sport Bike preview invalid: composition={sportCompositionValid}, " +
                    $"front={frontPercent:P2}, rear={rearPercent:P2}, mounts={sportMountsValid}, " +
                    $"visibleBodyHeight={bodyVisibleHeight:0.000}m.");
            for (int wheelIndex = 0; wheelIndex < assets.sportBike.wheels.Length; wheelIndex++)
            {
                VehicleDefinition.CustomizationOption wheelOption = assets.sportBike.wheels[wheelIndex];
                preview.Apply(assets.sportBike, assets.sportBike.skins[0], wheelOption, assets.stickmanBlack);
                float apparentFront = front.transform.localScale.x * wheelOption.visibleDiameter;
                float apparentRear = rear.transform.localScale.x * wheelOption.visibleDiameter;
                if (wheelOption.visibleDiameter <= 0f ||
                    Mathf.Abs(apparentFront - assets.sportBike.FrontWheelDiameter) > .001f ||
                    Mathf.Abs(apparentRear - assets.sportBike.RearWheelDiameter) > .001f ||
                    front.sortingOrder <= body.sortingOrder || rear.sortingOrder <= body.sortingOrder)
                    throw new System.InvalidOperationException(
                        $"Sport Bike wheel option {wheelOption.displayName} is not visibly normalized.");
            }
            preview.Apply(assets.jeep, assets.jeep.skins[0], assets.jeep.wheels[0], assets.stickmanBlack);
            if (!formulaLeftThigh.enabled || formulaRightThigh.enabled)
                throw new System.InvalidOperationException(
                    "Jeep driver pose must restore one clean side-view leg without duplicated limb sprites.");
            VehicleDefinition[] catalogVehicles = assets.catalog?.vehicles ?? System.Array.Empty<VehicleDefinition>();
            for (int vehicleIndex = 0; vehicleIndex < catalogVehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = catalogVehicles[vehicleIndex];
                if (vehicle == null) continue;
                VehicleDefinition.CustomizationOption skin = vehicle.skins != null && vehicle.skins.Length > 0
                    ? vehicle.skins[0] : null;
                VehicleDefinition.CustomizationOption wheel = vehicle.wheels != null && vehicle.wheels.Length > 0
                    ? vehicle.wheels[0] : null;
                preview.Apply(vehicle, skin, wheel, assets.stickmanBlack);
                if (preview.HorizontalCenterError > .001f)
                    throw new System.InvalidOperationException(
                        $"{vehicle.displayName}: Garage composition is not centered over the presentation stand.");
            }
            Debug.Log($"Garage live composition validation: Formula Wheel 02 correct; Sport Bike front " +
                      $"{assets.sportBike.FrontWheelDiameter:0.00}m ({frontPercent:P1}), rear " +
                      $"{assets.sportBike.RearWheelDiameter:0.00}m ({rearPercent:P1}), all three wheel skins " +
                      $"normalized; {catalogVehicles.Length} catalog vehicles centered.");
        }

        private static void ValidateCharacterAccessoryAndShopContract(BuiltAssets assets)
        {
            DriverDefinition driver = assets.stickmanBlack;
            string[] requiredAccessoryIds =
            {
                "default", "glasses", "fire-glasses", "scarf", "santa-hat", "neon-glasses",
                "tire-accessory", "baseball-cap", "baseball-cap-rainbow"
            };
            bool stableOrder = driver?.accessories != null &&
                driver.accessories.Length == requiredAccessoryIds.Length;
            for (int i = 0; stableOrder && i < requiredAccessoryIds.Length; i++)
                stableOrder &= driver.accessories[i] != null &&
                    driver.accessories[i].accessoryId == requiredAccessoryIds[i];
            DriverDefinition.CharacterAccessoryOption normalCap = stableOrder ? driver.accessories[7] : null;
            DriverDefinition.CharacterAccessoryOption rainbowCap = stableOrder ? driver.accessories[8] : null;
            if (!stableOrder || driver.accessories[0].sprite != null ||
                driver.accessories[1].sprite != assets.driverGlasses ||
                driver.accessories[2].sprite != assets.driverFireGlasses || normalCap.sprite == null ||
                !normalCap.unlockedByDefault || normalCap.gemPrice != 0 || !normalCap.supportsFlipVariant ||
                rainbowCap.sprite != normalCap.sprite || rainbowCap.unlockedByDefault ||
                rainbowCap.gemPrice != 0 || !rainbowCap.supportsFlipVariant || !rainbowCap.animateRainbow)
                throw new System.InvalidOperationException(
                    "Characters requires nine stable accessories, one shared-orientation free cap and " +
                    "the Starter Pack animated Neon Lightning cap.");

            VehicleDefinition[] vehicles = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            GaragePreviewRenderer preview = Object.FindFirstObjectByType<GaragePreviewRenderer>(FindObjectsInactive.Include);
            if (preview?.Driver == null)
                throw new System.InvalidOperationException("Character accessory preview driver is missing.");
            for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = vehicles[vehicleIndex];
                VehicleDefinition.DriverPartPose headPose = vehicle.driverPose.head;
                VehicleDefinition.DriverPartPose accessoryPose = vehicle.driverPose.accessory;
                if (!accessoryPose.visible || accessoryPose.sortingOrder <= headPose.sortingOrder ||
                    Mathf.Abs(accessoryPose.position.y - headPose.position.y) > .08f ||
                    accessoryPose.position.x >= headPose.position.x)
                    throw new System.InvalidOperationException(
                        $"{vehicle.displayName}: AccessoryAnchor is not aligned to the head/cockpit sorting contract.");
                for (int accessoryIndex = 0; accessoryIndex < driver.accessories.Length; accessoryIndex++)
                {
                    DriverDefinition.CharacterAccessoryOption option = driver.accessories[accessoryIndex];
                    preview.Driver.Apply(driver, vehicle.driverPose, option, null, false);
                    bool expectedVisible = option.sprite != null;
                    if (preview.Driver.AccessoryRenderer == null ||
                        preview.Driver.AccessoryRenderer.enabled != expectedVisible ||
                        preview.Driver.AccessoryRenderer.sprite != option.sprite ||
                        preview.Driver.AccessoryRenderer.sortingOrder != ArticulatedDriver2D.AccessorySortingOrder)
                        throw new System.InvalidOperationException(
                            $"{vehicle.displayName}: accessory preview transition failed for {option.accessoryId}.");
                }
            }

            const string selectionKey = "AHR.Customize.CharacterAccessory";
            bool hadSelection = PlayerPrefs.HasKey(selectionKey);
            string previousSelection = PlayerPrefs.GetString(selectionKey, string.Empty);
            SaveManager.SetSelectedCharacterAccessoryId("fire-glasses");
            if (SaveManager.GetSelectedCharacterAccessoryId("default") != "fire-glasses")
                throw new System.InvalidOperationException("Character accessory stable-ID persistence failed.");
            if (hadSelection) PlayerPrefs.SetString(selectionKey, previousSelection);
            else PlayerPrefs.DeleteKey(selectionKey);
            PlayerPrefs.Save();

            ScrollRect[] scrollRects = Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            ScrollRect shop = null;
            for (int i = 0; i < scrollRects.Length; i++)
                if (scrollRects[i] != null && scrollRects[i].gameObject.name == "Store Panel") shop = scrollRects[i];
            if (shop == null || shop.horizontal || !shop.vertical || shop.viewport == null || shop.content == null ||
                shop.movementType != ScrollRect.MovementType.Clamped ||
                shop.content.GetComponent<VerticalLayoutGroup>() == null ||
                shop.content.GetComponent<ContentSizeFitter>() == null ||
                shop.content.Find("Special Offers") == null ||
                shop.content.Find("Blue Prints Accesories") == null ||
                shop.content.Find("Character Accessories") == null)
                throw new System.InvalidOperationException(
                    "Shop requires one clamped vertical ScrollRect with Viewport/Content/layout and both ordered sections.");
            if (shop.transform.parent == null || shop.transform.parent.name != "Safe Area")
                throw new System.InvalidOperationException("Shop is not contained by the mobile Safe Area.");
            Debug.Log("CHARACTERS_SHOP_AUDIT_PASS: 9 stable accessories x 4 vehicle poses, shared cap " +
                      "orientation, animated Rainbow pricing, preview transitions, save/restore and clamped " +
                      "mobile vertical Shop structure validated; IAP offers remain disabled.");
        }

        private static VehicleDefinition.DriverPose CreateJeepDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float sx = .042f,
                float sy = .042f, bool visible = true, int order = 3) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, sx, sy, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-.42f, .24f), rootScale = .96f,
                torso = P(-.01f, .25f, -10f, .030f, .032f, true, 3),
                head = P(-.04f, .68f, -3f, .032f, .034f, true, 5),
                // The source arm/leg sprites already contain their complete bent limb.
                // Render one side silhouette instead of stacking duplicate limb sprites.
                leftUpperArm = P(.20f, .38f, -4f, .040f, .030f, true, 4),
                leftForearm = P(.39f, .27f, 0f, .021f, .021f, false, 4),
                rightUpperArm = P(.11f, .39f, 0f, .022f, .022f, false, 2),
                rightForearm = P(.35f, .25f, 0f, .021f, .021f, false, 2),
                leftThigh = P(.16f, .10f, 0f, .032f, .030f, true, 2),
                leftLowerLeg = P(.17f, -.28f, 0f, .021f, .021f, false, 2),
                rightThigh = P(-.11f, -.02f, 0f, .023f, .023f, false, 1),
                rightLowerLeg = P(.09f, -.27f, 0f, .021f, .021f, false, 1),
                accessory = P(-.34f, .67f, -3f, 1f, 1f, true, 6)
            };
        }

        private static VehicleDefinition.DriverPose CreateFormulaDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float sx = .038f,
                float sy = .038f, bool visible = true, int order = 3) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, sx, sy, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(.16f, .18f), rootScale = .86f,
                torso = P(-.02f, .12f, -8f, .026f, .029f, true, 12),
                head = P(.05f, .45f, -10f, .031f, .034f, true, 15),
                leftUpperArm = P(.17f, .22f, -8f, .020f, .020f, false, 14),
                leftForearm = P(.40f, .06f, 0f, .019f, .019f, false, 14),
                rightUpperArm = P(.11f, .18f, 0f, .020f, .020f, false, 13),
                rightForearm = P(.36f, .04f, 0f, .019f, .019f, false, 13),
                leftThigh = P(0f, 0f, 0f, .02f, .02f, false, 1),
                leftLowerLeg = P(0f, 0f, 0f, .02f, .02f, false, 1),
                rightThigh = P(0f, 0f, 0f, .02f, .02f, false, 1),
                rightLowerLeg = P(0f, 0f, 0f, .02f, .02f, false, 1),
                accessory = P(-.23f, .44f, -10f, 1f, 1f, true, 16)
            };
        }

        private static VehicleDefinition.DriverPose CreateRallyDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float scale = .03f,
                bool visible = false, int order = 3) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, scale, scale, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-.34f, .26f), rootScale = .88f,
                torso = P(0f, 0f, 0f),
                head = P(-.03f, .52f, -3f, .031f, true, 8),
                leftUpperArm = P(0f, 0f, 0f), leftForearm = P(0f, 0f, 0f),
                rightUpperArm = P(0f, 0f, 0f), rightForearm = P(0f, 0f, 0f),
                leftThigh = P(0f, 0f, 0f), leftLowerLeg = P(0f, 0f, 0f),
                rightThigh = P(0f, 0f, 0f), rightLowerLeg = P(0f, 0f, 0f),
                accessory = P(-.33f, .51f, -3f, 1f, true, 9)
            };
        }

        private static VehicleDefinition.DriverPose CreateSportBikeDriverPose()
        {
            VehicleDefinition.DriverPartPose P(float x, float y, float r, float scale = .020f,
                bool visible = true, int order = 4) =>
                VehicleDefinition.DriverPartPose.Create(x, y, r, scale, scale, visible, order);
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-.33f, .42f), rootScale = 1f,
                torso = P(.14f, .25f, -50f, .026f, true, 13),
                head = P(.42f, .54f, -24f, .031f, true, 17),
                leftUpperArm = VehicleDefinition.DriverPartPose.Create(
                    .57f, .27f, -6f, .047f, .030f, true, 16),
                leftForearm = P(.62f, .16f, 0f, .021f, false, 16),
                rightUpperArm = P(.30f, .31f, 0f, .022f, false, 15),
                rightForearm = P(.58f, .14f, 0f, .021f, false, 15),
                leftThigh = P(.25f, .05f, -5f, .032f, true, 14),
                leftLowerLeg = P(.20f, -.21f, 0f, .022f, false, 14),
                rightThigh = P(-.09f, 0f, 0f, .024f, false, 12),
                rightLowerLeg = P(.14f, -.23f, 0f, .022f, false, 12),
                accessory = P(.13f, .53f, -24f, 1f, true, 18)
            };
        }

        private static float[] CreateProgressionTargets(int maximumLevel, float stockValue, float maximumValue)
        {
            float[] values = new float[Mathf.Max(1, maximumLevel)];
            for (int level = 1; level <= values.Length; level++)
                values[level - 1] = Mathf.Lerp(stockValue, maximumValue,
                    VehicleDefinition.UpgradeProgress(level, values.Length));
            return values;
        }

        private static VehicleDefinition.TuneTargets CreateFormulaTuneTargets()
        {
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = CreateProgressionTargets(20, 160f, 300f),
                accelerationZeroToHundredSeconds = CreateProgressionTargets(20, 5f, 2f),
                gripRatings = CreateProgressionTargets(20, 200f, 520f),
                airControlTorque = new float[] { 9000f },
                airborneDownforceAcceleration = CreateProgressionTargets(20, 3f, 27f),
                fuelRunDurationSeconds = CreateProgressionTargets(25, 20f, 45f)
            };
        }

        private static VehicleDefinition.TuneTargets CreateRallyTuneTargets()
        {
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = CreateProgressionTargets(20, 145f, 280f),
                accelerationZeroToHundredSeconds = CreateProgressionTargets(20, 5.8f, 2.5f),
                gripRatings = CreateProgressionTargets(14, 167f, 750f),
                airControlTorque = new[] { 6500f },
                airborneDownforceAcceleration = new[] { 11f },
                fuelRunDurationSeconds = CreateProgressionTargets(15, 25f, 50f)
            };
        }

        private static VehicleDefinition.TuneTargets CreateSportBikeTuneTargets()
        {
            return new VehicleDefinition.TuneTargets
            {
                // Stock must already be usable: it can exceed 100 km/h, has a measurable 0-100,
                // and retains deliberately weak (but non-zero) traction. Design maxima are unchanged.
                engineTopSpeedKmh = CreateProgressionTargets(20, 120f, 360f),
                accelerationZeroToHundredSeconds = CreateProgressionTargets(20, 8f, 2.5f),
                gripRatings = CreateProgressionTargets(20, 160f, 500f),
                airControlTorque = new[] { 7600f },
                airborneDownforceAcceleration = new[] { 10f },
                fuelRunDurationSeconds = CreateProgressionTargets(15, 15f, 45f)
            };
        }

        private static void CreateVehiclePrefabs(BuiltAssets assets)
        {
            assets.jeep.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.jeep, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject, Prefabs + "/Jeep_2D.prefab");
            assets.jeep.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.jeep, null, null, assets.chassisPhysics).gameObject, Prefabs + "/Jeep_3D.prefab");
            assets.formula.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.formula, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject, Prefabs + "/Formula_2D.prefab");
            assets.formula.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.formula, null, null, assets.chassisPhysics).gameObject, Prefabs + "/Formula_3D.prefab");
            assets.rally.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.rally, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject, Prefabs + "/Rally_2D.prefab");
            assets.rally.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.rally, null, null, assets.chassisPhysics).gameObject, Prefabs + "/Rally_3D.prefab");
            assets.sportBike.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.sportBike, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject,
                Prefabs + "/SportBike_2D.prefab");
            assets.sportBike.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.sportBike, null, null, assets.chassisPhysics).gameObject,
                Prefabs + "/SportBike_3D.prefab");
            EditorUtility.SetDirty(assets.jeep);
            EditorUtility.SetDirty(assets.formula);
            EditorUtility.SetDirty(assets.rally);
            EditorUtility.SetDirty(assets.sportBike);
            assets.hillAdventurer.gameplayPrefab2D = SaveVehiclePrefab(
                CreateClimbVehicle(assets.hillAdventurer, null, null, assets.chassisPhysics, assets.tirePhysics).gameObject,
                Prefabs + "/HillAdventurer_2D.prefab");
            assets.hillAdventurer.gameplayPrefab3D = SaveVehiclePrefab(
                CreateRoadVehicle(assets.hillAdventurer, null, null, assets.chassisPhysics).gameObject,
                Prefabs + "/HillAdventurer_3D.prefab");
            EditorUtility.SetDirty(assets.hillAdventurer);

            // Permanently install 2D Box2D physics on all 2D Adventure prefabs
            AdventureBox2DGoldenInstaller.InstallGoldenBox2D();
        }

        private static GameObject SaveVehiclePrefab(GameObject root, string path)
        {
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private struct WheelStabilityResult
        {
            public float maximumAxisDrift;
            public float maximumAnchorDrift;
            public float settledCompressionSpan;
            public float maximumRelativeWheelVelocity;
            public float stationaryRelativeWheelVelocity;
            public float coastRelativeWheelVelocity;
            public float seamRelativeWheelVelocity;
            public float motorRelativeWheelVelocity;
            public float reverseRelativeWheelVelocity;
            public float maximumChassisVerticalVelocity;
            public float maximumTravelExcess;
        }

        private static void ValidateTerrainSeamContinuity(MapProfile profile)
        {
            Mesh first = new Mesh();
            Mesh second = new Mesh();
            TerrainGenerator.BuildMesh(first, profile, 0f, 80f, 48, 10f, false, 0f, 0f,
                out float[] firstHeights, out float endHeight, out float endSlope);
            TerrainGenerator.BuildMesh(second, profile, 80f, 80f, 48, 10f, true, endHeight, endSlope,
                out float[] secondHeights, out _, out _);
            float dx = 80f / 48f;
            float outgoingSlope = (firstHeights[firstHeights.Length - 1] -
                                   firstHeights[firstHeights.Length - 2]) / dx;
            float incomingSlope = (secondHeights[1] - secondHeights[0]) / dx;
            float heightGap = Mathf.Abs(firstHeights[firstHeights.Length - 1] - secondHeights[0]);
            float slopeGap = Mathf.Abs(outgoingSlope - incomingSlope);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            if (heightGap > .00001f || slopeGap > .00001f)
                throw new System.InvalidOperationException($"Terrain seam discontinuity: height={heightGap:0.000000}m, slope={slopeGap:0.000000}.");
            Debug.Log($"Terrain seam continuity PASS: heightGap={heightGap:0.000000}m, slopeGap={slopeGap:0.000000}; " +
                      "one MeshCollider surface plus a non-physical LineRenderer edge.");
        }

        private static WheelStabilityResult SimulateWheelStability(VehicleDefinition source, int level)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateSuspensionLevelClone(source, level);
            PhysicsMaterial terrainMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                Root + "/Materials/TerrainStable.physicMaterial");
            GameObject firstGround = new GameObject("Single Authoritative Flat Diagnostic Ground");
            MeshCollider firstCollider = firstGround.AddComponent<MeshCollider>();
            Mesh diagnosticGroundMesh = new Mesh { name = "Diagnostic Welded Seam Mesh" };
            diagnosticGroundMesh.vertices = new[]
            {
                new Vector3(-1000f, 0f, -10f), new Vector3(-1000f, 0f, 10f),
                new Vector3(100f, 0f, -10f), new Vector3(100f, 0f, 10f),
                new Vector3(1000f, 0f, -10f), new Vector3(1000f, 0f, 10f)
            };
            diagnosticGroundMesh.triangles = new[] { 0, 1, 2, 1, 3, 2, 2, 3, 4, 3, 5, 4 };
            diagnosticGroundMesh.RecalculateNormals();
            diagnosticGroundMesh.RecalculateBounds();
            firstCollider.sharedMesh = diagnosticGroundMesh;
            firstCollider.sharedMaterial = terrainMaterial;
            GameObject inputObject = new GameObject("Wheel Diagnostic Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D,
                new Vector3(0f, 1.25f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            SuspensionController[] points = instance.GetComponentsInChildren<SuspensionController>(true);
            controller.AssignRuntimeReferences(testDefinition, input, points);
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");
            Physics.SyncTransforms();

            Vector3[] expectedAnchors = new Vector3[points.Length];
            Vector3[] expectedConnectedAnchors = new Vector3[points.Length];
            float[] minimumCompression = new float[points.Length];
            float[] maximumCompression = new float[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                ConfigurableJoint joint = points[i].SuspensionJoint;
                if (joint == null || joint.gameObject != instance || joint.connectedBody != points[i].WheelBody)
                    throw new System.InvalidOperationException($"{source.displayName} L{level}: joint is not chassis-relative.");
                expectedAnchors[i] = joint.anchor;
                expectedConnectedAnchors[i] = joint.connectedAnchor;
                minimumCompression[i] = float.PositiveInfinity;
                maximumCompression[i] = float.NegativeInfinity;
            }

            WheelStabilityResult result = default;
            for (int frame = 0; frame < 350; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 250)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 0);
            }

            float[] speedsKmh = { 50f, 100f, 150f, 200f, source.topSpeedMps * 3.6f };
            for (int speedIndex = 0; speedIndex < speedsKmh.Length; speedIndex++)
            {
                float speed = speedsKmh[speedIndex] / 3.6f;
                controller.Body.linearVelocity = controller.transform.right * speed;
                for (int p = 0; p < points.Length; p++)
                {
                    points[p].WheelBody.linearVelocity = controller.Body.linearVelocity;
                    points[p].WheelBody.angularVelocity = controller.Body.angularVelocity +
                        Vector3.back * (speed / Mathf.Max(.05f, source.wheelRadius));
                }
                for (int frame = 0; frame < 100; frame++)
                {
                    StepWheelDiagnostic(controller, plane, input);
                    if (frame >= 30)
                        SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                            ref result, minimumCompression, maximumCompression, 1);
                }
            }

            // Isolate the collider-boundary test from the speed sweeps: approach the exact x=100 seam slowly.
            Vector3 relocation = new Vector3(98f - controller.Body.position.x, 0f, 0f);
            controller.Body.position += relocation;
            controller.Body.linearVelocity = Vector3.zero;
            controller.Body.angularVelocity = Vector3.zero;
            for (int p = 0; p < points.Length; p++)
            {
                points[p].WheelBody.position += relocation;
                points[p].WheelBody.linearVelocity = Vector3.zero;
                points[p].WheelBody.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            for (int frame = 0; frame < 200; frame++)
                StepWheelDiagnostic(controller, plane, input);
            controller.Body.linearVelocity = controller.transform.right * (5f / 3.6f);
            for (int p = 0; p < points.Length; p++)
            {
                points[p].WheelBody.linearVelocity = controller.Body.linearVelocity;
                points[p].WheelBody.angularVelocity = controller.Body.angularVelocity +
                    Vector3.back * ((5f / 3.6f) / Mathf.Max(.05f, source.wheelRadius));
            }
            for (int frame = 0; frame < 120; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 20)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 4);
            }

            input.SetTouchGas(true);
            for (int frame = 0; frame < 250; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 50)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 2);
            }
            input.SetTouchGas(false);
            input.SetTouchBrake(true);
            for (int frame = 0; frame < 250; frame++)
            {
                StepWheelDiagnostic(controller, plane, input);
                if (frame >= 50)
                    SampleWheelDiagnostic(controller, points, expectedAnchors, expectedConnectedAnchors,
                        ref result, minimumCompression, maximumCompression, 3);
            }

            for (int i = 0; i < points.Length; i++)
                result.settledCompressionSpan = Mathf.Max(result.settledCompressionSpan,
                    maximumCompression[i] - minimumCompression[i]);
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(firstGround);
            Object.DestroyImmediate(diagnosticGroundMesh);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return result;
        }

        private static void StepWheelDiagnostic(VehicleController controller, MovementPlaneConstraint plane,
            VehicleInputController input)
        {
            InvokePrivate(input, "Update");
            InvokePrivate(controller, "FixedUpdate");
            if (plane != null) InvokePrivate(plane, "FixedUpdate");
            Physics.Simulate(.02f);
        }

        private static void SampleWheelDiagnostic(VehicleController controller, SuspensionController[] points,
            Vector3[] expectedAnchors, Vector3[] expectedConnectedAnchors, ref WheelStabilityResult result,
            float[] minimumCompression, float[] maximumCompression, int phase)
        {
            result.maximumChassisVerticalVelocity = Mathf.Max(result.maximumChassisVerticalVelocity,
                Mathf.Abs(Vector3.Dot(controller.Body.linearVelocity, controller.transform.up)));
            for (int i = 0; i < points.Length; i++)
            {
                ConfigurableJoint joint = points[i].SuspensionJoint;
                result.maximumAxisDrift = Mathf.Max(result.maximumAxisDrift,
                    Vector3.Angle(points[i].SuspensionAxisWorld, controller.transform.up));
                result.maximumAnchorDrift = Mathf.Max(result.maximumAnchorDrift,
                    Vector3.Distance(joint.anchor, expectedAnchors[i]),
                    Vector3.Distance(joint.connectedAnchor, expectedConnectedAnchors[i]));
                Vector3 relativeVelocity = points[i].WheelBody.linearVelocity -
                                           controller.Body.GetPointVelocity(points[i].WheelBody.position);
                float verticalWheelVelocity = Mathf.Abs(Vector3.Dot(relativeVelocity, controller.transform.up));
                if (points[i].IsGrounded)
                {
                    result.maximumRelativeWheelVelocity = Mathf.Max(result.maximumRelativeWheelVelocity,
                        verticalWheelVelocity);
                    if (phase == 0) result.stationaryRelativeWheelVelocity = Mathf.Max(
                        result.stationaryRelativeWheelVelocity, verticalWheelVelocity);
                    else if (phase == 1) result.coastRelativeWheelVelocity = Mathf.Max(
                        result.coastRelativeWheelVelocity, verticalWheelVelocity);
                    else if (phase == 2) result.motorRelativeWheelVelocity = Mathf.Max(
                        result.motorRelativeWheelVelocity, verticalWheelVelocity);
                    else if (phase == 3) result.reverseRelativeWheelVelocity = Mathf.Max(
                        result.reverseRelativeWheelVelocity, verticalWheelVelocity);
                    else result.seamRelativeWheelVelocity = Mathf.Max(
                        result.seamRelativeWheelVelocity, verticalWheelVelocity);
                }
                float halfTravel = (points[i].MaximumCompression + points[i].MaximumExtension) * .5f;
                result.maximumTravelExcess = Mathf.Max(result.maximumTravelExcess,
                    Mathf.Max(0f, Mathf.Abs(points[i].JointTranslation) - halfTravel));
                if (phase != 0) continue;
                minimumCompression[i] = Mathf.Min(minimumCompression[i], points[i].Compression01);
                maximumCompression[i] = Mathf.Max(maximumCompression[i], points[i].Compression01);
            }
        }

        private struct VehiclePerformanceResult
        {
            public float maximumSpeedKmh;
            public float resolvedTopSpeedKmh;
            public float zeroToHundredSeconds;
            public float targetZeroToHundredSeconds;
            public float fuelDurationSeconds;
            public float reverseSpeedKmh;
            public float groundedRatio;
        }

        private static void ValidateVehiclePerformance(BuiltAssets assets)
        {
            VehiclePerformanceResult jeep = SimulateFlatPerformance(assets.jeep);
            VehiclePerformanceResult formula = SimulateFlatPerformance(assets.formula);
            VehiclePerformanceResult rally = SimulateFlatPerformance(assets.rally);
            VehiclePerformanceResult sportBike = SimulateFlatPerformance(assets.sportBike);
            Debug.Log($"TRACTION_LIMITED_FLAT_RAW | " +
                      $"J={jeep.maximumSpeedKmh:0.0}kmh/{jeep.zeroToHundredSeconds:0.00}s/{jeep.reverseSpeedKmh:0.0}rev/{jeep.groundedRatio:P0} | " +
                      $"F={formula.maximumSpeedKmh:0.0}/{formula.zeroToHundredSeconds:0.00}/{formula.reverseSpeedKmh:0.0}/{formula.groundedRatio:P0} | " +
                      $"R={rally.maximumSpeedKmh:0.0}/{rally.zeroToHundredSeconds:0.00}/{rally.reverseSpeedKmh:0.0}/{rally.groundedRatio:P0} | " +
                      $"S={sportBike.maximumSpeedKmh:0.0}/{sportBike.zeroToHundredSeconds:0.00}/{sportBike.reverseSpeedKmh:0.0}/{sportBike.groundedRatio:P0}.");
            if (jeep.maximumSpeedKmh < jeep.resolvedTopSpeedKmh * .94f ||
                jeep.maximumSpeedKmh > jeep.resolvedTopSpeedKmh * 1.03f ||
                !AccelerationMatchesDisplayedStat(jeep) ||
                jeep.fuelDurationSeconds < 29.9f || jeep.fuelDurationSeconds > 30.1f || jeep.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException("Jeep traction-limited flat performance, fuel or reverse contract failed.");
            if (formula.maximumSpeedKmh < formula.resolvedTopSpeedKmh * .85f ||
                formula.maximumSpeedKmh > formula.resolvedTopSpeedKmh * 1.03f ||
                !AccelerationMatchesDisplayedStat(formula) ||
                formula.fuelDurationSeconds < 44.9f || formula.fuelDurationSeconds > 45.1f || formula.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException("Formula traction-limited flat performance, fuel or reverse contract failed.");
            if (rally.maximumSpeedKmh < rally.resolvedTopSpeedKmh * .85f ||
                rally.maximumSpeedKmh > rally.resolvedTopSpeedKmh * 1.03f ||
                !AccelerationMatchesDisplayedStat(rally) ||
                rally.fuelDurationSeconds < 49.9f || rally.fuelDurationSeconds > 50.1f || rally.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException("Rally traction-limited flat performance, fuel or reverse contract failed.");
            if (sportBike.maximumSpeedKmh < sportBike.resolvedTopSpeedKmh * .70f ||
                sportBike.maximumSpeedKmh > sportBike.resolvedTopSpeedKmh * 1.03f ||
                !AccelerationMatchesDisplayedStat(sportBike) ||
                sportBike.fuelDurationSeconds < 44.9f || sportBike.fuelDurationSeconds > 45.1f ||
                sportBike.reverseSpeedKmh > -10f)
                throw new System.InvalidOperationException(
                    "Sport Bike traction-limited flat performance, fuel or reverse contract failed.");
            Debug.Log($"Vehicle flat validation (fixed 0.02 s) | JEEP max={jeep.maximumSpeedKmh:0.0} km/h, " +
                      $"target={jeep.resolvedTopSpeedKmh:0.0}, grounded={jeep.groundedRatio:P0}, " +
                      $"0-100={jeep.zeroToHundredSeconds:0.00}/{jeep.targetZeroToHundredSeconds:0.00} s, fuel={jeep.fuelDurationSeconds:0.00} s, " +
                      $"reverse={jeep.reverseSpeedKmh:0.0} km/h | " +
                      $"FORMULA max={formula.maximumSpeedKmh:0.0} km/h, 0-100={formula.zeroToHundredSeconds:0.00}/{formula.targetZeroToHundredSeconds:0.00} s, " +
                      $"target={formula.resolvedTopSpeedKmh:0.0}, grounded={formula.groundedRatio:P0}, " +
                       $"fuel={formula.fuelDurationSeconds:0.00} s, reverse={formula.reverseSpeedKmh:0.0} km/h | " +
                       $"RALLY max={rally.maximumSpeedKmh:0.0} km/h, 0-100={rally.zeroToHundredSeconds:0.00}/{rally.targetZeroToHundredSeconds:0.00} s, " +
                       $"fuel={rally.fuelDurationSeconds:0.00} s, reverse={rally.reverseSpeedKmh:0.0} km/h | " +
                       $"SPORT BIKE max={sportBike.maximumSpeedKmh:0.0} km/h, " +
                       $"0-100={sportBike.zeroToHundredSeconds:0.00}/{sportBike.targetZeroToHundredSeconds:0.00} s, " +
                       $"fuel={sportBike.fuelDurationSeconds:0.00} s, reverse={sportBike.reverseSpeedKmh:0.0} km/h.");
        }

        private static VehiclePerformanceResult SimulateFlatPerformance(VehicleDefinition source)
        {
            Scene simulationScene = NewScene();
            Physics.SyncTransforms();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateMaximumTuneClone(source);
            GameObject ground = new GameObject("Flat Performance Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -.5f, 0f);
            groundCollider.size = new Vector3(10000f, 1f, 20f);
            GameObject inputObject = new GameObject("Performance Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D, new Vector3(0f, 1f, 0f), Quaternion.identity);
            VehicleController controller = instance.GetComponent<VehicleController>();
            controller.AssignRuntimeReferences(testDefinition, input,
                instance.GetComponentsInChildren<SuspensionController>(true));
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");

            const float step = .02f;
            input.SetTouchGas(false);
            for (int i = 0; i < 100; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
            }

            input.SetTouchGas(true);
            float elapsed = 0f;
            float zeroToHundred = -1f;
            float maximumSpeed = 0f;
            int groundedSteps = 0;
            for (int i = 0; i < 1500; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                elapsed += step;
                float speed = controller.SpeedKmh;
                maximumSpeed = Mathf.Max(maximumSpeed, speed);
                if (controller.AnyGrounded) groundedSteps++;
                if (zeroToHundred < 0f && speed >= 100f) zeroToHundred = elapsed;
            }

            input.SetTouchGas(false);
            input.SetTouchBrake(true);
            float reverseSpeed = 0f;
            for (int i = 0; i < 1000; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                reverseSpeed = Mathf.Min(reverseSpeed, controller.SpeedKmh);
            }

            FuelSystem fuel = controller.Fuel;
            fuel.RefillFull();
            float fuelElapsed = 0f;
            while (fuel.NormalizedFuel > 0f && fuelElapsed < 120f)
            {
                fuel.TickRun(step, true);
                fuelElapsed += step;
            }
            ResolvedVehicleStats measuredStats = controller.Stats;

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return new VehiclePerformanceResult
            {
                maximumSpeedKmh = maximumSpeed,
                resolvedTopSpeedKmh = measuredStats.topSpeedMps * 3.6f,
                zeroToHundredSeconds = zeroToHundred,
                targetZeroToHundredSeconds = measuredStats.zeroToHundredTargetSeconds,
                fuelDurationSeconds = fuelElapsed,
                reverseSpeedKmh = reverseSpeed,
                groundedRatio = groundedSteps / 1500f
            };
        }

        private struct LandingResult
        {
            public int level;
            public float speedKmh;
            public float maximumReboundMps;
            public float maximumAngularVelocity;
            public float maximumGroundPenetration;
            public float maximumCompression01;
            public float impactSpeedMps;
            public float firstReboundPeakMps;
            public float secondReboundPeakMps;
            public float settledVerticalSpeed;
            public float settledAngularVelocity;
            public int reboundEpisodes;
            public bool touchedDown;
            public bool bodyTunneled;
        }

        private static void CollectSuspensionDropFailures(VehicleDefinition[] vehicles, List<string> failures)
        {
            for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
            {
                VehicleDefinition vehicle = vehicles[vehicleIndex];
                int maximumLevel = Mathf.Max(1, vehicle.suspensionMaxTunes);
                int middleLevel = Mathf.Max(1, (maximumLevel + 1) / 2);
                int[] levels = { 1, middleLevel, maximumLevel };
                float representativeSpeed = vehicle.archetype == VehicleArchetype.Jeep ? 40f : 100f;
                for (int i = 0; i < levels.Length; i++)
                {
                    if (i > 0 && levels[i] == levels[i - 1]) continue;
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, levels[i], representativeSpeed, -5f), false, failures);
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, levels[i], representativeSpeed, -12f), true, failures);
                }
                float[] maximumLevelSpeeds = vehicle.archetype == VehicleArchetype.Jeep
                    ? new[] { 100f }
                    : vehicle.archetype == VehicleArchetype.HillAdventurer ? new[] { 150f, 190f }
                    : vehicle.archetype == VehicleArchetype.Rally ? new[] { 200f, 280f }
                    : vehicle.archetype == VehicleArchetype.SportBike ? new[] { 200f, 360f }
                    : new[] { 200f, 300f };
                for (int i = 0; i < maximumLevelSpeeds.Length; i++)
                {
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, maximumLevel, maximumLevelSpeeds[i], -5f), false, failures);
                    CollectLandingFailures(vehicle,
                        SimulateFlatLanding(vehicle, maximumLevel, maximumLevelSpeeds[i], -12f), true, failures);
                }
                CollectLandingFailures(vehicle,
                    SimulateFlatLanding(vehicle, maximumLevel, representativeSpeed, -7f, -2f), false, failures);
                CollectLandingFailures(vehicle,
                    SimulateFlatLanding(vehicle, maximumLevel, representativeSpeed, -7f, 2f), false, failures);
            }
        }

        private static void CollectLandingFailures(VehicleDefinition vehicle, LandingResult result, bool hard,
            List<string> failures)
        {
            var conditions = new List<string>();
            float penetrationLimit = hard ? .15f : .05f;
            float reboundLimit = hard ? 7.5f : 5f;
            if (!result.touchedDown) conditions.Add("wheel-first contact was never detected");
            if (result.bodyTunneled) conditions.Add("body tunneled through terrain");
            if (result.maximumGroundPenetration > penetrationLimit)
                conditions.Add($"body penetration {result.maximumGroundPenetration:0.000}m > {penetrationLimit:0.000}m");
            if (result.maximumReboundMps > reboundLimit)
                conditions.Add($"rebound {result.maximumReboundMps:0.00}m/s > {reboundLimit:0.00}m/s");
            if (result.maximumAngularVelocity > 1.2f)
                conditions.Add($"pitch velocity {result.maximumAngularVelocity:0.00}rad/s > 1.20rad/s");
            if (hard && result.maximumCompression01 < .72f)
                conditions.Add($"hard-impact compression {result.maximumCompression01:P0} < 72%");
            if (result.reboundEpisodes > 1)
                conditions.Add($"{result.reboundEpisodes} significant rebound episodes > 1 " +
                               $"(impact={result.impactSpeedMps:0.00}m/s, peaks=" +
                               $"{result.firstReboundPeakMps:0.00}/{result.secondReboundPeakMps:0.00}m/s)");
            if (result.settledVerticalSpeed > .75f)
                conditions.Add($"settled vertical speed {result.settledVerticalSpeed:0.00}m/s > 0.75m/s");
            if (result.settledAngularVelocity > .35f)
                conditions.Add($"settled pitch speed {result.settledAngularVelocity:0.00}rad/s > 0.35rad/s");
            if (conditions.Count > 0)
                failures.Add($"{vehicle.displayName} LEVEL {result.level} {(hard ? "HARD DROP" : "NORMAL DROP")} " +
                             $"@ {result.speedKmh:0}km/h: " + string.Join("; ", conditions) +
                             $" | metrics impact={result.impactSpeedMps:0.00}m/s, " +
                             $"maxRebound={result.maximumReboundMps:0.00}m/s, " +
                             $"pitch={result.maximumAngularVelocity:0.00}rad/s, " +
                             $"compression={result.maximumCompression01:P0}, " +
                             $"penetration={result.maximumGroundPenetration:0.000}m, " +
                             $"episodes={result.reboundEpisodes}, " +
                             $"settledV={result.settledVerticalSpeed:0.00}m/s, " +
                             $"settledW={result.settledAngularVelocity:0.00}rad/s");
        }

        private static LandingResult SimulateFlatLanding(VehicleDefinition source, int level, float speedKmh,
            float verticalSpeedMps, float pitchDegrees = 0f)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateSuspensionLevelClone(source, level);
            GameObject ground = new GameObject("High Speed Landing Ground");
            BoxCollider groundCollider = ground.AddComponent<BoxCollider>();
            groundCollider.center = new Vector3(0f, -.5f, 0f);
            groundCollider.size = new Vector3(10000f, 1f, 20f);
            groundCollider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(
                Root + "/Materials/TerrainStable.physicMaterial");
            GameObject inputObject = new GameObject("Landing Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameObject instance = Object.Instantiate(source.gameplayPrefab2D, new Vector3(0f, 2.1f, 0f),
                Quaternion.Euler(0f, 0f, pitchDegrees));
            VehicleController controller = instance.GetComponent<VehicleController>();
            SuspensionController[] points = instance.GetComponentsInChildren<SuspensionController>(true);
            controller.AssignRuntimeReferences(testDefinition, input, points);
            InvokePrivate(controller, "Awake");
            MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
            if (plane != null) InvokePrivate(plane, "Awake");
            controller.Body.linearVelocity = new Vector3(speedKmh / 3.6f, verticalSpeedMps, 0f);
            bool touched = false;
            float rebound = 0f;
            float angular = 0f;
            float maximumPenetration = 0f;
            float maximumCompression = 0f;
            float impactSpeed = 0f;
            float firstReboundPeak = 0f;
            float secondReboundPeak = 0f;
            float settledVertical = 0f;
            float settledAngular = 0f;
            int reboundEpisodes = 0;
            bool upwardEpisode = false;
            float significantReboundThreshold = Mathf.Max(.75f, Mathf.Abs(verticalSpeedMps) * .20f);
            bool bodyTunneled = false;
            CapsuleCollider chassis = instance.GetComponent<CapsuleCollider>();
            const float step = .02f;
            for (int i = 0; i < 150; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
                bool wasTouched = touched;
                touched |= controller.AnyGrounded;
                if (touched)
                {
                    float vertical = controller.Body.linearVelocity.y;
                    if (!wasTouched) impactSpeed = Mathf.Abs(vertical);
                    rebound = Mathf.Max(rebound, vertical);
                    bool upwardNow = vertical > significantReboundThreshold;
                    if (upwardNow && !upwardEpisode) reboundEpisodes++;
                    if (reboundEpisodes == 1) firstReboundPeak = Mathf.Max(firstReboundPeak, vertical);
                    else if (reboundEpisodes >= 2) secondReboundPeak = Mathf.Max(secondReboundPeak, vertical);
                    upwardEpisode = upwardNow || upwardEpisode && vertical > significantReboundThreshold * .4f;
                }
                angular = Mathf.Max(angular, Mathf.Abs(controller.Body.angularVelocity.z));
                for (int p = 0; p < points.Length; p++)
                    maximumCompression = Mathf.Max(maximumCompression, points[p].Compression01);
                maximumPenetration = Mathf.Max(maximumPenetration, Mathf.Max(0f, -chassis.bounds.min.y));
                bodyTunneled |= chassis.bounds.max.y < 0f;
                if (i >= 125)
                {
                    settledVertical = Mathf.Max(settledVertical, Mathf.Abs(controller.Body.linearVelocity.y));
                    settledAngular = Mathf.Max(settledAngular, Mathf.Abs(controller.Body.angularVelocity.z));
                }
            }
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return new LandingResult
            {
                level = level, speedKmh = speedKmh, maximumReboundMps = rebound,
                maximumAngularVelocity = angular, maximumGroundPenetration = maximumPenetration,
                maximumCompression01 = maximumCompression, reboundEpisodes = reboundEpisodes,
                impactSpeedMps = impactSpeed, firstReboundPeakMps = firstReboundPeak,
                secondReboundPeakMps = secondReboundPeak,
                settledVerticalSpeed = settledVertical, settledAngularVelocity = settledAngular,
                touchedDown = touched, bodyTunneled = bodyTunneled
            };
        }

        private static VehicleDefinition CreateMaximumTuneClone(VehicleDefinition source)
        {
            VehicleDefinition clone = Object.Instantiate(source);
            VehicleDefinition.TuneTargets values = source.tuneTargets;
            clone.engineMaxTunes = clone.accelerationMaxTunes = clone.suspensionMaxTunes = clone.gripMaxTunes = 0;
            clone.airControlMaxTunes = clone.downforceMaxTunes = clone.fuelMaxTunes = clone.turboMaxTunes = 0;
            clone.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = LastOnly(values.engineTopSpeedKmh),
                accelerationZeroToHundredSeconds = LastOnly(values.accelerationZeroToHundredSeconds),
                gripRatings = LastOnly(values.gripRatings),
                airControlTorque = LastOnly(values.airControlTorque),
                airborneDownforceAcceleration = LastOnly(values.airborneDownforceAcceleration),
                fuelRunDurationSeconds = LastOnly(values.fuelRunDurationSeconds)
            };
            clone.suspension.minTravel = clone.suspension.maxTravel = source.suspension.maxTravel;
            clone.suspension.stiffnessTuneScale = Vector2.one * source.suspension.stiffnessTuneScale.y;
            clone.suspension.dampingRatio = Vector2.one * source.suspension.dampingRatio.y;
            clone.suspension.compressionDampingMultiplier = source.suspension.compressionDampingMultiplier *
                source.suspension.compressionDampingTuneScale.y;
            clone.suspension.reboundDampingMultiplier = source.suspension.reboundDampingMultiplier *
                source.suspension.reboundDampingTuneScale.y;
            clone.suspension.compressionDampingTuneScale = Vector2.one;
            clone.suspension.reboundDampingTuneScale = Vector2.one;
            clone.suspension.reboundAccelerationTune = Vector2.one * source.suspension.reboundAccelerationTune.y;
            clone.suspension.wheelContactTolerance = Vector2.one * source.suspension.wheelContactTolerance.y;
            clone.suspension.landingDampingScale = Vector2.one * source.suspension.landingDampingScale.y;
            clone.engine.minTorqueNm = clone.engine.maxTorqueNm = source.engine.maxTorqueNm;
            return clone;
        }

        private static void LogResolvedPhysicsProfiles(BuiltAssets assets)
        {
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            for (int i = 0; i < definitions.Length; i++)
            {
                VehicleDefinition definition = definitions[i];
                var upgrades = new VehicleUpgradeState
                {
                    engineTune = Mathf.Max(1, definition.engineMaxTunes),
                    accelerationTune = Mathf.Max(1, definition.accelerationMaxTunes),
                    suspensionTune = Mathf.Max(1, definition.suspensionMaxTunes),
                    gripTune = Mathf.Max(1, definition.gripMaxTunes),
                    airControlTune = Mathf.Max(1, definition.airControlMaxTunes),
                    downforceTune = Mathf.Max(1, definition.downforceMaxTunes),
                    turboTune = Mathf.Max(1, definition.turboMaxTunes),
                    fuelTune = Mathf.Max(1, definition.fuelMaxTunes)
                };
                ResolvedVehicleStats stats = definition.Resolve(upgrades);
                SuspensionPhysics.GetTravelLimits(definition.suspension, stats.suspensionTravel,
                    out float compression, out float extension);
                float staticCompression = definition.mass * .5f * Physics.gravity.magnitude /
                                          Mathf.Max(1f, stats.spring);
                float sag = SuspensionPhysics.CalculateSag01(staticCompression, compression, extension);
                Debug.Log($"PHYSICS PROFILE {definition.displayName} | chassis={definition.mass:0}kg, " +
                          $"wheel={definition.wheelMass:0}kg x2, radius={definition.wheelRadius:0.000}m, " +
                          $"COM={definition.centerOfMass}, rest={definition.suspension.restLength:0.000}m, " +
                          $"compression={compression:0.000}m, extension={extension:0.000}m, sag={sag:P1}, " +
                          $"spring={stats.spring:0}N/m, damping={stats.damping:0}Ns/m, " +
                          $"progressive={stats.progressiveSpringMultiplier:0.00}x, bumpStart={definition.suspension.bumpStopStart:P0}, " +
                          $"motor={stats.motorTorqueNm:0}Nm, grip={stats.gripRating}/{VehicleDefinition.GlobalGripMaximum} ({stats.grip:0.00}), " +
                          $"air={stats.airControlTorque:0}Nm, maxAir={definition.maxAirAngularVelocity:0.00}rad/s, " +
                          $"airDamping={definition.airAngularDamping:0.00}, downforce={stats.airborneDownforceAcceleration:0.00}m/s2");
            }
        }

        private static VehicleDefinition CreateSuspensionLevelClone(VehicleDefinition source, int level)
        {
            var upgrades = new VehicleUpgradeState { suspensionTune = level };
            ResolvedVehicleStats stats = source.Resolve(upgrades);
            VehicleDefinition clone = Object.Instantiate(source);
            clone.suspensionMaxTunes = 0;
            clone.suspension.minTravel = clone.suspension.maxTravel = stats.suspensionTravel;
            SuspensionPhysics.GetTravelLimits(clone.suspension, stats.suspensionTravel,
                out _, out float extension);
            float compression = Mathf.Max(.015f,
                clone.suspension.targetSag01 * stats.suspensionTravel - extension);
            float baseSpring = clone.mass * .5f * Physics.gravity.magnitude / compression;
            clone.suspension.stiffnessTuneScale = Vector2.one * (stats.spring / Mathf.Max(1f, baseSpring));
            float ratio = stats.damping / Mathf.Max(.001f,
                2f * Mathf.Sqrt(stats.spring * clone.mass * .5f));
            clone.suspension.dampingRatio = Vector2.one * ratio;
            clone.suspension.progressiveSpringMultiplier = stats.progressiveSpringMultiplier / .90f;
            clone.suspension.compressionDampingMultiplier = stats.compressionDampingMultiplier;
            clone.suspension.reboundDampingMultiplier = stats.reboundDampingMultiplier;
            clone.suspension.compressionDampingTuneScale = Vector2.one;
            clone.suspension.reboundDampingTuneScale = Vector2.one;
            clone.suspension.reboundAccelerationTune = Vector2.one * stats.maximumReboundAcceleration;
            clone.suspension.wheelContactTolerance = Vector2.one * stats.wheelContactTolerance;
            clone.suspension.landingDampingScale = Vector2.one * stats.landingDampingMultiplier;
            return clone;
        }

        private static void ValidateAirControlContract(BuiltAssets assets)
        {
            ValidateTuningContract(assets);
            if (assets.formula.airControlMaxTunes != 0 || assets.formula.GetTuneSlotCount() != 6 ||
                assets.formula.GetTuneTypeForSlot(5) != VehicleUpgradeType.Downforce)
                throw new System.InvalidOperationException(
                    "Formula must expose six tunes with Downforce in slot 6 and no Air Control tune.");
            for (int i = 0; i < assets.formula.GetTuneSlotCount(); i++)
                if (assets.formula.GetTuneTypeForSlot(i) == VehicleUpgradeType.AirControl)
                    throw new System.InvalidOperationException("Formula tuning UI still exposes Air Control.");

            float jeepGas2D = SimulateAirTorque(assets.jeep, false, true);
            float jeepBrake2D = SimulateAirTorque(assets.jeep, false, false);
            float formulaGas2D = SimulateAirTorque(assets.formula, false, true);
            float formulaBrake2D = SimulateAirTorque(assets.formula, false, false);
            float formulaGas3D = SimulateAirTorque(assets.formula, true, true);
            float formulaBrake3D = SimulateAirTorque(assets.formula, true, false);
            float emptyJeepGas2D = SimulateAirTorque(assets.jeep, false, true, true);
            float emptyJeepBrake2D = SimulateAirTorque(assets.jeep, false, false, true);
            float emptyFormulaGas2D = SimulateAirTorque(assets.formula, false, true, true);
            float emptyFormulaBrake2D = SimulateAirTorque(assets.formula, false, false, true);
            float emptyFormulaGas3D = SimulateAirTorque(assets.formula, true, true, true);
            float emptyFormulaBrake3D = SimulateAirTorque(assets.formula, true, false, true);
            if (jeepGas2D <= 0f || jeepBrake2D >= 0f || formulaGas2D <= 0f || formulaBrake2D >= 0f ||
                formulaGas3D >= 0f || formulaBrake3D <= 0f)
                throw new System.InvalidOperationException("Air-control torque sign does not match GAS nose-up / BRAKE nose-down.");
            if (emptyJeepGas2D <= 0f || emptyJeepBrake2D >= 0f || emptyFormulaGas2D <= 0f ||
                emptyFormulaBrake2D >= 0f || emptyFormulaGas3D >= 0f || emptyFormulaBrake3D <= 0f)
                throw new System.InvalidOperationException("Fuel-zero air control must retain GAS nose-up / BRAKE nose-down.");
            if (Mathf.Abs(formulaGas2D) <= Mathf.Abs(jeepGas2D))
                throw new System.InvalidOperationException("Formula must have more airborne authority than Jeep.");

            AirLimitResult jeepLimit = SimulateAirLimit(assets.jeep, false);
            AirLimitResult formulaLimit2D = SimulateAirLimit(assets.formula, false);
            AirLimitResult formulaLimit3D = SimulateAirLimit(assets.formula, true);
            ValidateAirLimitResult(assets.jeep, jeepLimit, "JEEP 2D");
            ValidateAirLimitResult(assets.formula, formulaLimit2D, "FORMULA 2D");
            ValidateAirLimitResult(assets.formula, formulaLimit3D, "FORMULA 3D");

            Debug.Log($"Air control validation | JEEP 2D gasZ={jeepGas2D:0.000}, brakeZ={jeepBrake2D:0.000} | " +
                      $"FORMULA 2D gasZ={formulaGas2D:0.000}, brakeZ={formulaBrake2D:0.000} | " +
                      $"FORMULA 3D gasPitch={formulaGas3D:0.000}, brakePitch={formulaBrake3D:0.000} | " +
                      $"limits J={jeepLimit.held:0.00}, F2D={formulaLimit2D.held:0.00}, F3D={formulaLimit3D.held:0.00} rad/s | " +
                      "mapping=OK, damping=OK, fuel-zero rotation=OK, Formula Downforce slot=OK.");
        }

        private struct AirLimitResult
        {
            public float held;
            public float released;
        }

        private static void ValidateAirLimitResult(VehicleDefinition definition, AirLimitResult result, string label)
        {
            float maximum = definition.maxAirAngularVelocity;
            if (result.held > maximum + .06f || result.held < maximum * .72f)
                throw new System.InvalidOperationException($"{label} did not converge to its air angular limit: {result.held:0.00}/{maximum:0.00}.");
            if (result.released >= result.held * .65f || result.released <= .001f)
                throw new System.InvalidOperationException($"{label} airborne release damping is ineffective or freezes rotation.");
        }

        private static AirLimitResult SimulateAirLimit(VehicleDefinition source, bool roadMode)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateMaximumTuneClone(source);
            GameObject inputObject = new GameObject("Air Limit Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(roadMode ? GameModeType.Adventure3D : GameModeType.Adventure2_5D);
            GameObject prefab = roadMode ? source.gameplayPrefab3D : source.gameplayPrefab2D;
            GameObject instance = Object.Instantiate(prefab, new Vector3(0f, 10000f, 0f), Quaternion.identity);
            VehicleRuntimeBase controller;
            MovementPlaneConstraint plane = null;
            if (roadMode)
            {
                RoadVehicleController road = instance.GetComponent<RoadVehicleController>();
                road.Assign(testDefinition, input);
                InvokePrivate(road, "Awake");
                controller = road;
            }
            else
            {
                VehicleController climb = instance.GetComponent<VehicleController>();
                climb.AssignRuntimeReferences(testDefinition, input,
                    instance.GetComponentsInChildren<SuspensionController>(true));
                InvokePrivate(climb, "Awake");
                plane = instance.GetComponent<MovementPlaneConstraint>();
                if (plane != null) InvokePrivate(plane, "Awake");
                controller = climb;
            }
            input.SetTouchGas(true);
            const float step = .02f;
            for (int i = 0; i < 250; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
            }
            Vector3 axis = roadMode ? -instance.transform.right : Vector3.forward;
            float held = Mathf.Abs(Vector3.Dot(controller.Body.angularVelocity, axis));
            input.SetTouchGas(false);
            for (int i = 0; i < 50; i++)
            {
                InvokePrivate(input, "Update");
                InvokePrivate(controller, "FixedUpdate");
                if (plane != null) InvokePrivate(plane, "FixedUpdate");
                Physics.Simulate(step);
            }
            axis = roadMode ? -instance.transform.right : Vector3.forward;
            float released = Mathf.Abs(Vector3.Dot(controller.Body.angularVelocity, axis));
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return new AirLimitResult { held = held, released = released };
        }

        private static void ValidateTuningContract(BuiltAssets assets)
        {
            int[] jeepMax = { 14, 15, 12, 12, 15 };
            int[] formulaMax = { 20, 20, 20, 20, 25, 20 };
            var fresh = new VehicleUpgradeState();
            if (assets.jeep.GetTuneSlotCount() != jeepMax.Length ||
                assets.formula.GetTuneSlotCount() != formulaMax.Length)
                throw new System.InvalidOperationException("Jeep or Formula tune-slot count is invalid.");
            for (int i = 0; i < jeepMax.Length; i++)
            {
                VehicleUpgradeType jeepType = assets.jeep.GetTuneTypeForSlot(i);
                if (assets.jeep.GetMaxTunes(jeepType) != jeepMax[i] ||
                    fresh.GetTune(jeepType) != 1)
                    throw new System.InvalidOperationException("Vehicle tuning slots, maxima or fresh Level 1 defaults are invalid.");
            }
            for (int i = 0; i < formulaMax.Length; i++)
            {
                VehicleUpgradeType formulaType = assets.formula.GetTuneTypeForSlot(i);
                if (assets.formula.GetMaxTunes(formulaType) != formulaMax[i] ||
                    fresh.GetTune(formulaType) != 1)
                    throw new System.InvalidOperationException(
                        "Formula tuning slots, maxima or fresh Level 1 defaults are invalid.");
            }
            VehicleUpgradeType[] rallyTypes = { VehicleUpgradeType.Engine, VehicleUpgradeType.Turbo,
                VehicleUpgradeType.Suspension, VehicleUpgradeType.Grip, VehicleUpgradeType.Fuel };
            int[] rallyMax = { 20, 20, 14, 14, 15 };
            if (assets.rally.GetTuneSlotCount() != 5 || assets.rally.accelerationMaxTunes != 0)
                throw new System.InvalidOperationException("Rally must expose the five common performance stats.");
            for (int i = 0; i < rallyTypes.Length; i++)
                if (assets.rally.GetTuneTypeForSlot(i) != rallyTypes[i] ||
                    assets.rally.GetMaxTunes(rallyTypes[i]) != rallyMax[i] || fresh.GetTune(rallyTypes[i]) != 1)
                    throw new System.InvalidOperationException("Rally tuning order, maxima or fresh Level 1 defaults are invalid.");
            int[] sportMax = { 20, 20, 15, 20, 15 };
            for (int i = 0; i < 5; i++)
            {
                VehicleUpgradeType type = assets.sportBike.GetTuneTypeForSlot(i);
                if (assets.sportBike.GetMaxTunes(type) != sportMax[i] || fresh.GetTune(type) != 1)
                    throw new System.InvalidOperationException("Sport Bike tune maxima or Level 1 defaults are invalid.");
            }
            if (VehicleDefinition.GlobalGripMaximum != 1000 ||
                ResolveMaximumGrip(assets.jeep) != 300 || ResolveMaximumGrip(assets.formula) != 520 ||
                ResolveMaximumGrip(assets.rally) != 750 || ResolveMaximumGrip(assets.sportBike) != 500)
                throw new System.InvalidOperationException("Global 0-1000 Grip contract is invalid.");
            ResolvedVehicleStats sportStock = assets.sportBike.Resolve(new VehicleUpgradeState());
            ResolvedVehicleStats sportMaximum = ResolveAllMaximum(assets.sportBike);
            if (sportStock.topSpeedMps * 3.6f < 100f || sportStock.zeroToHundredTargetSeconds > 10f ||
                sportStock.gripRating < 100 ||
                Mathf.Abs(sportMaximum.topSpeedMps * 3.6f - 360f) > .01f ||
                Mathf.Abs(sportMaximum.zeroToHundredTargetSeconds - 2.5f) > .001f ||
                Mathf.Abs(sportMaximum.totalRunFuelDuration - 45f) > .001f ||
                sportMaximum.gripRating != 500)
                throw new System.InvalidOperationException(
                    "Sport Bike stock must be playable while preserving 360km/h, 2.5s, 45s and Grip500 maxima.");
            ValidateUpgradeCurvesAndCosts(assets);
            ValidateGripAuthority(assets);
            ValidateFormulaDownforce(assets.formula);
            Debug.Log("Tuning validation: all fresh tunes=Level 1; Formula has six tunes; " +
                      "Grip max Jeep/Formula/Rally/Sport=300/520/750/500 over 1000.");
        }

        private static bool AccelerationMatchesDisplayedStat(VehiclePerformanceResult result)
        {
            float tolerance = Mathf.Max(.12f, result.targetZeroToHundredSeconds * .08f);
            return result.zeroToHundredSeconds > 0f &&
                   Mathf.Abs(result.zeroToHundredSeconds - result.targetZeroToHundredSeconds) <= tolerance;
        }

        private static void ValidateUpgradeCurvesAndCosts(BuiltAssets assets)
        {
            VehicleDefinition[] definitions = { assets.jeep, assets.formula, assets.rally, assets.sportBike };
            for (int d = 0; d < definitions.Length; d++)
            {
                VehicleDefinition definition = definitions[d];
                for (int slot = 0; slot < definition.GetTuneSlotCount(); slot++)
                {
                    VehicleUpgradeType type = definition.GetTuneTypeForSlot(slot);
                    int maximum = definition.GetMaxTunes(type);
                    float expectedGain = 1f / Mathf.Max(1, maximum - 1);
                    for (int level = 1; level < maximum; level++)
                        if (Mathf.Abs(VehicleDefinition.UpgradeGain(level, maximum) - expectedGain) > .000001f)
                            throw new System.InvalidOperationException(
                                $"{definition.displayName} {type} Level {level} applies a non-linear or repeated percentage.");
                    if (VehicleDefinition.UpgradeProgress(1, maximum) != 0f ||
                        VehicleDefinition.UpgradeProgress(maximum, maximum) != 1f)
                        throw new System.InvalidOperationException(
                            $"{definition.displayName} {type} does not preserve its exact Level 1/max endpoints.");

                    float previousValue = GetImprovementScalar(definition, type, 1);
                    for (int currentLevel = 1; currentLevel < maximum; currentLevel++)
                    {
                        float nextValue = GetImprovementScalar(definition, type, currentLevel + 1);
                        if (nextValue <= previousValue + .000001f)
                            throw new System.InvalidOperationException(
                                $"{definition.displayName} {type} Level {currentLevel}->{currentLevel + 1} " +
                                "does not improve the resolved gameplay stat.");
                        previousValue = nextValue;
                    }

                    float[] authoredTargets = GetAuthoredTuneTargets(definition, type);
                    if (authoredTargets != null && authoredTargets.Length != maximum)
                        throw new System.InvalidOperationException(
                            $"{definition.displayName} {type} has {authoredTargets.Length} authored values for " +
                            $"{maximum} one-based levels (legacy Level-0 entry detected).");

                    int previousCost = -1;
                    for (int currentLevel = 1; currentLevel < maximum; currentLevel++)
                    {
                        int cost = assets.economy.GetCostForNextTune(definition, type, currentLevel);
                        if (cost <= previousCost)
                            throw new System.InvalidOperationException(
                                $"{definition.displayName} {type} cost is not strictly monotonic at Level {currentLevel}.");
                        previousCost = cost;
                    }
                    if (previousCost != assets.economy.FinalCost(definition.archetype))
                        throw new System.InvalidOperationException(
                            $"{definition.displayName} final {type} cost is {previousCost}, expected " +
                            $"{assets.economy.FinalCost(definition.archetype)}.");
                    LogTuneProgression(definition, type, maximum);
                }
            }
            Debug.Log("UPGRADE_CURVES: each one-based level applies its normalized percentage exactly once; " +
                      "Level 1/max preserve the authored endpoints; every purchase changes the resolved stat; " +
                      "prices strictly increase to J/F/R/S=50000/150000/100000/200000.");
        }

        private static float GetImprovementScalar(VehicleDefinition definition, VehicleUpgradeType type, int level)
        {
            ResolvedVehicleStats stats = ResolveAtTune(definition, type, level);
            switch (type)
            {
                case VehicleUpgradeType.Engine: return stats.topSpeedMps;
                case VehicleUpgradeType.Acceleration:
                case VehicleUpgradeType.Turbo: return -stats.zeroToHundredTargetSeconds;
                case VehicleUpgradeType.Suspension: return stats.suspensionTravel;
                case VehicleUpgradeType.Grip: return stats.gripRating;
                case VehicleUpgradeType.Downforce: return stats.airborneDownforceAcceleration;
                default: return stats.totalRunFuelDuration;
            }
        }

        [MenuItem("Anime Hill Racing/Chests/Build Chest + Blueprint Content")]
        public static void BuildChestBlueprintContent()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before rebuilding Chest content.");
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            BuildGarageModern(assets);
            BuildMainMenu(assets);
            BuildGarageHillAdventurer(assets);
            BuildMainMenuHillAdventurer(assets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ChestDebugTools.ValidateSystem();
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Debug.Log("CHEST_BLUEPRINT_BUILD_OK: GameCatalog, ChestCatalog, MainMenu stand and Garage Shop rebuilt.");
        }

        [MenuItem("Anime Hill Racing/Chests/Rebuild Embedded Main Menu Stand")]
        public static void RebuildEmbeddedChestStand()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before rebuilding the embedded Chest stand.");
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuiltAssets assets = CreateAssets();
            BuildMainMenu(assets);
            BuildMainMenuHillAdventurer(assets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ChestDebugTools.ValidateSystem();
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            Debug.Log("EMBEDDED_CHEST_STAND_BUILD_OK: permanent left-side stand rebuilt; no Chest entry button.");
        }

        private static float[] GetAuthoredTuneTargets(VehicleDefinition definition, VehicleUpgradeType type)
        {
            VehicleDefinition.TuneTargets targets = definition.tuneTargets;
            if (targets == null) return null;
            switch (type)
            {
                case VehicleUpgradeType.Engine: return targets.engineTopSpeedKmh;
                case VehicleUpgradeType.Acceleration:
                case VehicleUpgradeType.Turbo: return targets.accelerationZeroToHundredSeconds;
                case VehicleUpgradeType.Grip: return targets.gripRatings;
                case VehicleUpgradeType.Downforce: return targets.airborneDownforceAcceleration;
                case VehicleUpgradeType.Fuel: return targets.fuelRunDurationSeconds;
                default: return null;
            }
        }

        private static void LogTuneProgression(VehicleDefinition definition, VehicleUpgradeType type, int maximum)
        {
            int[] requested = { 1, 2, 3, Mathf.Max(2, (maximum + 1) / 2),
                Mathf.Max(1, maximum - 3), Mathf.Max(1, maximum - 2),
                Mathf.Max(1, maximum - 1), maximum };
            var levels = new List<int>(requested.Length);
            for (int i = 0; i < requested.Length; i++)
            {
                int level = Mathf.Clamp(requested[i], 1, maximum);
                if (!levels.Contains(level)) levels.Add(level);
            }
            string[] entries = new string[levels.Count];
            for (int i = 0; i < levels.Count; i++)
            {
                int level = levels[i];
                ResolvedVehicleStats stats = ResolveAtTune(definition, type, level);
                string value;
                switch (type)
                {
                    case VehicleUpgradeType.Engine: value = $"{stats.topSpeedMps * 3.6f:0.0}km/h"; break;
                    case VehicleUpgradeType.Acceleration:
                    case VehicleUpgradeType.Turbo: value = $"{stats.zeroToHundredTargetSeconds:0.00}s"; break;
                    case VehicleUpgradeType.Suspension:
                        value = $"{stats.suspensionTravel:0.000}m/{stats.damping:0}Ns/m"; break;
                    case VehicleUpgradeType.Grip:
                        value = $"{stats.gripRating}/{VehicleDefinition.GlobalGripMaximum}"; break;
                    case VehicleUpgradeType.Downforce:
                        value = $"{stats.airborneDownforceAcceleration:0.00}m/s2"; break;
                    default: value = $"{stats.totalRunFuelDuration:0.00}s"; break;
                }
                entries[i] = $"L{level}={value}";
            }
            Debug.Log($"TUNE_TABLE {definition.displayName} {definition.GetTuneDisplayName(type)} | " +
                      string.Join(" | ", entries));
        }

        private static void ValidateGripAuthority(BuiltAssets assets)
        {
            float groundFriction = assets.groundPhysics.dynamicFriction;
            if (assets.groundPhysics.frictionCombine != PhysicsMaterialCombine.Multiply ||
                assets.tirePhysics.frictionCombine != PhysicsMaterialCombine.Multiply)
                throw new System.InvalidOperationException("Tire and terrain friction must use Multiply.");
            float low200 = ResolveAtTune(assets.formula, VehicleUpgradeType.Grip, 1).grip * groundFriction;
            float jeep300 = ResolveAtTune(assets.jeep, VehicleUpgradeType.Grip,
                assets.jeep.gripMaxTunes).grip * groundFriction;
            float sport500 = ResolveAtTune(assets.sportBike, VehicleUpgradeType.Grip,
                assets.sportBike.gripMaxTunes).grip * groundFriction;
            float formula520 = ResolveAtTune(assets.formula, VehicleUpgradeType.Grip,
                assets.formula.gripMaxTunes).grip * groundFriction;
            float rally750 = ResolveAtTune(assets.rally, VehicleUpgradeType.Grip,
                assets.rally.gripMaxTunes).grip * groundFriction;
            float testSlopeDemand = Mathf.Tan(22f * Mathf.Deg2Rad);
            if (low200 >= testSlopeDemand || !(low200 < jeep300 && jeep300 < sport500 &&
                sport500 < formula520 && formula520 < rally750) || rally750 >= 2f)
                throw new System.InvalidOperationException(
                    "Grip physics does not preserve low-level slip, ordered improvement and a finite maximum.");
            float lowSlide = SimulatePassiveSlopeSlide(ResolveAtTune(assets.formula,
                VehicleUpgradeType.Grip, 1).grip, groundFriction, 30f);
            float mediumSlide = SimulatePassiveSlopeSlide(ResolveAtTune(assets.formula,
                VehicleUpgradeType.Grip, assets.formula.gripMaxTunes).grip, groundFriction, 30f);
            if (lowSlide < .25f || mediumSlide > .15f)
                throw new System.InvalidOperationException(
                    $"Real 30-degree PhysX slope test failed: Grip200 slide={lowSlide:0.00}m, " +
                    $"Grip520 slide={mediumSlide:0.00}m.");
            Debug.Log($"GRIP_SLOPE_TEST 22deg: Grip200 mu={low200:0.000} < demand={testSlopeDemand:0.000} " +
                      $"(analytical slip); real 30deg slide Grip200={lowSlide:0.00}m, " +
                      $"Grip520={mediumSlide:0.00}m; " +
                      $"maxima J300={jeep300:0.000}, S500={sport500:0.000}, " +
                      $"F520={formula520:0.000}, R750={rally750:0.000}; no hidden friction floor.");
        }

        private static float SimulatePassiveSlopeSlide(float tireGrip, float groundGrip,
            float slopeDegrees)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            PhysicsMaterial groundMaterial = new PhysicsMaterial("Slope Audit Ground")
            {
                dynamicFriction = groundGrip,
                staticFriction = groundGrip,
                frictionCombine = PhysicsMaterialCombine.Multiply,
                bounciness = 0f
            };
            PhysicsMaterial tireMaterial = new PhysicsMaterial("Slope Audit Tire")
            {
                dynamicFriction = tireGrip,
                staticFriction = tireGrip * 1.04f,
                frictionCombine = PhysicsMaterialCombine.Multiply,
                bounciness = 0f
            };
            GameObject slope = new GameObject($"{slopeDegrees:0} Degree Grip Audit Slope");
            slope.transform.rotation = Quaternion.Euler(0f, 0f, slopeDegrees);
            BoxCollider slopeCollider = slope.AddComponent<BoxCollider>();
            slopeCollider.size = new Vector3(30f, 1f, 8f);
            slopeCollider.sharedMaterial = groundMaterial;
            GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyObject.name = "Grip Audit Body";
            bodyObject.transform.rotation = slope.transform.rotation;
            bodyObject.transform.position = slope.transform.TransformPoint(Vector3.up * 1.01f);
            BoxCollider bodyCollider = bodyObject.GetComponent<BoxCollider>();
            bodyCollider.sharedMaterial = tireMaterial;
            Rigidbody auditBody = bodyObject.AddComponent<Rigidbody>();
            auditBody.mass = 100f;
            auditBody.constraints = RigidbodyConstraints.FreezeRotation |
                                    RigidbodyConstraints.FreezePositionZ;
            auditBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            auditBody.sleepThreshold = 0f;
            Physics.SyncTransforms();
            Vector3 start = auditBody.position;
            Vector3 uphill = slope.transform.right;
            // Start in the dynamic-friction regime so the audit cannot be hidden by an initial
            // sleeping/static-contact island before gravity has resolved the slope tangent.
            auditBody.linearVelocity = -uphill * .05f;
            auditBody.WakeUp();
            for (int i = 0; i < 150; i++) Physics.Simulate(.02f);
            float downhillDistance = Mathf.Max(0f, -Vector3.Dot(auditBody.position - start, uphill));
            Object.DestroyImmediate(bodyObject);
            Object.DestroyImmediate(slope);
            Object.DestroyImmediate(groundMaterial);
            Object.DestroyImmediate(tireMaterial);
            Physics.simulationMode = previousMode;
            return downhillDistance;
        }

        private static void ValidateFormulaDownforce(VehicleDefinition formula)
        {
            const float auditSpeed = 25f;
            int middleLevel = 10;
            ResolvedVehicleStats level1 = ResolveAtTune(formula, VehicleUpgradeType.Downforce, 1);
            ResolvedVehicleStats level10 = ResolveAtTune(formula, VehicleUpgradeType.Downforce, middleLevel);
            ResolvedVehicleStats level20 = ResolveAtTune(formula, VehicleUpgradeType.Downforce,
                formula.downforceMaxTunes);
            float a1 = VehicleController.EvaluateAerodynamicDownforceAcceleration(formula, level1, auditSpeed);
            float a10 = VehicleController.EvaluateAerodynamicDownforceAcceleration(formula, level10, auditSpeed);
            float a20 = VehicleController.EvaluateAerodynamicDownforceAcceleration(formula, level20, auditSpeed);
            float airtime1 = 16f / (Physics.gravity.magnitude + a1);
            float airtime10 = 16f / (Physics.gravity.magnitude + a10);
            float airtime20 = 16f / (Physics.gravity.magnitude + a20);
            Vector3 groundedForce = VehicleController.EvaluateAppliedDownforceVector(Vector3.up, a20,
                true, false);
            Vector3 airborneForce = VehicleController.EvaluateAppliedDownforceVector(Vector3.right, a20,
                false, false);
            Vector3 chassisBounceForce = VehicleController.EvaluateAppliedDownforceVector(Vector3.up, a20,
                false, true);
            if (!(a1 < a10 && a10 < a20) || !(airtime1 > airtime10 && airtime10 > airtime20) ||
                a20 > formula.maximumDownforceAcceleration + .001f ||
                Vector3.Distance(groundedForce, Vector3.down * a20) > .001f ||
                Vector3.Distance(airborneForce, Vector3.down * a20) > .001f ||
                chassisBounceForce.sqrMagnitude > .000001f)
                throw new System.InvalidOperationException(
                    "Formula Downforce tuning or wheel/air/chassis contact routing is invalid.");
            Debug.Log($"FORMULA_DOWNFORCE_TEST @ {auditSpeed:0}m/s: L1={a1:0.00}m/s2/" +
                      $"{airtime1:0.000}s, L10={a10:0.00}/{airtime10:0.000}s, " +
                      $"L20={a20:0.00}/{airtime20:0.000}s; applied downward on ground and in air.");
        }

        private static ResolvedVehicleStats ResolveAtTune(VehicleDefinition definition,
            VehicleUpgradeType type, int level)
        {
            var state = new VehicleUpgradeState();
            state.SetTune(type, level);
            return definition.Resolve(state);
        }

        private static int ResolveMaximumGrip(VehicleDefinition definition)
        {
            var state = new VehicleUpgradeState();
            state.gripTune = definition.gripMaxTunes;
            state.engineTune = Mathf.Max(1, definition.engineMaxTunes);
            state.accelerationTune = Mathf.Max(1, definition.accelerationMaxTunes);
            state.suspensionTune = Mathf.Max(1, definition.suspensionMaxTunes);
            state.fuelTune = Mathf.Max(1, definition.fuelMaxTunes);
            state.turboTune = Mathf.Max(1, definition.turboMaxTunes);
            return definition.Resolve(state).gripRating;
        }

        private static ResolvedVehicleStats ResolveAllMaximum(VehicleDefinition definition)
        {
            var state = new VehicleUpgradeState
            {
                engineTune = Mathf.Max(1, definition.engineMaxTunes),
                accelerationTune = Mathf.Max(1, definition.accelerationMaxTunes),
                suspensionTune = Mathf.Max(1, definition.suspensionMaxTunes),
                gripTune = Mathf.Max(1, definition.gripMaxTunes),
                airControlTune = Mathf.Max(1, definition.airControlMaxTunes),
                downforceTune = Mathf.Max(1, definition.downforceMaxTunes),
                fuelTune = Mathf.Max(1, definition.fuelMaxTunes),
                turboTune = Mathf.Max(1, definition.turboMaxTunes)
            };
            return definition.Resolve(state);
        }

        private static float SimulateAirTorque(VehicleDefinition source, bool roadMode, bool gas,
            bool emptyFuel = false)
        {
            NewScene();
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            VehicleDefinition testDefinition = CreateMaximumTuneClone(source);
            GameObject inputObject = new GameObject("Air Control Input");
            VehicleInputController input = inputObject.AddComponent<VehicleInputController>();
            input.SetInputMode(roadMode ? GameModeType.Adventure3D : GameModeType.Adventure2_5D);
            GameObject prefab = roadMode ? source.gameplayPrefab3D : source.gameplayPrefab2D;
            GameObject instance = Object.Instantiate(prefab, new Vector3(0f, 10000f, 0f), Quaternion.identity);
            VehicleRuntimeBase controller;
            if (roadMode)
            {
                RoadVehicleController road = instance.GetComponent<RoadVehicleController>();
                road.Assign(testDefinition, input);
                InvokePrivate(road, "Awake");
                controller = road;
            }
            else
            {
                VehicleController climb = instance.GetComponent<VehicleController>();
                climb.AssignRuntimeReferences(testDefinition, input,
                    instance.GetComponentsInChildren<SuspensionController>(true));
                InvokePrivate(climb, "Awake");
                MovementPlaneConstraint plane = instance.GetComponent<MovementPlaneConstraint>();
                if (plane != null) InvokePrivate(plane, "Awake");
                controller = climb;
            }
            if (emptyFuel) controller.Fuel.SetNormalized(0f);
            input.SetTouchGas(gas);
            input.SetTouchBrake(!gas);
            InvokePrivate(input, "Update");
            InvokePrivate(controller, "FixedUpdate");
            Physics.Simulate(.02f);
            float longitudinalVelocity = roadMode
                ? Vector3.Dot(controller.Body.linearVelocity, instance.transform.forward)
                : controller.Body.linearVelocity.x;
            if (emptyFuel && Mathf.Abs(longitudinalVelocity) > .01f)
                throw new System.InvalidOperationException("Fuel-zero airborne input generated horizontal propulsion.");
            float angularVelocity = roadMode
                ? Vector3.Dot(controller.Body.angularVelocity, instance.transform.right)
                : controller.Body.angularVelocity.z;
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(testDefinition);
            Physics.simulationMode = previousMode;
            return angularVelocity;
        }

        private static float[] LastOnly(float[] values) =>
            new[] { values != null && values.Length > 0 ? values[values.Length - 1] : 0f };

        private static float[] FirstOnly(float[] values) =>
            new[] { values != null && values.Length > 0 ? values[0] : 0f };

        private static void InvokePrivate(object target, string method)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

        private static BuiltAssets CreateAssets()
        {
            var assets = new BuiltAssets();
            string[] spritePaths = { JeepBodyPath, JeepRetroSkinPath, JeepWheel01Path, JeepWheel02Path,
                JeepStarryWheelPath, FormulaBodyPath,
                FormulaNeonSkinPath, FormulaElectricitySkinPath, FormulaHillAdventurerSkinPath,
                FormulaWheel01Path, FormulaWheel02Path,
                FormulaLuxuryWheelPath,
                FormulaReferencePath, HillAdventurerPurpleSkinPath,
                RallyBodyPath, RallyHillAdventurerPath, RallyNeoPath, RallyNoRoofPath, RallyRoofPath, RallyWheelPath,
                RallyGhostyWheelPath, RallySkyBlueWheelPath, SportBikeBodyPath,
                SportBikeSunRayPath, SportBikeRetroLuxePath, SportBikeNeonFlowSkinPath,
                SportBikeWheel01Path, SportBikeWheel02Path, SportBikeGoldWheelPath,
                SportBikeGhostyWheelPath, SportBikeNeonFlowWheelPath,
                NeonSkyPath, NeonBackgroundPath, NeonTerrainPath, NeonGroundPath,
                NeonLiquidPath, NeonCautionPath, MainMenuBackgroundPath, HomeButtonPath, RaceButtonPath, SettingsButtonPath,
                SkinsButtonPath, StoreButtonPath, TuningButtonPath, UpgradeButtonPath, VehicleButtonPath,
                BrakePedalPath, BrakePedalPressedPath, GasPedalPath, GasPedalPressedPath,
                RpmGaugePath, RpmNeedlePath, SpeedGaugePath, SpeedNeedlePath,
                DriverPreviewPath, DriverHeadPath, DriverTorsoPath,
                DriverUpperArmPath, DriverForearmPath, DriverThighPath, DriverLowerLegPath,
                DriverGlassesPath, DriverFireGlassesPath, DriverScarfPath, DriverSantaHatPath,
                DriverNeonGlassesPath, DriverTireAccessoryPath, DriverBaseballCapPath,
                 CountrysideSkyPath, CountrysideMountainsPath, CountrysideSunPath, CountrysideTerrainPath, CountrysideGroundPath,
                 NorthPoleSkyPath, NorthPolePanoramaPath, NorthPoleNearPinesPath,
                 NorthPoleTerrainPath, NorthPoleGroundPath,
                 CitySkyPath, CityBackgroundPath, CityTerrainPath, CityGroundPath,
                 CityBridgePath, CityWaterPath, CityBoatSmallPath, CityBoatMediumPath,
                 CityBoatLargePath, FuelCanPath, Coin01Path, Coin05Path,
                BronzeChestClosedPath, BronzeChestOpenPath, SilverChestClosedPath, SilverChestOpenPath,
                GoldChestClosedPath, GoldChestOpenPath, MasterChestClosedPath, MasterChestOpenPath,
                LegendaryChestClosedPath, LegendaryChestOpenPath, CoinsChestPath, ChestShelfPath };
            for (int i = 0; i < spritePaths.Length; i++)
                ConfigureFormulaSprite(spritePaths[i], spritePaths[i].StartsWith(CountrysideRoot) ||
                    spritePaths[i].StartsWith(NorthPoleRoot) ||
                    spritePaths[i].StartsWith(NeonRoot) ||
                    spritePaths[i].StartsWith(CityRoot) ||
                    spritePaths[i].StartsWith(ButtonRoot) ||
                    spritePaths[i].StartsWith(GameUiRoot) ||
                    spritePaths[i] == FormulaHillAdventurerSkinPath ||
                    spritePaths[i] == HillAdventurerPurpleSkinPath ? 4096 :
                    spritePaths[i] == FuelCanPath || spritePaths[i] == Coin01Path || spritePaths[i] == Coin05Path ? 512 :
                    spritePaths[i].StartsWith(ChestRoot) ? 2048 : 2048);
            assets.jeepBody = AssetDatabase.LoadAssetAtPath<Sprite>(JeepBodyPath);
            assets.jeepRetroSkin = AssetDatabase.LoadAssetAtPath<Sprite>(JeepRetroSkinPath);
            assets.jeepWheel01 = AssetDatabase.LoadAssetAtPath<Sprite>(JeepWheel01Path);
            assets.jeepWheel02 = AssetDatabase.LoadAssetAtPath<Sprite>(JeepWheel02Path);
            assets.jeepStarryWheel = AssetDatabase.LoadAssetAtPath<Sprite>(JeepStarryWheelPath);
            assets.formulaBody = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaBodyPath);
            assets.formulaNeonSkin = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaNeonSkinPath);
            assets.formulaElectricitySkin = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaElectricitySkinPath);
            assets.formulaHillAdventurerSkin = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaHillAdventurerSkinPath);
            assets.formulaWheel01 = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaWheel01Path);
            assets.formulaWheel02 = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaWheel02Path);
            assets.luxuryWheel = AssetDatabase.LoadAssetAtPath<Sprite>(FormulaLuxuryWheelPath);
            assets.rallyBody = AssetDatabase.LoadAssetAtPath<Sprite>(RallyBodyPath);
            assets.rallyHillAdventurer = AssetDatabase.LoadAssetAtPath<Sprite>(RallyHillAdventurerPath);
            assets.rallyNeo = AssetDatabase.LoadAssetAtPath<Sprite>(RallyNeoPath);
            assets.rallyNoRoof = AssetDatabase.LoadAssetAtPath<Sprite>(RallyNoRoofPath);
            assets.rallyRoof = AssetDatabase.LoadAssetAtPath<Sprite>(RallyRoofPath);
            assets.rallyWheel = AssetDatabase.LoadAssetAtPath<Sprite>(RallyWheelPath);
            assets.rallyGhostyWheel = AssetDatabase.LoadAssetAtPath<Sprite>(RallyGhostyWheelPath);
            assets.rallySkyBlueWheel = AssetDatabase.LoadAssetAtPath<Sprite>(RallySkyBlueWheelPath);
            assets.sportBikeBody = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeBodyPath);
            assets.sportBikeSunRay = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeSunRayPath);
            assets.sportBikeRetroLuxe = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeRetroLuxePath);
            assets.sportBikeNeonFlowSkin = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeNeonFlowSkinPath);
            assets.sportBikeWheel01 = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeWheel01Path);
            assets.sportBikeWheel02 = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeWheel02Path);
            assets.sportBikeGoldWheel = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeGoldWheelPath);
            assets.sportBikeGhostyWheel = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeGhostyWheelPath);
            assets.sportBikeNeonFlowWheel = AssetDatabase.LoadAssetAtPath<Sprite>(SportBikeNeonFlowWheelPath);
            assets.hillAdventurerSkin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Vehicles/Hill Adventurer 4x4/Skins/Default.png");
            assets.hillAdventurerPurpleSkin = AssetDatabase.LoadAssetAtPath<Sprite>(HillAdventurerPurpleSkinPath);
            assets.hillAdventurerWheel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Vehicles/Hill Adventurer 4x4/Wheels/Default.png");
            assets.driverGlasses = AssetDatabase.LoadAssetAtPath<Sprite>(DriverGlassesPath);
            assets.driverFireGlasses = AssetDatabase.LoadAssetAtPath<Sprite>(DriverFireGlassesPath);
            assets.driverScarf = AssetDatabase.LoadAssetAtPath<Sprite>(DriverScarfPath);
            assets.driverSantaHat = AssetDatabase.LoadAssetAtPath<Sprite>(DriverSantaHatPath);
            assets.driverNeonGlasses = AssetDatabase.LoadAssetAtPath<Sprite>(DriverNeonGlassesPath);
            assets.driverTireAccessory = AssetDatabase.LoadAssetAtPath<Sprite>(DriverTireAccessoryPath);
            assets.driverBaseballCap = AssetDatabase.LoadAssetAtPath<Sprite>(DriverBaseballCapPath);
            assets.neonSky = AssetDatabase.LoadAssetAtPath<Sprite>(NeonSkyPath);
            assets.neonBackground = AssetDatabase.LoadAssetAtPath<Sprite>(NeonBackgroundPath);
            assets.neonTerrain = AssetDatabase.LoadAssetAtPath<Sprite>(NeonTerrainPath);
            assets.neonGround = AssetDatabase.LoadAssetAtPath<Sprite>(NeonGroundPath);
            assets.neonLiquid = AssetDatabase.LoadAssetAtPath<Sprite>(NeonLiquidPath);
            assets.neonCaution = AssetDatabase.LoadAssetAtPath<Sprite>(NeonCautionPath);
            assets.countrysideTerrain = AssetDatabase.LoadAssetAtPath<Sprite>(CountrysideTerrainPath);
            assets.countrysideGround = AssetDatabase.LoadAssetAtPath<Sprite>(CountrysideGroundPath);
            assets.northPoleSky = AssetDatabase.LoadAssetAtPath<Sprite>(NorthPoleSkyPath);
            assets.northPolePanorama = AssetDatabase.LoadAssetAtPath<Sprite>(NorthPolePanoramaPath);
            assets.northPoleNearPines = AssetDatabase.LoadAssetAtPath<Sprite>(NorthPoleNearPinesPath);
            assets.northPoleTerrain = AssetDatabase.LoadAssetAtPath<Sprite>(NorthPoleTerrainPath);
            assets.northPoleGround = AssetDatabase.LoadAssetAtPath<Sprite>(NorthPoleGroundPath);
            assets.citySky = AssetDatabase.LoadAssetAtPath<Sprite>(CitySkyPath);
            assets.cityBackground = AssetDatabase.LoadAssetAtPath<Sprite>(CityBackgroundPath);
            assets.cityTerrain = AssetDatabase.LoadAssetAtPath<Sprite>(CityTerrainPath);
            assets.cityGround = AssetDatabase.LoadAssetAtPath<Sprite>(CityGroundPath);
            assets.cityBridge = AssetDatabase.LoadAssetAtPath<Sprite>(CityBridgePath);
            assets.cityWater = AssetDatabase.LoadAssetAtPath<Sprite>(CityWaterPath);
            assets.cityBoats = new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>(CityBoatSmallPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(CityBoatMediumPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(CityBoatLargePath)
            };
            assets.mainMenuBackground = AssetDatabase.LoadAssetAtPath<Sprite>(MainMenuBackgroundPath);
            assets.homeButton = AssetDatabase.LoadAssetAtPath<Sprite>(HomeButtonPath);
            assets.raceButton = AssetDatabase.LoadAssetAtPath<Sprite>(RaceButtonPath);
            assets.settingsButton = AssetDatabase.LoadAssetAtPath<Sprite>(SettingsButtonPath);
            assets.skinsButton = AssetDatabase.LoadAssetAtPath<Sprite>(SkinsButtonPath);
            assets.storeButton = AssetDatabase.LoadAssetAtPath<Sprite>(StoreButtonPath);
            assets.tuningButton = AssetDatabase.LoadAssetAtPath<Sprite>(TuningButtonPath);
            assets.upgradeButton = AssetDatabase.LoadAssetAtPath<Sprite>(UpgradeButtonPath);
            assets.vehicleButton = AssetDatabase.LoadAssetAtPath<Sprite>(VehicleButtonPath);
            assets.brakePedal = AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPath);
            assets.brakePedalPressed = AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPressedPath);
            assets.gasPedal = AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPath);
            assets.gasPedalPressed = AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPressedPath);
            assets.rpmGauge = AssetDatabase.LoadAssetAtPath<Sprite>(RpmGaugePath);
            assets.rpmNeedle = AssetDatabase.LoadAssetAtPath<Sprite>(RpmNeedlePath);
            assets.speedGauge = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedGaugePath);
            assets.speedNeedle = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedNeedlePath);
            assets.fuelCan = AssetDatabase.LoadAssetAtPath<Sprite>(FuelCanPath);
            assets.coin01 = AssetDatabase.LoadAssetAtPath<Sprite>(Coin01Path);
            assets.coin05 = AssetDatabase.LoadAssetAtPath<Sprite>(Coin05Path);
            assets.bronzeChestClosed = AssetDatabase.LoadAssetAtPath<Sprite>(BronzeChestClosedPath);
            assets.bronzeChestOpen = AssetDatabase.LoadAssetAtPath<Sprite>(BronzeChestOpenPath);
            assets.silverChestClosed = AssetDatabase.LoadAssetAtPath<Sprite>(SilverChestClosedPath);
            assets.silverChestOpen = AssetDatabase.LoadAssetAtPath<Sprite>(SilverChestOpenPath);
            assets.goldChestClosed = AssetDatabase.LoadAssetAtPath<Sprite>(GoldChestClosedPath);
            assets.goldChestOpen = AssetDatabase.LoadAssetAtPath<Sprite>(GoldChestOpenPath);
            assets.masterChestClosed = AssetDatabase.LoadAssetAtPath<Sprite>(MasterChestClosedPath);
            assets.masterChestOpen = AssetDatabase.LoadAssetAtPath<Sprite>(MasterChestOpenPath);
            assets.legendaryChestClosed = AssetDatabase.LoadAssetAtPath<Sprite>(LegendaryChestClosedPath);
            assets.legendaryChestOpen = AssetDatabase.LoadAssetAtPath<Sprite>(LegendaryChestOpenPath);
            assets.coinsChest = AssetDatabase.LoadAssetAtPath<Sprite>(CoinsChestPath);
            assets.chestShelf = AssetDatabase.LoadAssetAtPath<Sprite>(ChestShelfPath);
            assets.countrysideMaterial = UnlitTextureMaterialAsset("CountrysideTerrain",
                assets.countrysideTerrain != null ? assets.countrysideTerrain.texture : null);
            assets.countrysideEdgeMaterial = UnlitTextureMaterialAsset("CountrysideGround",
                assets.countrysideGround != null ? assets.countrysideGround.texture : null);
            assets.northPoleMaterial = UnlitTextureMaterialAsset("NorthPoleTerrain",
                assets.northPoleTerrain != null ? assets.northPoleTerrain.texture : null);
            assets.northPoleEdgeMaterial = UnlitTextureMaterialAsset("NorthPoleGround",
                assets.northPoleGround != null ? assets.northPoleGround.texture : null);
            assets.neonCityMaterial = UnlitTextureMaterialAsset("NeonCityTerrain", assets.neonTerrain != null
                ? assets.neonTerrain.texture : null);
            assets.neonCityEdgeMaterial = UnlitTextureMaterialAsset("NeonCityEdge", assets.neonGround != null
                ? assets.neonGround.texture : null);
            assets.cityTerrainMaterial = UnlitTextureMaterialAsset("CityTerrain", assets.cityTerrain != null
                ? assets.cityTerrain.texture : null);
            assets.cityGroundMaterial = UnlitTextureMaterialAsset("CityGround", assets.cityGround != null
                ? assets.cityGround.texture : null);
            assets.nearMountains = MaterialAsset("MountainsNear", new Color(0.12f, 0.32f, 0.24f));
            assets.farMountains = MaterialAsset("MountainsFar", new Color(0.22f, 0.38f, 0.46f));
            assets.road = MaterialAsset("Road", new Color(0.17f, 0.18f, 0.20f));
            assets.grass = MaterialAsset("RoadGrass", new Color(0.18f, 0.42f, 0.19f));
            assets.shortcut = MaterialAsset("ShortcutTech", new Color(0.72f, 0.12f, 0.68f));
            // Motor torque remains exclusive to the physical wheels. The rounded chassis uses a low,
            // non-zero physical coefficient so momentum produces a long slide without behaving like ice.
            assets.chassisPhysics = PhysicsMaterialAsset("VehicleBodyLowBounce", .16f, 0f);
            assets.chassisPhysics.staticFriction = .20f;
            assets.chassisPhysics.frictionCombine = PhysicsMaterialCombine.Multiply;
            EditorUtility.SetDirty(assets.chassisPhysics);
            assets.groundPhysics = PhysicsMaterialAsset("TerrainStable", .72f, 0f);
            assets.groundPhysics.frictionCombine = PhysicsMaterialCombine.Multiply;
            EditorUtility.SetDirty(assets.groundPhysics);
            assets.tirePhysics = PhysicsMaterialAsset("VehicleTireArcade", 1f, 0f);
            assets.tirePhysics.frictionCombine = PhysicsMaterialCombine.Multiply;
            EditorUtility.SetDirty(assets.tirePhysics);

            assets.jeep = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/MainVehicle.asset");
            assets.jeep.vehicleId = "car-01";
            assets.jeep.unlockedByDefault = true; assets.jeep.coinPrice = 0; assets.jeep.gemPrice = 0;
            assets.jeep.allowsBlueprintProgression = true;
            assets.jeep.displayName = "JEEP";
            assets.jeep.archetype = VehicleArchetype.Jeep;
            assets.jeep.engineMaxTunes = 14; assets.jeep.accelerationMaxTunes = 15;
            assets.jeep.suspensionMaxTunes = 12; assets.jeep.gripMaxTunes = 12;
            assets.jeep.airControlMaxTunes = 0; assets.jeep.downforceMaxTunes = 0; assets.jeep.fuelMaxTunes = 15;
            assets.jeep.tuneTargets = new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = CreateProgressionTargets(14, 60f, 100f),
                accelerationZeroToHundredSeconds = CreateProgressionTargets(15, 12f, 7f),
                gripRatings = CreateProgressionTargets(12, 50f, 300f),
                airControlTorque = new float[] { 3200f }, airborneDownforceAcceleration = new float[] { 0f },
                fuelRunDurationSeconds = CreateProgressionTargets(15, 15f, 30f)
            };
            assets.jeep.suspension.minTravel = .28f; assets.jeep.suspension.maxTravel = .44f;
            assets.jeep.suspension.restLength = .52f;
            assets.jeep.suspension.compressionTravelRatio = .90f;
            assets.jeep.suspension.targetSag01 = .30f;
            assets.jeep.suspension.stiffnessTuneScale = new Vector2(.82f, 1.0f);
            assets.jeep.suspension.dampingRatio = new Vector2(.66f, 1.04f);
            assets.jeep.suspension.compressionDampingMultiplier = .88f;
            assets.jeep.suspension.reboundDampingMultiplier = 1.15f;
            assets.jeep.suspension.compressionDampingTuneScale = new Vector2(.74f, 1.10f);
            assets.jeep.suspension.reboundDampingTuneScale = new Vector2(.92f, 1.50f);
            assets.jeep.suspension.reboundAccelerationTune = new Vector2(2.0f, .50f);
            assets.jeep.suspension.wheelContactTolerance = new Vector2(.008f, .020f);
            assets.jeep.suspension.landingDampingScale = new Vector2(1.02f, 1.32f);
            assets.jeep.suspension.landingDampingDuration = .14f;
            assets.jeep.suspension.springMin = 70000f; assets.jeep.suspension.springMax = 82000f;
            assets.jeep.suspension.dampingMin = 10000f; assets.jeep.suspension.dampingMax = 12000f;
            assets.jeep.suspension.bumpStopStart = .72f;
            assets.jeep.suspension.progressiveSpringMultiplier = 1.5f;
            assets.jeep.suspension.hardStopSpringMultiplier = 8f;
            assets.jeep.suspension.maximumAcceleration = 24f;
            assets.jeep.suspension.maximumReboundAcceleration = 2.2f;
            assets.jeep.grip.minGrip = .14f; assets.jeep.grip.maxGrip = 2.20f;
            assets.jeep.grip.slipThreshold = .16f; assets.jeep.grip.slidingGripMultiplier = .38f;
            assets.jeep.grip.tractionRecoveryPerSecond = 3.2f;
            assets.jeep.engine.minTorqueNm = 155f; assets.jeep.engine.maxTorqueNm = 230f;
            assets.jeep.fuel.capacityUnits = 100f; assets.jeep.fuel.minimumRunDurationSeconds = 15f;
            assets.jeep.fuel.maximumRunDurationSeconds = 30f; assets.jeep.fuel.referenceRunDurationSeconds = 30f;
            assets.jeep.mass = 1150f; assets.jeep.wheelMass = 48f; assets.jeep.wheelRadius = .45f;
            assets.jeep.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.jeep.collisionPitchInertiaMultiplier = 1.30f;
            assets.jeep.maximumDepenetrationVelocity = 5.0f;
            assets.jeep.chassisAirControlMultiplier = .10f;
            assets.jeep.halfWheelbase = 1.18f; assets.jeep.halfTrack = .78f;
            assets.jeep.topSpeedMps = 100f / 3.6f; assets.jeep.airControlTorque = 3200f;
            assets.jeep.maxAirAngularVelocity = 2.5f; assets.jeep.airAngularAcceleration = 14f;
            assets.jeep.airAngularDamping = .34f;
            assets.jeep.oneWheelAngularDamping = .10f; assets.jeep.twoWheelAngularDamping = .34f;
            assets.jeep.maximumPassiveAngularVelocity = 8f;
            assets.jeep.centerOfMass = new Vector3(.08f, -.06f, 0f);
            assets.jeep.accelerationForceCalibration = .400f;
            assets.jeep.previewSprite = assets.jeepBody;
            assets.jeep.skins = new[]
            {
                Customization("jeep-skin-default", "JEEP DEFAULT", assets.jeepBody),
                Customization("jeep-skin-retro", "RETRO SKIN", assets.jeepRetroSkin, false, 1000)
            };
            assets.jeep.wheels = new[]
            {
                Customization("jeep-wheel-01", "JEEP WHEEL 01", assets.jeepWheel01),
                Customization("jeep-wheel-02", "JEEP WHEEL 02", assets.jeepWheel02),
                Customization("jeep-wheel-starry", "STARRY WHEEL", assets.jeepStarryWheel, false, 500)
            };
            assets.jeep.visualWidth = 3.80f;
            assets.jeep.flipBodyX = false;
            assets.jeep.bodyVisualOffset = new Vector2(0f, .16f);
            if (!assets.jeep.preserveWheelVisualPositions)
            {
                assets.jeep.rearWheelVisualPosition = new Vector2(-1.12f, -.44f);
                assets.jeep.frontWheelVisualPosition = new Vector2(1.24f, -.44f);
                assets.jeep.rearWheelVisualDiameter = 0f;
                assets.jeep.frontWheelVisualDiameter = 0f;
            }
            assets.jeep.wheelVisualDiameter = .90f;
            if (!assets.jeep.preserveDriverPose || assets.jeep.driverPose == null)
                assets.jeep.driverPose = CreateJeepDriverPose();
            EditorUtility.SetDirty(assets.jeep);

            assets.formula = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/FormulaVehicle.asset");
            assets.formula.vehicleId = "formula-01"; assets.formula.displayName = "FORMULA";
            assets.formula.unlockedByDefault = false; assets.formula.coinPrice = 100000; assets.formula.gemPrice = 0;
            assets.formula.allowsBlueprintProgression = true;
            assets.formula.archetype = VehicleArchetype.Formula; assets.formula.previewSprite = assets.formulaBody;
            assets.formula.engineMaxTunes = 20; assets.formula.accelerationMaxTunes = 20;
            assets.formula.suspensionMaxTunes = 20; assets.formula.gripMaxTunes = 20;
            assets.formula.airControlMaxTunes = 0; assets.formula.downforceMaxTunes = 20; assets.formula.fuelMaxTunes = 25;
            assets.formula.tuneTargets = CreateFormulaTuneTargets();
            assets.formula.mass = 780f; assets.formula.wheelMass = 40f; assets.formula.wheelRadius = .48f;
            assets.formula.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            // Formula remains the lowest-COM car, but its physical inertia must still transmit a strong
            // nose/tail or one-wheel impact instead of acting like a four-times heavier flywheel.
            assets.formula.collisionPitchInertiaMultiplier = 1.35f;
            assets.formula.maximumDepenetrationVelocity = 5.0f;
            assets.formula.chassisAirControlMultiplier = .08f;
            assets.formula.halfWheelbase = 1.75f; assets.formula.halfTrack = .78f;
            assets.formula.topSpeedMps = 300f / 3.6f;
            assets.formula.brakeForce = 18000f; assets.formula.reverseForce = 6000f;
            assets.formula.accelerationForceCalibration = .377f;
            assets.formula.airControlTorque = 3800f;
            assets.formula.downforceReferenceSpeedMps = 30f;
            assets.formula.maximumDownforceAcceleration = 28f;
            assets.formula.maxAirAngularVelocity = 1.8f; assets.formula.airAngularAcceleration = 11f;
            assets.formula.airAngularDamping = .50f;
            assets.formula.oneWheelAngularDamping = .06f; assets.formula.twoWheelAngularDamping = .28f;
            assets.formula.maximumPassiveAngularVelocity = 9f;
            assets.formula.centerOfMass = new Vector3(0f, -.17f, 0f);
            assets.formula.engine.minTorqueNm = 300f; assets.formula.engine.maxTorqueNm = 700f;
            assets.formula.engine.drivetrainEfficiency = .88f;
            assets.formula.transmission.forwardGearRatios = new[] { 3.80f, 2.75f, 2.15f, 1.75f, 1.48f, 1.30f, 1.18f };
            assets.formula.suspension.restLength = .38f; assets.formula.suspension.minTravel = .16f;
            assets.formula.suspension.maxTravel = .25f; assets.formula.suspension.compressionTravelRatio = .90f;
            assets.formula.suspension.targetSag01 = .29f;
            assets.formula.suspension.stiffnessTuneScale = new Vector2(.78f, 1.25f);
            assets.formula.suspension.dampingRatio = new Vector2(.72f, 1.10f);
            assets.formula.suspension.compressionDampingMultiplier = .78f;
            assets.formula.suspension.reboundDampingMultiplier = 1.20f;
            assets.formula.suspension.compressionDampingTuneScale = new Vector2(.76f, .95f);
            assets.formula.suspension.reboundDampingTuneScale = new Vector2(.90f, 1.50f);
            assets.formula.suspension.reboundAccelerationTune = new Vector2(1.4f, .50f);
            assets.formula.suspension.wheelContactTolerance = new Vector2(.007f, .018f);
            assets.formula.suspension.landingDampingScale = new Vector2(1.02f, 1.45f);
            assets.formula.suspension.landingDampingDuration = .13f;
            assets.formula.suspension.springMin = 82000f; assets.formula.suspension.springMax = 96000f;
            assets.formula.suspension.dampingMin = 14000f; assets.formula.suspension.dampingMax = 16000f;
            assets.formula.suspension.bumpStopStart = .76f;
            assets.formula.suspension.progressiveSpringMultiplier = 1.6f;
            assets.formula.suspension.hardStopSpringMultiplier = 8f;
            assets.formula.suspension.maximumAcceleration = 36f;
            assets.formula.suspension.maximumReboundAcceleration = 2.2f;
            assets.formula.grip.minGrip = .14f; assets.formula.grip.maxGrip = 2.20f;
            assets.formula.grip.slipThreshold = .13f; assets.formula.grip.slidingGripMultiplier = .46f;
            assets.formula.grip.tractionRecoveryPerSecond = 4.2f;
            assets.formula.grip.rollingResistance = .012f; assets.formula.grip.aerodynamicDrag = .28f;
            assets.formula.fuel.capacityUnits = 100f; assets.formula.fuel.minimumRunDurationSeconds = 20f;
            assets.formula.fuel.maximumRunDurationSeconds = 45f; assets.formula.fuel.referenceRunDurationSeconds = 45f;
            assets.formula.skins = new[]
            {
                Customization("formula-skin-default", "FORMULA DEFAULT", assets.formulaBody),
                Customization("formula-skin-neon", "NEON SKIN", assets.formulaNeonSkin, false, 1000),
                Customization(FormulaElectricSkinEffect.ElectricSkinId, "ELECTRICITY",
                    assets.formulaElectricitySkin, false, 1000),
                Customization("formula-skin-hill-adventurer-gold", "HILL ADVENTURER GOLD",
                    assets.formulaHillAdventurerSkin, false, 0, 0f,
                    new Vector2(21.07f, 4.93f), new Vector2(.11f, .125f), false)
            };
            assets.formula.wheels = new[]
            {
                Customization("formula-wheel-02", "FORMULA WHEEL 02", assets.formulaWheel02),
                Customization("formula-wheel-01", "FORMULA GHOSTY WHEELS", assets.formulaWheel01, false, 500),
                Customization("formula-wheel-luxury", "HILL ADVENTURER GOLD WHEEL", assets.luxuryWheel,
                    false, 0, 0f, default, default, false)
            };
            assets.formula.electricSkinOverlay = assets.formulaElectricitySkin;
            assets.formula.visualWidth = 5.60f;
            assets.formula.flipBodyX = false;
            assets.formula.bodyVisualOffset = new Vector2(0f, .08f);
            if (!assets.formula.preserveWheelVisualPositions)
            {
                assets.formula.rearWheelVisualPosition = new Vector2(-1.75f, -.28f);
                assets.formula.frontWheelVisualPosition = new Vector2(1.75f, -.28f);
                assets.formula.rearWheelVisualDiameter = 1.112685f;
                assets.formula.frontWheelVisualDiameter = 1.112685f;
            }
            assets.formula.wheelVisualDiameter = 1.08f;
            if (!assets.formula.preserveDriverPose || assets.formula.driverPose == null)
                assets.formula.driverPose = CreateFormulaDriverPose();
            EditorUtility.SetDirty(assets.formula);

            assets.rally = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/RallyVehicle.asset");
            assets.rally.vehicleId = "rally-01"; assets.rally.displayName = "RALLY";
            assets.rally.unlockedByDefault = false; assets.rally.coinPrice = 50000; assets.rally.gemPrice = 0;
            assets.rally.allowsBlueprintProgression = true;
            assets.rally.archetype = VehicleArchetype.Rally; assets.rally.previewSprite = assets.rallyBody;
            assets.rally.engineMaxTunes = 20; assets.rally.accelerationMaxTunes = 0;
            assets.rally.suspensionMaxTunes = 14; assets.rally.gripMaxTunes = 14;
            assets.rally.airControlMaxTunes = 0; assets.rally.downforceMaxTunes = 0;
            assets.rally.fuelMaxTunes = 15; assets.rally.turboMaxTunes = 20;
            assets.rally.tuneTargets = CreateRallyTuneTargets();
            assets.rally.mass = 1030f; assets.rally.wheelMass = 36f; assets.rally.wheelRadius = .51f;
            assets.rally.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.rally.collisionPitchInertiaMultiplier = 1.20f;
            assets.rally.maximumDepenetrationVelocity = 5.0f;
            assets.rally.chassisAirControlMultiplier = .09f;
            assets.rally.halfWheelbase = 1.35f; assets.rally.halfTrack = .78f;
            assets.rally.topSpeedMps = 280f / 3.6f;
            assets.rally.brakeForce = 16500f; assets.rally.reverseForce = 5800f;
            assets.rally.accelerationForceCalibration = .361f;
            assets.rally.airControlTorque = 4200f;
            assets.rally.downforceReferenceSpeedMps = 30f;
            assets.rally.maximumDownforceAcceleration = 11f;
            assets.rally.maxAirAngularVelocity = 2.2f; assets.rally.airAngularAcceleration = 13f;
            assets.rally.airAngularDamping = .45f;
            assets.rally.oneWheelAngularDamping = .08f; assets.rally.twoWheelAngularDamping = .32f;
            assets.rally.maximumPassiveAngularVelocity = 8.5f;
            assets.rally.centerOfMass = new Vector3(.06f, -.07f, 0f);
            assets.rally.engine.minTorqueNm = 280f; assets.rally.engine.maxTorqueNm = 650f;
            assets.rally.engine.drivetrainEfficiency = .86f;
            assets.rally.transmission.forwardGearRatios = new[] { 4.20f, 3.10f, 2.50f, 2.10f, 1.82f, 1.58f, 1.38f };
            assets.rally.suspension.restLength = .56f; assets.rally.suspension.minTravel = .25f;
            assets.rally.suspension.maxTravel = .40f; assets.rally.suspension.compressionTravelRatio = .89f;
            assets.rally.suspension.targetSag01 = .30f;
            assets.rally.suspension.stiffnessTuneScale = new Vector2(.82f, 1.0f);
            assets.rally.suspension.dampingRatio = new Vector2(.68f, 1.06f);
            assets.rally.suspension.compressionDampingMultiplier = .86f;
            assets.rally.suspension.reboundDampingMultiplier = 1.18f;
            assets.rally.suspension.compressionDampingTuneScale = new Vector2(.74f, 1.10f);
            assets.rally.suspension.reboundDampingTuneScale = new Vector2(.94f, 1.50f);
            assets.rally.suspension.reboundAccelerationTune = new Vector2(1.8f, .50f);
            assets.rally.suspension.wheelContactTolerance = new Vector2(.008f, .022f);
            assets.rally.suspension.landingDampingScale = new Vector2(1.05f, 1.45f);
            assets.rally.suspension.landingDampingDuration = .16f;
            assets.rally.suspension.springMin = 72000f; assets.rally.suspension.springMax = 84000f;
            assets.rally.suspension.dampingMin = 12500f; assets.rally.suspension.dampingMax = 15000f;
            assets.rally.suspension.bumpStopStart = .72f;
            assets.rally.suspension.progressiveSpringMultiplier = 1.9f;
            assets.rally.suspension.hardStopSpringMultiplier = 7f;
            assets.rally.suspension.maximumAcceleration = 30f;
            assets.rally.suspension.maximumReboundAcceleration = 1.6f;
            assets.rally.grip.minGrip = .14f; assets.rally.grip.maxGrip = 2.20f;
            assets.rally.grip.slipThreshold = .15f; assets.rally.grip.slidingGripMultiplier = .52f;
            assets.rally.grip.tractionRecoveryPerSecond = 5f;
            assets.rally.grip.rollingResistance = .014f; assets.rally.grip.aerodynamicDrag = .34f;
            assets.rally.fuel.capacityUnits = 100f; assets.rally.fuel.minimumRunDurationSeconds = 50f;
            assets.rally.fuel.maximumRunDurationSeconds = 50f; assets.rally.fuel.referenceRunDurationSeconds = 50f;
            assets.rally.skins = new[]
            {
                Customization("rally-skin-default", "RALLY DEFAULT", assets.rallyBody, true, 0, 0f, new Vector2(24.11f, 8.67f), new Vector2(0f, 0.005f)),
                Customization("rally-skin-hill-adventurer", "HILL ADVENTURER", assets.rallyHillAdventurer, false, 1000, 0f, new Vector2(18.41f, 6.71f), new Vector2(0.02f, 0.22f)),
                Customization("rally-skin-neo-rally", "NEO RALLY", assets.rallyNeo, false, 1000, 0f, new Vector2(17.72f, 6.88f), new Vector2(0.005f, -0.02f))
            };
            assets.rally.wheels = new[]
            {
                Customization("rally-wheel-01", "RALLY WHEEL 01", assets.rallyWheel),
                Customization("rally-wheel-ghosty", "RALLY GHOSTY WHEEL", assets.rallyGhostyWheel, false, 500),
                Customization("rally-wheel-sky-blue", "SKY BLUE WHEEL", assets.rallySkyBlueWheel, false, 500),
                Customization("rally-wheel-luxury", "LUXURY WHEEL", assets.luxuryWheel, false, 500)
            };
            assets.rally.damagedBodySprite = assets.rallyNoRoof;
            assets.rally.detachableRoofSprite = assets.rallyRoof;
            assets.rally.visualWidth = 4.65f;
            assets.rally.flipBodyX = false;
            assets.rally.bodyVisualOffset = new Vector2(0f, .16f);
            if (!assets.rally.preserveWheelVisualPositions)
            {
                assets.rally.rearWheelVisualPosition = new Vector2(-1.37f, -.49f);
                assets.rally.frontWheelVisualPosition = new Vector2(1.29f, -.49f);
                assets.rally.rearWheelVisualDiameter = 0f;
                assets.rally.frontWheelVisualDiameter = 0f;
            }
            assets.rally.wheelVisualDiameter = 1.02f;
            if (!assets.rally.preserveDriverPose || assets.rally.driverPose == null)
                assets.rally.driverPose = CreateRallyDriverPose();
            EditorUtility.SetDirty(assets.rally);

            assets.sportBike = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/SportBikeVehicle.asset");
            assets.sportBike.vehicleId = "sport-bike-01";
            assets.sportBike.unlockedByDefault = false; assets.sportBike.coinPrice = 50000; assets.sportBike.gemPrice = 0;
            assets.sportBike.allowsBlueprintProgression = true;
            assets.sportBike.displayName = "SPORT BIKE";
            assets.sportBike.archetype = VehicleArchetype.SportBike;
            assets.sportBike.previewSprite = assets.sportBikeBody;
            assets.sportBike.engineMaxTunes = 20;
            assets.sportBike.accelerationMaxTunes = 20;
            assets.sportBike.suspensionMaxTunes = 15;
            assets.sportBike.gripMaxTunes = 20;
            assets.sportBike.airControlMaxTunes = 0;
            assets.sportBike.downforceMaxTunes = 0;
            assets.sportBike.fuelMaxTunes = 15;
            assets.sportBike.turboMaxTunes = 0;
            assets.sportBike.tuneTargets = CreateSportBikeTuneTargets();
            assets.sportBike.mass = 260f;
            assets.sportBike.wheelMass = 15f;
            assets.sportBike.wheelRadius = .38f;
            assets.sportBike.halfWheelbase = 1.39f;
            assets.sportBike.halfTrack = .46f;
            assets.sportBike.topSpeedMps = 360f / 3.6f;
            assets.sportBike.brakeForce = 9800f;
            assets.sportBike.reverseForce = 3300f;
            assets.sportBike.airControlTorque = 8200f;
            assets.sportBike.downforceReferenceSpeedMps = 30f;
            assets.sportBike.maximumDownforceAcceleration = 10f;
            assets.sportBike.collisionPitchInertiaMultiplier = 1f;
            assets.sportBike.maximumDepenetrationVelocity = 5.0f;
            assets.sportBike.chassisAirControlMultiplier = .07f;
            assets.sportBike.maxAirAngularVelocity = 3.6f;
            assets.sportBike.airAngularAcceleration = 24f;
            assets.sportBike.airAngularDamping = .18f;
            assets.sportBike.oneWheelAngularDamping = .03f; assets.sportBike.twoWheelAngularDamping = .16f;
            assets.sportBike.maximumPassiveAngularVelocity = 10f;
            assets.sportBike.accelerationForceCalibration = .811f;
            assets.sportBike.centerOfMass = new Vector3(-.015f, -.04f, 0f);
            assets.sportBike.engine.minTorqueNm = 85f;
            assets.sportBike.engine.maxTorqueNm = 260f;
            assets.sportBike.engine.drivetrainEfficiency = .86f;
            assets.sportBike.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.sportBike.transmission.forwardGearRatios =
                new[] { 3.60f, 2.65f, 2.08f, 1.70f, 1.44f, 1.25f, 1.10f };
            assets.sportBike.transmission.finalDrive = 3.05f;
            assets.sportBike.suspension.restLength = .40f;
            assets.sportBike.suspension.minTravel = .165f;
            assets.sportBike.suspension.maxTravel = .27f;
            assets.sportBike.suspension.compressionTravelRatio = .88f;
            assets.sportBike.suspension.targetSag01 = .29f;
            assets.sportBike.suspension.stiffnessTuneScale = new Vector2(.78f, 1.0f);
            assets.sportBike.suspension.dampingRatio = new Vector2(.72f, 1.10f);
            assets.sportBike.suspension.compressionDampingMultiplier = .88f;
            assets.sportBike.suspension.reboundDampingMultiplier = 1.18f;
            assets.sportBike.suspension.compressionDampingTuneScale = new Vector2(.76f, .95f);
            assets.sportBike.suspension.reboundDampingTuneScale = new Vector2(.96f, 1.50f);
            assets.sportBike.suspension.reboundAccelerationTune = new Vector2(1.8f, .50f);
            assets.sportBike.suspension.wheelContactTolerance = new Vector2(.007f, .018f);
            assets.sportBike.suspension.landingDampingScale = new Vector2(1.10f, 1.50f);
            assets.sportBike.suspension.landingDampingDuration = .18f;
            assets.sportBike.suspension.springMin = 18000f;
            assets.sportBike.suspension.springMax = 26000f;
            assets.sportBike.suspension.dampingMin = 2500f;
            assets.sportBike.suspension.dampingMax = 3900f;
            assets.sportBike.suspension.bumpStopStart = .74f;
            assets.sportBike.suspension.progressiveSpringMultiplier = 1.6f;
            assets.sportBike.suspension.hardStopSpringMultiplier = 7f;
            assets.sportBike.suspension.maximumAcceleration = 26f;
            assets.sportBike.suspension.maximumReboundAcceleration = 1.6f;
            assets.sportBike.grip.minGrip = .12f;
            assets.sportBike.grip.maxGrip = 1.85f;
            assets.sportBike.grip.slipThreshold = .13f;
            assets.sportBike.grip.slidingGripMultiplier = .40f;
            assets.sportBike.grip.tractionRecoveryPerSecond = 4.0f;
            assets.sportBike.grip.rollingResistance = .010f;
            assets.sportBike.grip.aerodynamicDrag = .20f;
            assets.sportBike.fuel.capacityUnits = 100f;
            assets.sportBike.fuel.minimumRunDurationSeconds = 15f;
            assets.sportBike.fuel.maximumRunDurationSeconds = 45f;
            assets.sportBike.fuel.referenceRunDurationSeconds = 45f;
            assets.sportBike.skins = new[]
            {
                Customization("sport-bike-default", "SPORT BIKE DEFAULT", assets.sportBikeBody, true, 0,
                    0f, new Vector2(14.94f, 7.86f), new Vector2(-.02f, .075f)),
                Customization("sport-bike-sun-ray", "SUN RAY", assets.sportBikeSunRay, false, 1000,
                    0f, new Vector2(15.09f, 8.04f), new Vector2(.005f, .185f)),
                Customization("sport-bike-retro-luxe", "RETRO LUXE", assets.sportBikeRetroLuxe, false, 1000,
                    0f, new Vector2(15.01f, 7.93f), new Vector2(-.025f, .085f)),
                Customization(SportBikeNeonFlowEffect.SkinId, "NEON FLOW", assets.sportBikeNeonFlowSkin,
                    false, 1000, 0f, new Vector2(15.34f, 9.10f), new Vector2(.01f, .205f))
            };
            assets.sportBike.wheels = new[]
            {
                Customization("sport-bike-wheel-01", "BIKE WHEEL 01", assets.sportBikeWheel01, true, 0,
                    VisibleSpriteDiameter(assets.sportBikeWheel01)),
                Customization("sport-bike-wheel-02", "BIKE WHEEL 02", assets.sportBikeWheel02, true, 0,
                    VisibleSpriteDiameter(assets.sportBikeWheel02)),
                Customization("sport-bike-gold-wheel", "GOLD WHEEL", assets.sportBikeGoldWheel, false, 500,
                    VisibleSpriteDiameter(assets.sportBikeGoldWheel)),
                Customization("sport-bike-wheel-ghosty", "SPORT BIKE GHOSTY WHEEL", assets.sportBikeGhostyWheel, false, 500,
                    VisibleSpriteDiameter(assets.sportBikeGhostyWheel)),
                Customization(SportBikeNeonFlowEffect.WheelId, "NEON FLOW WHEEL",
                    assets.sportBikeNeonFlowWheel, false, 500, 7.44f, new Vector2(7.36f, 7.44f))
            };
            assets.sportBike.visualWidth = 3.30f;
            // All authored bike skins already face the rightward direction of travel.
            assets.sportBike.flipBodyX = false;
            assets.sportBike.bodyVisualOffset = new Vector2(0f, .10f);
            if (!assets.sportBike.preserveWheelVisualPositions)
            {
                assets.sportBike.rearWheelVisualPosition = new Vector2(-1.40f, -.48f);
                assets.sportBike.frontWheelVisualPosition = new Vector2(1.37f, -.49f);
                assets.sportBike.rearWheelVisualDiameter = .96f;
                assets.sportBike.frontWheelVisualDiameter = .96f;
            }
            assets.sportBike.wheelVisualDiameter = .93f;
            if (!assets.sportBike.preserveDriverPose || assets.sportBike.driverPose == null)
                assets.sportBike.driverPose = CreateSportBikeDriverPose();
            EditorUtility.SetDirty(assets.sportBike);

            assets.hillAdventurer = LoadOrCreate<VehicleDefinition>(Root + "/VehicleData/HillAdventurerVehicle.asset");
            assets.hillAdventurer.vehicleId = "hill-adventurer-01";
            assets.hillAdventurer.displayName = "HILL ADVENTURER 4x4";
            assets.hillAdventurer.unlockedByDefault = false;
            assets.hillAdventurer.coinPrice = 20000;
            assets.hillAdventurer.gemPrice = 0;
            assets.hillAdventurer.allowsBlueprintProgression = true;
            assets.hillAdventurer.archetype = VehicleArchetype.HillAdventurer;
            assets.hillAdventurer.previewSprite = assets.hillAdventurerSkin != null ? assets.hillAdventurerSkin : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Vehicles/Hill Adventurer 4x4/Skins/Default.png");
            assets.hillAdventurer.engineMaxTunes = 20;
            assets.hillAdventurer.accelerationMaxTunes = 14;
            assets.hillAdventurer.suspensionMaxTunes = 14;
            assets.hillAdventurer.gripMaxTunes = 14;
            assets.hillAdventurer.airControlMaxTunes = 0;
            assets.hillAdventurer.downforceMaxTunes = 0;
            assets.hillAdventurer.fuelMaxTunes = 18;
            assets.hillAdventurer.turboMaxTunes = 0;
            assets.hillAdventurer.tuneTargets = CreateHillAdventurerTuneTargets();
            assets.hillAdventurer.mass = 1280f;
            assets.hillAdventurer.wheelMass = 48f;
            assets.hillAdventurer.wheelRadius = .55f;
            assets.hillAdventurer.halfWheelbase = 1.99f;
            assets.hillAdventurer.halfTrack = .82f;
            assets.hillAdventurer.topSpeedMps = 190f / 3.6f;
            assets.hillAdventurer.brakeForce = 15000f;
            assets.hillAdventurer.reverseForce = 5200f;
            assets.hillAdventurer.accelerationForceCalibration = .380f;
            assets.hillAdventurer.airControlTorque = 3400f;
            assets.hillAdventurer.collisionPitchInertiaMultiplier = 1.25f;
            assets.hillAdventurer.maximumDepenetrationVelocity = 5.0f;
            assets.hillAdventurer.chassisAirControlMultiplier = .10f;
            assets.hillAdventurer.maxAirAngularVelocity = 2.0f;
            assets.hillAdventurer.airAngularAcceleration = 12f;
            assets.hillAdventurer.airAngularDamping = .40f;
            assets.hillAdventurer.oneWheelAngularDamping = .09f;
            assets.hillAdventurer.twoWheelAngularDamping = .34f;
            assets.hillAdventurer.maximumPassiveAngularVelocity = 8.0f;
            assets.hillAdventurer.centerOfMass = new Vector3(.05f, -.10f, 0f);
            assets.hillAdventurer.engine.idleRpm = 1100f;
            assets.hillAdventurer.engine.redlineRpm = 7000f;
            assets.hillAdventurer.engine.limiterRpm = 7400f;
            assets.hillAdventurer.engine.minTorqueNm = 240f;
            assets.hillAdventurer.engine.maxTorqueNm = 520f;
            assets.hillAdventurer.engine.drivetrainEfficiency = .84f;
            assets.hillAdventurer.engine.speedTorqueCurve = ArcadeSpeedTorqueCurve();
            assets.hillAdventurer.transmission.forwardGearRatios = new[] { 4.50f, 3.30f, 2.60f, 2.15f, 1.85f, 1.62f, 1.40f };
            assets.hillAdventurer.transmission.reverseRatio = 3.20f;
            assets.hillAdventurer.transmission.finalDrive = 3.45f;
            assets.hillAdventurer.suspension.restLength = .62f;
            assets.hillAdventurer.suspension.minTravel = .24f;
            assets.hillAdventurer.suspension.maxTravel = .38f;
            assets.hillAdventurer.suspension.compressionTravelRatio = .90f;
            assets.hillAdventurer.suspension.targetSag01 = .30f;
            assets.hillAdventurer.suspension.stiffnessTuneScale = new Vector2(.84f, 1.05f);
            assets.hillAdventurer.suspension.dampingRatio = new Vector2(.75f, 1.10f);
            assets.hillAdventurer.suspension.compressionDampingMultiplier = .85f;
            assets.hillAdventurer.suspension.reboundDampingMultiplier = 1.20f;
            assets.hillAdventurer.suspension.compressionDampingTuneScale = new Vector2(.76f, 1.10f);
            assets.hillAdventurer.suspension.reboundDampingTuneScale = new Vector2(.88f, 1.45f);
            assets.hillAdventurer.suspension.reboundAccelerationTune = new Vector2(1.8f, .60f);
            assets.hillAdventurer.suspension.wheelContactTolerance = new Vector2(.01f, .024f);
            assets.hillAdventurer.suspension.landingDampingScale = new Vector2(1.05f, 1.40f);
            assets.hillAdventurer.suspension.landingDampingDuration = .16f;
            assets.hillAdventurer.suspension.springMin = 32000f;
            assets.hillAdventurer.suspension.springMax = 46000f;
            assets.hillAdventurer.suspension.dampingMin = 4500f;
            assets.hillAdventurer.suspension.dampingMax = 6500f;
            assets.hillAdventurer.suspension.bumpStopStart = .70f;
            assets.hillAdventurer.suspension.progressiveSpringMultiplier = 2.0f;
            assets.hillAdventurer.suspension.hardStopSpringMultiplier = 7.5f;
            assets.hillAdventurer.suspension.maximumAcceleration = 28f;
            assets.hillAdventurer.suspension.maximumReboundAcceleration = 1.0f;
            assets.hillAdventurer.grip.minGrip = .14f;
            assets.hillAdventurer.grip.maxGrip = 2.20f;
            assets.hillAdventurer.grip.slipThreshold = .17f;
            assets.hillAdventurer.grip.slidingGripMultiplier = .48f;
            assets.hillAdventurer.grip.tractionRecoveryPerSecond = 4.5f;
            assets.hillAdventurer.grip.rollingResistance = .016f;
            assets.hillAdventurer.grip.aerodynamicDrag = .42f;
            assets.hillAdventurer.fuel.capacityUnits = 100f;
            assets.hillAdventurer.fuel.minimumRunDurationSeconds = 28f;
            assets.hillAdventurer.fuel.maximumRunDurationSeconds = 55f;
            assets.hillAdventurer.fuel.referenceRunDurationSeconds = 55f;
            assets.hillAdventurer.skins = new[]
            {
                Customization("hill-adventurer-skin-default", "DEFAULT", assets.hillAdventurerSkin != null ? assets.hillAdventurerSkin : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Vehicles/Hill Adventurer 4x4/Skins/Default.png"), true, 0, 0f, new Vector2(21.72f, 6.91f), Vector2.zero),
                Customization("hill-adventurer-skin-purple-4x4", "PURPLE 4x4",
                    assets.hillAdventurerPurpleSkin, false, 1000, 0f,
                    new Vector2(21.34f, 6.44f), new Vector2(0f, .355f))
            };
            assets.hillAdventurer.wheels = new[]
            {
                Customization("hill-adventurer-wheel-default", "DEFAULT WHEEL", assets.hillAdventurerWheel != null ? assets.hillAdventurerWheel : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Vehicles/Hill Adventurer 4x4/Wheels/Default.png"), true, 0, 0f, new Vector2(7.45f, 7.57f), Vector2.zero)
            };
            assets.hillAdventurer.visualWidth = 6.20f;
            assets.hillAdventurer.flipBodyX = false;
            assets.hillAdventurer.bodyVisualOffset = new Vector2(0f, .35f);
            if (!assets.hillAdventurer.preserveWheelVisualPositions)
            {
                assets.hillAdventurer.rearWheelVisualPosition = new Vector2(-1.88f, -.65f);
                assets.hillAdventurer.frontWheelVisualPosition = new Vector2(2.10f, -.65f);
            }
            assets.hillAdventurer.wheelVisualDiameter = 1.38f;
            if (!assets.hillAdventurer.preserveDriverPose || assets.hillAdventurer.driverPose == null)
                assets.hillAdventurer.driverPose = CreateHillAdventurerDriverPose();
            EditorUtility.SetDirty(assets.hillAdventurer);

            assets.stickmanBlack = LoadOrCreate<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset");
            assets.stickmanBlack.driverId = "stickman-black";
            assets.stickmanBlack.displayName = "STICKMAN BLACK";
            assets.stickmanBlack.unlockedByDefault = true;
            assets.stickmanBlack.bodyMaterial = null;
            assets.stickmanBlack.previewSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DriverPreviewPath);
            assets.stickmanBlack.head = AssetDatabase.LoadAssetAtPath<Sprite>(DriverHeadPath);
            assets.stickmanBlack.torso = AssetDatabase.LoadAssetAtPath<Sprite>(DriverTorsoPath);
            assets.stickmanBlack.upperArm = AssetDatabase.LoadAssetAtPath<Sprite>(DriverUpperArmPath);
            assets.stickmanBlack.forearm = AssetDatabase.LoadAssetAtPath<Sprite>(DriverForearmPath);
            assets.stickmanBlack.thigh = AssetDatabase.LoadAssetAtPath<Sprite>(DriverThighPath);
            assets.stickmanBlack.lowerLeg = AssetDatabase.LoadAssetAtPath<Sprite>(DriverLowerLegPath);
            assets.stickmanBlack.accessories = new[]
            {
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "default", displayName = "DEFAULT / NO ACCESSORY",
                    sprite = null, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .72f, allowsBlueprintProgression = false
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "glasses", displayName = "GLASSES",
                    sprite = assets.driverGlasses, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .50f, anchorOffset = new Vector2(.28f, 0f),
                    allowsBlueprintProgression = true
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "fire-glasses", displayName = "FIRE GLASSES",
                    sprite = assets.driverFireGlasses, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .52f, anchorOffset = new Vector2(.28f, 0f),
                    allowsBlueprintProgression = true
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "scarf", displayName = "SCARF",
                    sprite = assets.driverScarf, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .68f, anchorOffset = new Vector2(.04f, -.04f),
                    allowsBlueprintProgression = true
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "santa-hat", displayName = "SANTA HAT",
                    sprite = assets.driverSantaHat, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .78f, anchorOffset = new Vector2(0f, .22f),
                    allowsBlueprintProgression = false
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "neon-glasses", displayName = "NEON GLASSES",
                    sprite = assets.driverNeonGlasses, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .56f, anchorOffset = new Vector2(.28f, 0f),
                    allowsBlueprintProgression = false
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "tire-accessory", displayName = "TIRE",
                    sprite = assets.driverTireAccessory, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .62f, anchorOffset = new Vector2(.20f, -.05f),
                    allowsBlueprintProgression = false
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "baseball-cap", displayName = "CAP - FORWARD / BACKWARD",
                    sprite = assets.driverBaseballCap, unlockedByDefault = true, gemPrice = 0,
                    visibleWidth = .78f, anchorOffset = new Vector2(.18f, .18f),
                    supportsFlipVariant = true,
                    allowsBlueprintProgression = false
                },
                new DriverDefinition.CharacterAccessoryOption
                {
                    accessoryId = "baseball-cap-rainbow", displayName = "CAP - NEON LIGHTNING",
                    sprite = assets.driverBaseballCap, unlockedByDefault = false, gemPrice = 0,
                    visibleWidth = .78f, anchorOffset = new Vector2(.18f, .18f),
                    animateRainbow = true, rainbowSpeed = 2.6f, rainbowFrequency = 1.15f,
                    supportsFlipVariant = true, rainbowNeonGlow = 1.6f,
                    allowsBlueprintProgression = false
                }
            };
            EditorUtility.SetDirty(assets.stickmanBlack);

            assets.stickmanWhite = LoadOrCreate<DriverDefinition>(Root + "/VehicleData/StickmanWhite.asset");
            assets.stickmanWhite.driverId = "stickman-white";
            assets.stickmanWhite.displayName = "STICKMAN WHITE";
            assets.stickmanWhite.unlockedByDefault = true;
            assets.stickmanWhite.poseReference = assets.stickmanBlack;
            assets.stickmanWhite.bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                Root + "/Materials/StickmanWhite.mat");
            assets.stickmanWhite.previewSprite = assets.stickmanBlack.previewSprite;
            assets.stickmanWhite.head = assets.stickmanBlack.head;
            assets.stickmanWhite.torso = assets.stickmanBlack.torso;
            assets.stickmanWhite.upperArm = assets.stickmanBlack.upperArm;
            assets.stickmanWhite.forearm = assets.stickmanBlack.forearm;
            assets.stickmanWhite.thigh = assets.stickmanBlack.thigh;
            assets.stickmanWhite.lowerLeg = assets.stickmanBlack.lowerLeg;
            assets.stickmanWhite.accessories = assets.stickmanBlack.accessories;
            EditorUtility.SetDirty(assets.stickmanWhite);

            CreateVehiclePrefabs(assets);

            assets.countrysideCoinRoute = LoadOrCreate<CoinRouteDefinition>(
                Root + "/MapProfiles/CountrysideCoinRoute.asset");
            assets.countrysideCoinRoute.routeSeed = 9137;
            assets.countrysideCoinRoute.cellLength = 48f;
            assets.countrysideCoinRoute.firstCoinDistance = 20f;
            assets.countrysideCoinRoute.anchorJitter = 5f;
            assets.countrysideCoinRoute.surfaceOffset = 1.05f;
            assets.countrysideCoinRoute.rowVerticalSpacing = .9f;
            assets.countrysideCoinRoute.coinHorizontalSpacing = 1.75f;
            assets.countrysideCoinRoute.sparseEndDistance = 300f;
            assets.countrysideCoinRoute.growingEndDistance = 600f;
            assets.countrysideCoinRoute.variedEndDistance = 1200f;
            assets.countrysideCoinRoute.allFiveStartDistance = 10000f;
            EditorUtility.SetDirty(assets.countrysideCoinRoute);

            assets.countryside = LoadOrCreate<MapProfile>(Root + "/MapProfiles/Countryside.asset");
            assets.countryside.mapId = "countryside";
            assets.countryside.displayName = "COUNTRYSIDE";
            assets.countryside.supportedGameMode = GameModeType.Adventure2_5D;
            assets.countryside.seed = 18472;
            assets.countryside.unlockedByDefault = true;
            assets.countryside.coinPrice = 0;
            assets.countryside.coinRoute = assets.countrysideCoinRoute;
            assets.countryside.amplitude = 20f;
            assets.countryside.frequency = 0.019f;
            assets.countryside.introductoryAmplitude = 0.12f;
            assets.countryside.introEndDistance = 750f;
            assets.countryside.easyEndDistance = 3000f;
            assets.countryside.normalEndDistance = 6000f;
            assets.countryside.hardEndDistance = 10000f;
            assets.countryside.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.25f, 0.22f), new Keyframe(0.60f, 0.55f),
                new Keyframe(0.80f, 0.82f), new Keyframe(1f, 1f));
            assets.countryside.maximumDifficulty = 1f;
            assets.countryside.sectionLength = 52f;
            assets.countryside.technicalSectionChance = 0.48f;
            assets.countryside.technicalDetailStrength = 0.78f;
            assets.countryside.flatFeatureWeight = 0.03f;
            assets.countryside.hillFeatureWeight = 0.49f;
            assets.countryside.valleyFeatureWeight = 0.29f;
            assets.countryside.technicalFeatureWeight = 0.19f;
            assets.countryside.usePointedTerrainRhythm = true;
            assets.countryside.pointedMiniFeatureStrength = .11f;
            assets.countryside.pointedFeatureAsymmetry = .26f;
            assets.countryside.pointedPeakMiniFeatureCount = 10;
            assets.countryside.pointedExtremeShapeChance = .72f;
            assets.countryside.pointedExtremeShapeStrength = .42f;
            assets.countryside.useCountrysideSpecialFeatures = true;
            assets.countryside.countrysideSpecialFeatures = new CountrysideSpecialFeatureSettings
            {
                eventInterval = 500f,
                firstEventDistance = 260f,
                eventChance = .88f,
                longFlatChance = .58f,
                transitionLength = 45f,
                longFlatLengths = new Vector2(150f, 200f),
                slashFlatLead = 18f,
                slashLength = 96f,
                slashHeight = 18f,
                maximumStartJitter = 70f
            };
            assets.countryside.maxSlopeAngle = 38f;
            assets.countryside.maxHeightDeltaPerSample = Mathf.Tan(38f * Mathf.Deg2Rad) * (80f / 48f);
            assets.countryside.minimumValleyWidth = 20f;
            assets.countryside.minimumLandingLength = 16f;
            assets.countryside.maximumCrestSharpness = 14f;
            assets.countryside.smoothingPasses = 0;
            assets.countryside.initialFuelGap = 5f;
            assets.countryside.fuelGapIncrease = 5f;
            assets.countryside.terrainMaterial = assets.countrysideMaterial;
            assets.countryside.terrainEdgeMaterial = assets.countrysideEdgeMaterial;
            assets.countryside.useSeparateTerrainMaterials = true;
            assets.countryside.fitAuthoredTerrainTexture = true;
            assets.countryside.terrainTextureRepeatWidth = TerrainGenerator.CountrysideGroundModuleWidth;
            assets.countryside.terrainWallUvRange = new Vector2(0f, 1f);
            // Repeat only the straight interior of the long authored platform. The mesh supplies
            // Countryside's hills, so the flat yellow contact band needs no painted height samples.
            assets.countryside.terrainSurfaceUvRange = new Vector2(.32f, .676f);
            assets.countryside.terrainSurfaceContactV = .555f;
            assets.countryside.terrainSurfaceContactVSamples = new float[0];
            assets.countryside.terrainFillStartV = .555f;
            assets.countryside.groundPhysicsMaterial = assets.groundPhysics;
            assets.countryside.terrainEdgeWidth = 0.18f;
            assets.countryside.terrainMaximumVisualExtent =
                TerrainGenerator.CountrysideGroundMaximumVisualExtent;
            assets.countryside.terrainMaximumVisualRiseAboveSurface =
                TerrainGenerator.CountrysideGroundMaximumVisualRise;
            assets.countryside.terrainVisualDepth = 20f;
            assets.countryside.supportsLiquidExposure = false;
            assets.countrysideEnvironment = LoadOrCreate<EnvironmentDefinition>(Root + "/MapProfiles/CountrysideEnvironment.asset");
            assets.countrysideEnvironment.environmentId = "countryside";
            assets.countrysideEnvironment.sky = AssetDatabase.LoadAssetAtPath<Sprite>(CountrysideSkyPath);
            assets.countrysideEnvironment.mountains = AssetDatabase.LoadAssetAtPath<Sprite>(CountrysideMountainsPath);
            assets.countrysideEnvironment.sun = null;
            assets.countrysideEnvironment.mountainParallaxX = .18f;
            assets.countrysideEnvironment.mountainParallaxY = .04f;
            assets.countrysideEnvironment.lockVerticalToCamera = true;
            assets.countrysideEnvironment.mountainVisualHeight = 19f;
            assets.countrysideEnvironment.mountainBaseY = 1f;
            assets.countrysideEnvironment.pooledMountainSegments = 5;
            assets.countrysideEnvironment.seamOverlap = .004f;
            assets.countrysideEnvironment.mirrorAlternatingMountainTiles = true;
            assets.countryside.environment = assets.countrysideEnvironment;
            EditorUtility.SetDirty(assets.countrysideEnvironment);

            // North Pole deliberately shares Countryside's proven chunk/collider/pickup architecture.
            // Only its authored visuals and independent terrain profile are more aggressive.
            assets.northPoleEnvironment = LoadOrCreate<EnvironmentDefinition>(
                Root + "/MapProfiles/NorthPoleEnvironment.asset");
            assets.northPoleEnvironment.environmentId = "north-pole";
            assets.northPoleEnvironment.sky = assets.northPoleSky;
            assets.northPoleEnvironment.mountains = assets.northPolePanorama;
            assets.northPoleEnvironment.clouds = new Sprite[0];
            assets.northPoleEnvironment.mountainParallaxX = .04f;
            assets.northPoleEnvironment.mountainParallaxY = 0f;
            assets.northPoleEnvironment.useRelativeParallaxSpeeds = true;
            assets.northPoleEnvironment.lockVerticalToCamera = true;
            assets.northPoleEnvironment.coverViewportVertically = true;
            assets.northPoleEnvironment.coverViewportVerticalOffset = 2.6f;
            assets.northPoleEnvironment.mountainVisualHeight = 24.5f;
            assets.northPoleEnvironment.mountainBaseY = 0f;
            assets.northPoleEnvironment.pooledMountainSegments = 5;
            assets.northPoleEnvironment.seamOverlap = .008f;
            assets.northPoleEnvironment.mirrorAlternatingMountainTiles = true;
            assets.northPoleEnvironment.nearMountains = assets.northPoleNearPines;
            assets.northPoleEnvironment.nearMountainParallaxX = .11f;
            assets.northPoleEnvironment.nearMountainParallaxY = 0f;
            assets.northPoleEnvironment.lockNearMountainsVerticalToCamera = true;
            assets.northPoleEnvironment.nearMountainVisualHeight = 23f;
            assets.northPoleEnvironment.nearMountainBaseY = 5.3f;
            assets.northPoleEnvironment.pooledNearMountainSegments = 5;
            assets.northPoleEnvironment.nearMountainSeamOverlap = .008f;
            assets.northPoleEnvironment.mirrorAlternatingNearMountainTiles = true;
            EditorUtility.SetDirty(assets.northPoleEnvironment);

            assets.northPole = LoadOrCreate<MapProfile>(Root + "/MapProfiles/NorthPole.asset");
            assets.northPole.mapId = "north-pole";
            assets.northPole.displayName = "NORTH POLE";
            assets.northPole.supportedGameMode = GameModeType.Adventure2_5D;
            assets.northPole.seed = 87231;
            assets.northPole.unlockedByDefault = false;
            assets.northPole.coinPrice = 50000;
            assets.northPole.coinRoute = assets.countrysideCoinRoute;
            assets.northPole.baseHeight = 0f;
            assets.northPole.amplitude = 38f;
            assets.northPole.frequency = .012f;
            assets.northPole.smoothing = .72f;
            assets.northPole.hillSize = 1.2f;
            assets.northPole.introductoryAmplitude = .20f;
            assets.northPole.introEndDistance = 750f;
            assets.northPole.easyEndDistance = 3000f;
            assets.northPole.normalEndDistance = 6000f;
            assets.northPole.hardEndDistance = 15000f;
            assets.northPole.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, .04f), new Keyframe(.25f, .30f), new Keyframe(.60f, .64f),
                new Keyframe(.80f, .86f), new Keyframe(1f, 1f));
            assets.northPole.maximumDifficulty = 1f;
            assets.northPole.sectionLength = 80f;
            assets.northPole.recoveryEverySections = 4;
            assets.northPole.technicalSectionChance = .52f;
            assets.northPole.technicalDetailStrength = .82f;
            assets.northPole.flatFeatureWeight = .06f;
            assets.northPole.hillFeatureWeight = .46f;
            assets.northPole.valleyFeatureWeight = .32f;
            assets.northPole.technicalFeatureWeight = .16f;
            assets.northPole.maxSlopeAngle = 40f;
            assets.northPole.maxHeightDeltaPerSample = Mathf.Tan(40f * Mathf.Deg2Rad) * (80f / 48f);
            assets.northPole.minimumValleyWidth = 30f;
            assets.northPole.minimumLandingLength = 22f;
            assets.northPole.maximumCrestSharpness = 5.5f;
            assets.northPole.smoothingPasses = 2;
            assets.northPole.initialFuelGap = 5f;
            assets.northPole.fuelGapIncrease = 5f;
            assets.northPole.fuelPlacementSearchRadius = 36f;
            assets.northPole.fuelPlacementSearchStep = 2f;
            assets.northPole.fuelMaximumPlacementSlope = 14f;
            assets.northPole.environment = assets.northPoleEnvironment;
            assets.northPole.terrainMaterial = assets.northPoleMaterial;
            assets.northPole.terrainEdgeMaterial = assets.northPoleEdgeMaterial;
            assets.northPole.useSeparateTerrainMaterials = true;
            assets.northPole.fitAuthoredTerrainTexture = true;
            // Ground.png contains one 1,100 px authored long tile followed by optional short pieces.
            // Repeat only the long tile at its native 100 PPU scale.
            assets.northPole.terrainTextureRepeatWidth = 11f;
            assets.northPole.terrainWallUvRange = new Vector2(0f, 1f);
            // Preserve the entire vertical artwork of the requested snow platform. Its central blue line
            // is the physical contact row, exactly like City's yellow line.
            assets.northPole.terrainSurfaceUvRange = new Vector2(.36f, .63f);
            assets.northPole.terrainSurfaceContactV = .519f;
            assets.northPole.terrainFillStartV = .519f;
            assets.northPole.groundPhysicsMaterial = assets.groundPhysics;
            assets.northPole.terrainEdgeWidth = .18f;
            assets.northPole.terrainVisualDepth = 30f;
            assets.northPole.supportsLiquidExposure = false;
            EditorUtility.SetDirty(assets.northPole);

            assets.northPoleElevation = LoadOrCreate<TerrainElevationProfile>(
                Root + "/MapProfiles/NorthPoleElevation.asset");
            assets.northPoleElevation.AssignSource(assets.northPole);
            assets.northPole.elevationProfile = assets.northPoleElevation;
            EditorUtility.SetDirty(assets.northPoleElevation);
            EditorUtility.SetDirty(assets.northPole);

            assets.cityEnvironment = LoadOrCreate<EnvironmentDefinition>(Root + "/MapProfiles/CityEnvironment.asset");
            assets.cityEnvironment.environmentId = "city";
            assets.cityEnvironment.sky = assets.citySky;
            assets.cityEnvironment.mountains = assets.cityBackground;
            assets.cityEnvironment.clouds = new Sprite[0];
            assets.cityEnvironment.pooledClouds = 0;
            assets.cityEnvironment.mountainParallaxX = .18f;
            assets.cityEnvironment.mountainParallaxY = .04f;
            assets.cityEnvironment.lockVerticalToCamera = true;
            assets.cityEnvironment.mountainVisualHeight = 15.5f;
            assets.cityEnvironment.mountainBaseY = 4.2f;
            assets.cityEnvironment.pooledMountainSegments = 5;
            assets.cityEnvironment.seamOverlap = .016f;
            assets.cityEnvironment.mirrorAlternatingMountainTiles = false;
            EditorUtility.SetDirty(assets.cityEnvironment);

            assets.city = LoadOrCreate<MapProfile>(Root + "/MapProfiles/City.asset");
            assets.city.mapId = "city";
            assets.city.displayName = "CITY";
            assets.city.supportedGameMode = GameModeType.Adventure2_5D;
            assets.city.seed = 4152026;
            assets.city.unlockedByDefault = false;
            assets.city.coinPrice = 10000;
            assets.city.coinRoute = assets.countrysideCoinRoute;
            assets.city.elevationProfile = null;
            assets.city.baseHeight = 0f;
            assets.city.amplitude = 21.5f;
            assets.city.frequency = .012f;
            assets.city.smoothing = .72f;
            assets.city.hillSize = 1.15f;
            assets.city.useCityHighwayTerrain = true;
            assets.city.cityFeatureLength = 48f;
            assets.city.cityMiniRhythmStrength = .22f;
            assets.city.useCityBridges = true;
            assets.city.cityBridges ??= new CityBridgeSettings();
            CityBridgeSettings cityBridges = assets.city.cityBridges;
            cityBridges.cycleLength = CityBridgeSettings.RequiredCycleLength;
            cityBridges.minimumZonesPerCycle = CityBridgeSettings.RequiredMinimumZones;
            cityBridges.maximumZonesPerCycle = CityBridgeSettings.RequiredMaximumZones;
            cityBridges.minimumProgressiveZones = CityBridgeSettings.RequiredMinimumProgressiveZones;
            cityBridges.maximumProgressiveZones = CityBridgeSettings.RequiredMaximumProgressiveZones;
            cityBridges.minimumBoatZonesPerCycle = CityBridgeSettings.RequiredMinimumBoatZones;
            cityBridges.maximumBoatZonesPerCycle = CityBridgeSettings.RequiredMaximumBoatZones;
            cityBridges.cycleStartMargin = 320f;
            cityBridges.cycleEndMargin = 320f;
            cityBridges.minimumZoneSpacing = 300f;
            cityBridges.approachBlendLength = 50f;
            cityBridges.recoveryBlendLength = 50f;
            cityBridges.crestApproachChance = .36f;
            cityBridges.crestApproachRise = 3.2f;
            cityBridges.bridgeLength = 14f;
            cityBridges.normalWaterLength = 22f;
            cityBridges.progressiveBaseWaterLength = 22f;
            cityBridges.progressiveGrowthPerCycle = CityBridgeSettings.RequiredProgressiveGrowth;
            cityBridges.waterVisualDepth = 10f;
            cityBridges.waterSurfaceOffset = -.45f;
            cityBridges.waterVisualAlpha = .5f;
            cityBridges.waterBedThickness = 22f;
            cityBridges.waterEndBankWidth = 3f;
            cityBridges.waterSpeedRetainedPerSecond = .4f;
            cityBridges.waterAngularSpeedRetainedPerSecond = .3f;
            cityBridges.waterBuoyancySpring = 4f;
            cityBridges.waterVerticalDamping = 3f;
            cityBridges.waterMaximumSinkAcceleration = 8f;
            cityBridges.waterDriverDownClearance = 5f;
            cityBridges.bridgeColliderThickness = .45f;
            cityBridges.bridgeGroundOverlap = 1f;
            cityBridges.exitRampLength = 14f;
            cityBridges.exitRampRise = 1.8f;
            cityBridges.closedAngleDegrees = 0f;
            cityBridges.openAngleDegrees = CityBridgeSettings.RequiredBridgeAngle;
            cityBridges.transitionSeconds = CityBridgeSettings.RequiredTransitionSeconds;
            cityBridges.closedHoldSeconds = 0f;
            cityBridges.openHoldSeconds = 0f;
            cityBridges.minimumBoatsPerZone = 1;
            cityBridges.maximumBoatsPerZone = 3;
            cityBridges.minimumBoatWidth = 5.2f;
            cityBridges.maximumBoatWidth = 9.5f;
            cityBridges.boatColliderThickness = .62f;
            cityBridges.boatDeckHeightFraction = .18f;
            cityBridges.boatMass = 420f;
            cityBridges.boatVerticalSpring = 24f;
            cityBridges.boatVerticalDamping = 8f;
            cityBridges.boatCenteringTorque = 2.4f;
            cityBridges.boatAngularDamping = 1.1f;
            cityBridges.boatMaximumTilt = 10f;
            cityBridges.boatMaximumSink = .1f;
            cityBridges.boatMaximumRise = .1f;
            cityBridges.boatLaunchImpulse = 2f;
            cityBridges.boatSprites = assets.cityBoats;
            cityBridges.bridgeSprite = assets.cityBridge;
            cityBridges.waterSprite = assets.cityWater;
            cityBridges.bridgeArtSlopeDegrees = 16.1f;
            cityBridges.bridgeArtSurfaceOffset = -.61f;
            cityBridges.waterSortingOrder = 1;
            cityBridges.waterBedSortingOrder = 0;
            cityBridges.bridgeSortingOrder = 5;
            assets.city.introductoryAmplitude = .34f;
            assets.city.introEndDistance = 600f;
            assets.city.easyEndDistance = 3000f;
            assets.city.normalEndDistance = 6500f;
            assets.city.hardEndDistance = 10000f;
            assets.city.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(.25f, .20f),
                new Keyframe(.60f, .58f), new Keyframe(1f, 1f));
            assets.city.maximumDifficulty = 1f;
            assets.city.sectionLength = 72f;
            assets.city.recoveryEverySections = 5;
            assets.city.technicalSectionChance = .42f;
            assets.city.technicalDetailStrength = .72f;
            assets.city.flatFeatureWeight = .02f;
            assets.city.hillFeatureWeight = .54f;
            assets.city.valleyFeatureWeight = .27f;
            assets.city.technicalFeatureWeight = .17f;
            assets.city.maxSlopeAngle = 36f;
            assets.city.maxHeightDeltaPerSample = Mathf.Tan(36f * Mathf.Deg2Rad) * (80f / 48f);
            assets.city.minimumValleyWidth = 18f;
            assets.city.minimumLandingLength = 16f;
            assets.city.maximumCrestSharpness = 7f;
            assets.city.smoothingPasses = 2;
            assets.city.initialFuelGap = 5f;
            assets.city.fuelGapIncrease = 5f;
            assets.city.fuelPlacementSearchRadius = 24f;
            assets.city.fuelPlacementSearchStep = 2f;
            assets.city.fuelMaximumPlacementSlope = 16f;
            assets.city.environment = assets.cityEnvironment;
            assets.city.terrainMaterial = assets.cityTerrainMaterial;
            assets.city.terrainEdgeMaterial = assets.cityGroundMaterial;
            assets.city.useSeparateTerrainMaterials = true;
            assets.city.fitAuthoredTerrainTexture = true;
            assets.city.terrainTextureRepeatWidth = TerrainGenerator.CompactGroundModuleWidth;
            assets.city.terrainWallUvRange = new Vector2(.171f, .82f);
            // Preserve the complete visible long road tile from Ground.png. The yellow stripe is the
            // physical contact line; these values must match CityMapContentInstaller.
            assets.city.terrainSurfaceUvRange = new Vector2(.313f, .701f);
            assets.city.terrainSurfaceContactV = .579f;
            assets.city.terrainFillStartV = .579f;
            assets.city.groundPhysicsMaterial = assets.groundPhysics;
            assets.city.terrainEdgeWidth = .18f;
            assets.city.terrainMaximumVisualExtent =
                TerrainGenerator.CompactGroundMaximumVisualExtent;
            assets.city.terrainVisualDepth = 22f;
            assets.city.supportsLiquidExposure = false;
            EditorUtility.SetDirty(assets.city);

            assets.neonCityEnvironment = LoadOrCreate<EnvironmentDefinition>(
                Root + "/MapProfiles/NeonCityEnvironment.asset");
            assets.neonCityEnvironment.environmentId = "neon-city";
            assets.neonCityEnvironment.sky = assets.neonSky;
            assets.neonCityEnvironment.mountains = assets.neonBackground;
            assets.neonCityEnvironment.mountainParallaxX = .18f;
            assets.neonCityEnvironment.mountainParallaxY = .035f;
            assets.neonCityEnvironment.lockVerticalToCamera = true;
            assets.neonCityEnvironment.mountainVisualHeight = 15.5f;
            assets.neonCityEnvironment.mountainBaseY = 2f;
            assets.neonCityEnvironment.pooledMountainSegments = 5;
            assets.neonCityEnvironment.seamOverlap = .018f;
            assets.neonCityEnvironment.mirrorAlternatingMountainTiles = false;
            EditorUtility.SetDirty(assets.neonCityEnvironment);

            assets.neonCity = LoadOrCreate<MapProfile>(Root + "/MapProfiles/NeonCity.asset");
            assets.neonCity.mapId = "neon-city";
            assets.neonCity.displayName = "NEON CITY";
            assets.neonCity.supportedGameMode = GameModeType.Adventure2_5D;
            assets.neonCity.seed = 620031;
            assets.neonCity.unlockedByDefault = false;
            assets.neonCity.coinPrice = 25000;
            assets.neonCity.coinRoute = assets.countrysideCoinRoute;
            assets.neonCity.elevationProfile = null;
            assets.neonCity.baseHeight = 0f;
            assets.neonCity.amplitude = 46f;
            assets.neonCity.neonMaximumAmplitude = 58f;
            assets.neonCity.neonAmplitudeSaturationCycles = 2f;
            assets.neonCity.frequency = .0105f;
            assets.neonCity.introductoryAmplitude = .45f;
            assets.neonCity.introEndDistance = 750f;
            assets.neonCity.easyEndDistance = 9000f;
            assets.neonCity.normalEndDistance = 18000f;
            assets.neonCity.hardEndDistance = 36000f;
            assets.neonCity.maximumDifficulty = 1f;
            assets.neonCity.sectionLength = 70f;
            assets.neonCity.recoveryEverySections = 4;
            assets.neonCity.technicalSectionChance = .58f;
            assets.neonCity.technicalDetailStrength = .85f;
            assets.neonCity.flatFeatureWeight = .03f;
            assets.neonCity.hillFeatureWeight = .54f;
            assets.neonCity.valleyFeatureWeight = .25f;
            assets.neonCity.technicalFeatureWeight = .18f;
            assets.neonCity.maxSlopeAngle = 50f;
            assets.neonCity.maxHeightDeltaPerSample = Mathf.Tan(50f * Mathf.Deg2Rad) * (80f / 48f);
            assets.neonCity.minimumValleyWidth = 24f;
            assets.neonCity.minimumLandingLength = 26f;
            assets.neonCity.maximumCrestSharpness = 7f;
            assets.neonCity.smoothingPasses = 2;
            assets.neonCity.initialFuelGap = 55f;
            assets.neonCity.fuelGapIncrease = 10f;
            assets.neonCity.fuelPlacementSearchRadius = 28f;
            assets.neonCity.fuelPlacementSearchStep = 2f;
            assets.neonCity.fuelMaximumPlacementSlope = 14f;
            assets.neonCity.environment = assets.neonCityEnvironment;
            assets.neonCity.terrainMaterial = assets.neonCityMaterial;
            assets.neonCity.terrainEdgeMaterial = assets.neonCityEdgeMaterial;
            assets.neonCity.useSeparateTerrainMaterials = true;
            assets.neonCity.fitAuthoredTerrainTexture = true;
            assets.neonCity.terrainTextureRepeatWidth = TerrainGenerator.CompactGroundModuleWidth;
            assets.neonCity.terrainWallUvRange = new Vector2(0f, 1f);
            assets.neonCity.terrainSurfaceUvRange = new Vector2(.313f, .83f);
            assets.neonCity.terrainSurfaceContactV = .579f;
            assets.neonCity.terrainSurfaceContactVSamples = new float[0];
            // Match City/North Pole: overlap the deep Terrain wall behind the complete Ground
            // ribbon from the wheel-contact line, rather than joining two irregular transparent edges.
            assets.neonCity.terrainFillStartV = assets.neonCity.terrainSurfaceContactV;
            assets.neonCity.groundPhysicsMaterial = assets.groundPhysics;
            assets.neonCity.terrainEdgeWidth = .18f;
            assets.neonCity.terrainMaximumVisualExtent =
                TerrainGenerator.NeonCityGroundMaximumVisualExtent;
            assets.neonCity.terrainMaximumVisualRiseAboveSurface =
                TerrainGenerator.NeonCityGroundMaximumVisualRise;
            assets.neonCity.terrainVisualDepth = 22f;
            assets.neonCity.supportsLiquidExposure = true;
            assets.neonCity.liquidChallenges = new LiquidChallengeSettings
            {
                cycleLength = 6000f,
                challengesPerCycle = 8,
                cycleStartSafetyMargin = 260f,
                structuralRunUpPositions = LiquidChallengeSettings.DefaultStructuralRunUpPositions(),
                runUpLength = 42f,
                rampTransitionLength = 6f,
                rampHorizontalLength = 12f,
                rampAngleDegrees = 22f,
                landingLength = 26f,
                recoveryLength = 48f,
                postRecoveryBlendLength = 50f,
                basePoolLength = 21f,
                minimumPoolLength = 18f,
                maximumPoolLength = 34f,
                cycleGrowth = 1f,
                growthSaturation = 2.5f,
                deterministicLengthVariation = .12f,
                visualDepth = 7f,
                surfaceOffset = .15f,
                cautionSprite = assets.neonCaution,
                cautionDistanceBeforeRamp = 28f,
                cautionBelowSurface = 1.65f,
                cautionWidth = 3.2f,
                cautionSortingOrder = 4,
                liquidSprite = assets.neonLiquid,
                liquidSortingOrder = 3,
                normalAlpha = 1f,
                submergedAlpha = .65f,
                alphaLerpSpeed = 7f,
                exposureLimitSeconds = 10f,
                entrySpeedRemaining = .10f,
                entrySlowdownSeconds = .35f,
                continuousViscosity = 2.4f,
                liquidDriveAcceleration = 9f,
                maximumSinkAcceleration = 8f,
                exitRampHorizontalLength = 12f
            };
            EditorUtility.SetDirty(assets.neonCity);
            assets.elevation = LoadOrCreate<TerrainElevationProfile>(Root + "/MapProfiles/CountrysideElevation.asset");
            assets.elevation.AssignSource(assets.countryside);
            assets.countryside.elevationProfile = assets.elevation;
            EditorUtility.SetDirty(assets.elevation);
            EditorUtility.SetDirty(assets.countryside);

            assets.curvature = LoadOrCreate<RoadCurvatureProfile>(Root + "/MapProfiles/CountrysideCurvature.asset");
            assets.curvature.seed = 18472; assets.curvature.intensity = 10f; assets.curvature.frequency = 0.006f;
            EditorUtility.SetDirty(assets.curvature);
            assets.countrysideRoad = LoadOrCreate<RoadMapProfile>(Root + "/MapProfiles/CountrysideRoad.asset");
            assets.countrysideRoad.mapId = "countryside";
            assets.countrysideRoad.displayName = "COUNTRYSIDE";
            assets.countrysideRoad.supportedGameMode = GameModeType.Adventure3D;
            assets.countrysideRoad.seed = 18472;
            assets.countrysideRoad.coinRoute = assets.countrysideCoinRoute;
            assets.countrysideRoad.roadWidth = 12f;
            assets.countrysideRoad.curveIntensity = 10f;
            assets.countrysideRoad.elevationIntensity = 8.5f;
            assets.countrysideRoad.introEndDistance = 225f;
            assets.countrysideRoad.easyEndDistance = 750f;
            assets.countrysideRoad.normalEndDistance = 1800f;
            assets.countrysideRoad.hardEndDistance = 3750f;
            assets.countrysideRoad.difficultyCurve = new AnimationCurve(
                new Keyframe(0f, 0.15f), new Keyframe(0.2f, 0.3f),
                new Keyframe(0.5f, 0.65f), new Keyframe(1f, 1f));
            assets.countrysideRoad.initialFuelGap = 5f;
            assets.countrysideRoad.fuelGapIncrease = 5f;
            assets.countrysideRoad.elevationProfile = assets.elevation;
            assets.countrysideRoad.curvatureProfile = assets.curvature;
            assets.countrysideRoad.roadMaterial = assets.road;
            assets.countrysideRoad.shoulderMaterial = assets.grass;
            assets.countrysideRoad.groundPhysicsMaterial = assets.groundPhysics;
            EditorUtility.SetDirty(assets.countrysideRoad);

            assets.testCircuit = LoadOrCreate<CircuitDefinition>(Root + "/MapProfiles/ProceduralTestCircuit.asset");
            assets.testCircuit.mapId = "procedural-test-circuit"; assets.testCircuit.displayName = "PROCEDURAL TEST CIRCUIT";
            assets.testCircuit.supportedGameMode = GameModeType.TimeTrial3D; assets.testCircuit.seed = 12345;
            assets.testCircuit.laps = 1; assets.testCircuit.fuelEnabled = false;
            assets.testCircuit.difficulty = CircuitDifficulty.Normal; assets.testCircuit.respawnPenaltySeconds = 3f;
            assets.testCircuit.shortcutChance = 0.65f; assets.testCircuit.techChance = 0.18f;
            assets.testCircuit.roadMaterial = assets.road; assets.testCircuit.shortcutMaterial = assets.shortcut;
            EditorUtility.SetDirty(assets.testCircuit);

            assets.economy = LoadOrCreate<UpgradeEconomyDefinition>(Root + "/VehicleData/UpgradeEconomy.asset");
            assets.economy.jeepFinalUpgradeCost = 50000;
            assets.economy.formulaFinalUpgradeCost = 150000;
            assets.economy.rallyFinalUpgradeCost = 100000;
            assets.economy.sportBikeFinalUpgradeCost = 200000;
            assets.economy.jeepFirstUpgradeCost = 1000;
            assets.economy.formulaFirstUpgradeCost = 2500;
            assets.economy.rallyFirstUpgradeCost = 1800;
            assets.economy.sportBikeFirstUpgradeCost = 3000;
            assets.economy.costExponent = 1.45f;
            assets.economy.roundingStep = 250;
            EditorUtility.SetDirty(assets.economy);
            assets.chestCatalog = LoadOrCreate<ChestCatalog>(Root + "/VehicleData/ChestCatalog.asset");
            assets.chestCatalog.shelfSprite = assets.chestShelf;
            assets.chestCatalog.coinsChestSprite = assets.coinsChest;
            assets.chestCatalog.chests = new[]
            {
                Chest(ChestType.Bronze, "BRONZE CHEST", 90, 2, 1200, 1500, 1, 3, 0,
                    assets.bronzeChestClosed, assets.bronzeChestOpen, new Color(.70f, .38f, .15f)),
                Chest(ChestType.Silver, "SILVER CHEST", 70, 4, 2500, 3700, 2, 5, 0,
                    assets.silverChestClosed, assets.silverChestOpen, new Color(.75f, .84f, .94f)),
                Chest(ChestType.Gold, "GOLD CHEST", 50, 6, 5000, 7500, 6, 12, 0,
                    assets.goldChestClosed, assets.goldChestOpen, new Color(1f, .72f, .12f)),
                Chest(ChestType.Master, "MASTER CHEST", 10, 12, 15000, 20000, 20, 35, 5,
                    assets.masterChestClosed, assets.masterChestOpen, new Color(.62f, .20f, 1f)),
                Chest(ChestType.Legendary, "LEGENDARY CHEST", 1, 24, 30000, 50000, 50, 70, 10,
                    assets.legendaryChestClosed, assets.legendaryChestOpen, new Color(1f, .20f, .62f))
            };
            EditorUtility.SetDirty(assets.chestCatalog);
            assets.catalog = LoadOrCreate<GameCatalog>(Root + "/VehicleData/GameCatalog.asset");
            assets.space = AssetDatabase.LoadAssetAtPath<MapProfile>(SpaceMapPath);
            MapDefinition rainyer = AssetDatabase.LoadAssetAtPath<MapDefinition>(
                Root + "/MapProfiles/Rainyer.asset");
            MapDefinition mountains = AssetDatabase.LoadAssetAtPath<MapDefinition>(
                Root + "/MapProfiles/Mountains.asset");
            MapDefinition lastHorizon = AssetDatabase.LoadAssetAtPath<MapDefinition>(
                Root + "/MapProfiles/LastHorizon.asset");
            var maps = new System.Collections.Generic.List<MapDefinition>
            {
                assets.countryside, assets.northPole, assets.space, assets.neonCity, assets.city
            };
            if (rainyer != null) maps.Add(rainyer);
            if (mountains != null) maps.Add(mountains);
            if (lastHorizon != null) maps.Add(lastHorizon);
            maps.Add(assets.countrysideRoad);
            maps.Add(assets.testCircuit);
            assets.catalog.maps = maps.ToArray();
            VehicleDefinition buggy = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/BuggyVehicle.asset");
            VehicleDefinition aerialSatelite = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                Root + "/VehicleData/AerialSateliteVehicle.asset");
            var vehicles = new System.Collections.Generic.List<VehicleDefinition>
            {
                assets.jeep, assets.formula, assets.rally, assets.sportBike, assets.hillAdventurer
            };
            if (buggy != null) vehicles.Add(buggy);
            if (aerialSatelite != null) vehicles.Add(aerialSatelite);
            assets.catalog.vehicles = vehicles.ToArray();
            assets.catalog.SortPurchasableContent();
            assets.catalog.drivers = new[] { assets.stickmanBlack, assets.stickmanWhite };
            assets.catalog.upgradeEconomy = assets.economy;
            assets.catalog.chestCatalog = assets.chestCatalog;
            EditorUtility.SetDirty(assets.catalog);
            return assets;
        }

        private static ChestDefinition Chest(ChestType type, string name, int weight, int hours,
            int minimumCoins, int maximumCoins, int minimumGems, int maximumGems, int blueprints,
            Sprite closed, Sprite open, Color accent)
        {
            return new ChestDefinition
            {
                type = type, displayName = name, rarityWeight = weight, openingSeconds = hours * 60 * 60,
                minimumCoins = minimumCoins, maximumCoins = maximumCoins,
                minimumGems = minimumGems, maximumGems = maximumGems, blueprintReward = blueprints,
                closedSprite = closed, openSprite = open, accentColor = accent
            };
        }

        private static void BuildMainMenu(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.01f, .015f, .03f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            CreateAspectCoverBackground(canvas.transform, assets.mainMenuBackground);
            CreateImage("Readability Overlay", canvas.transform, new Color(.005f, .015f, .04f, .12f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            MainMenuUI menu = new GameObject("Main Menu Controller").AddComponent<MainMenuUI>();
            GameObject coinCard = CreateHudCard("Total Coins Card", safe, Vector2.one,
                new Vector2(-175f, -56f), new Vector2(300f, 76f));
            CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01, new Vector2(.5f, .5f),
                new Vector2(-112f, 0f), new Vector2(52f, 52f));
            Text coins = CreateText("Total Coins", coinCard.transform, "COINS  0", 27, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(-15f, 0f), new Vector2(-30f, 0f));
            GameObject gemCard = CreateHudCard("Total Gems Card", safe, Vector2.one,
                new Vector2(-175f, -134f), new Vector2(300f, 64f));
            Text gemIcon = CreateText("Gem Icon", gemCard.transform, "◆", 34, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(.28f, 1f), Vector2.zero, Vector2.zero);
            gemIcon.color = new Color(.18f, .92f, 1f);
            Text gems = CreateText("Total Gems", gemCard.transform, "GEMS  0", 25, TextAnchor.MiddleRight,
                new Vector2(.20f, 0f), Vector2.one, new Vector2(-15f, 0f), new Vector2(-30f, 0f));

            Button skins = CreateSpriteButton("Skins", safe, assets.skinsButton, new Vector2(0f, 1f),
                new Vector2(155f, -62f), new Vector2(260f, 98f));
            Button store = CreateSpriteButton("Store", safe, assets.storeButton, new Vector2(0f, 1f),
                new Vector2(430f, -62f), new Vector2(260f, 98f));
            Button settings = CreateSpriteButton("Settings", safe, assets.settingsButton, new Vector2(0f, 1f),
                new Vector2(705f, -62f), new Vector2(260f, 98f));

            Text vehicleName = CreateText("Selected Vehicle Name", safe, "JEEP", 42, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 178f), new Vector2(520f, 66f));
            vehicleName.fontStyle = FontStyle.Bold;
            RawImage previewImage = UIObject("Selected Vehicle Preview", safe, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(0f, -52f), new Vector2(790f, 390f)).AddComponent<RawImage>();
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;
            Button previous = CreateGrayNavButton("Previous Vehicle", safe, "<", new Vector2(-420f, -50f),
                new Vector2(72f, 85f));
            Button next = CreateGrayNavButton("Next Vehicle", safe, ">", new Vector2(420f, -50f),
                new Vector2(72f, 85f));

            // Quarter-width anchors keep the four footer actions separated on both 16:9 and 4:3 canvases.
            Button vehicle = CreateSpriteButton("Vehicle", safe, assets.vehicleButton, new Vector2(.125f, 0f),
                new Vector2(0f, 64f), new Vector2(350f, 150f));
            Button tuning = CreateSpriteButton("Tuning", safe, assets.tuningButton, new Vector2(.375f, 0f),
                new Vector2(0f, 64f), new Vector2(350f, 150f));
            Button upgrade = CreateSpriteButton("Upgrade", safe, assets.upgradeButton, new Vector2(.625f, 0f),
                new Vector2(0f, 64f), new Vector2(350f, 150f));
            Button race = CreateSpriteButton("Race", safe, assets.raceButton, new Vector2(.875f, 0f),
                new Vector2(0f, 70f), new Vector2(380f, 164f));

            Bind(skins, menu.Skins); Bind(store, menu.Store); Bind(settings, menu.Settings);
            Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);
            Bind(vehicle, menu.Vehicle); Bind(tuning, menu.Tuning); Bind(upgrade, menu.Upgrade); Bind(race, menu.Race);
            GaragePreviewRenderer preview = CreateGarageVehiclePreview(assets, previewImage, "Home");
            GameObject chestPanel = CreateImage("Chest Stand", safe, Color.clear,
                new Vector2(.005f, .12f), new Vector2(.285f, .88f), Vector2.zero, Vector2.zero);
            chestPanel.GetComponent<Image>().raycastTarget = false;
            CreateText("Chest Title", chestPanel.transform, "CHESTS", 30, TextAnchor.MiddleCenter,
                new Vector2(.28f, .93f), new Vector2(.72f, .995f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Image shelf = CreateImage("Chest Shelf", chestPanel.transform, Color.white,
                new Vector2(.02f, .12f), new Vector2(.98f, .94f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            shelf.sprite = assets.chestShelf; shelf.preserveAspect = true; shelf.raycastTarget = false;
            ChestPanelUI chestUi = chestPanel.AddComponent<ChestPanelUI>();
            var chestSlots = new Button[3]; var chestImages = new Image[3]; var chestLabels = new Text[3];
            UnityAction[] chestActions = { chestUi.SelectSlot0, chestUi.SelectSlot1, chestUi.SelectSlot2 };
            // Shelf.png contains three authored vertical platforms. These centers map directly to
            // the top, middle and bottom pedestals; no extra empty slot frames are drawn.
            float[] shelfSlotY = { 155f, -15f, -185f };
            for (int i = 0; i < chestSlots.Length; i++)
            {
                chestSlots[i] = CreateButton("Chest Slot " + (i + 1), chestPanel.transform, "",
                    new Vector2(0f, shelfSlotY[i]), new Vector2(250f, 172f));
                chestSlots[i].image.color = Color.clear;
                chestSlots[i].transition = Selectable.Transition.None;
                chestImages[i] = CreateImage("Chest", chestSlots[i].transform, Color.white,
                    new Vector2(.10f, .14f), new Vector2(.90f, .98f), Vector2.zero, Vector2.zero).GetComponent<Image>();
                chestImages[i].raycastTarget = false; chestImages[i].preserveAspect = true;
                chestImages[i].enabled = false;
                chestLabels[i] = CreateText("Status", chestSlots[i].transform, "", 13, TextAnchor.LowerCenter,
                    new Vector2(.02f, .01f), new Vector2(.98f, .18f), Vector2.zero, Vector2.zero);
                Outline labelOutline = chestLabels[i].gameObject.AddComponent<Outline>();
                labelOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
                labelOutline.effectDistance = new Vector2(1f, -1f);
                Bind(chestSlots[i], chestActions[i]);
                chestSlots[i].gameObject.SetActive(false);
            }
            GameObject progressBack = CreateImage("Next Chest Progress", chestPanel.transform,
                new Color(.05f, .08f, .13f, .88f), new Vector2(.10f, .045f), new Vector2(.90f, .075f),
                Vector2.zero, Vector2.zero);
            Image progressFill = CreateImage("Fill", progressBack.transform, new Color(0.16f, 0.86f, 0.32f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
            progressFill.type = Image.Type.Filled; progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0; progressFill.fillAmount = 0f;
            Text progressText = CreateText("Next Chest Text", chestPanel.transform, "NEXT CHEST  30:00", 16,
                TextAnchor.MiddleCenter, new Vector2(.04f, .075f), new Vector2(.96f, .115f), Vector2.zero, Vector2.zero);
            Outline timerOutline = progressText.gameObject.AddComponent<Outline>();
            timerOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            timerOutline.effectDistance = new Vector2(1.5f, -1.5f);
            Text pageText = CreateText("Inventory Count", chestPanel.transform, "INVENTORY  0", 13, TextAnchor.MiddleCenter,
                new Vector2(.34f, .005f), new Vector2(.66f, .04f), Vector2.zero, Vector2.zero);
            Text chestFeedback = CreateText("Chest Feedback", chestPanel.transform, "", 13, TextAnchor.MiddleLeft,
                new Vector2(.22f, .84f), new Vector2(.98f, .925f), Vector2.zero, Vector2.zero);
            Image bundleRewardImage = CreateImage("Bundle Reward Thumbnail", chestPanel.transform, Color.white,
                new Vector2(.02f, .82f), new Vector2(.20f, .925f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            bundleRewardImage.enabled = false; bundleRewardImage.preserveAspect = true;
            chestUi.Assign(assets.catalog, chestSlots, chestImages, chestLabels, progressFill, progressText,
                pageText, chestFeedback, bundleRewardImage);
            menu.Assign(assets.catalog, preview, coins, vehicleName, gems);
            VehicleDefinition.CustomizationOption initialSkin = assets.jeep.skins != null && assets.jeep.skins.Length > 0
                ? assets.jeep.skins[0] : null;
            VehicleDefinition.CustomizationOption initialWheel = assets.jeep.wheels != null && assets.jeep.wheels.Length > 0
                ? assets.jeep.wheels[0] : null;
            preview.Apply(assets.jeep, initialSkin, initialWheel, assets.stickmanBlack);
            vehicleName.text = assets.jeep.displayName.ToUpperInvariant();
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        private static void BuildGarage(BuiltAssets assets)
        {
            Scene scene = NewScene();
            Color background = new Color(.035f, .065f, .11f);
            CreateMenuCamera(background);
            Canvas canvas = CreateCanvas(background);
            Transform safe = CreateSafeArea(canvas.transform);
            GarageUI menu = new GameObject("Garage Controller").AddComponent<GarageUI>();
            CreateText("Heading", safe, "GARAGE / CUSTOMIZE", 46, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, -52f), new Vector2(680f, 72f));
            Text garageSections = CreateText("Section Guide", safe,
                "CUSTOMIZE  •  SKIN / WHEELS / DRIVER        PERFORMANCE  •  TUNING", 20,
                TextAnchor.MiddleRight, new Vector2(.46f, 1f), Vector2.one,
                new Vector2(-35f, -58f), new Vector2(-45f, 48f));
            garageSections.color = new Color(.63f, .82f, 1f, 1f);
            Text vehicleName = CreateText("Vehicle Name", safe, "JEEP", 38, TextAnchor.MiddleCenter,
                new Vector2(.05f, .82f), new Vector2(.48f, .94f), Vector2.zero, Vector2.zero);
            GameObject previewPanel = CreateImage("Live Composed Preview", safe, new Color(.055f, .105f, .17f, 1f),
                new Vector2(.04f, .22f), new Vector2(.49f, .82f), Vector2.zero, Vector2.zero);
            RawImage previewImage = UIObject("Preview Render", previewPanel.transform, new Vector2(.03f, .06f),
                new Vector2(.97f, .94f), Vector2.zero, Vector2.zero).AddComponent<RawImage>();
            previewImage.color = Color.white;
            Button previous = CreateButton("Previous Vehicle", safe, "<", new Vector2(-690f, 347f), new Vector2(76f, 64f));
            Button next = CreateButton("Next Vehicle", safe, ">", new Vector2(-95f, 347f), new Vector2(76f, 64f));
            Text skinSummary = CreateText("Skin Summary", safe, "SKIN\nDEFAULT", 18, TextAnchor.MiddleCenter,
                new Vector2(.04f, .105f), new Vector2(.185f, .205f), Vector2.zero, Vector2.zero);
            Text wheelSummary = CreateText("Wheel Summary", safe, "WHEELS\nWHEEL 01", 18, TextAnchor.MiddleCenter,
                new Vector2(.19f, .105f), new Vector2(.34f, .205f), Vector2.zero, Vector2.zero);
            Text driverSummary = CreateText("Driver Summary", safe, "DRIVER\nSTICKMAN BLACK", 18, TextAnchor.MiddleCenter,
                new Vector2(.345f, .105f), new Vector2(.49f, .205f), Vector2.zero, Vector2.zero);

            var categories = new Button[4];
            categories[0] = CreateButton("Vehicle Skin Category", safe, "VEHICLE SKIN", new Vector2(125f, 365f), new Vector2(205f, 66f));
            categories[1] = CreateButton("Wheels Category", safe, "WHEELS", new Vector2(345f, 365f), new Vector2(205f, 66f));
            categories[2] = CreateButton("Driver Category", safe, "DRIVER", new Vector2(565f, 365f), new Vector2(205f, 66f));
            categories[3] = CreateButton("Tuning Category", safe, "TUNING", new Vector2(785f, 365f), new Vector2(205f, 66f));
            Bind(categories[0], menu.ShowSkins); Bind(categories[1], menu.ShowWheels); Bind(categories[2], menu.ShowDrivers);
            Bind(categories[3], menu.ShowTuning);
            Text categoryName = CreateText("Category Name", safe, "VEHICLE SKIN", 28, TextAnchor.MiddleLeft,
                new Vector2(.53f, .75f), new Vector2(.95f, .82f), Vector2.zero, Vector2.zero);

            var optionButtons = new Button[6];
            var optionImages = new Image[6];
            var optionLabels = new Text[6];
            UnityAction[] selections = { menu.SelectOption0, menu.SelectOption1, menu.SelectOption2,
                menu.SelectOption3, menu.SelectOption4, menu.SelectOption5 };
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int column = i % 3; int row = i / 3;
                float x = 215f + column * 285f; float y = 120f - row * 285f;
                optionButtons[i] = CreateButton("Option " + (i + 1), safe, "", new Vector2(x, y), new Vector2(260f, 255f));
                optionImages[i] = CreateImage("Icon", optionButtons[i].transform, Color.white,
                    new Vector2(.12f, .30f), new Vector2(.88f, .92f), Vector2.zero, Vector2.zero).GetComponent<Image>();
                optionImages[i].raycastTarget = false; optionImages[i].preserveAspect = true;
                optionLabels[i] = CreateText("Status", optionButtons[i].transform, "OPTION", 19, TextAnchor.MiddleCenter,
                    new Vector2(.04f, .02f), new Vector2(.96f, .28f), Vector2.zero, Vector2.zero);
                Bind(optionButtons[i], selections[i]);
            }

            GameObject tuningPanel = UIObject("Garage Tuning Panel", safe, new Vector2(.52f, .12f),
                new Vector2(.97f, .75f), Vector2.zero, Vector2.zero);
            Text tuningCoins = CreateText("Tuning Coins", tuningPanel.transform, "COINS  0", 22, TextAnchor.MiddleRight,
                new Vector2(.62f, .90f), new Vector2(.98f, .99f), Vector2.zero, Vector2.zero);
            var tuningNames = new Text[6]; var tuningLevels = new Text[6]; var tuningValues = new Text[6];
            var tuningCosts = new Text[6]; var tuningButtons = new Button[6];
            UnityAction[] upgrades = { menu.UpgradeTune0, menu.UpgradeTune1, menu.UpgradeTune2,
                menu.UpgradeTune3, menu.UpgradeTune4, menu.UpgradeTune5 };
            UnityAction[] decreases = { menu.DecreaseTune0, menu.DecreaseTune1, menu.DecreaseTune2,
                menu.DecreaseTune3, menu.DecreaseTune4, menu.DecreaseTune5 };
            for (int i = 0; i < tuningNames.Length; i++)
            {
                float y = 235f - i * 94f;
                GameObject row = CreateImage("Tune Row " + (i + 1), tuningPanel.transform,
                    new Color(.065f, .125f, .20f, .98f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(0f, y), new Vector2(810f, 82f));
                tuningNames[i] = CreateText("Name", row.transform, "SPEED", 20, TextAnchor.MiddleLeft,
                    new Vector2(.03f, .50f), new Vector2(.25f, .96f), Vector2.zero, Vector2.zero);
                tuningLevels[i] = CreateText("Level", row.transform, "LEVEL 1 / 20", 18, TextAnchor.MiddleLeft,
                    new Vector2(.25f, .50f), new Vector2(.48f, .96f), Vector2.zero, Vector2.zero);
                tuningValues[i] = CreateText("Value", row.transform, "0", 17, TextAnchor.MiddleLeft,
                    new Vector2(.48f, .50f), new Vector2(.69f, .96f), Vector2.zero, Vector2.zero);
                Button decrease = CreateButton("Decrease", row.transform, "↓", new Vector2(220f, 0f), new Vector2(62f, 56f));
                tuningButtons[i] = CreateButton("Upgrade", row.transform, "↑  UPGRADE", new Vector2(325f, 0f), new Vector2(138f, 56f));
                tuningCosts[i] = tuningButtons[i].GetComponentInChildren<Text>(true);
                Bind(decrease, decreases[i]);
                Bind(tuningButtons[i], upgrades[i]);
            }
            Button back = CreateButton("Back", safe, "BACK", new Vector2(-720f, -455f), new Vector2(230f, 66f));
            Bind(back, menu.Back); Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);

            GameObject previewCameraObject = new GameObject("Garage Preview Camera");
            previewCameraObject.transform.position = new Vector3(1000f, 0f, -10f);
            Camera previewCamera = previewCameraObject.AddComponent<Camera>();
            previewCamera.orthographic = true; previewCamera.orthographicSize = 2.35f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.07f, .13f, .20f, 1f);
            previewCamera.enabled = false;
            Transform previewRoot = new GameObject("Reusable Composed Vehicle Preview").transform;
            previewRoot.position = new Vector3(1000f, 0f, 0f);
            VehicleDefinition initial = assets.jeep;
            Transform body = CreateSpriteVisual("Preview Body", previewRoot, assets.jeepBody,
                new Vector3(initial.bodyVisualOffset.x, initial.bodyVisualOffset.y, 0f), initial.visualWidth, 10);
            Transform rearWheel = CreateWheelVisual("Preview Rear Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.rearWheelVisualPosition.x, initial.rearWheelVisualPosition.y, -.02f),
                initial.RearWheelDiameter, 11);
            Transform frontWheel = CreateWheelVisual("Preview Front Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.frontWheelVisualPosition.x, initial.frontWheelVisualPosition.y, -.02f),
                initial.FrontWheelDiameter, 11);
            ArticulatedDriver2D driver = CreateArticulatedDriver(previewRoot, assets.stickmanBlack, initial, null, false);
            GaragePreviewRenderer preview = new GameObject("Garage Preview Renderer").AddComponent<GaragePreviewRenderer>();
            preview.Assign(previewCamera, previewImage, previewRoot, body.GetComponent<SpriteRenderer>(),
                rearWheel.GetComponent<SpriteRenderer>(), frontWheel.GetComponent<SpriteRenderer>(), driver);
            menu.Assign(assets.catalog, preview, vehicleName, categoryName, skinSummary, wheelSummary, driverSummary,
                categories, optionButtons, optionImages, optionLabels, tuningPanel, tuningNames, tuningLevels,
                tuningValues, tuningCosts, tuningButtons, tuningCoins);
            CreateEventSystem();
            ValidateGarageComposition(assets);
            EditorSceneManager.SaveScene(scene, GaragePath);
        }

        private static void BuildGarageModern(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.01f, .015f, .03f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            CreateAspectCoverBackground(canvas.transform, assets.mainMenuBackground);
            CreateImage("Garage Overlay", canvas.transform, new Color(.005f, .015f, .04f, .30f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            GarageUI menu = new GameObject("Garage Controller").AddComponent<GarageUI>();
            Button home = CreateSpriteButton("Home", safe, assets.homeButton, new Vector2(0f, 1f),
                new Vector2(165f, -62f), new Vector2(285f, 110f));
            Text sectionTitle = CreateText("Section Title", safe, "VEHICLE", 42, TextAnchor.MiddleCenter,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -58f), new Vector2(520f, 72f));
            sectionTitle.fontStyle = FontStyle.Bold;
            GameObject coinCard = CreateHudCard("Coins Card", safe, Vector2.one,
                new Vector2(-175f, -56f), new Vector2(300f, 76f));
            CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01, new Vector2(.5f, .5f),
                new Vector2(-112f, 0f), new Vector2(52f, 52f));
            Text tuningCoins = CreateText("Tuning Coins", coinCard.transform, "COINS  0", 27,
                TextAnchor.MiddleRight, new Vector2(.24f, .08f), new Vector2(.95f, .92f),
                Vector2.zero, Vector2.zero);
            tuningCoins.resizeTextForBestFit = true;
            tuningCoins.resizeTextMinSize = 15;
            tuningCoins.resizeTextMaxSize = 27;
            GameObject gemsCard = CreateHudCard("Gems Card", safe, Vector2.one,
                new Vector2(-175f, -132f), new Vector2(300f, 62f));
            Text gemMark = CreateText("Gem Mark", gemsCard.transform, "◆", 32, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(.26f, 1f), Vector2.zero, Vector2.zero);
            gemMark.color = new Color(.18f, .92f, 1f);
            Text gemsBalance = CreateText("Gems Balance", gemsCard.transform, "GEMS  0", 24,
                TextAnchor.MiddleRight, new Vector2(.24f, .08f), new Vector2(.95f, .92f),
                Vector2.zero, Vector2.zero);
            gemsBalance.resizeTextForBestFit = true;
            gemsBalance.resizeTextMinSize = 15;
            gemsBalance.resizeTextMaxSize = 24;
            Text vehicleName = CreateText("Vehicle Name", safe, "JEEP", 38, TextAnchor.MiddleCenter,
                new Vector2(.04f, .80f), new Vector2(.48f, .91f), Vector2.zero, Vector2.zero);
            RawImage previewImage = UIObject("Live Composed Preview", safe, new Vector2(.04f, .25f),
                new Vector2(.48f, .80f), Vector2.zero, Vector2.zero).AddComponent<RawImage>();
            previewImage.color = Color.white; previewImage.raycastTarget = false;
            Button previous = CreateButton("Previous Vehicle", safe, "<", new Vector2(-820f, -30f), new Vector2(76f, 100f));
            Button next = CreateButton("Next Vehicle", safe, ">", new Vector2(-80f, -30f), new Vector2(76f, 100f));
            Text skinSummary = CreateText("Skin Summary", safe, "SKIN\nDEFAULT", 15, TextAnchor.MiddleCenter,
                new Vector2(.04f, .12f), new Vector2(.145f, .23f), Vector2.zero, Vector2.zero);
            Text wheelSummary = CreateText("Wheel Summary", safe, "WHEELS\nWHEEL 01", 15, TextAnchor.MiddleCenter,
                new Vector2(.15f, .12f), new Vector2(.255f, .23f), Vector2.zero, Vector2.zero);
            Text driverSummary = CreateText("Driver Summary", safe, "DRIVER\nSTICKMAN BLACK", 15, TextAnchor.MiddleCenter,
                new Vector2(.26f, .12f), new Vector2(.365f, .23f), Vector2.zero, Vector2.zero);
            Text characterSummary = CreateText("Character Accessory Summary", safe, "CHARACTERS\nDEFAULT", 15,
                TextAnchor.MiddleCenter, new Vector2(.37f, .12f), new Vector2(.48f, .23f), Vector2.zero, Vector2.zero);

            GameObject vehiclePanel = CreateImage("Vehicle Panel", safe, new Color(.025f, .06f, .12f, .88f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            CreateText("Title", vehiclePanel.transform, "VEHICLE SELECTION", 34, TextAnchor.MiddleCenter,
                new Vector2(.08f, .78f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            CreateText("Description", vehiclePanel.transform,
                "USE THE ARROWS TO SELECT A VEHICLE\n\nALL AVAILABLE VEHICLES USE THEIR REAL SAVED STATS, SKIN AND WHEELS.",
                24, TextAnchor.MiddleCenter, new Vector2(.10f, .25f), new Vector2(.90f, .72f), Vector2.zero, Vector2.zero);

            GameObject customizationPanel = UIObject("Skins Panel", safe, new Vector2(.51f, .13f),
                new Vector2(.98f, .86f), Vector2.zero, Vector2.zero);
            var categories = new Button[4];
            categories[0] = CreateButton("Vehicle Skin Category", customizationPanel.transform, "VEHICLE SKIN", new Vector2(-315f, 280f), new Vector2(190f, 64f));
            categories[1] = CreateButton("Wheels Category", customizationPanel.transform, "WHEELS", new Vector2(-105f, 280f), new Vector2(190f, 64f));
            categories[2] = CreateButton("Driver Category", customizationPanel.transform, "DRIVER", new Vector2(105f, 280f), new Vector2(190f, 64f));
            categories[3] = CreateButton("Characters Category", customizationPanel.transform, "CHARACTERS", new Vector2(315f, 280f), new Vector2(190f, 64f));
            Bind(categories[0], menu.ShowSkins); Bind(categories[1], menu.ShowWheels);
            Bind(categories[2], menu.ShowDrivers); Bind(categories[3], menu.ShowCharacters);
            Text categoryName = CreateText("Category Name", customizationPanel.transform, "VEHICLE SKIN", 28,
                TextAnchor.MiddleCenter, new Vector2(.15f, .69f), new Vector2(.85f, .80f), Vector2.zero, Vector2.zero);
            var optionButtons = new Button[6]; var optionImages = new Image[6]; var optionLabels = new Text[6];
            var optionBlueprintFills = new Image[6]; var optionBlueprintTexts = new Text[6];
            UnityAction[] selections = { menu.SelectOption0, menu.SelectOption1, menu.SelectOption2,
                menu.SelectOption3, menu.SelectOption4, menu.SelectOption5 };
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int column = i % 3; int rowIndex = i / 3;
                float x = -260f + column * 260f; float y = 65f - rowIndex * 235f;
                optionButtons[i] = CreateButton("Option " + (i + 1), customizationPanel.transform, "", new Vector2(x, y), new Vector2(235f, 210f));
                optionImages[i] = CreateImage("Icon", optionButtons[i].transform, Color.white,
                    new Vector2(.12f, .34f), new Vector2(.88f, .92f), Vector2.zero, Vector2.zero).GetComponent<Image>();
                optionImages[i].raycastTarget = false; optionImages[i].preserveAspect = true;
                optionLabels[i] = CreateText("Status", optionButtons[i].transform, "OPTION", 17,
                    TextAnchor.MiddleCenter, new Vector2(.035f, .025f), new Vector2(.965f, .365f),
                    Vector2.zero, Vector2.zero);
                optionLabels[i].resizeTextForBestFit = true;
                optionLabels[i].resizeTextMinSize = 10;
                optionLabels[i].resizeTextMaxSize = 17;
                optionLabels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                optionLabels[i].verticalOverflow = VerticalWrapMode.Truncate;
                optionLabels[i].lineSpacing = .88f;
                GameObject blueprintProgress = CreateImage("Blueprint Progress", optionButtons[i].transform,
                    new Color(.025f, .055f, .09f, .98f), new Vector2(.07f, .025f), new Vector2(.93f, .105f),
                    Vector2.zero, Vector2.zero);
                optionBlueprintFills[i] = CreateImage("Fill", blueprintProgress.transform,
                    new Color(.20f, .82f, 1f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
                optionBlueprintFills[i].type = Image.Type.Filled;
                optionBlueprintFills[i].fillMethod = Image.FillMethod.Horizontal;
                optionBlueprintFills[i].fillOrigin = 0;
                optionBlueprintFills[i].fillAmount = 0f;
                optionBlueprintFills[i].raycastTarget = false;
                optionBlueprintTexts[i] = CreateText("Progress", blueprintProgress.transform, "BLUEPRINTS  0 / 100",
                    13, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                blueprintProgress.SetActive(false);
                Bind(optionButtons[i], selections[i]);
            }

            GameObject tuningPanel = UIObject("Garage Tuning Panel", safe, new Vector2(.50f, .10f),
                new Vector2(.98f, .86f), Vector2.zero, Vector2.zero);
            var tuningNames = new Text[6]; var tuningLevels = new Text[6]; var tuningValues = new Text[6];
            var tuningCosts = new Text[6]; var tuningButtons = new Button[6];
            UnityAction[] upgrades = { menu.UpgradeTune0, menu.UpgradeTune1, menu.UpgradeTune2,
                menu.UpgradeTune3, menu.UpgradeTune4, menu.UpgradeTune5 };
            UnityAction[] decreases = { menu.DecreaseTune0, menu.DecreaseTune1, menu.DecreaseTune2,
                menu.DecreaseTune3, menu.DecreaseTune4, menu.DecreaseTune5 };
            for (int i = 0; i < tuningNames.Length; i++)
            {
                float y = 260f - i * 104f;
                GameObject row = CreateImage("Tune Row " + (i + 1), tuningPanel.transform,
                    new Color(.065f, .125f, .20f, .98f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(0f, y), new Vector2(870f, 92f));
                tuningNames[i] = CreateText("Name", row.transform, "SPEED", 19, TextAnchor.MiddleLeft,
                    new Vector2(.02f, .52f), new Vector2(.25f, .96f), Vector2.zero, Vector2.zero);
                tuningLevels[i] = CreateText("Level", row.transform, "LEVEL 1 / 20", 17, TextAnchor.MiddleLeft,
                    new Vector2(.02f, .06f), new Vector2(.25f, .52f), Vector2.zero, Vector2.zero);
                tuningValues[i] = CreateText("Value", row.transform, "CURRENT\nNEXT", 15, TextAnchor.MiddleLeft,
                    new Vector2(.27f, .05f), new Vector2(.72f, .95f), Vector2.zero, Vector2.zero);
                Button decrease = CreateButton("Decrease", row.transform, "↓", new Vector2(235f, 0f), new Vector2(70f, 60f));
                tuningButtons[i] = CreateButton("Upgrade", row.transform, "↑  PRICE", new Vector2(350f, 0f), new Vector2(150f, 60f));
                tuningCosts[i] = tuningButtons[i].GetComponentInChildren<Text>(true);
                Bind(decrease, decreases[i]);
                Bind(tuningButtons[i], upgrades[i]);
            }

            GameObject upgradePanel = CreateImage("Upgrade Panel", safe, new Color(.025f, .06f, .12f, .90f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            Text upgradeSummary = CreateText("Upgrade Summary", upgradePanel.transform, "CURRENT PERFORMANCE", 27,
                TextAnchor.UpperLeft, new Vector2(.10f, .12f), new Vector2(.90f, .88f), Vector2.zero, Vector2.zero);
            GameObject storePanel = CreateImage("Store Panel", safe, new Color(.025f, .06f, .12f, .90f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            GameObject viewport = CreateImage("Shop Viewport", storePanel.transform, new Color(0f, 0f, 0f, .015f),
                new Vector2(.025f, .025f), new Vector2(.975f, .975f), Vector2.zero, Vector2.zero);
            Mask viewportMask = viewport.AddComponent<Mask>(); viewportMask.showMaskGraphic = false;
            GameObject content = UIObject("Shop Content", viewport.transform, new Vector2(0f, 1f), Vector2.one,
                Vector2.zero, new Vector2(0f, 1800f));
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.pivot = new Vector2(.5f, 1f); contentRect.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup shopLayout = content.AddComponent<VerticalLayoutGroup>();
            shopLayout.padding = new RectOffset(24, 24, 24, 34); shopLayout.spacing = 24f;
            shopLayout.childAlignment = TextAnchor.UpperCenter; shopLayout.childControlWidth = true;
            shopLayout.childControlHeight = true; shopLayout.childForceExpandWidth = true;
            shopLayout.childForceExpandHeight = false;
            ContentSizeFitter shopFitter = content.AddComponent<ContentSizeFitter>();
            shopFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect shopScroll = storePanel.AddComponent<ScrollRect>();
            shopScroll.viewport = viewport.GetComponent<RectTransform>(); shopScroll.content = contentRect;
            shopScroll.horizontal = false; shopScroll.vertical = true;
            shopScroll.movementType = ScrollRect.MovementType.Clamped;
            shopScroll.inertia = true; shopScroll.decelerationRate = .135f; shopScroll.scrollSensitivity = 45f;

            GameObject specialOffers = CreateImage("Special Offers", content.transform,
                new Color(.08f, .045f, .12f, .98f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(900f, 330f));
            specialOffers.AddComponent<LayoutElement>().preferredHeight = 330f;
            CreateText("Special Offers Title", specialOffers.transform, "Special Offers", 31,
                TextAnchor.MiddleLeft, new Vector2(.04f, .78f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            CreateText("IAP Status", specialOffers.transform,
                "REAL-MONEY OFFERS\nPURCHASES DISABLED — GOOGLE PLAY BILLING / IAP IS NOT CONFIGURED",
                17, TextAnchor.MiddleLeft, new Vector2(.04f, .55f), new Vector2(.96f, .78f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < 3; i++)
            {
                GameObject offer = CreateImage("Special Offer " + (i + 1), specialOffers.transform,
                    new Color(.16f, .09f, .20f, .94f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                    new Vector2(-285f + i * 285f, -72f), new Vector2(255f, 125f));
                CreateText("Status", offer.transform, "COMING SOON\nIAP DISABLED", 18,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            GameObject blueprintSection = CreateImage("Blue Prints Accesories", content.transform,
                new Color(.025f, .075f, .13f, .98f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(900f, 1030f));
            blueprintSection.AddComponent<LayoutElement>().preferredHeight = 1030f;
            CreateText("Blueprint Shop Title", blueprintSection.transform, "Blue Prints / Accesories", 31,
                TextAnchor.MiddleLeft, new Vector2(.04f, .89f), new Vector2(.63f, .98f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Text blueprintCoins = CreateText("Blueprint Coins", blueprintSection.transform, "COINS  0", 22,
                TextAnchor.MiddleRight, new Vector2(.63f, .89f), new Vector2(.96f, .98f), Vector2.zero, Vector2.zero);
            CreateText("Pack Selector Title", blueprintSection.transform, "SELECT BLUEPRINT PACK", 16,
                TextAnchor.MiddleCenter, new Vector2(.22f, .81f), new Vector2(.78f, .88f), Vector2.zero, Vector2.zero);
            var blueprintPackButtons = new Button[BlueprintPackCatalog.Count];
            UnityAction[] blueprintPackActions = { menu.SelectBlueprintPack1, menu.SelectBlueprintPack5,
                menu.SelectBlueprintPack10, menu.SelectBlueprintPack100 };
            for (int i = 0; i < blueprintPackButtons.Length; i++)
            {
                BlueprintPackTier tier = (BlueprintPackTier)i;
                int amount = BlueprintPackCatalog.GetBlueprintAmount(tier);
                int price = BlueprintPackCatalog.GetCoinPrice(tier);
                blueprintPackButtons[i] = CreateButton("Blueprint Pack " + amount, blueprintSection.transform,
                    $"+{amount} BP\n{(price >= 1000000 ? "1M" : (price / 1000) + "K")}",
                    new Vector2(-300f + i * 200f, 330f), new Vector2(185f, 70f));
                Bind(blueprintPackButtons[i], blueprintPackActions[i]);
            }
            var blueprintButtons = new Button[6]; var blueprintImages = new Image[6]; var blueprintLabels = new Text[6];
            UnityAction[] blueprintActions = { menu.SelectBlueprint0, menu.SelectBlueprint1, menu.SelectBlueprint2,
                menu.SelectBlueprint3, menu.SelectBlueprint4, menu.SelectBlueprint5 };
            for (int i = 0; i < blueprintButtons.Length; i++)
            {
                int column = i % 3; int rowIndex = i / 3;
                float x = -280f + column * 280f; float y = 130f - rowIndex * 235f;
                blueprintButtons[i] = CreateButton("Blueprint Bundle " + (i + 1), blueprintSection.transform, "",
                    new Vector2(x, y), new Vector2(255f, 205f));
                blueprintImages[i] = CreateImage("Thumbnail", blueprintButtons[i].transform, Color.white,
                    new Vector2(.10f, .39f), new Vector2(.90f, .95f), Vector2.zero, Vector2.zero).GetComponent<Image>();
                blueprintImages[i].raycastTarget = false; blueprintImages[i].preserveAspect = true;
                blueprintLabels[i] = CreateText("Details", blueprintButtons[i].transform, "BUNDLE", 14,
                    TextAnchor.MiddleCenter, new Vector2(.03f, .01f), new Vector2(.97f, .40f), Vector2.zero, Vector2.zero);
                Bind(blueprintButtons[i], blueprintActions[i]);
            }
            Text blueprintPage = CreateText("Blueprint Page", blueprintSection.transform, "PAGE 1 / 1", 18,
                TextAnchor.MiddleCenter, new Vector2(.37f, .055f), new Vector2(.63f, .115f), Vector2.zero, Vector2.zero);
            Button blueprintPrev = CreateButton("Previous Blueprint Page", blueprintSection.transform, "<", new Vector2(-180f, -430f), new Vector2(90f, 52f));
            Button blueprintNext = CreateButton("Next Blueprint Page", blueprintSection.transform, ">", new Vector2(180f, -430f), new Vector2(90f, 52f));
            Bind(blueprintPrev, menu.PreviousBlueprintPage); Bind(blueprintNext, menu.NextBlueprintPage);

            GameObject accessorySection = CreateImage("Character Accessories", content.transform,
                new Color(.035f, .085f, .13f, .98f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(900f, 650f));
            accessorySection.AddComponent<LayoutElement>().preferredHeight = 650f;
            CreateText("Character Accessories Title", accessorySection.transform, "CHARACTER ACCESSORIES", 27,
                TextAnchor.MiddleLeft, new Vector2(.04f, .78f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            var shopAccessoryButtons = new Button[4]; var shopAccessoryImages = new Image[4];
            var shopAccessoryLabels = new Text[4];
            UnityAction[] accessoryActions = { menu.SelectShopAccessory0, menu.SelectShopAccessory1,
                menu.SelectShopAccessory2, menu.SelectShopAccessory3 };
            for (int i = 0; i < shopAccessoryButtons.Length; i++)
            {
                int column = i % 2;
                int accessoryRow = i / 2;
                shopAccessoryButtons[i] = CreateButton("Accessory " + (i + 1), accessorySection.transform, "",
                    new Vector2(-215f + column * 430f, 85f - accessoryRow * 260f),
                    new Vector2(390f, 235f));
                shopAccessoryImages[i] = CreateImage("Thumbnail", shopAccessoryButtons[i].transform, Color.white,
                    new Vector2(.08f, .30f), new Vector2(.92f, .93f), Vector2.zero, Vector2.zero).GetComponent<Image>();
                shopAccessoryImages[i].raycastTarget = false; shopAccessoryImages[i].preserveAspect = true;
                shopAccessoryLabels[i] = CreateText("Details", shopAccessoryButtons[i].transform, "ACCESSORY", 16,
                    TextAnchor.MiddleCenter, new Vector2(.04f, .03f), new Vector2(.96f, .29f), Vector2.zero, Vector2.zero);
                Bind(shopAccessoryButtons[i], accessoryActions[i]);
            }
            GameObject settingsPanel = CreateImage("Settings Panel", safe, new Color(.025f, .06f, .12f, .90f),
                new Vector2(.52f, .16f), new Vector2(.97f, .84f), Vector2.zero, Vector2.zero);
            CreateText("Settings Placeholder", settingsPanel.transform, "SETTINGS", 42, TextAnchor.MiddleCenter,
                new Vector2(.08f, .12f), new Vector2(.92f, .88f), Vector2.zero, Vector2.zero);

            GameObject modal = CreateImage("Purchase Confirmation", safe, new Color(.01f, .025f, .055f, .985f),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(760f, 470f));
            modal.AddComponent<Outline>().effectColor = new Color(.20f, .74f, 1f, 1f);
            Text modalTitle = CreateText("Title", modal.transform, "UPGRADE SPEED?", 36, TextAnchor.MiddleCenter,
                new Vector2(.08f, .76f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero);
            Text modalValues = CreateText("Values", modal.transform, "CURRENT\nNEXT", 25, TextAnchor.MiddleCenter,
                new Vector2(.08f, .39f), new Vector2(.92f, .75f), Vector2.zero, Vector2.zero);
            Text modalCost = CreateText("Cost", modal.transform, "COST", 26, TextAnchor.MiddleCenter,
                new Vector2(.08f, .25f), new Vector2(.92f, .40f), Vector2.zero, Vector2.zero);
            Text modalStatus = CreateText("Status", modal.transform, "", 22, TextAnchor.MiddleCenter,
                new Vector2(.08f, .15f), new Vector2(.92f, .26f), Vector2.zero, Vector2.zero);
            modalStatus.color = new Color(1f, .30f, .28f);
            Button yes = CreateButton("Yes", modal.transform, "UPGRADE", new Vector2(-170f, -175f), new Vector2(260f, 72f));
            Button no = CreateButton("No", modal.transform, "CANCEL", new Vector2(170f, -175f), new Vector2(260f, 72f));
            yes.image.color = new Color(.16f, .72f, .26f, 1f); no.image.color = new Color(.82f, .16f, .14f, 1f);
            Bind(yes, menu.ConfirmPurchase); Bind(no, menu.CancelPurchase); modal.SetActive(false);

            Bind(home, menu.Back); Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);
            GaragePreviewRenderer preview = CreateGarageVehiclePreview(assets, previewImage, "Garage");
            menu.Assign(assets.catalog, preview, vehicleName, categoryName, skinSummary, wheelSummary, driverSummary,
                categories, optionButtons, optionImages, optionLabels, tuningPanel, tuningNames, tuningLevels,
                tuningValues, tuningCosts, tuningButtons, tuningCoins, sectionTitle, upgradeSummary, vehiclePanel,
                customizationPanel, upgradePanel, storePanel, settingsPanel, modal, modalTitle, modalValues,
                modalCost, modalStatus, yes, gemsBalance, blueprintButtons, blueprintImages, blueprintLabels,
                blueprintPage, blueprintCoins, optionBlueprintFills, optionBlueprintTexts, blueprintPackButtons,
                characterSummary, shopAccessoryButtons, shopAccessoryImages, shopAccessoryLabels);
            CreateEventSystem();
            ValidateGarageComposition(assets);
            EditorSceneManager.SaveScene(scene, GaragePath);
        }

        private static void BuildPreStartModern(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.01f, .015f, .03f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            CreateAspectCoverBackground(canvas.transform, assets.mainMenuBackground);
            CreateImage("Race Overlay", canvas.transform, new Color(.005f, .015f, .04f, .36f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            PreStartMenuUI menu = new GameObject("Pre-Start Controller").AddComponent<PreStartMenuUI>();
            Button home = CreateSpriteButton("Home", safe, assets.homeButton, new Vector2(0f, 1f),
                new Vector2(165f, -62f), new Vector2(285f, 110f));
            Text coins = CreateText("Coins", safe, "COINS  0", 27, TextAnchor.MiddleRight,
                Vector2.one, Vector2.one, new Vector2(-190f, -55f), new Vector2(340f, 70f));
            Text progress = CreateText("Progress", safe, "1 MODE   >   2 VEHICLE", 25,
                TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -62f), new Vector2(800f, 52f));

            var panels = new GameObject[2];
            for (int i = 0; i < panels.Length; i++)
                panels[i] = UIObject($"Step {i + 1}", safe, Vector2.zero, Vector2.one,
                    new Vector2(0f, -20f), new Vector2(-80f, -180f));

            // --- STEP 1: SELECT MODE & TRACK ---
            CreateText("Title", panels[0].transform, "SELECT MODE", 42, TextAnchor.MiddleCenter,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -42f), new Vector2(600f, 52f))
                .fontStyle = FontStyle.Bold;

            // Compact Adventure Card
            GameObject advCard = CreateImage("Adventure Mode Card", panels[0].transform,
                new Color(0.08f, 0.22f, 0.42f, 0.95f), new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -95f), new Vector2(320f, 48f));
            Outline advOutline = advCard.AddComponent<Outline>();
            advOutline.effectColor = new Color(0.30f, 0.85f, 1f, 0.90f);
            advOutline.effectDistance = new Vector2(1.5f, -1.5f);
            Text advText = CreateText("Mode Name", advCard.transform, "ADVENTURE", 24, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            advText.fontStyle = FontStyle.Bold;
            advText.color = new Color(0.45f, 0.92f, 1f, 1f);

            // Coming Soon Panel (Top-Right Corner)
            GameObject comingSoon = CreateImage("Coming Soon Panel", panels[0].transform,
                new Color(0.05f, 0.09f, 0.16f, 0.85f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-165f, -50f), new Vector2(240f, 62f));
            Outline csOutline = comingSoon.AddComponent<Outline>();
            csOutline.effectColor = new Color(0.35f, 0.42f, 0.52f, 0.65f);
            csOutline.effectDistance = new Vector2(1.5f, -1.5f);
            Text csHeader = CreateText("CS Header", comingSoon.transform, "COMING SOON...", 15, TextAnchor.MiddleCenter,
                new Vector2(0f, .45f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            csHeader.fontStyle = FontStyle.Bold;
            csHeader.color = new Color(0.90f, 0.85f, 0.45f, 1f);
            Text csSub = CreateText("CS Sub", comingSoon.transform, "3D  •  TIME TRIAL", 13, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, .52f), Vector2.zero, Vector2.zero);
            csSub.color = new Color(0.55f, 0.65f, 0.78f, 0.90f);

            // Center Track Preview Card
            GameObject previewFrame = CreateImage("Track Preview Frame", panels[0].transform,
                new Color(0.02f, 0.05f, 0.10f, 0.95f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(0f, 30f), new Vector2(620f, 340f));
            previewFrame.AddComponent<RectMask2D>();
            Outline frameOutline = previewFrame.AddComponent<Outline>();
            frameOutline.effectColor = new Color(0.25f, 0.72f, 1f, 0.85f);
            frameOutline.effectDistance = new Vector2(2f, -2f);
            Shadow frameShadow = previewFrame.AddComponent<Shadow>();
            frameShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            frameShadow.effectDistance = new Vector2(0f, -6f);

            // Layered Composite Real Track Assets
            Image skyImg = CreateImage("Sky Preview", previewFrame.transform, Color.clear,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
            skyImg.preserveAspect = false; skyImg.raycastTarget = false; skyImg.enabled = false;

            Image bgImg = CreateImage("Mountains Preview", previewFrame.transform, Color.clear,
                new Vector2(0f, 0.08f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            bgImg.preserveAspect = false; bgImg.raycastTarget = false; bgImg.enabled = false;

            Image groundImg = CreateImage("Ground Preview", previewFrame.transform, Color.clear,
                new Vector2(0f, 0f), new Vector2(1f, 0.40f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            groundImg.preserveAspect = false; groundImg.raycastTarget = false; groundImg.enabled = false;

            // Track Selector & Gray Arrows
            GameObject trackSelector = UIObject("Adventure Track Selector", panels[0].transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button previousTrack = CreateGrayNavButton("Previous Adventure Track", trackSelector.transform, "<",
                new Vector2(-365f, 28f), new Vector2(68f, 85f));
            Button nextTrack = CreateGrayNavButton("Next Adventure Track", trackSelector.transform, ">",
                new Vector2(365f, 28f), new Vector2(68f, 85f));

            // Track Info under Preview
            Text trackTitle = CreateText("Track Title", panels[0].transform, "COUNTRYSIDE", 32, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -182f), new Vector2(500f, 44f));
            trackTitle.fontStyle = FontStyle.Bold;
            trackTitle.color = Color.white;

            Text trackBestDist = CreateText("Track Best Distance", panels[0].transform, "BEST: 0m", 21, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -224f), new Vector2(400f, 36f));
            trackBestDist.fontStyle = FontStyle.Bold;
            trackBestDist.color = new Color(1f, 0.88f, 0.28f, 1f);

            Button continueMode = CreateButton("Continue", panels[0].transform, "CONTINUE",
                new Vector2(0f, -385f), new Vector2(320f, 72f));

            Bind(previousTrack, menu.PreviousMap); Bind(nextTrack, menu.NextMap);
            Bind(home, menu.BackStep); Bind(continueMode, menu.ContinueStep);

            // --- STEP 2: SELECT VEHICLE ---
            CreateText("Title", panels[1].transform, "SELECT VEHICLE", 45, TextAnchor.MiddleCenter,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -52f), new Vector2(700f, 72f));
            GameObject vehicleCard = CreateImage("Vehicle Card", panels[1].transform,
                new Color(.025f, .06f, .12f, .90f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(0f, -20f), new Vector2(1310f, 600f));
            RawImage previewImage = UIObject("Selected Vehicle Preview", vehicleCard.transform,
                new Vector2(.04f, .10f), new Vector2(.57f, .88f), Vector2.zero, Vector2.zero).AddComponent<RawImage>();
            previewImage.color = Color.white; previewImage.raycastTarget = false;
            Text vehicle = CreateText("Vehicle", vehicleCard.transform, "JEEP", 38, TextAnchor.UpperLeft,
                new Vector2(.61f, .78f), new Vector2(.95f, .93f), Vector2.zero, Vector2.zero);
            Text stats = CreateText("Vehicle Stats", vehicleCard.transform, "", 23, TextAnchor.UpperLeft,
                new Vector2(.61f, .16f), new Vector2(.95f, .77f), Vector2.zero, Vector2.zero);
            CreateText("Transmission", vehicleCard.transform, "AUTOMATIC TRANSMISSION", 21,
                TextAnchor.MiddleCenter, new Vector2(.61f, .05f), new Vector2(.95f, .15f), Vector2.zero, Vector2.zero)
                .color = new Color(.38f, .88f, 1f);
            Button previous = CreateGrayNavButton("Previous Vehicle", panels[1].transform, "<",
                new Vector2(-710f, -20f), new Vector2(76f, 95f));
            Button next = CreateGrayNavButton("Next Vehicle", panels[1].transform, ">",
                new Vector2(710f, -20f), new Vector2(76f, 95f));
            Button back = CreateButton("Back", panels[1].transform, "BACK",
                new Vector2(-520f, -365f), new Vector2(250f, 68f));
            Button start = CreateButton("Start Race", panels[1].transform, "START RACE",
                new Vector2(520f, -365f), new Vector2(340f, 76f));
            Bind(previous, menu.PreviousVehicle); Bind(next, menu.NextVehicle);
            Bind(back, menu.BackStep); Bind(start, menu.ContinueStep);
            GaragePreviewRenderer preview = CreateGarageVehiclePreview(assets, previewImage, "PreStart");
            var modeCardsArray = new Image[] { advCard.GetComponent<Image>(), comingSoon.GetComponent<Image>(), comingSoon.GetComponent<Image>() };
            var modeCardRectsArray = new RectTransform[] { advCard.GetComponent<RectTransform>(), comingSoon.GetComponent<RectTransform>(), comingSoon.GetComponent<RectTransform>() };
            var modeCardStatusTextsArray = new Text[] { advText, csHeader, csSub };
            menu.Assign(assets.catalog, coins, progress, panels, modeCardsArray, modeCardRectsArray, modeCardStatusTextsArray, advText, trackTitle, vehicle,
                null, null, stats, null, null, null, null, null, null, null, null, null, preview, trackSelector);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, PreStartPath);
        }

        private static void BuildPreStart(BuiltAssets assets)
        {
            Scene scene = NewScene();
            Color background = new Color(0.10f, 0.17f, 0.29f);
            CreateMenuCamera(background);
            Canvas canvas = CreateCanvas(background);
            Transform safe = CreateSafeArea(canvas.transform);
            PreStartMenuUI menu = new GameObject("Pre-Start Controller").AddComponent<PreStartMenuUI>();
            CreateText("Heading", safe, "CLIMB ANIME  /  PRE-START", 40, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(390f, -46f), new Vector2(700f, 60f));
            Text coins = CreateText("Coins", safe, "COINS  0", 27, TextAnchor.MiddleRight,
                Vector2.one, Vector2.one, new Vector2(-190f, -46f), new Vector2(340f, 60f));
            Text progress = CreateText("Progress", safe, "1 MODE   >   2 MAP   >   3 VEHICLE   >   4 SETUP", 23,
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(1000f, 44f));

            var panels = new GameObject[4];
            for (int i = 0; i < panels.Length; i++)
                panels[i] = UIObject($"Step {i + 1}", safe, Vector2.zero, Vector2.one, new Vector2(0f, -30f), new Vector2(-90f, -190f));

            CreateText("Title", panels[0].transform, "SELECT MODE", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject cardGrid = UIObject("Mode Card Grid", panels[0].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(1510f, 550f));
            GridLayoutGroup grid = cardGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(470f, 530f); grid.spacing = new Vector2(45f, 0f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;
            var cards = new Image[2]; var cardRects = new RectTransform[2]; var statuses = new Text[2];
            CreateModeCard(cardGrid.transform, "Adventure", "SIDE-VIEW CLIMB", "GOAL  DISTANCE\nCAMERA  SIDE VIEW\nInfinite adventure.",
                menu.SelectAdventure2_5D, out cards[0], out cardRects[0], out statuses[0]);
            CreateModeCard(cardGrid.transform, "3D", "OPEN ROAD", "GOAL  DISTANCE\nCAMERA  CHASE 3D\nDrive as far as possible.",
                menu.SelectAdventure3D, out cards[1], out cardRects[1], out statuses[1]);
            Text mode = CreateText("Selected Mode", panels[0].transform, "ADVENTURE", 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 83f), new Vector2(520f, 38f));
            Button modeBack = CreateButton("Back", panels[0].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button modeNext = CreateButton("Continue", panels[0].transform, "CONTINUE", new Vector2(660f, -350f), new Vector2(260f, 64f));
            Bind(modeBack, menu.BackStep); Bind(modeNext, menu.ContinueStep);

            CreateText("Title", panels[1].transform, "SELECT MAP", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject mapCard = CreateImage("Map Card", panels[1].transform, new Color(0.055f, 0.11f, 0.19f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(980f, 530f));
            Image mapSky = CreateImage("Countryside Sky Preview", mapCard.transform, Color.white,
                new Vector2(.03f, .35f), new Vector2(.97f, .96f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            mapSky.sprite = assets.countrysideEnvironment.sky; mapSky.preserveAspect = true; mapSky.raycastTarget = false;
            Image mapMountains = CreateImage("Countryside Mountains Preview", mapCard.transform, Color.white,
                new Vector2(.03f, .35f), new Vector2(.97f, .70f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            mapMountains.sprite = assets.countrysideEnvironment.mountains; mapMountains.preserveAspect = true;
            mapMountains.raycastTarget = false;
            Text map = CreateText("Map Details", mapCard.transform, "COUNTRYSIDE", 27, TextAnchor.UpperCenter,
                Vector2.zero, new Vector2(1f, 0.35f), new Vector2(0f, 15f), new Vector2(-60f, -20f));
            Button mapPrev = CreateButton("Previous Map", panels[1].transform, "<", new Vector2(-590f, -20f), new Vector2(90f, 90f));
            Button mapNext = CreateButton("Next Map", panels[1].transform, ">", new Vector2(590f, -20f), new Vector2(90f, 90f));
            Button mapBack = CreateButton("Back", panels[1].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button mapContinue = CreateButton("Continue", panels[1].transform, "CONTINUE", new Vector2(660f, -350f), new Vector2(260f, 64f));
            Bind(mapPrev, menu.PreviousMap); Bind(mapNext, menu.NextMap); Bind(mapBack, menu.BackStep); Bind(mapContinue, menu.ContinueStep);

            CreateText("Title", panels[2].transform, "SELECT VEHICLE", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject vehicleCard = CreateImage("Vehicle Card", panels[2].transform, new Color(0.055f, 0.11f, 0.19f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1120f, 540f));
            GameObject previewRoot = CreateImage("Vehicle Preview", vehicleCard.transform, Color.white,
                new Vector2(0.04f, 0.12f), new Vector2(0.53f, 0.88f), Vector2.zero, Vector2.zero);
            Image vehiclePreview = previewRoot.GetComponent<Image>(); vehiclePreview.preserveAspect = true;
            GameObject jeepPreview = UIObject("Jeep Preview Fallback", previewRoot.transform, Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero);
            CreateImage("Jeep Body", jeepPreview.transform, new Color(.82f, .16f, .22f),
                new Vector2(.16f, .31f), new Vector2(.84f, .58f), Vector2.zero, Vector2.zero);
            CreateImage("Jeep Cabin", jeepPreview.transform, new Color(.12f, .34f, .68f),
                new Vector2(.30f, .58f), new Vector2(.62f, .78f), Vector2.zero, Vector2.zero);
            CreateImage("Rear Wheel", jeepPreview.transform, Color.black,
                new Vector2(.24f, .20f), new Vector2(.37f, .38f), Vector2.zero, Vector2.zero);
            CreateImage("Front Wheel", jeepPreview.transform, Color.black,
                new Vector2(.65f, .20f), new Vector2(.78f, .38f), Vector2.zero, Vector2.zero);
            CreateText("Jeep Preview Label", jeepPreview.transform, "JEEP  •  ORIGINAL", 25,
                TextAnchor.MiddleCenter, new Vector2(0f, .03f), new Vector2(1f, .18f), Vector2.zero, Vector2.zero).color =
                new Color(.12f, .25f, .40f);
            Text vehicle = CreateText("Vehicle", vehicleCard.transform, "CAR 01", 32, TextAnchor.UpperLeft,
                new Vector2(0.57f, 0.78f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
            Text stats = CreateText("Vehicle Stats", vehicleCard.transform, "", 22, TextAnchor.UpperLeft,
                new Vector2(0.57f, 0.08f), new Vector2(0.95f, 0.78f), Vector2.zero, Vector2.zero);
            Text selectedVehicle = CreateText("Selected Vehicle", vehicleCard.transform, "✓  SELECTED VEHICLE", 21,
                TextAnchor.MiddleCenter, new Vector2(.60f, .01f), new Vector2(.93f, .10f), Vector2.zero, Vector2.zero);
            selectedVehicle.color = new Color(.38f, .88f, 1f);
            Button vehiclePrev = CreateButton("Previous Vehicle", panels[2].transform, "<", new Vector2(-660f, -20f), new Vector2(90f, 90f));
            Button vehicleNext = CreateButton("Next Vehicle", panels[2].transform, ">", new Vector2(660f, -20f), new Vector2(90f, 90f));
            Button vehicleBack = CreateButton("Back", panels[2].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button vehicleContinue = CreateButton("Continue", panels[2].transform, "SELECT", new Vector2(660f, -350f), new Vector2(300f, 64f));
            Bind(vehiclePrev, menu.PreviousVehicle); Bind(vehicleNext, menu.NextVehicle); Bind(vehicleBack, menu.BackStep); Bind(vehicleContinue, menu.ContinueStep);

            CreateText("Title", panels[3].transform, "SETUP", 46, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(600f, 70f));
            GameObject setupCard = CreateImage("Setup Card", panels[3].transform, new Color(0.055f, 0.11f, 0.19f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1160f, 520f));
            Text summary = CreateText("Tuning Summary", setupCard.transform, "", 25, TextAnchor.UpperLeft,
                new Vector2(0.05f, 0.08f), new Vector2(0.52f, 0.92f), Vector2.zero, Vector2.zero);
            CreateText("Transmission Label", setupCard.transform, "TRANSMISSION", 23, TextAnchor.MiddleCenter,
                new Vector2(0.57f, 0.67f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
            Text transmission = CreateText("Transmission", setupCard.transform, "AUTO", 42, TextAnchor.MiddleCenter,
                new Vector2(0.57f, 0.51f), new Vector2(0.95f, 0.67f), Vector2.zero, Vector2.zero);
            Button transmissionButton = CreateButton("Toggle Transmission", setupCard.transform, "TOGGLE AUTO / MANUAL",
                new Vector2(300f, -55f), new Vector2(390f, 66f));
            Button setupBack = CreateButton("Back", panels[3].transform, "BACK", new Vector2(-660f, -350f), new Vector2(220f, 64f));
            Button start = CreateButton("Start", panels[3].transform, "START", new Vector2(660f, -350f), new Vector2(300f, 72f));
            Bind(transmissionButton, menu.ToggleTransmission);
            Bind(setupBack, menu.BackStep); Bind(start, menu.ContinueStep);

            menu.Assign(assets.catalog, coins, progress, panels, cards, cardRects, statuses, mode, map, vehicle,
                vehiclePreview, jeepPreview, stats, summary, transmission, null, null, null, null,
                null, null, null);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, PreStartPath);
        }

        private static void CreateModeCard(Transform parent, string title, string subtitle, string description,
            UnityAction action, out Image image, out RectTransform rect, out Text status)
        {
            GameObject card = CreateImage(title + " Card", parent, new Color(0.055f, 0.10f, 0.18f, 0.98f),
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            image = card.GetComponent<Image>(); rect = card.GetComponent<RectTransform>();
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.30f, 0.42f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            CreateText("Title", card.transform, title.ToUpperInvariant(), 31, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            CreateText("Preview", card.transform, subtitle, 25, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.75f), Vector2.zero, Vector2.zero).color = new Color(0.55f, 0.82f, 0.96f);
            CreateText("Description", card.transform, description, 21, TextAnchor.UpperCenter,
                new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.42f), Vector2.zero, Vector2.zero);
            Button select = CreateButton("Select", card.transform, "SELECT", new Vector2(0f, -218f), new Vector2(290f, 62f));
            status = select.GetComponentInChildren<Text>(); Bind(select, action);
        }

        private static void BuildTuningPanel(Transform safe, PreStartMenuUI menu, out GameObject panel,
            out Text[] nameTexts, out Text[] levels, out Text[] values, out Text[] costs, out Button[] buttons, out Text coins)
        {
            panel = CreateImage("Tuning Panel", safe, new Color(0.035f, 0.06f, 0.11f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1450f, 900f));
            CreateText("Title", panel.transform, "TUNE CAR 01", 42, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(320f, -52f), new Vector2(520f, 70f));
            coins = CreateText("Coins", panel.transform, "COINS 0", 28, TextAnchor.UpperRight,
                Vector2.one, Vector2.one, new Vector2(-190f, -38f), new Vector2(330f, 60f));
            string[] labels = { "SPEED", "ACCELERATION", "SUSPENSION", "GRIP", "FUEL" };
            UnityAction[] actions = { menu.UpgradeEngine, menu.UpgradeAcceleration, menu.UpgradeSuspension,
                menu.UpgradeFourth, menu.UpgradeFuel };
            actions[2] = menu.UpgradeThird;
            nameTexts = new Text[5];
            levels = new Text[5];
            values = new Text[5];
            costs = new Text[5];
            buttons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                float y = 300f - i * 142f;
                GameObject row = CreateImage(labels[i] + " Row", panel.transform, new Color(0.065f, 0.12f, 0.20f, 0.96f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1330f, 122f));
                nameTexts[i] = CreateText(labels[i] + " Name", row.transform, labels[i], 27, TextAnchor.MiddleLeft,
                    new Vector2(0.025f, 0f), new Vector2(0.23f, 1f), Vector2.zero, Vector2.zero);
                nameTexts[i].fontStyle = FontStyle.Bold;
                levels[i] = CreateText(labels[i] + " Tune", row.transform, "TUNE 0 / 0", 22, TextAnchor.MiddleLeft,
                    new Vector2(0.23f, 0f), new Vector2(0.40f, 1f), Vector2.zero, Vector2.zero);
                values[i] = CreateText(labels[i] + " Values", row.transform, "CURRENT -> NEXT", 20, TextAnchor.MiddleLeft,
                    new Vector2(0.40f, 0f), new Vector2(0.76f, 1f), Vector2.zero, Vector2.zero);
                buttons[i] = CreateButton(labels[i] + " Upgrade", row.transform, "TUNE", new Vector2(530f, 0f), new Vector2(285f, 78f));
                costs[i] = buttons[i].GetComponentInChildren<Text>();
                Bind(buttons[i], actions[i]);
            }
            Button close = CreateButton("Close", panel.transform, "BACK TO SETUP", new Vector2(0f, -405f), new Vector2(360f, 66f));
            Bind(close, menu.CloseTuning);
        }

        private static void BuildAdventure2_5D(BuiltAssets assets)
        {
            Scene scene = NewScene();
            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<GameplayBootstrap>().Configure(GameModeType.Adventure2_5D,
                assets.countryside.mapId, true);
            VehicleInputController input = systems.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure2_5D);
            GameManager manager = systems.AddComponent<GameManager>();
            PickupPool pool = CreatePickupPool(assets);
            TerrainManager terrain = new GameObject("Terrain Manager").AddComponent<TerrainManager>();
            terrain.AssignRuntimeReferences(assets.countryside, null, pool);
            VehicleCamera camera = CreateClimbCamera(null);
            CreateCountrysideBackground(camera.GetComponent<Camera>(), assets.countrysideEnvironment);
            CreateSunsetLighting(false);
            Canvas canvas = CreateCanvas(Color.clear, false);
            Transform safe = CreateSafeArea(canvas.transform);
            GameplayHUD hud = CreateGameplayHud(safe, input, assets);
            ResultsUI results = CreateResults(safe, manager, out RawImage resultImage);
            CanvasGroup controls = CreateClimbControls(safe, input);
            ResultCaptureService capture = CreateResultCapture(null, resultImage, true);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureAdventure2D(assets.catalog, input, manager, hud,
                results, capture, controls, null, terrain, assets.countryside, pool, camera);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, Adventure2DPath);
        }

        private static void BuildAdventure3D(BuiltAssets assets)
        {
            Scene scene = NewScene();
            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<GameplayBootstrap>().Configure(GameModeType.Adventure3D, assets.countrysideRoad.mapId);
            VehicleInputController input = systems.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.Adventure3D);
            GameManager manager = systems.AddComponent<GameManager>();
            PickupPool pool = CreatePickupPool(assets);
            RoadTrackGenerator road = new GameObject("Road Track Generator").AddComponent<RoadTrackGenerator>();
            road.Assign(assets.countrysideRoad, null, pool);
            RoadChaseCamera camera = CreateRoadCamera(null);
            CreateSunsetLighting(false);
            Canvas canvas = CreateCanvas(Color.clear, false);
            Transform safe = CreateSafeArea(canvas.transform);
            GameplayHUD hud = CreateGameplayHud(safe, input, assets);
            ResultsUI results = CreateResults(safe, manager, out RawImage resultImage);
            CanvasGroup controls = CreateRoadControls(safe, input);
            ResultCaptureService capture = CreateResultCapture(null, resultImage);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureAdventure3D(assets.catalog, input, manager, hud,
                results, capture, controls, null, road, assets.countrysideRoad, pool, camera);
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, Adventure3DPath);
        }

        private static void BuildTimeTrial3D(BuiltAssets assets)
        {
            Scene scene = NewScene();
            GameObject systems = new GameObject("Time Trial Systems");
            systems.AddComponent<GameplayBootstrap>().Configure(GameModeType.TimeTrial3D, assets.testCircuit.mapId);
            VehicleInputController input = systems.AddComponent<VehicleInputController>();
            input.SetInputMode(GameModeType.TimeTrial3D);
            TimeTrialManager manager = systems.AddComponent<TimeTrialManager>();
            float radius = assets.testCircuit.targetLength / (Mathf.PI * 2f);
            TimeTrialCircuitGenerator circuit = new GameObject("Procedural Test Circuit").AddComponent<TimeTrialCircuitGenerator>();
            circuit.Assign(assets.testCircuit, manager);
            RoadChaseCamera camera = CreateRoadCamera(null);
            CreateSunsetLighting(false);
            Canvas canvas = CreateCanvas(Color.clear, false);
            Transform safe = CreateSafeArea(canvas.transform);
            TimeTrialHUD hud = CreateTimeTrialHud(safe, input);
            ResultsUI results = CreateResults(safe, null, out RawImage resultImage);
            results.AssignTimeTrial(manager);
            CanvasGroup controls = CreateRoadControls(safe, input);
            Button respawn = CreateButton("Respawn", controls.transform, "RESPAWN +3s", new Vector2(0f, 310f), new Vector2(270f, 70f));
            Bind(respawn, manager.Respawn);
            Button horizontalJump = CreateButton("Horizontal Jump", controls.transform, "HORIZONTAL JUMP  +2m",
                new Vector2(0f, 225f), new Vector2(330f, 70f));
            horizontalJump.image.color = new Color(.48f, .16f, .68f, .98f);
            horizontalJump.GetComponent<Outline>().effectColor = new Color(.94f, .45f, 1f, .95f);
            respawn.image.color = new Color(.42f, .22f, .08f, .96f);
            respawn.GetComponent<Outline>().effectColor = new Color(1f, .62f, .20f, .9f);
            Bind(horizontalJump, manager.HorizontalJump);
            ResultCaptureService capture = CreateResultCapture(null, resultImage);
            systems.AddComponent<SelectedVehicleFactory>().ConfigureTimeTrial(assets.catalog, input, manager, circuit,
                assets.testCircuit, hud, results, capture, controls, null, camera, new Vector3(radius, 3f, -5f));
            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, TimeTrialPath);
        }

        private static VehicleController CreateClimbVehicle(VehicleDefinition definition, VehicleInputController input,
            GameManager manager, PhysicsMaterial chassisMaterial = null, PhysicsMaterial tireMaterial = null)
        {
            bool formula = definition.archetype == VehicleArchetype.Formula;
            bool rally = definition.archetype == VehicleArchetype.Rally;
            bool sportBike = definition.archetype == VehicleArchetype.SportBike;
            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0;
            collider.center = new Vector3(formula ? 0f : rally ? .01f : sportBike ? -.03f : .06f,
                formula ? .29f : rally ? .33f : sportBike ? .18f : .24f, 0f);
            collider.radius = formula ? .34f : rally ? .38f : sportBike ? .27f : .40f;
            collider.height = formula ? 3.65f : rally ? 4.05f : sportBike ? 2.95f : 2.90f;
            collider.sharedMaterial = chassisMaterial;
            root.AddComponent<MovementPlaneConstraint>(); root.AddComponent<FuelSystem>();
            root.AddComponent<EngineController>(); root.AddComponent<TransmissionController>();
            VehicleController controller = root.AddComponent<VehicleController>();
            CrashDetector crash = root.AddComponent<CrashDetector>(); crash.AssignGameManager(manager);
            VehicleVisual2D visual2D = CreateVehicleVisual2D(root.transform, controller, definition);
            SuspensionController[] points = new SuspensionController[2];
            points[0] = CreateClimbSuspension(root.transform, definition, definition.RearWheelAnchor,
                visual2D.RearWheelSprite, tireMaterial);
            points[1] = CreateClimbSuspension(root.transform, definition, definition.FrontWheelAnchor,
                visual2D.FrontWheelSprite, tireMaterial);
            controller.AssignRuntimeReferences(definition, input, points);
            return controller;
        }

        private static RoadVehicleController CreateRoadVehicle(VehicleDefinition definition, VehicleInputController input,
            GameManager manager, PhysicsMaterial chassisMaterial = null)
        {
            bool formula = definition.archetype == VehicleArchetype.Formula;
            bool rally = definition.archetype == VehicleArchetype.Rally;
            bool sportBike = definition.archetype == VehicleArchetype.SportBike;
            GameObject root = new GameObject("VehiclePhysics - " + definition.displayName);
            root.transform.position = Vector3.zero;
            root.AddComponent<Rigidbody>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 2;
            collider.center = new Vector3(0f, formula ? .38f : rally ? .40f : sportBike ? .30f : .36f, 0f);
            collider.radius = formula ? .62f : rally ? .66f : sportBike ? .48f : .72f;
            collider.height = formula ? 4.20f : rally ? 4.35f : sportBike ? 3.05f : 3.05f;
            collider.sharedMaterial = chassisMaterial;
            root.AddComponent<FuelSystem>(); root.AddComponent<EngineController>(); root.AddComponent<TransmissionController>();
            RoadVehicleController controller = root.AddComponent<RoadVehicleController>();
            CrashDetector crash = root.AddComponent<CrashDetector>(); crash.AssignGameManager(manager);
            controller.Assign(definition, input);
            CreateVehicleVisual3D(root.transform, controller, definition);
            return controller;
        }

        private static void CreateVehicleVisual3D(Transform root, RoadVehicleController controller,
            VehicleDefinition definition)
        {
            Transform visual = new GameObject("VehicleVisual3D - " + definition.displayName).transform;
            visual.SetParent(root, false);
            Sprite bodySprite = definition.skins != null && definition.skins.Length > 0 ? definition.skins[0].sprite : definition.previewSprite;
            Sprite wheelSprite = definition.wheels != null && definition.wheels.Length > 0 ? definition.wheels[0].sprite : null;
            SpriteRenderer[] bodyRenderers = new SpriteRenderer[2];
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -.06f : .06f;
                Transform body = CreateSpriteVisual(definition.displayName + (side == 0 ? " Body Left" : " Body Right"),
                    visual, bodySprite, new Vector3(x, definition.bodyVisualOffset.y, definition.bodyVisualOffset.x),
                    definition.visualWidth, 10);
                body.localRotation = Quaternion.Euler(0f, side == 0 ? -90f : 90f, 0f);
                bodyRenderers[side] = body.GetComponent<SpriteRenderer>();
                bodyRenderers[side].flipX = definition.flipBodyX ^ (side == 1);
            }
            float[] xs = { -definition.halfTrack - .03f, definition.halfTrack + .03f };
            Transform[] wheels = new Transform[4];
            SpriteRenderer[] wheelRenderers = new SpriteRenderer[4];
            bool[] frontWheels = new bool[4];
            int wheelIndex = 0;
            for (int z = 0; z < 2; z++) for (int x = 0; x < 2; x++)
            {
                bool front = z == 1;
                Vector2 wheelPosition = front ? definition.frontWheelVisualPosition : definition.rearWheelVisualPosition;
                Transform wheel = CreateWheelVisual(front ? "Front Wheel" : "Rear Wheel", visual, wheelSprite,
                    new Vector3(xs[x], wheelPosition.y, wheelPosition.x),
                    front ? definition.FrontWheelDiameter : definition.RearWheelDiameter, 11,
                    VehicleDefinition.WheelSourceDiameter(definition.wheels[0]));
                wheel.localRotation = Quaternion.Euler(0f, x == 0 ? -90f : 90f, 0f);
                wheelRenderers[wheelIndex] = wheel.GetComponent<SpriteRenderer>();
                if (x == 1) wheelRenderers[wheelIndex].flipX = true;
                wheels[wheelIndex] = wheel;
                frontWheels[wheelIndex] = z == 1;
                wheelIndex++;
            }
            DriverDefinition defaultDriver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset");
            ArticulatedDriver2D driver = CreateArticulatedDriver(visual, defaultDriver, definition, root.GetComponent<Rigidbody>(), true);
            driver.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            VehicleVisual3D component = visual.gameObject.AddComponent<VehicleVisual3D>();
            component.Assign(controller, wheels, frontWheels, definition.wheelRadius, Vector3.forward);
            component.AssignCustomization(definition, bodyRenderers, wheelRenderers, driver, defaultDriver);
            CreateRallyRoofSensor(root, visual, definition, bodyRenderers, true);
        }

        private static VehicleVisual2D CreateVehicleVisual2D(Transform root, VehicleRuntimeBase vehicle,
            VehicleDefinition definition)
        {
            Transform visual = new GameObject("VehicleVisual2D - " + definition.displayName).transform;
            visual.SetParent(root, false);
            Sprite bodySprite = definition.skins != null && definition.skins.Length > 0 ? definition.skins[0].sprite : definition.previewSprite;
            Sprite wheelSprite = definition.wheels != null && definition.wheels.Length > 0 ? definition.wheels[0].sprite : null;
            Transform body = CreateSpriteVisual(definition.displayName + " Body", visual, bodySprite,
                new Vector3(definition.bodyVisualOffset.x, definition.bodyVisualOffset.y, 0f), definition.visualWidth, 10);
            body.GetComponent<SpriteRenderer>().flipX = definition.flipBodyX;
            Transform rearWheel = CreateWheelVisual("Rear Wheel", visual, wheelSprite,
                new Vector3(definition.rearWheelVisualPosition.x, definition.rearWheelVisualPosition.y, -.04f),
                definition.RearWheelDiameter, 11,
                VehicleDefinition.WheelSourceDiameter(definition.wheels[0]));
            Transform frontWheel = CreateWheelVisual("Front Wheel", visual, wheelSprite,
                new Vector3(definition.frontWheelVisualPosition.x, definition.frontWheelVisualPosition.y, -.04f),
                definition.FrontWheelDiameter, 11,
                VehicleDefinition.WheelSourceDiameter(definition.wheels[0]));
            DriverDefinition defaultDriver = AssetDatabase.LoadAssetAtPath<DriverDefinition>(Root + "/VehicleData/StickmanBlack.asset");
            ArticulatedDriver2D articulated = CreateArticulatedDriver(visual, defaultDriver, definition,
                root.GetComponent<Rigidbody>(), true);
            VehicleVisual2D component = visual.gameObject.AddComponent<VehicleVisual2D>();
            component.Assign(vehicle, body, frontWheel, rearWheel, articulated.transform, definition.wheelRadius);
            component.AssignCustomization(definition, body.GetComponent<SpriteRenderer>(),
                frontWheel.GetComponent<SpriteRenderer>(), rearWheel.GetComponent<SpriteRenderer>(), articulated, defaultDriver);
            CreateRallyRoofSensor(root, visual, definition,
                new[] { body.GetComponent<SpriteRenderer>() }, false);
            return component;
        }

        private static void CreateRallyRoofSensor(Transform vehicleRoot, Transform visualRoot,
            VehicleDefinition definition, SpriteRenderer[] bodyRenderers, bool roadMode)
        {
            if (definition.archetype != VehicleArchetype.Rally) return;
            GameObject sensor = new GameObject("Rally Roof Terrain Impact Sensor");
            sensor.transform.SetParent(vehicleRoot, false);
            sensor.transform.localPosition = roadMode ? new Vector3(0f, .92f, 0f) : new Vector3(-.05f, .92f, 0f);
            BoxCollider trigger = sensor.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = roadMode ? new Vector3(1.25f, .18f, 1.55f) : new Vector3(1.85f, .18f, .5f);
            RallyRoofDamage damage = sensor.AddComponent<RallyRoofDamage>();
            damage.Assign(definition, vehicleRoot.GetComponent<Rigidbody>(), bodyRenderers, visualRoot);
        }

        private static Transform CreateSpriteVisual(string name, Transform parent, Sprite sprite,
            Vector3 localPosition, float targetWidth, int sortingOrder)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            gameObject.transform.localScale = Vector3.one *
                                              VehicleDefinition.SpriteWidthScale(sprite, targetWidth);
            return gameObject.transform;
        }

        private static Transform CreateWheelVisual(string name, Transform parent, Sprite sprite,
            Vector3 localPosition, float targetDiameter, int sortingOrder, float visibleDiameter = 0f)
        {
            Transform wheel = CreateSpriteVisual(name, parent, sprite, localPosition, targetDiameter, sortingOrder);
            wheel.localScale = Vector3.one *
                               VehicleDefinition.WheelVisualScale(sprite, targetDiameter, visibleDiameter);
            return wheel;
        }

        private static ArticulatedDriver2D CreateArticulatedDriver(Transform parent, DriverDefinition definition,
            VehicleDefinition vehicle, Rigidbody body, bool physicalReaction)
        {
            Transform root = new GameObject("Driver - Articulated").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 0f, -.03f);
            SpriteRenderer Part(string name)
            {
                GameObject part = new GameObject(name);
                part.transform.SetParent(root, false);
                return part.AddComponent<SpriteRenderer>();
            }
            SpriteRenderer torso = Part("Torso");
            SpriteRenderer head = Part("Head");
            SpriteRenderer leftUpperArm = Part("Left Upper Arm");
            SpriteRenderer leftForearm = Part("Left Forearm / Hand");
            SpriteRenderer rightUpperArm = Part("Right Upper Arm");
            SpriteRenderer rightForearm = Part("Right Forearm / Hand");
            SpriteRenderer leftThigh = Part("Left Thigh");
            SpriteRenderer leftLowerLeg = Part("Left Lower Leg");
            SpriteRenderer rightThigh = Part("Right Thigh");
            SpriteRenderer rightLowerLeg = Part("Right Lower Leg");
            SpriteRenderer accessory = Part("Accessory Anchor");
            GameObject impactObject = new GameObject("Head Terrain Reaction Trigger");
            impactObject.transform.SetParent(head.transform, false);
            impactObject.transform.localPosition = Vector3.zero;
            SphereCollider impactCollider = impactObject.AddComponent<SphereCollider>();
            impactCollider.isTrigger = true; impactCollider.radius = .27f;
            DriverHeadImpact impact = impactObject.AddComponent<DriverHeadImpact>();
            ArticulatedDriver2D articulated = root.gameObject.AddComponent<ArticulatedDriver2D>();
            articulated.AssignParts(torso, head, leftUpperArm, leftForearm, rightUpperArm, rightForearm,
                leftThigh, leftLowerLeg, rightThigh, rightLowerLeg, impact);
            articulated.AssignAccessory(accessory);
            articulated.Apply(definition, vehicle.driverPose, body, physicalReaction);
            return articulated;
        }

        private static SuspensionController CreateClimbSuspension(Transform root, VehicleDefinition definition,
            float x, Transform wheelVisual, PhysicsMaterial tireMaterial)
        {
            GameObject origin = new GameObject(x < 0f ? "Rear Suspension" : "Front Suspension");
            origin.transform.SetParent(root, false);
            origin.transform.localPosition = new Vector3(x, -definition.suspension.restLength, 0f);
            Rigidbody wheelBody = origin.AddComponent<Rigidbody>();
            wheelBody.mass = definition.wheelMass;
            wheelBody.solverIterations = 12;
            wheelBody.solverVelocityIterations = 8;
            wheelBody.interpolation = RigidbodyInterpolation.Interpolate;
            wheelBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            wheelBody.constraints = RigidbodyConstraints.FreezePositionZ |
                                    RigidbodyConstraints.FreezeRotationX |
                                    RigidbodyConstraints.FreezeRotationY;
            SphereCollider wheelCollider = origin.AddComponent<SphereCollider>();
            wheelCollider.radius = definition.wheelRadius;
            wheelCollider.sharedMaterial = tireMaterial;
            ConfigurableJoint joint = root.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = wheelBody;
            joint.enableCollision = false;
            SuspensionController suspension = origin.AddComponent<SuspensionController>();
            suspension.AssignWheelVisual(wheelVisual);
            suspension.AssignPhysicalReferences(wheelBody, wheelCollider, joint);
            return suspension;
        }

        private static AnimationCurve ArcadeSpeedTorqueCurve() => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(.55f, 1f), new Keyframe(.82f, .68f),
            new Keyframe(.96f, .22f), new Keyframe(1.04f, 0f));

        private static PickupPool CreatePickupPool(BuiltAssets assets)
        {
            GameObject root = new GameObject("Pickup Pool");
            PickupPool pool = root.AddComponent<PickupPool>();
            CoinPickup coinOne = CreateCoinTemplate("Coin 01 Template", root.transform, assets.coin01, 1);
            CoinPickup coinFive = CreateCoinTemplate("Coin 05 Template", root.transform, assets.coin05, 5);
            GameObject fuelObject = new GameObject("Fuel Can Template");
            fuelObject.transform.SetParent(root.transform, false);
            GameObject fuelVisual = new GameObject("Fuel Can Visual");
            fuelVisual.transform.SetParent(fuelObject.transform, false);
            SpriteRenderer fuelRenderer = fuelVisual.AddComponent<SpriteRenderer>();
            fuelRenderer.sprite = assets.fuelCan;
            fuelRenderer.sortingOrder = 12;
            float fuelSourceHeight = assets.fuelCan != null ? assets.fuelCan.bounds.size.y : 1f;
            fuelVisual.transform.localScale = Vector3.one * (1.10f / Mathf.Max(.001f, fuelSourceHeight));
            BoxCollider fuelTrigger = fuelObject.AddComponent<BoxCollider>();
            fuelTrigger.isTrigger = true;
            fuelTrigger.size = new Vector3(.96f, 1.06f, .66f);
            FuelPickup fuel = fuelObject.AddComponent<FuelPickup>();
            pool.AssignPrefabs(coinOne, coinFive, fuel);
            coinOne.gameObject.SetActive(false); coinFive.gameObject.SetActive(false); fuelObject.SetActive(false);
            return pool;
        }

        private static CoinPickup CreateCoinTemplate(string name, Transform parent, Sprite sprite, int value)
        {
            GameObject coinObject = new GameObject(name);
            coinObject.transform.SetParent(parent, false);
            GameObject visual = new GameObject("Coin Visual");
            visual.transform.SetParent(coinObject.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;
            float sourceDiameter = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            visual.transform.localScale = Vector3.one * (.82f / Mathf.Max(.001f, sourceDiameter));
            SphereCollider trigger = coinObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = .40f;
            CoinPickup pickup = coinObject.AddComponent<CoinPickup>();
            pickup.AssignValue(value);
            return pickup;
        }

        private static GameplayHUD CreateGameplayHud(Transform safe, VehicleInputController input, BuiltAssets assets)
        {
            GameObject root = UIObject("Gameplay HUD", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject distanceCard = CreateHudCard("Distance Card", root.transform, new Vector2(.5f, 1f),
                new Vector2(0f, -58f), new Vector2(430f, 84f));
            Text distance = CreateText("Distance", distanceCard.transform, "DISTANCE\n0 m", 32,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject bestCard = CreateHudCard("Best Card", root.transform, new Vector2(0f, 1f),
                new Vector2(170f, -48f), new Vector2(280f, 70f));
            Text best = CreateText("Best", bestCard.transform, "BEST\n0 m", 25, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject coinCard = CreateHudCard("Coin Balance Card", root.transform, Vector2.one,
                new Vector2(-170f, -48f), new Vector2(280f, 70f));
            Image coinIcon = CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01,
                new Vector2(.5f, .5f), new Vector2(-98f, 0f), new Vector2(52f, 52f));
            Text coins = CreateText("Coins", coinCard.transform, "0", 30, TextAnchor.MiddleRight,
                Vector2.zero, Vector2.one, new Vector2(-22f, 0f), new Vector2(-28f, 0f));
            GameObject nextFuelCard = CreateHudCard("Next Fuel Card", root.transform, new Vector2(.5f, 1f),
                new Vector2(0f, -148f), new Vector2(390f, 62f));
            Image nextFuelIcon = CreateSpriteImage("Next Fuel Icon", nextFuelCard.transform, assets.fuelCan,
                new Vector2(.5f, .5f), new Vector2(-155f, 0f), new Vector2(48f, 48f));
            Text nextFuel = CreateText("Next Fuel", nextFuelCard.transform, "NEXT FUEL   ---", 23,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(25f, 0f), new Vector2(-60f, 0f));
            GameObject fuelCard = CreateHudCard("Fuel Card", root.transform, new Vector2(0f, 1f),
                new Vector2(205f, -132f), new Vector2(350f, 70f));
            Image fuelIcon = CreateSpriteImage("Fuel Icon", fuelCard.transform, assets.fuelCan,
                new Vector2(.5f, .5f), new Vector2(-145f, 0f), new Vector2(52f, 52f));
            Text fuelText = CreateText("Fuel Text", fuelCard.transform, "100%", 21, TextAnchor.MiddleCenter,
                new Vector2(.12f, 0f), new Vector2(.35f, 1f), Vector2.zero, Vector2.zero);
            GameObject fuelBack = CreateImage("Fuel Background", fuelCard.transform,
                new Color(0.015f, 0.02f, 0.035f, 0.92f), new Vector2(.36f, .5f), new Vector2(.36f, .5f),
                new Vector2(92f, 0f), new Vector2(190f, 20f));
            GameObject fuelFillObject = CreateImage("Fuel Fill", fuelBack.transform, new Color(0.18f, 0.88f, 0.34f),
                Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-8f, -8f));
            Image fuelFill = fuelFillObject.GetComponent<Image>(); fuelFill.type = Image.Type.Filled;
            fuelFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fuelFill.fillMethod = Image.FillMethod.Horizontal;
            fuelFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fuelFill.fillAmount = 1f;
            GameObject liquidCard = CreateHudCard("Liquid Exposure Card", root.transform, new Vector2(0f, 1f),
                new Vector2(205f, -205f), new Vector2(350f, 58f));
            Text liquidText = CreateText("Liquid Exposure Text", liquidCard.transform, "LIQUID  0.0 / 10.0s", 18,
                TextAnchor.MiddleLeft, new Vector2(.05f, .46f), new Vector2(.95f, .95f), Vector2.zero, Vector2.zero);
            GameObject liquidBack = CreateImage("Liquid Background", liquidCard.transform,
                new Color(.01f, .03f, .06f, .95f), new Vector2(.05f, .16f), new Vector2(.95f, .43f),
                Vector2.zero, Vector2.zero);
            Image liquidFill = CreateImage("Liquid Fill", liquidBack.transform,
                new Color(.08f, .88f, 1f, 1f), Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-6f, -6f)).GetComponent<Image>();
            liquidFill.type = Image.Type.Filled;
            liquidFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            liquidFill.fillMethod = Image.FillMethod.Horizontal;
            liquidFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            liquidFill.fillAmount = 0f;
            RectTransform rpmNeedle = CreateVehicleGauge("RPM Gauge", root.transform,
                assets.rpmGauge, assets.rpmNeedle, new Vector2(-150f, 118f), 200f,
                new Vector2(.499694f, .357008f), new Vector2(.136906f, .202061f), 104f, 159f);
            RectTransform speedNeedle = CreateVehicleGauge("KMH Gauge", root.transform,
                assets.speedGauge, assets.speedNeedle, new Vector2(150f, 118f), 200f,
                new Vector2(.499704f, .316062f), new Vector2(.138627f, .166769f), 116f, 172f);
            Text status = CreateText("Status", root.transform, "", 34, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(500f, 60f));
            Image lowFuelWarning = CreateSpriteImage("Low Fuel Warning", root.transform,
                AssetDatabase.LoadAssetAtPath<Sprite>(HillLowFuelPath), new Vector2(.5f, 1f),
                new Vector2(0f, -278f), new Vector2(300f, 200f));
            lowFuelWarning.gameObject.SetActive(false);
            GameObject debugPanel = CreateImage("Debug Panel", root.transform, new Color(0f, 0f, 0f, 0.75f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 80f), new Vector2(540f, 570f));
            Text debug = CreateText("Debug Text", debugPanel.transform, "", 20, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(15f, -15f), new Vector2(-30f, -30f));
            GameplayHUD hud = root.AddComponent<GameplayHUD>();
            hud.Assign(distance, best, coins, coinIcon, nextFuel, nextFuelIcon, fuelText, fuelIcon, fuelFill,
                speedNeedle, rpmNeedle, status,
                debugPanel, debug, input, nextFuelCard);
            hud.AssignLiquidExposure(liquidCard, liquidText, liquidFill);
            hud.AssignLowFuelWarning(lowFuelWarning);
            debugPanel.SetActive(false);
            return hud;
        }

        private static ResultsUI CreateResults(Transform safe, GameManager manager, out RawImage resultImage)
        {
            GameObject panel = CreateImage("Results Panel", safe, new Color(0f, 0f, 0f, 0.50f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1540f, 820f));
            Outline resultsOutline = panel.AddComponent<Outline>();
            resultsOutline.effectColor = new Color(0f, 0f, 0f, .50f);
            resultsOutline.effectDistance = new Vector2(2f, -2f);
            Text title = CreateText("Result Title", panel.transform, "DRIVER DOWN", 46,
                TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -62f), new Vector2(1400f, 82f));
            title.fontStyle = FontStyle.Bold;
            GameObject photoFrame = CreateImage("Result Photo Frame", panel.transform,
                new Color(.035f, .08f, .14f, 1f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(-390f, -15f), new Vector2(650f, 650f));
            Outline photoOutline = photoFrame.AddComponent<Outline>();
            photoOutline.effectColor = new Color(.18f, .74f, 1f, .9f);
            photoOutline.effectDistance = new Vector2(2f, -2f);
            GameObject photo = UIObject("Result Photo", photoFrame.transform, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(610f, 610f));
            resultImage = photo.AddComponent<RawImage>(); resultImage.color = Color.white;
            resultImage.raycastTarget = false;
            Text text = CreateText("Result Stats", panel.transform, "DISTANCE", 27, TextAnchor.UpperLeft,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(390f, -5f),
                new Vector2(610f, 590f));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            ResultsUI results = panel.AddComponent<ResultsUI>(); results.Assign(panel, title, text, manager);
            Button retry = CreateButton("Retry", panel.transform, "RETRY", new Vector2(225f, -355f), new Vector2(300f, 72f));
            Button menu = CreateButton("Main Menu", panel.transform, "HOME", new Vector2(565f, -355f), new Vector2(300f, 72f));
            Bind(retry, results.Retry); Bind(menu, results.Menu); panel.SetActive(false);
            return results;
        }

        private static CanvasGroup CreateClimbControls(Transform safe, VehicleInputController input)
        {
            GameObject root = UIObject("Gameplay Controls", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            CreateHalfScreenPedal("Brake", root.transform, input, HoldControl.Brake,
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPressedPath),
                Vector2.zero, new Vector2(.5f, 1f), Vector2.zero, new Vector2(165f, 177f));
            CreateHalfScreenPedal("Gas", root.transform, input, HoldControl.Gas,
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPressedPath),
                new Vector2(.5f, 0f), Vector2.one, new Vector2(1f, 0f), new Vector2(-165f, 177f));
            return group;
        }

        private static CanvasGroup CreateRoadControls(Transform safe, VehicleInputController input)
        {
            GameObject root = UIObject("Gameplay Controls", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            Button left = CreateButton("Left", root.transform, "LEFT", new Vector2(155f, 290f), new Vector2(250f, 120f), Vector2.zero);
            Button right = CreateButton("Right", root.transform, "RIGHT", new Vector2(435f, 290f), new Vector2(250f, 120f), Vector2.zero);
            left.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Left);
            right.gameObject.AddComponent<HoldButton>().Assign(input, HoldControl.Right);
            CreatePedalButton("Brake", root.transform, input, HoldControl.Brake,
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(BrakePedalPressedPath),
                Vector2.zero, new Vector2(156f, 147f), new Vector2(219f, 274f));
            CreatePedalButton("Gas", root.transform, input, HoldControl.Gas,
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(GasPedalPressedPath),
                new Vector2(1f, 0f), new Vector2(-156f, 147f), new Vector2(219f, 274f));
            return group;
        }

        private static ShifterUI CreateShifter(Transform safe, VehicleRuntimeBase vehicle, VehicleInputController input,
            GameModeType mode)
        {
            Vector2 size = mode == GameModeType.Adventure2_5D ? new Vector2(330f, 120f) : new Vector2(145f, 250f);
            GameObject root = CreateImage("Visible Shifter", safe, new Color(0.04f, 0.05f, 0.08f, 0.88f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), size);
            Text directions = CreateText("Directions", root.transform, "", 22, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject knobObject = CreateImage("Shifter Knob", root.transform, new Color(0.92f, 0.22f, 0.18f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
            ShifterUI shifter = root.AddComponent<ShifterUI>();
            shifter.Assign(vehicle, input, knobObject.GetComponent<RectTransform>(), directions, mode);
            return shifter;
        }

        private static ResultCaptureService CreateResultCapture(VehicleRuntimeBase vehicle, RawImage resultImage,
            bool sideView = false)
        {
            GameObject root = new GameObject("Result Capture Service");
            Camera camera = new GameObject("ResultCaptureCamera").AddComponent<Camera>();
            camera.transform.SetParent(root.transform, false); camera.fieldOfView = 52f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.11f, 0.17f, 0.25f);
            camera.orthographic = sideView;
            if (sideView) camera.orthographicSize = 12.5f;
            ResultCaptureService service = root.AddComponent<ResultCaptureService>();
            service.Assign(camera, vehicle != null ? vehicle.transform : null, resultImage);
            return service;
        }

        private static TimeTrialHUD CreateTimeTrialHud(Transform safe, VehicleInputController input)
        {
            GameObject root = UIObject("Time Trial HUD", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject timerCard = CreateHudCard("Timer Card", root.transform, new Vector2(.5f, 1f),
                new Vector2(0f, -58f), new Vector2(430f, 92f));
            Text timer = CreateText("Time", timerCard.transform, "TIME\n00:00.000", 36, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject lapCard = CreateHudCard("Lap Card", root.transform, Vector2.up,
                new Vector2(155f, -52f), new Vector2(270f, 78f));
            Text lap = CreateText("Lap", lapCard.transform, "LAP\n1 / 1", 28, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            GameObject bestCard = CreateHudCard("Best Lap Card", root.transform, Vector2.one,
                new Vector2(-180f, -52f), new Vector2(320f, 78f));
            Text best = CreateText("Best Lap", bestCard.transform, "BEST LAP\n--:--.---", 25,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Sprite rpmGauge = AssetDatabase.LoadAssetAtPath<Sprite>(RpmGaugePath);
            Sprite rpmArrow = AssetDatabase.LoadAssetAtPath<Sprite>(RpmNeedlePath);
            Sprite speedGauge = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedGaugePath);
            Sprite speedArrow = AssetDatabase.LoadAssetAtPath<Sprite>(SpeedNeedlePath);
            RectTransform rpmNeedle = CreateVehicleGauge("RPM Gauge", root.transform, rpmGauge, rpmArrow,
                new Vector2(-150f, 118f), 200f, new Vector2(.499694f, .357008f),
                new Vector2(.136906f, .202061f), 104f, 159f);
            RectTransform speedNeedle = CreateVehicleGauge("KMH Gauge", root.transform, speedGauge, speedArrow,
                new Vector2(150f, 118f), 200f, new Vector2(.499704f, .316062f),
                new Vector2(.138627f, .166769f), 116f, 172f);
            Image lowFuelWarning = CreateSpriteImage("Low Fuel Warning", root.transform,
                AssetDatabase.LoadAssetAtPath<Sprite>(HillLowFuelPath), new Vector2(.5f, 1f),
                new Vector2(0f, -150f), new Vector2(280f, 90f));
            lowFuelWarning.raycastTarget = false;
            lowFuelWarning.gameObject.SetActive(false);
            GameObject debugPanel = CreateImage("Debug Panel", root.transform, new Color(0f, 0f, 0f, 0.75f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 80f), new Vector2(500f, 340f));
            Text debug = CreateText("Debug Text", debugPanel.transform, "", 20, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, new Vector2(15f, -15f), new Vector2(-30f, -30f));
            TimeTrialHUD hud = root.AddComponent<TimeTrialHUD>();
            hud.Assign(timer, lap, best, speedNeedle, rpmNeedle, debugPanel, debug, input);
            hud.AssignLowFuelWarning(lowFuelWarning);
            debugPanel.SetActive(false); return hud;
        }

        private static VehicleCamera CreateClimbCamera(VehicleRuntimeBase target)
        {
            GameObject cameraObject = CameraObject(new Color(0.92f, 0.48f, 0.35f));
            cameraObject.transform.position = new Vector3(7.4f, 4.8f, -20f);
            cameraObject.transform.rotation = Quaternion.identity;
            Camera displayCamera = cameraObject.GetComponent<Camera>();
            displayCamera.orthographic = true;
            displayCamera.orthographicSize = 11.5f;
            VehicleCamera followCamera = cameraObject.AddComponent<VehicleCamera>(); followCamera.AssignTarget(target);
            return followCamera;
        }

        private static MountainBackground CreateCountrysideBackground(Camera camera, EnvironmentDefinition environment)
        {
            GameObject root = new GameObject("Countryside Background - Recycled");
            GameObject skyObject = new GameObject("Sky - No Physics");
            skyObject.transform.SetParent(root.transform, false);
            SpriteRenderer sky = skyObject.AddComponent<SpriteRenderer>();
            sky.sprite = environment.sky; sky.sortingOrder = -100;
            int count = Mathf.Clamp(environment.pooledMountainSegments, 3, 7);
            SpriteRenderer[] mountains = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                GameObject segment = new GameObject("Mountain Segment " + (i + 1) + " - Pooled");
                segment.transform.SetParent(root.transform, false);
                mountains[i] = segment.AddComponent<SpriteRenderer>();
                mountains[i].sprite = environment.mountains;
                mountains[i].sortingOrder = -50;
            }
            MountainBackground background = root.AddComponent<MountainBackground>();
            background.Assign(camera, environment, sky, mountains);
            return background;
        }

        private static RoadChaseCamera CreateRoadCamera(VehicleRuntimeBase target)
        {
            GameObject cameraObject = CameraObject(new Color(0.86f, 0.43f, 0.36f));
            cameraObject.transform.position = new Vector3(0f, 9f, -13f);
            cameraObject.GetComponent<Camera>().fieldOfView = 58f;
            RoadChaseCamera camera = cameraObject.AddComponent<RoadChaseCamera>(); camera.Assign(target);
            return camera;
        }

        private static GameObject CameraObject(Color background)
        {
            GameObject cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background; cameraObject.AddComponent<AudioListener>(); return cameraObject;
        }

        private static void CreateMenuCamera(Color background)
        {
            GameObject cameraObject = CameraObject(background);
            cameraObject.name = "MainMenuCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.targetDisplay = 0;
            camera.enabled = true;
        }

        private static void CreateSunsetLighting(bool addSunDisc)
        {
            GameObject lightObject = new GameObject("Sun Light"); lightObject.transform.rotation = Quaternion.Euler(70f, -25f, 0f);
            Light light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 0.95f;
            light.shadows = LightShadows.None; RenderSettings.ambientLight = new Color(0.48f, 0.38f, 0.40f);
            if (addSunDisc)
            {
                GameObject sun = Primitive(PrimitiveType.Sphere, "Sun", null, new Vector3(30f, 11f, 9f),
                    Vector3.one * 4.5f, new Color(1f, 0.68f, 0.20f)); RemoveCollider(sun);
            }
        }

        private static Canvas CreateCanvas(Color background, bool addBackground = true)
        {
            GameObject canvasObject = new GameObject("UI Canvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            if (addBackground) CreateImage("Background", canvas.transform, background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return canvas;
        }

        private static Transform CreateSafeArea(Transform canvas)
        {
            GameObject safe = UIObject("Safe Area", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.AddComponent<SafeArea>(); return safe.transform;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem"); eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static Material MaterialAsset(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) throw new System.InvalidOperationException("No compatible built-in lit shader was found.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            material.color = color; EditorUtility.SetDirty(material); return material;
        }

        private static PhysicsMaterial PhysicsMaterialAsset(string name, float friction, float bounciness)
        {
            string path = Root + "/Materials/" + name + ".physicMaterial";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(material, path);
            }
            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounciness = bounciness;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material UnlitMaterialAsset(string name, Color color)
        {
            string path = Root + "/Materials/Unlit_" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new System.InvalidOperationException("No compatible built-in unlit shader was found.");
            if (material == null)
            {
                material = new Material(shader) { name = "Unlit_" + name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            material.color = color; EditorUtility.SetDirty(material); return material;
        }

        private static Material UnlitTextureMaterialAsset(string name, Texture texture)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool needsTransparency = name == "CityTerrain" || name == "CityGround" ||
                                     name == "NeonCityTerrain" ||
                                     name == "CountrysideTerrain" || name == "CountrysideGround" ||
                                     name == "NorthPoleTerrain" || name == "NorthPoleGround" ||
                                     name == "NeonCityEdge";
            Shader shader = needsTransparency
                ? Shader.Find("AnimeHillRacing/Transparent Texture") ?? Shader.Find("Sprites/Default")
                : Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new System.InvalidOperationException("No compatible unlit texture shader was found.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader) material.shader = shader;
            material.mainTexture = texture;
            material.color = Color.white;
            bool useSeamlessTerrainTiling = name == "CityTerrain" || name == "CityGround" ||
                                             name == "NeonCityTerrain" ||
                                             name == "CountrysideTerrain" || name == "CountrysideGround" ||
                                             name == "NorthPoleTerrain" || name == "NorthPoleGround" ||
                                             name == "NeonCityEdge";
            if (material.HasProperty("_SeamlessHorizontalTiling"))
                material.SetFloat("_SeamlessHorizontalTiling", useSeamlessTerrainTiling ? 1f : 0f);
            if (material.HasProperty("_HorizontalInset"))
                material.SetFloat("_HorizontalInset", name == "CityGround" ? .008f :
                    name == "CountrysideGround" ? .004f :
                    name == "NorthPoleGround" ? .004f :
                    name == "NeonCityEdge" ? .008f :
                    useSeamlessTerrainTiling ? .012f : 0f);
            if (material.HasProperty("_HorizontalUvRange"))
                material.SetVector("_HorizontalUvRange", name == "CityGround"
                    ? new Vector4(.004f, .688f, 0f, 0f)
                    : name == "CountrysideGround"
                        ? new Vector4(.03f, .695f, 0f, 0f)
                        : name == "NorthPoleGround"
                            ? new Vector4(0f, .714f, 0f, 0f)
                            : new Vector4(0f, 1f, 0f, 0f));
            if (material.HasProperty("_DepthWrite"))
                // CityGround extends above its physical yellow contact line. Depth writing keeps
                // the transparent background sprites from covering that authored upper section.
                material.SetFloat("_DepthWrite", 1f);
            if (name == "CityTerrain" || name == "NeonCityTerrain" ||
                name == "CountrysideTerrain" || name == "NorthPoleTerrain")
                material.renderQueue = 2450;
            else if (name == "CityGround" || name == "CountrysideGround" || name == "NorthPoleGround" ||
                     name == "NeonCityEdge")
                material.renderQueue = 2451;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject FlatQuad(string name, Transform parent, Vector3 position, Vector2 size, Color color)
        {
            GameObject quad = Primitive(PrimitiveType.Quad, name, parent, position,
                new Vector3(size.x, size.y, 1f), color);
            RemoveCollider(quad);
            quad.GetComponent<Renderer>().sharedMaterial = UnlitMaterialAsset(ColorUtility.ToHtmlStringRGB(color), color);
            return quad;
        }

        private static GameObject FlatDisc(string name, Transform parent, Vector3 position, float diameter, Color color)
        {
            GameObject disc = new GameObject(name);
            disc.transform.SetParent(parent, false); disc.transform.localPosition = position;
            disc.transform.localScale = new Vector3(diameter, diameter, 1f);
            disc.AddComponent<MeshFilter>().sharedMesh = LoadOrCreateDiscMesh();
            disc.AddComponent<MeshRenderer>().sharedMaterial = UnlitMaterialAsset(ColorUtility.ToHtmlStringRGB(color), color);
            return disc;
        }

        private static Mesh LoadOrCreateDiscMesh()
        {
            string path = Root + "/Materials/FlatDisc2D.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            const int segments = 32;
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero; uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
                uvs[i + 1] = vertices[i + 1] + Vector3.one * 0.5f;
                int next = (i + 1) % segments;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = next + 1; triangles[i * 3 + 2] = i + 1;
            }
            mesh = new Mesh { name = "FlatDisc2D" };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, Color color)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type); gameObject.name = name;
            if (parent != null) { gameObject.transform.SetParent(parent, false); gameObject.transform.localPosition = position; }
            else gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = MaterialAsset("Primitive_" + ColorUtility.ToHtmlStringRGB(color), color);
            return gameObject;
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
        }

        private static GameObject UIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform)); RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.anchoredPosition = position; rect.sizeDelta = size; return gameObject;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject gameObject = UIObject(name, parent, anchorMin, anchorMax, position, size);
            gameObject.AddComponent<Image>().color = color; return gameObject;
        }

        private static Image CreateSpriteImage(string name, Transform parent, Sprite sprite, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = UIObject(name, parent, anchor, anchor, position, size);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateVehicleGauge(string name, Transform parent, Sprite face, Sprite needle,
            Vector2 position, float faceHeight, Vector2 faceCenter, Vector2 needlePivot,
            float needleTipLength, float zeroAngle)
        {
            float faceAspect = face != null && face.rect.height > 0f ? face.rect.width / face.rect.height : 1.3f;
            GameObject gaugeObject = UIObject(name, parent, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                position, new Vector2(faceHeight * faceAspect, faceHeight));
            Image faceImage = gaugeObject.AddComponent<Image>();
            faceImage.sprite = face; faceImage.preserveAspect = true; faceImage.raycastTarget = false;

            Vector2 sourceTip = needle != null
                ? new Vector2((1f - needlePivot.x) * needle.rect.width,
                    (1f - needlePivot.y) * needle.rect.height)
                : Vector2.one;
            float needleScale = needleTipLength / Mathf.Max(1f, sourceTip.magnitude);
            Vector2 needleSize = needle != null
                ? new Vector2(needle.rect.width * needleScale, needle.rect.height * needleScale)
                : new Vector2(needleTipLength, needleTipLength * .7f);
            GameObject needleObject = UIObject(name + " Needle", gaugeObject.transform,
                faceCenter, faceCenter, Vector2.zero, needleSize);
            RectTransform needleRect = needleObject.GetComponent<RectTransform>();
            needleRect.pivot = needlePivot;
            needleRect.localRotation = Quaternion.Euler(0f, 0f, zeroAngle);
            Image needleImage = needleObject.AddComponent<Image>();
            needleImage.sprite = needle; needleImage.preserveAspect = true; needleImage.raycastTarget = false;
            return needleRect;
        }

        private static HoldButton CreateHalfScreenPedal(string name, Transform parent,
            VehicleInputController input, HoldControl control, Sprite normal, Sprite pressed,
            Vector2 zoneAnchorMin, Vector2 zoneAnchorMax, Vector2 visualAnchor, Vector2 visualPosition)
        {
            GameObject zone = UIObject(name + " Touch Zone", parent, zoneAnchorMin, zoneAnchorMax,
                Vector2.zero, Vector2.zero);
            Image hitArea = zone.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, .001f);
            hitArea.raycastTarget = true;
            Image visual = CreateSpriteImage(name + " Pedal", zone.transform, normal, visualAnchor,
                visualPosition, new Vector2(265f, 334f));
            HoldButton hold = zone.AddComponent<HoldButton>();
            hold.Assign(input, control, visual, normal, pressed);
            return hold;
        }

        private static HoldButton CreatePedalButton(string name, Transform parent, VehicleInputController input,
            HoldControl control, Sprite normal, Sprite pressed, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject pedal = UIObject(name, parent, anchor, anchor, position, size);
            Image visual = pedal.AddComponent<Image>();
            visual.sprite = normal; visual.preserveAspect = true; visual.raycastTarget = true;
            HoldButton hold = pedal.AddComponent<HoldButton>();
            hold.Assign(input, control, visual, normal, pressed);
            return hold;
        }

        private static Image CreateAspectCoverBackground(Transform parent, Sprite sprite)
        {
            GameObject gameObject = UIObject("Garage Background", parent, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            AspectRatioFitter fitter = gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite != null && sprite.rect.height > 0f
                ? sprite.rect.width / sprite.rect.height : 1.5f;
            return image;
        }

        private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = UIObject(name, parent, anchor, anchor, position, size);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(.72f, .86f, 1f, 1f);
            colors.disabledColor = new Color(.35f, .38f, .42f, .65f);
            colors.fadeDuration = .06f;
            button.colors = colors;
            gameObject.AddComponent<ButtonPressFeedback>();
            return button;
        }

        private static GaragePreviewRenderer CreateGarageVehiclePreview(BuiltAssets assets, RawImage target,
            string context)
        {
            GameObject previewCameraObject = new GameObject(context + " Preview Camera");
            previewCameraObject.transform.position = new Vector3(1000f, 0f, -10f);
            Camera previewCamera = previewCameraObject.AddComponent<Camera>();
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 2.35f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            previewCamera.enabled = false;
            Transform previewRoot = new GameObject(context + " Composed Vehicle Preview").transform;
            previewRoot.position = new Vector3(1000f, 0f, 0f);
            VehicleDefinition initial = assets.jeep;
            Transform body = CreateSpriteVisual("Preview Body", previewRoot, assets.jeepBody,
                new Vector3(initial.bodyVisualOffset.x, initial.bodyVisualOffset.y, 0f), initial.visualWidth, 10);
            Transform rearWheel = CreateWheelVisual("Preview Rear Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.rearWheelVisualPosition.x, initial.rearWheelVisualPosition.y, -.02f),
                initial.RearWheelDiameter, 11);
            Transform frontWheel = CreateWheelVisual("Preview Front Wheel", previewRoot, assets.jeepWheel01,
                new Vector3(initial.frontWheelVisualPosition.x, initial.frontWheelVisualPosition.y, -.02f),
                initial.FrontWheelDiameter, 11);
            ArticulatedDriver2D driver = CreateArticulatedDriver(previewRoot, assets.stickmanBlack, initial, null, false);
            GaragePreviewRenderer preview = new GameObject(context + " Preview Renderer").AddComponent<GaragePreviewRenderer>();
            preview.Assign(previewCamera, target, previewRoot, body.GetComponent<SpriteRenderer>(),
                rearWheel.GetComponent<SpriteRenderer>(), frontWheel.GetComponent<SpriteRenderer>(), driver);
            return preview;
        }

        private static GameObject CreateHudCard(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size)
        {
            GameObject card = CreateImage(name, parent, new Color(.025f, .06f, .12f, .88f),
                anchor, anchor, position, size);
            Image image = card.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .56f, .84f, .72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return card;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
        {
            GameObject gameObject = UIObject(name, parent, anchorMin, anchorMax, position, dimensions);
            Text text = gameObject.AddComponent<Text>(); font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font; text.text = value; text.fontSize = size; text.alignment = alignment; text.color = Color.white;
            text.supportRichText = true; text.resizeTextForBestFit = true; text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size; text.raycastTarget = false; return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
            Vector2? anchor = null)
        {
            Vector2 selectedAnchor = anchor ?? new Vector2(0.5f, 0.5f);
            GameObject gameObject = CreateImage(name, parent, new Color(0.13f, 0.30f, 0.53f, 0.95f),
                selectedAnchor, selectedAnchor, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(.72f, .84f, 1f, 1f);
            colors.selectedColor = new Color(.82f, .94f, 1f, 1f);
            colors.disabledColor = new Color(.34f, .38f, .45f, .72f);
            colors.fadeDuration = .06f;
            button.colors = colors;
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.20f, .60f, .90f, .78f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Shadow shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .55f);
            shadow.effectDistance = new Vector2(0f, -5f);
            gameObject.AddComponent<ButtonPressFeedback>();
            Text text = CreateText("Label", gameObject.transform,
                label, 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.fontStyle = FontStyle.Bold; return button;
        }

        private static Button CreateGrayNavButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
            Vector2? anchor = null)
        {
            Vector2 selectedAnchor = anchor ?? new Vector2(0.5f, 0.5f);
            GameObject gameObject = CreateImage(name, parent, new Color(0.18f, 0.22f, 0.28f, 0.82f),
                selectedAnchor, selectedAnchor, position, size);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.62f, 0.68f, 0.76f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.32f, 0.36f, 0.42f, 0.5f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.52f, 0.62f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Shadow shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(0f, -4f);
            gameObject.AddComponent<ButtonPressFeedback>();
            Text text = CreateText("Label", gameObject.transform,
                label, 36, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.92f, 0.94f, 0.97f, 1f);
            Outline textOutline = text.gameObject.AddComponent<Outline>();
            textOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            textOutline.effectDistance = new Vector2(1.5f, -1.5f);
            return button;
        }

        private static Button CreateModernMenuButton(string name, Transform parent, string label, Vector2 position)
        {
            Button button = CreateButton(name, parent, label, position, new Vector2(510f, 88f));
            Image image = button.image;
            image.color = new Color(.055f, .16f, .34f, .96f);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.72f, .82f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = .08f;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            Outline border = button.GetComponent<Outline>();
            border.effectColor = new Color(.25f, .72f, 1f, .92f);
            border.effectDistance = new Vector2(2f, -2f);
            Shadow[] effects = button.GetComponents<Shadow>();
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].GetType() != typeof(Shadow)) continue;
                effects[i].effectColor = new Color(0f, 0f, 0f, .65f);
                effects[i].effectDistance = new Vector2(0f, -7f);
            }
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) { text.fontSize = 29; text.fontStyle = FontStyle.Bold; }
            return button;
        }

        private static void Bind(Button button, UnityAction action)
        {
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static VehicleDefinition.TuneTargets CreateHillAdventurerTuneTargets()
        {
            return new VehicleDefinition.TuneTargets
            {
                engineTopSpeedKmh = new float[]
                {
                    110f, 115f, 120f, 125f, 130f, 135f, 140f, 145f, 150f, 155f,
                    160f, 165f, 170f, 175f, 180f, 183f, 186f, 188f, 189f, 190f
                },
                accelerationZeroToHundredSeconds = new float[]
                {
                    8.8f, 8.4f, 8.0f, 7.6f, 7.2f, 6.8f, 6.4f, 6.0f, 5.6f, 5.3f,
                    5.0f, 4.8f, 4.7f, 4.6f
                },
                gripRatings = new float[]
                {
                    180f, 215f, 250f, 290f, 330f, 375f, 420f, 465f, 510f, 550f,
                    580f, 600f, 612f, 620f
                },
                airControlTorque = new float[] { 3400f },
                airborneDownforceAcceleration = new float[] { 0f },
                fuelRunDurationSeconds = new float[]
                {
                    28f, 29f, 30f, 31f, 33f, 35f, 37f, 39f, 41f, 43f,
                    45f, 47f, 49f, 51f, 52f, 53f, 54f, 55f
                }
            };
        }

        private static VehicleDefinition.DriverPose CreateHillAdventurerDriverPose()
        {
            return new VehicleDefinition.DriverPose
            {
                rootPosition = new Vector2(-0.25f, 0.32f),
                rootScale = 0.92f,
                torso = VehicleDefinition.DriverPartPose.Create(-0.01f, 0.22f, -8f, 0.029f, 0.031f, true, 3),
                head = VehicleDefinition.DriverPartPose.Create(-0.03f, 0.62f, -3f, 0.031f, 0.033f, true, 5),
                leftUpperArm = VehicleDefinition.DriverPartPose.Create(0.18f, 0.34f, -6f, 0.038f, 0.029f, true, 4),
                leftForearm = VehicleDefinition.DriverPartPose.Create(0.36f, 0.24f, 0f, 0.036f, 0.029f, true, 4),
                rightUpperArm = VehicleDefinition.DriverPartPose.Create(0.10f, 0.30f, 2f, 0.038f, 0.029f, true, 2),
                rightForearm = VehicleDefinition.DriverPartPose.Create(0.28f, 0.20f, 6f, 0.036f, 0.029f, true, 2),
                leftThigh = VehicleDefinition.DriverPartPose.Create(0.12f, -0.06f, 12f, 0.038f, 0.029f, true, 1),
                leftLowerLeg = VehicleDefinition.DriverPartPose.Create(0.26f, -0.32f, -14f, 0.036f, 0.029f, true, 1),
                rightThigh = VehicleDefinition.DriverPartPose.Create(0.06f, -0.08f, 16f, 0.038f, 0.029f, true, 0),
                rightLowerLeg = VehicleDefinition.DriverPartPose.Create(0.20f, -0.34f, -10f, 0.036f, 0.029f, true, 0),
                accessory = VehicleDefinition.DriverPartPose.Create(-0.03f, 0.62f, -3f, 1f, 1f, true, 6)
            };
        }

        private static void EnsureFolders()
        {
            string[] paths = { Root, Scenes, Root + "/VehicleData", Root + "/MapProfiles", Root + "/Materials", Prefabs };
            for (int i = 0; i < paths.Length; i++) if (!Directory.Exists(paths[i])) Directory.CreateDirectory(paths[i]);
        }
    }
}
#endif
