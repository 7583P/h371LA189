#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.TimeTrial;
using AnimeHillRacing.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnimeHillRacing.Editor
{
    /// <summary>
    /// Read-only integration audit for the rebuilt Hill Adventurer menus. It opens scenes additively,
    /// never saves them, and does not invoke any SaveManager/PlayerPrefs mutation.
    /// </summary>
    public static class HillAdventurerMenuVerifier
    {
        private const string SceneRoot = "Assets/AnimeHillRacing/Scenes/";
        private const string MainMenuScene = SceneRoot + "MainMenu.unity";
        private const string GarageScene = SceneRoot + "Garage.unity";
        private const string TrackSelectionScene = SceneRoot + "PreStart.unity";
        private const string Adventure2DScene = SceneRoot + "Adventure2_5D.unity";
        private const string Adventure3DScene = SceneRoot + "Adventure3D.unity";
        private const string TimeTrial3DScene = SceneRoot + "TimeTrial3D.unity";
        private const string CatalogPath = "Assets/AnimeHillRacing/VehicleData/GameCatalog.asset";

        private const string AdventureButtonPath =
            "Assets/Art/Buttons/MainNavigation/AdventureButton.png";
        private const string GarageButtonPath =
            "Assets/Art/Buttons/MainNavigation/GarageButton.png";
        private const string ShopButtonPath =
            "Assets/Art/Buttons/MainNavigation/StoreButton.png";
        private const string SettingsIconPath =
            "Assets/Art/Interface/Common/SettingsIcon.png";
        private const string MainDefaultBackgroundPath =
            "Assets/Art/Backgrounds/Menu/Default_Background.png";
        private const string MainNeonBackgroundPath =
            "Assets/Art/Backgrounds/Menu/Neon_Background.png";
        private const string ChestSlotsPath =
            "Assets/Art/Interface/Adventure/ChestSlots.png";
        private const string DefaultGaragePath =
            "Assets/Art/Backgrounds/Garage/Default_Garage.png";
        private const string NeonGaragePath = "Assets/Art/Buttons/mainmenu.png";
        private const string AnimeGaragePath =
            "Assets/Art/Backgrounds/Garage/Anime_Garage_Background.png";
        private const string SkinsIconPath =
            "Assets/Art/Interface/Garage/SkinsIcon.png";
        private const string TuneRoot = "Assets/Art/Interface/Garage/Tunes/";
        private const string TuneStatesPath = TuneRoot + "UpgradeTuneStates.png";
        private const string LowFuelPath = "Assets/Art/Game/HUD/LowFuel.png";

        [MenuItem("Anime Hill Racing/Validate/Hill Adventurer Menus")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
        }

        /// <summary>Entry point for Unity -batchmode -executeMethod.</summary>
        public static void ValidateBatch()
        {
            ValidateOrThrow();
        }

        /// <summary>Deterministic batch entry point used to reimport supplied art and rebuild all scenes.</summary>
        public static void RebuildAndValidateBatch()
        {
            HillAdventurerUiAssetImporter.ReimportSuppliedArtwork();
            PrototypeSceneBuilder.BuildAllGameScenes();
            ValidateOrThrow();
        }

        /// <summary>
        /// CI entry point for a real, non-development Android Player build. The validation APK uses
        /// Unity's debug signing so unattended verification never needs or mutates production secrets.
        /// </summary>
        public static void BuildAndroidBatch()
        {
            ValidateOrThrow();
            var scenePaths = new List<string>();
            EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < configuredScenes.Length; i++)
                if (configuredScenes[i].enabled) scenePaths.Add(configuredScenes[i].path);
            if (scenePaths.Count == 0)
                throw new InvalidOperationException("Android verification build has no enabled scenes.");

            string outputDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Logs", "HillAdventurerValidation");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "HillAdventurerValidation.apk");
            bool previousDevelopment = EditorUserBuildSettings.development;
            bool previousDebugging = EditorUserBuildSettings.allowDebugging;
            bool previousCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            try
            {
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                PlayerSettings.Android.useCustomKeystore = false;
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenePaths.ToArray(),
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Android Player build failed: {report.summary.result}, " +
                                                        $"{report.summary.totalErrors} error(s).");
                Debug.Log($"HILL_ADVENTURER_ANDROID_BUILD_OK: {outputPath} " +
                          $"({report.summary.totalSize} bytes, non-development, validation-signed).");
            }
            finally
            {
                PlayerSettings.Android.useCustomKeystore = previousCustomKeystore;
                EditorUserBuildSettings.development = previousDevelopment;
                EditorUserBuildSettings.allowDebugging = previousDebugging;
            }
        }

        public static void ValidateOrThrow()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Hill Adventurer menu validation requires Edit Mode.");

            using (new GameSessionStaticScope())
            {
                var errors = new List<string>();
                ValidateRequiredAssets(errors);
                ValidateScene(MainMenuScene, errors, scene =>
                {
                    ValidateCommonMenuScene(scene, errors);
                    ValidateMainMenu(scene, errors);
                });
                ValidateScene(TrackSelectionScene, errors, scene =>
                {
                    ValidateCommonMenuScene(scene, errors);
                    ValidateTrackSelection(scene, errors);
                });
                ValidateScene(GarageScene, errors, scene =>
                {
                    ValidateCommonMenuScene(scene, errors);
                    ValidateGarage(scene, errors);
                });
                ValidateScene(Adventure2DScene, errors,
                    scene => ValidateGameplayLowFuel(scene, errors));
                ValidateScene(Adventure3DScene, errors,
                    scene => ValidateGameplayLowFuel(scene, errors));
                ValidateScene(TimeTrial3DScene, errors,
                    scene => ValidateGameplayLowFuel(scene, errors));
                ValidateGameSessionSemantics(errors);
                ValidateLowFuelMath(errors);

                if (errors.Count > 0)
                {
                    string report = "HILL_ADVENTURER_MENU_VALIDATION_FAILED\n - " +
                                    string.Join("\n - ", errors);
                    Debug.LogError(report);
                    throw new InvalidOperationException(report);
                }

                Debug.Log("HILL_ADVENTURER_MENU_VALIDATION_OK: menu flow, references, track grid, " +
                          "chests, Garage, supplied sprites and Low Fuel passed.");
            }
        }

        private static void ValidateRequiredAssets(List<string> errors)
        {
            string[] spritePaths =
            {
                AdventureButtonPath, GarageButtonPath, ShopButtonPath, SettingsIconPath,
                MainDefaultBackgroundPath, MainNeonBackgroundPath,
                DefaultGaragePath, NeonGaragePath, AnimeGaragePath, SkinsIconPath,
                TuneRoot + "MotorTune.png", TuneRoot + "AccelerationTune.png",
                TuneRoot + "SuspensionTune.png", TuneRoot + "GripTune.png",
                TuneRoot + "FuelTune.png", TuneRoot + "DownforceTune.png",
                TuneRoot + "ThrustersTune.png", TuneRoot + "InclinationBuggyTune.png",
                LowFuelPath
            };
            for (int i = 0; i < spritePaths.Length; i++)
                if (AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]) == null)
                    Error(errors, "Assets", "required Sprite is missing or not imported: " + spritePaths[i]);

            int tuneStateSprites = 0;
            bool hasAffordableState = false;
            bool hasUnaffordableState = false;
            Object[] stateAssets = AssetDatabase.LoadAllAssetsAtPath(TuneStatesPath);
            for (int i = 0; i < stateAssets.Length; i++)
            {
                if (!(stateAssets[i] is Sprite stateSprite)) continue;
                tuneStateSprites++;
                hasAffordableState |= stateSprite.name == "UpgradeAffordable";
                hasUnaffordableState |= stateSprite.name == "UpgradeUnaffordable";
            }
            if (tuneStateSprites < 2 || !hasAffordableState || !hasUnaffordableState)
                Error(errors, "Assets", "UpgradeTuneStates Multiple atlas must expose distinct " +
                                       "UpgradeAffordable and UpgradeUnaffordable sub-sprites.");

            bool hasChestSlotFrame = false;
            Object[] chestAssets = AssetDatabase.LoadAllAssetsAtPath(ChestSlotsPath);
            for (int i = 0; i < chestAssets.Length; i++)
                hasChestSlotFrame |= chestAssets[i] is Sprite sprite && sprite.name == "ChestSlotFrame";
            if (!hasChestSlotFrame)
                Error(errors, "Assets", "ChestSlots must expose the ChestSlotFrame sub-sprite.");

            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null)
                Error(errors, "Assets", "GameCatalog is missing at " + CatalogPath);
            else if (catalog.CountMaps(GameModeType.Adventure2_5D) <= 0)
                Error(errors, "Assets", "GameCatalog has no Adventure 2.5D tracks.");
        }

        private static void ValidateScene(string path, List<string> errors, Action<Scene> validator)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Error(errors, path, "scene asset is missing.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (openedForValidation)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Error(errors, path, "scene could not be opened.");
                    return;
                }

                ValidateNoMissingScriptsOrReferences(scene, errors);
                validator(scene);
            }
            catch (Exception exception)
            {
                Error(errors, path, exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateCommonMenuScene(Scene scene, List<string> errors)
        {
            string scope = scene.name;
            RequireCount(ComponentsInScene<Canvas>(scene), 1, scope, "Canvas", errors);
            RequireCount(ComponentsInScene<CanvasScaler>(scene), 1, scope, "CanvasScaler", errors);
            RequireCount(ComponentsInScene<SafeArea>(scene), 1, scope, "SafeArea", errors);
            EventSystem[] eventSystems = ComponentsInScene<EventSystem>(scene);
            RequireCount(eventSystems, 1, scope, "EventSystem", errors);
            if (eventSystems.Length == 1 && eventSystems[0].GetComponent<BaseInputModule>() == null)
                Error(errors, scope, "EventSystem has no input module.");

            TopNavigationUI[] navigations = ComponentsInScene<TopNavigationUI>(scene);
            RequireCount(navigations, 1, scope, "TopNavigationUI", errors);
            if (navigations.Length != 1) return;

            TopNavigationUI navigation = navigations[0];
            SerializedObject serializedNavigation = new SerializedObject(navigation);
            RequireObjectReference(serializedNavigation, "adventureSelected", scope, errors);
            RequireObjectReference(serializedNavigation, "garageSelected", scope, errors);
            RequireObjectReference(serializedNavigation, "shopSelected", scope, errors);

            ValidateNavigationButton(scene, navigation, "ADVENTURE", "Adventure",
                AdventureButtonPath, errors);
            ValidateNavigationButton(scene, navigation, "GARAGE", "Garage",
                GarageButtonPath, errors);
            ValidateNavigationButton(scene, navigation, "SHOP", "Shop",
                ShopButtonPath, errors);
            ValidateNavigationButton(scene, navigation, "Settings", "Settings",
                SettingsIconPath, errors);
            ValidateResponsiveLayout(scene, errors);
        }

        private static void ValidateMainMenu(Scene scene, List<string> errors)
        {
            string scope = scene.name;
            ValidateTopNavigationState(scene, TopNavigationSection.None, false, errors);
            MainMenuUI[] menus = ComponentsInScene<MainMenuUI>(scene);
            RequireCount(menus, 1, scope, "MainMenuUI", errors);
            if (menus.Length == 1)
            {
                SerializedObject menu = new SerializedObject(menus[0]);
                RequireObjectReference(menu, "catalog", scope, errors);
                RequireObjectReference(menu, "preview", scope, errors);
                RequireObjectReference(menu, "coinsText", scope, errors);
                RequireObjectReference(menu, "gemsText", scope, errors);
                RequireObjectReference(menu, "vehicleNameText", scope, errors);
            }
            RequireSpriteUsed(scene, MainDefaultBackgroundPath, "default menu background", errors);
        }

        private static void ValidateTrackSelection(Scene scene, List<string> errors)
        {
            string scope = scene.name;
            ValidateTopNavigationState(scene, TopNavigationSection.Adventure, false, errors);
            PreStartMenuUI[] menus = ComponentsInScene<PreStartMenuUI>(scene);
            RequireCount(menus, 1, scope, "PreStartMenuUI", errors);
            if (menus.Length == 1)
            {
                SerializedObject menu = new SerializedObject(menus[0]);
                RequireBoolean(menu, "trackSelectionLayout", true, scope, errors);
                RequireObjectReference(menu, "catalog", scope, errors);
                ScrollRect scroll = RequireObjectReference<ScrollRect>(menu, "trackScroll", scope, errors);
                RectTransform content = RequireObjectReference<RectTransform>(menu, "trackContent", scope, errors);
                GridLayoutGroup grid = RequireObjectReference<GridLayoutGroup>(menu, "trackGrid", scope, errors);
                RequireObjectReference(menu, "trackSelectionRoot", scope, errors);
                RequireObjectReference(menu, "trackSelectionTitleText", scope, errors);

                if (scroll != null)
                {
                    if (!scroll.vertical || scroll.horizontal)
                        Error(errors, scope, "Track ScrollRect must be vertical-only.");
                    if (scroll.viewport == null || scroll.content == null)
                        Error(errors, scope, "Track ScrollRect needs viewport and content references.");
                    if (content != null && scroll.content != content)
                        Error(errors, scope, "Track ScrollRect content differs from PreStart trackContent.");
                }
                if (grid != null)
                {
                    if (grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
                        grid.constraintCount != 2)
                        Error(errors, scope, "Track Grid must use FixedColumnCount = 2 exactly.");
                    ContentSizeFitter fitter = grid.GetComponent<ContentSizeFitter>();
                    if (fitter == null || fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                        Error(errors, scope, "Track Grid needs a vertical PreferredSize ContentSizeFitter.");
                }

                TrackSelectionCardUI template = OptionalObjectReference<TrackSelectionCardUI>(menu,
                    "trackCardTemplate");
                SerializedProperty cards = menu.FindProperty("trackCards");
                bool hasCards = cards != null && cards.isArray && cards.arraySize > 0;
                if (template == null && !hasCards)
                    Error(errors, scope, "Track Selection needs a card template or serialized cards.");
                if (template != null)
                {
                    if (template.gameObject.activeSelf)
                        Error(errors, scope, "Track card template must remain inactive before runtime population.");
                    ValidateTrackCard(template, scope, errors);
                }
                else if (hasCards)
                {
                    for (int i = 0; i < cards.arraySize; i++)
                    {
                        TrackSelectionCardUI card = cards.GetArrayElementAtIndex(i)
                            .objectReferenceValue as TrackSelectionCardUI;
                        if (card == null)
                            Error(errors, scope, "trackCards contains a null element at index " + i + ".");
                        else
                            ValidateTrackCard(card, scope, errors);
                    }
                }
            }

            ChestPanelUI[] chestPanels = ComponentsInScene<ChestPanelUI>(scene);
            RequireCount(chestPanels, 1, scope, "ChestPanelUI", errors);
            if (chestPanels.Length == 1) ValidateChestPanel(chestPanels[0], scope, errors);
            RequireSpriteUsed(scene, MainNeonBackgroundPath, "neon track background", errors);
            RequireSpriteUsed(scene, ChestSlotsPath, "supplied Chest Slots artwork", errors);
        }

        private static void ValidateTrackCard(TrackSelectionCardUI card, string scope,
            List<string> errors)
        {
            SerializedObject serializedCard = new SerializedObject(card);
            RequireObjectReference(serializedCard, "selectionButton", scope, errors);
            RequireObjectReference(serializedCard, "environmentImage", scope, errors);
            RequireObjectReference(serializedCard, "titleText", scope, errors);
            RequireObjectReference(serializedCard, "bestDistanceText", scope, errors);
            RequireObjectReference(serializedCard, "statusText", scope, errors);
            RequireObjectReference(serializedCard, "selectedMarker", scope, errors);
        }

        private static void ValidateChestPanel(ChestPanelUI chestPanel, string scope,
            List<string> errors)
        {
            SerializedObject chest = new SerializedObject(chestPanel);
            RequireObjectReference(chest, "catalog", scope, errors);
            RequireObjectReference(chest, "progressFill", scope, errors);
            RequireObjectReference(chest, "progressText", scope, errors);
            RequireObjectReference(chest, "pageText", scope, errors);
            RequireArray(chest, "slotButtons", ChestPanelUI.SlotsPerPage, true, scope, errors);
            RequireArray(chest, "slotImages", ChestPanelUI.SlotsPerPage, true, scope, errors);
            RequireArray(chest, "slotLabels", ChestPanelUI.SlotsPerPage, true, scope, errors);
            Button previous = RequireObjectReference<Button>(chest, "previousPageButton", scope, errors);
            Button next = RequireObjectReference<Button>(chest, "nextPageButton", scope, errors);
            if (previous != null)
                RequirePersistentListener(previous.onClick, chestPanel, "PreviousPage", scope, errors);
            if (next != null)
                RequirePersistentListener(next.onClick, chestPanel, "NextPage", scope, errors);

            SerializedProperty buttons = chest.FindProperty("slotButtons");
            if (buttons == null || !buttons.isArray) return;
            for (int i = 0; i < Mathf.Min(buttons.arraySize, ChestPanelUI.SlotsPerPage); i++)
            {
                Button button = buttons.GetArrayElementAtIndex(i).objectReferenceValue as Button;
                if (button != null)
                {
                    RequirePersistentListener(button.onClick, chestPanel, "SelectSlot" + i, scope, errors);
                    RequireAssetPath(button.image != null ? button.image.sprite : null,
                        ChestSlotsPath, scope, "Chest Slot " + (i + 1) + " visual frame", errors);
                    if (button.image == null || button.image.sprite == null ||
                        button.image.sprite.name != "ChestSlotFrame")
                        Error(errors, scope, "Chest Slot " + (i + 1) +
                            " must use the supplied ChestSlotFrame slice.");
                }
            }
        }

        private static void ValidateGarage(Scene scene, List<string> errors)
        {
            string scope = scene.name;
            GarageUI[] garages = ComponentsInScene<GarageUI>(scene);
            RequireCount(garages, 1, scope, "GarageUI", errors);
            if (garages.Length != 1) return;

            GarageUI garage = garages[0];
            ValidateTopNavigationState(scene, TopNavigationSection.Garage, true, errors);
            SerializedObject serializedGarage = new SerializedObject(garage);
            RequireObjectReference(serializedGarage, "catalog", scope, errors);
            RequireObjectReference(serializedGarage, "preview", scope, errors);
            Button start = RequireObjectReference<Button>(serializedGarage, "startButton", scope, errors);
            RequireObjectReference(serializedGarage, "pendingTrackLabel", scope, errors);
            TopNavigationUI topNavigation = RequireObjectReference<TopNavigationUI>(serializedGarage,
                "topNavigation", scope, errors);
            if (start != null)
                RequirePersistentListener(start.onClick, garage, "StartAdventure", scope, errors);
            if (topNavigation != null && topNavigation.gameObject.scene != scene)
                Error(errors, scope, "Garage topNavigation points outside the Garage scene.");

            RequireObjectReference(serializedGarage, "garageBackground", scope, errors);
            RequireSpriteField(serializedGarage, "defaultGarageBackground", DefaultGaragePath, scope, errors);
            RequireSpriteField(serializedGarage, "neonGarageBackground", NeonGaragePath, scope, errors);
            RequireSpriteField(serializedGarage, "animeGarageBackground", AnimeGaragePath, scope, errors);
            RequireArray(serializedGarage, "backgroundButtons", 3, true, scope, errors);
            ValidateButtonArrayListeners(serializedGarage, "backgroundButtons", garage,
                "SelectGarageBackground", 3, scope, errors);

            RequireArray(serializedGarage, "tuningNames", 6, true, scope, errors);
            RequireArray(serializedGarage, "tuningLevels", 6, true, scope, errors);
            RequireArray(serializedGarage, "tuningCosts", 6, true, scope, errors);
            RequireArray(serializedGarage, "tuningButtons", 6, true, scope, errors);
            RequireArray(serializedGarage, "tuningIcons", 6, true, scope, errors);
            RequireArray(serializedGarage, "tuningCoinIcons", 6, true, scope, errors);
            ValidateButtonArrayListeners(serializedGarage, "tuningButtons", garage,
                "UpgradeTune", 6, scope, errors);
            SerializedProperty tuneButtons = serializedGarage.FindProperty("tuningButtons");
            if (tuneButtons != null && tuneButtons.isArray)
            {
                for (int i = 0; i < tuneButtons.arraySize; i++)
                {
                    Button tuneButton = tuneButtons.GetArrayElementAtIndex(i).objectReferenceValue as Button;
                    Image card = tuneButton != null && tuneButton.transform.parent != null
                        ? tuneButton.transform.parent.GetComponent<Image>() : null;
                    RequireAssetPath(card != null ? card.sprite : null, TuneStatesPath, scope,
                        "Tune card " + (i + 1) + " state frame", errors);
                    RectTransform cardRect = card != null ? card.rectTransform : null;
                    if (cardRect != null && cardRect.sizeDelta.y > 0f &&
                        Mathf.Abs(cardRect.sizeDelta.x / cardRect.sizeDelta.y - .75f) > .08f)
                        Error(errors, scope, "Tune card " + (i + 1) +
                            " does not preserve the supplied portrait frame proportions.");
                }
            }

            RequireSpriteField(serializedGarage, "engineTuneIcon", TuneRoot + "MotorTune.png", scope, errors);
            RequireSpriteField(serializedGarage, "accelerationTuneIcon", TuneRoot + "AccelerationTune.png", scope, errors);
            RequireSpriteField(serializedGarage, "suspensionTuneIcon", TuneRoot + "SuspensionTune.png", scope, errors);
            RequireSpriteField(serializedGarage, "gripTuneIcon", TuneRoot + "GripTune.png", scope, errors);
            RequireSpriteField(serializedGarage, "fuelTuneIcon", TuneRoot + "FuelTune.png", scope, errors);
            RequireSpriteField(serializedGarage, "downforceTuneIcon", TuneRoot + "DownforceTune.png", scope, errors);
            RequireSpriteField(serializedGarage, "thrustersTuneIcon", TuneRoot + "ThrustersTune.png", scope, errors);
            RequireSpriteField(serializedGarage, "buggyInclinationTuneIcon",
                TuneRoot + "InclinationBuggyTune.png", scope, errors);
            Sprite affordable = RequireSpriteField(serializedGarage, "tuneAffordableBackground",
                TuneStatesPath, scope, errors);
            Sprite unaffordable = RequireSpriteField(serializedGarage, "tuneUnaffordableBackground",
                TuneStatesPath, scope, errors);
            if (affordable != null && unaffordable != null && affordable == unaffordable)
                Error(errors, scope, "affordable and unaffordable Tune states must use different slices.");
            if (affordable != null && affordable.name != "UpgradeAffordable")
                Error(errors, scope, "tuneAffordableBackground uses the wrong atlas slice: " +
                                     affordable.name + ".");
            if (unaffordable != null && unaffordable.name != "UpgradeUnaffordable")
                Error(errors, scope, "tuneUnaffordableBackground uses the wrong atlas slice: " +
                                     unaffordable.name + ".");

            GameObject modal = RequireObjectReference<GameObject>(serializedGarage,
                "confirmationModal", scope, errors);
            Image confirmationPanel = RequireObjectReference<Image>(serializedGarage,
                "confirmationPanel", scope, errors);
            RequireObjectReference(serializedGarage, "confirmationTitle", scope, errors);
            RequireObjectReference(serializedGarage, "confirmationValues", scope, errors);
            RequireObjectReference(serializedGarage, "confirmationCost", scope, errors);
            RequireObjectReference(serializedGarage, "confirmationStatus", scope, errors);
            RequireObjectReference(serializedGarage, "confirmationIcon", scope, errors);
            RequireObjectReference(serializedGarage, "confirmationCoinIcon", scope, errors);
            RequireObjectReference(serializedGarage, "confirmationDescription", scope, errors);
            Button confirmation = RequireObjectReference<Button>(serializedGarage,
                "confirmationYes", scope, errors);
            if (confirmation != null)
            {
                RequirePersistentListener(confirmation.onClick, garage, "ConfirmPurchase", scope, errors);
                Text confirmationLabel = confirmation.GetComponentInChildren<Text>(true);
                if (confirmationLabel == null || confirmationLabel.text != "UPGRADE")
                    Error(errors, scope, "Tune modal confirmation must start with the UPGRADE label.");
            }
            if (modal != null) ValidateGrayModal(modal, scope, errors);
            if (confirmationPanel != null && modal != null &&
                confirmationPanel.gameObject != modal)
                Error(errors, scope, "confirmationPanel does not reference the reusable modal panel.");

            GameObject store = RequireObjectReference<GameObject>(serializedGarage, "storePanel", scope, errors);
            if (store != null)
            {
                ScrollRect[] shopScrolls = store.GetComponentsInChildren<ScrollRect>(true);
                bool validScroll = false;
                for (int i = 0; i < shopScrolls.Length; i++)
                    validScroll |= shopScrolls[i].vertical && !shopScrolls[i].horizontal &&
                                   shopScrolls[i].content != null && shopScrolls[i].viewport != null;
                if (!validScroll)
                    Error(errors, scope, "Shop panel needs a configured vertical ScrollRect.");
            }
            RequireArray(serializedGarage, "shopAccessoryButtons", 4, true, scope, errors);
            ValidateButtonArrayListeners(serializedGarage, "shopAccessoryButtons", garage,
                "SelectShopAccessory", 4, scope, errors);

            RequireObjectReference(serializedGarage, "settingsPanel", scope, errors);
            if (FindGameObject(scene, "Settings Placeholder") != null)
                Error(errors, scope, "legacy Settings Placeholder still exists.");
            Toggle audioToggle = RequireObjectReference<Toggle>(serializedGarage,
                "masterAudioToggle", scope, errors);
            RequireObjectReference(serializedGarage, "masterAudioStatus", scope, errors);
            if (audioToggle != null && !HasPersistentListener(audioToggle.onValueChanged,
                    garage, "SetMasterAudioEnabled") &&
                !HasPersistentListener(audioToggle.onValueChanged, garage, "ToggleMasterAudio"))
                Error(errors, scope, "Master Audio Toggle is not wired to GarageUI.");

            RequireObjectReference(serializedGarage, "customizationPanel", scope, errors);
            RequireSpriteUsed(scene, SkinsIconPath, "Paint / Skins icon", errors);

            Type garageType = typeof(GarageUI);
            if (garageType.GetField("purchaseInFlight", BindingFlags.Instance | BindingFlags.NonPublic) == null ||
                garageType.GetField("lastPurchaseAttemptFrame", BindingFlags.Instance | BindingFlags.NonPublic) == null)
                Error(errors, scope, "Garage tune purchase double-tap guards are missing.");
        }

        private static void ValidateGrayModal(GameObject modal, string scope, List<string> errors)
        {
            Image panel = modal.GetComponent<Image>();
            if (panel == null)
            {
                Error(errors, scope, "Tune confirmation modal root has no Image panel.");
                return;
            }
            Color color = panel.color;
            float minimum = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (maximum - minimum > .24f || color.a < .5f)
                Error(errors, scope, "Tune confirmation panel is not an opaque neutral gray treatment.");
        }

        private static void ValidateGameplayLowFuel(Scene scene, List<string> errors)
        {
            string scope = scene.name;
            GameplayHUD[] huds = ComponentsInScene<GameplayHUD>(scene);
            TimeTrialHUD[] timeTrialHuds = ComponentsInScene<TimeTrialHUD>(scene);
            Object hudObject;
            if (scene.name == "TimeTrial3D")
            {
                RequireCount(huds, 0, scope, "GameplayHUD", errors);
                RequireCount(timeTrialHuds, 1, scope, "TimeTrialHUD", errors);
                if (timeTrialHuds.Length != 1) return;
                hudObject = timeTrialHuds[0];
            }
            else
            {
                RequireCount(huds, 1, scope, "GameplayHUD", errors);
                RequireCount(timeTrialHuds, 0, scope, "TimeTrialHUD", errors);
                if (huds.Length != 1) return;
                hudObject = huds[0];
            }

            SerializedObject hud = new SerializedObject(hudObject);
            Image warning = RequireObjectReference<Image>(hud, "lowFuelWarning", scope, errors);
            RequireFloat(hud, "lowFuelWarningSeconds", 5f, .05f, scope, errors);
            if (warning != null)
            {
                RequireAssetPath(warning.sprite, LowFuelPath, scope, "Low Fuel sprite", errors);
                if (warning.gameObject.activeSelf)
                    Error(errors, scope, "Low Fuel warning must start hidden.");
                if (warning.raycastTarget)
                    Error(errors, scope, "Low Fuel warning must not block touch input.");
            }
        }

        private static void ValidateGameSessionSemantics(List<string> errors)
        {
            const string scope = "GameSession";
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null) return;
            MapDefinition expected = catalog.GetCompatibleMap(GameModeType.Adventure2_5D, 0);
            if (expected == null) return;

            Type sessionType = typeof(GameSession);
            FieldInfo contextField = sessionType.GetField("adventureGarageContext",
                BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo mapIdField = sessionType.GetField("pendingAdventureMapId",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo commit = sessionType.GetMethod("TryCommitPendingAdventureRun",
                BindingFlags.Static | BindingFlags.Public);
            if (contextField == null || mapIdField == null)
            {
                Error(errors, scope, "pending Adventure context fields are missing.");
                return;
            }
            if (commit == null || commit.ReturnType != typeof(bool))
                Error(errors, scope, "TryCommitPendingAdventureRun contract is missing.");

            object originalContext = contextField.GetValue(null);
            object originalMapId = mapIdField.GetValue(null);
            GarageSection originalSection = GameSession.RequestedGarageSection;
            try
            {
                contextField.SetValue(null, false);
                mapIdField.SetValue(null, expected.mapId);
                if (GameSession.HasPendingAdventureTrack)
                    Error(errors, scope, "map id alone incorrectly enables Adventure START.");

                contextField.SetValue(null, true);
                mapIdField.SetValue(null, string.Empty);
                if (GameSession.HasPendingAdventureTrack)
                    Error(errors, scope, "empty map id incorrectly enables Adventure START.");

                contextField.SetValue(null, true);
                mapIdField.SetValue(null, expected.mapId);
                if (!GameSession.TryResolvePendingAdventureTrack(catalog, out MapDefinition resolved) ||
                    resolved != expected || resolved.supportedGameMode != GameModeType.Adventure2_5D)
                    Error(errors, scope, "pending track is not resolved by exact id and Adventure mode.");

                contextField.SetValue(null, true);
                mapIdField.SetValue(null, expected.mapId);
                GameSession.RequestDirectGarage(GarageSection.Settings);
                if (GameSession.HasPendingAdventureTrack ||
                    GameSession.RequestedGarageSection != GarageSection.Settings)
                    Error(errors, scope, "direct Garage navigation does not clear pending Adventure context.");

                contextField.SetValue(null, true);
                mapIdField.SetValue(null, "__missing_hill_adventurer_track__");
                if (GameSession.TryResolvePendingAdventureTrack(catalog, out _) ||
                    GameSession.HasPendingAdventureTrack)
                    Error(errors, scope, "missing track id falls back instead of failing safely.");
            }
            finally
            {
                contextField.SetValue(null, originalContext);
                mapIdField.SetValue(null, originalMapId);
                GameSession.RequestedGarageSection = originalSection;
            }
        }

        private static void ValidateLowFuelMath(List<string> errors)
        {
            const string scope = "Low Fuel math";
            if (GameplayHUD.ShouldDisplayLowFuel(5.01f, 1f, 5f))
                Error(errors, scope, "warning appears with more than five seconds remaining.");
            if (!GameplayHUD.ShouldDisplayLowFuel(5f, 1f, 5f))
                Error(errors, scope, "warning does not appear at five seconds remaining.");
            if (!GameplayHUD.ShouldDisplayLowFuel(10f, 2f, 5f))
                Error(errors, scope, "accelerated consumption is not reflected in remaining time.");
            if (GameplayHUD.ShouldDisplayLowFuel(0f, 1f, 5f) ||
                GameplayHUD.ShouldDisplayLowFuel(5f, 0f, 5f))
                Error(errors, scope, "zero fuel or zero consumption is not handled safely.");
        }

        private static void ValidateNoMissingScriptsOrReferences(Scene scene, List<string> errors)
        {
            string scope = scene.name;
            GameObject[] objects = GameObjectsInScene(scene);
            for (int i = 0; i < objects.Length; i++)
            {
                int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objects[i]);
                if (missingScripts > 0)
                    Error(errors, scope, HierarchyPath(objects[i]) + " has " + missingScripts +
                                         " Missing MonoBehaviour(s).");
            }

            MonoBehaviour[] behaviours = ComponentsInScene<MonoBehaviour>(scene);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;
                SerializedObject serialized;
                try
                {
                    serialized = new SerializedObject(behaviour);
                }
                catch
                {
                    continue;
                }
                SerializedProperty iterator = serialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = true;
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference ||
                        iterator.objectReferenceValue != null ||
                        iterator.objectReferenceInstanceIDValue == 0) continue;
                    Error(errors, scope, HierarchyPath(behaviour.gameObject) + "." +
                                         iterator.propertyPath + " is a missing object reference.");
                }
            }
        }

        private static void ValidateNavigationButton(Scene scene, TopNavigationUI navigation,
            string objectName, string method, string spritePath, List<string> errors)
        {
            string scope = scene.name;
            Button[] all = ComponentsInScene<Button>(scene);
            var matches = new List<Button>();
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                    matches.Add(all[i]);
            if (matches.Count != 1)
            {
                Error(errors, scope, "expected one top navigation button named " + objectName +
                                     ", found " + matches.Count + ".");
                return;
            }

            Button button = matches[0];
            RequireAssetPath(button.image != null ? button.image.sprite : null,
                spritePath, scope, objectName + " sprite", errors);
            RequirePersistentListener(button.onClick, navigation, method, scope, errors);

            if (objectName == "GARAGE" || objectName == "SHOP")
            {
                Transform mask = button.transform.Find("Embedded Text Mask");
                Text dominant = mask != null ? mask.Find("Dominant English Label")?.GetComponent<Text>() : null;
                if (mask == null || dominant == null || dominant.text != objectName || dominant.fontSize < 30)
                    Error(errors, scope, objectName +
                        " must cover the source bitmap wording with a dominant exact English label.");
            }
        }

        private static void ValidateTopNavigationState(Scene scene, TopNavigationSection expected,
            bool deriveFromGarageSection, List<string> errors)
        {
            TopNavigationUI[] navigations = ComponentsInScene<TopNavigationUI>(scene);
            if (navigations.Length != 1) return;
            SerializedObject navigation = new SerializedObject(navigations[0]);
            SerializedProperty selected = navigation.FindProperty("selectedSection");
            if (!deriveFromGarageSection &&
                (selected == null || selected.propertyType != SerializedPropertyType.Enum ||
                 selected.enumValueIndex != (int)expected))
                Error(errors, scene.name, "Top Navigation selected section is not " + expected + ".");
            RequireBoolean(navigation, "deriveFromRequestedGarageSection", deriveFromGarageSection,
                scene.name, errors);
        }

        private static void ValidateResponsiveLayout(Scene scene, List<string> errors)
        {
            CanvasScaler[] scalers = ComponentsInScene<CanvasScaler>(scene);
            SafeArea[] safeAreas = ComponentsInScene<SafeArea>(scene);
            if (scalers.Length != 1 || safeAreas.Length != 1) return;
            CanvasScaler scaler = scalers[0];
            RectTransform safe = safeAreas[0].transform as RectTransform;
            if (safe == null) return;

            Vector2[] screens =
            {
                new Vector2(1920f, 1080f), // 16:9
                new Vector2(2160f, 1080f), // 18:9
                new Vector2(2340f, 1080f), // 19.5:9
                new Vector2(2400f, 1080f), // 20:9
                new Vector2(2048f, 1536f)  // tablet 4:3
            };
            string[] profileNames = { "16:9", "18:9", "19.5:9", "20:9", "tablet 4:3" };
            string[] commonNames = { "ADVENTURE", "GARAGE", "SHOP", "Settings" };
            string[] sceneNames = scene.name == "PreStart"
                ? new[] { "Adventure Chest Slots", "Track Scroll" }
                : scene.name == "Garage"
                    ? new[] { "Purchase Confirmation", "Adventure Start", "Live Composed Preview" }
                    : new[] { "Selected Vehicle Preview" };

            for (int profile = 0; profile < screens.Length; profile++)
            {
                Vector2 virtualSize = VirtualCanvasSize(scaler, screens[profile]);
                Rect safeBounds = new Rect(-virtualSize.x * safe.pivot.x,
                    -virtualSize.y * safe.pivot.y, virtualSize.x, virtualSize.y);
                for (int i = 0; i < commonNames.Length; i++)
                    ValidateRectFits(scene, safe, safeBounds, commonNames[i], profileNames[profile], errors);
                for (int i = 0; i < sceneNames.Length; i++)
                    ValidateRectFits(scene, safe, safeBounds, sceneNames[i], profileNames[profile], errors);

                if (scene.name == "PreStart")
                {
                    ValidateSiblingSet(scene, safe, safeBounds, "Chest Slot ", 4,
                        profileNames[profile], errors);
                }
                else if (scene.name == "Garage")
                {
                    ValidateSiblingSet(scene, safe, safeBounds, "Tune Row ", 6,
                        profileNames[profile], errors);
                    RectTransform settings = FindRectTransform(scene, "Settings");
                    RectTransform start = FindRectTransform(scene, "Adventure Start");
                    if (settings != null && start != null)
                    {
                        Rect settingsBounds = CalculateBounds(settings, safe, safeBounds);
                        Rect startBounds = CalculateBounds(start, safe, safeBounds);
                        if (RectsOverlap(settingsBounds, startBounds, 2f))
                            Error(errors, scene.name, "Settings overlaps START at " + profileNames[profile] + ".");
                    }
                }
            }
        }

        private static Vector2 VirtualCanvasSize(CanvasScaler scaler, Vector2 screen)
        {
            Vector2 reference = scaler.referenceResolution;
            float widthScale = screen.x / Mathf.Max(1f, reference.x);
            float heightScale = screen.y / Mathf.Max(1f, reference.y);
            float match = Mathf.Clamp01(scaler.matchWidthOrHeight);
            float scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(widthScale, 2f),
                Mathf.Log(heightScale, 2f), match));
            return screen / Mathf.Max(.0001f, scale);
        }

        private static void ValidateRectFits(Scene scene, RectTransform safe, Rect safeBounds,
            string objectName, string profile, List<string> errors)
        {
            RectTransform target = FindRectTransform(scene, objectName);
            if (target == null)
            {
                Error(errors, scene.name, objectName + " is missing from responsive validation at " +
                    profile + ".");
                return;
            }
            Rect bounds = CalculateBounds(target, safe, safeBounds);
            if (bounds.xMin < safeBounds.xMin - 1f || bounds.xMax > safeBounds.xMax + 1f ||
                bounds.yMin < safeBounds.yMin - 1f || bounds.yMax > safeBounds.yMax + 1f)
                Error(errors, scene.name, objectName + " leaves Safe Area at " + profile + ".");
        }

        private static void ValidateSiblingSet(Scene scene, RectTransform safe, Rect safeBounds,
            string prefix, int count, string profile, List<string> errors)
        {
            var bounds = new List<Rect>();
            for (int i = 1; i <= count; i++)
            {
                RectTransform target = FindRectTransform(scene, prefix + i);
                if (target == null)
                {
                    Error(errors, scene.name, prefix + i +
                        " is missing from responsive validation at " + profile + ".");
                    continue;
                }
                Rect rect = CalculateBounds(target, safe, safeBounds);
                if (rect.xMin < safeBounds.xMin - 1f || rect.xMax > safeBounds.xMax + 1f ||
                    rect.yMin < safeBounds.yMin - 1f || rect.yMax > safeBounds.yMax + 1f)
                    Error(errors, scene.name, prefix + i + " leaves Safe Area at " + profile + ".");
                for (int previous = 0; previous < bounds.Count; previous++)
                    if (RectsOverlap(bounds[previous], rect, 2f))
                        Error(errors, scene.name, prefix + i + " overlaps another " +
                            prefix.Trim() + " at " + profile + ".");
                bounds.Add(rect);
            }
        }

        private static Rect CalculateBounds(RectTransform target, RectTransform root, Rect rootBounds)
        {
            var chain = new List<RectTransform>();
            Transform current = target;
            while (current != null && current != root)
            {
                if (current is RectTransform rect) chain.Add(rect);
                current = current.parent;
            }
            if (current != root) return new Rect(float.MinValue, float.MinValue, 0f, 0f);
            chain.Reverse();
            Rect parentBounds = rootBounds;
            for (int i = 0; i < chain.Count; i++)
            {
                RectTransform child = chain[i];
                Vector2 anchorMin = new Vector2(
                    parentBounds.xMin + parentBounds.width * child.anchorMin.x,
                    parentBounds.yMin + parentBounds.height * child.anchorMin.y);
                Vector2 anchorMax = new Vector2(
                    parentBounds.xMin + parentBounds.width * child.anchorMax.x,
                    parentBounds.yMin + parentBounds.height * child.anchorMax.y);
                Vector2 size = anchorMax - anchorMin + child.sizeDelta;
                Vector2 pivotPosition = new Vector2(
                    Mathf.Lerp(anchorMin.x, anchorMax.x, child.pivot.x),
                    Mathf.Lerp(anchorMin.y, anchorMax.y, child.pivot.y)) + child.anchoredPosition;
                parentBounds = new Rect(pivotPosition.x - size.x * child.pivot.x,
                    pivotPosition.y - size.y * child.pivot.y, size.x, size.y);
            }
            return parentBounds;
        }

        private static bool RectsOverlap(Rect a, Rect b, float tolerance) =>
            a.xMin < b.xMax - tolerance && a.xMax > b.xMin + tolerance &&
            a.yMin < b.yMax - tolerance && a.yMax > b.yMin + tolerance;

        private static RectTransform FindRectTransform(Scene scene, string name)
        {
            GameObject gameObject = FindGameObject(scene, name);
            return gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
        }

        private static void RequireSpriteUsed(Scene scene, string expectedPath, string label,
            List<string> errors)
        {
            Image[] images = ComponentsInScene<Image>(scene);
            for (int i = 0; i < images.Length; i++)
                if (SameAssetPath(images[i].sprite, expectedPath)) return;
            Error(errors, scene.name, label + " is not assigned in the scene: " + expectedPath);
        }

        private static Sprite RequireSpriteField(SerializedObject serialized, string propertyName,
            string expectedPath, string scope, List<string> errors)
        {
            Sprite sprite = RequireObjectReference<Sprite>(serialized, propertyName, scope, errors);
            if (sprite != null) RequireAssetPath(sprite, expectedPath, scope, propertyName, errors);
            return sprite;
        }

        private static void RequireAssetPath(Object asset, string expectedPath, string scope,
            string label, List<string> errors)
        {
            if (asset == null)
            {
                Error(errors, scope, label + " is null.");
                return;
            }
            if (!SameAssetPath(asset, expectedPath))
                Error(errors, scope, label + " uses " + AssetDatabase.GetAssetPath(asset) +
                                     " instead of " + expectedPath + ".");
        }

        private static bool SameAssetPath(Object asset, string expectedPath) =>
            asset != null && string.Equals(NormalizePath(AssetDatabase.GetAssetPath(asset)),
                NormalizePath(expectedPath), StringComparison.OrdinalIgnoreCase);

        private static string NormalizePath(string path) => (path ?? string.Empty).Replace('\\', '/');

        private static void ValidateButtonArrayListeners(SerializedObject serialized, string propertyName,
            Object target, string methodPrefix, int expected, string scope, List<string> errors)
        {
            SerializedProperty array = serialized.FindProperty(propertyName);
            if (array == null || !array.isArray) return;
            for (int i = 0; i < Mathf.Min(array.arraySize, expected); i++)
            {
                Button button = array.GetArrayElementAtIndex(i).objectReferenceValue as Button;
                if (button != null)
                    RequirePersistentListener(button.onClick, target, methodPrefix + i, scope, errors);
            }
        }

        private static bool HasPersistentListener(UnityEventBase unityEvent, Object target,
            string methodName) => CountPersistentListeners(unityEvent, target, methodName) > 0;

        private static int CountPersistentListeners(UnityEventBase unityEvent, Object target,
            string methodName)
        {
            if (unityEvent == null) return 0;
            int count = 0;
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
                if (unityEvent.GetPersistentTarget(i) == target &&
                    unityEvent.GetPersistentMethodName(i) == methodName)
                    count++;
            return count;
        }

        private static void RequirePersistentListener(UnityEventBase unityEvent, Object target,
            string methodName, string scope, List<string> errors)
        {
            int count = CountPersistentListeners(unityEvent, target, methodName);
            if (count != 1)
                Error(errors, scope, target.GetType().Name + "." + methodName +
                                     " must have exactly one persistent listener, found " + count + ".");
        }

        private static Object RequireObjectReference(SerializedObject serialized, string propertyName,
            string scope, List<string> errors) =>
            RequireObjectReference<Object>(serialized, propertyName, scope, errors);

        private static T RequireObjectReference<T>(SerializedObject serialized, string propertyName,
            string scope, List<string> errors) where T : Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " is not a serialized object-reference field.");
                return null;
            }
            T value = property.objectReferenceValue as T;
            if (value == null)
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " is not assigned.");
            return value;
        }

        private static T OptionalObjectReference<T>(SerializedObject serialized, string propertyName)
            where T : Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property != null && property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue as T : null;
        }

        private static void RequireArray(SerializedObject serialized, string propertyName,
            int expectedSize, bool requireEveryElement, string scope, List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " is not a serialized array.");
                return;
            }
            if (property.arraySize != expectedSize)
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " must contain exactly " + expectedSize + " entries, found " +
                                     property.arraySize + ".");
            if (!requireEveryElement) return;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                if (element.propertyType == SerializedPropertyType.ObjectReference &&
                    element.objectReferenceValue == null)
                    Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                         "[" + i + "] is null.");
            }
        }

        private static void RequireBoolean(SerializedObject serialized, string propertyName,
            bool expected, string scope, List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Boolean)
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " is not a serialized bool.");
            else if (property.boolValue != expected)
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " must be " + expected + ".");
        }

        private static void RequireFloat(SerializedObject serialized, string propertyName,
            float expected, float tolerance, string scope, List<string> errors)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Float)
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " is not a serialized float.");
            else if (Mathf.Abs(property.floatValue - expected) > tolerance)
                Error(errors, scope, serialized.targetObject.GetType().Name + "." + propertyName +
                                     " must be approximately " + expected + ".");
        }

        private static void RequireCount<T>(T[] values, int expected, string scope, string label,
            List<string> errors) where T : Object
        {
            if (values.Length != expected)
                Error(errors, scope, "expected " + expected + " " + label + ", found " +
                                     values.Length + ".");
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            var values = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                values.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return values.ToArray();
        }

        private static GameObject[] GameObjectsInScene(Scene scene)
        {
            var values = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) AddHierarchy(roots[i].transform, values);
            return values.ToArray();
        }

        private static void AddHierarchy(Transform transform, List<GameObject> values)
        {
            values.Add(transform.gameObject);
            for (int i = 0; i < transform.childCount; i++)
                AddHierarchy(transform.GetChild(i), values);
        }

        private static string HierarchyPath(GameObject gameObject)
        {
            string path = gameObject.name;
            Transform current = gameObject.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            GameObject[] objects = GameObjectsInScene(scene);
            for (int i = 0; i < objects.Length; i++)
                if (objects[i].name == name) return objects[i];
            return null;
        }

        private static void Error(List<string> errors, string scope, string message)
        {
            errors.Add("[" + scope + "] " + message);
        }

        private sealed class GameSessionStaticScope : IDisposable
        {
            private readonly FieldInfo contextField;
            private readonly FieldInfo mapIdField;
            private readonly object contextValue;
            private readonly object mapIdValue;
            private readonly GarageSection requestedSection;

            public GameSessionStaticScope()
            {
                Type type = typeof(GameSession);
                contextField = type.GetField("adventureGarageContext",
                    BindingFlags.Static | BindingFlags.NonPublic);
                mapIdField = type.GetField("pendingAdventureMapId",
                    BindingFlags.Static | BindingFlags.NonPublic);
                contextValue = contextField?.GetValue(null);
                mapIdValue = mapIdField?.GetValue(null);
                requestedSection = GameSession.RequestedGarageSection;
            }

            public void Dispose()
            {
                contextField?.SetValue(null, contextValue);
                mapIdField?.SetValue(null, mapIdValue);
                GameSession.RequestedGarageSection = requestedSection;
            }
        }
    }
}
#endif
