#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private const string HillUiRoot = "Assets/Art";
        private const string HillAdventureButtonPath =
            HillUiRoot + "/Buttons/MainNavigation/AdventureButton.png";
        private const string HillGarageButtonPath =
            HillUiRoot + "/Buttons/MainNavigation/GarageButton.png";
        private const string HillShopButtonPath =
            HillUiRoot + "/Buttons/MainNavigation/StoreButton.png";
        private const string HillDefaultBackgroundPath =
            HillUiRoot + "/Backgrounds/Menu/Default_Background.png";
        private const string HillNeonBackgroundPath =
            HillUiRoot + "/Backgrounds/Menu/Neon_Background.png";
        private const string HillDefaultGaragePath =
            HillUiRoot + "/Backgrounds/Garage/Default_Garage.png";
        private const string HillAnimeGaragePath =
            HillUiRoot + "/Backgrounds/Garage/Anime_Garage_Background.png";
        // The supplied Neon_Garage source is byte-identical to this existing GUID-backed asset.
        private const string HillNeonGaragePath = MainMenuBackgroundPath;
        private const string HillSettingsIconPath =
            HillUiRoot + "/Interface/Common/SettingsIcon.png";
        private const string HillChestSlotsPath =
            HillUiRoot + "/Interface/Adventure/ChestSlots.png";
        private const string HillSkinsIconPath =
            HillUiRoot + "/Interface/Garage/SkinsIcon.png";
        private const string HillLowFuelPath = HillUiRoot + "/Game/HUD/LowFuel.png";
        private const string HillTuneRoot = HillUiRoot + "/Interface/Garage/Tunes";

        private readonly struct HillNavigationWidgets
        {
            public readonly TopNavigationUI Controller;
            public readonly Button Adventure;
            public readonly Button Garage;
            public readonly Button Shop;
            public readonly Button Settings;

            public HillNavigationWidgets(TopNavigationUI controller, Button adventure,
                Button garage, Button shop, Button settings)
            {
                Controller = controller;
                Adventure = adventure;
                Garage = garage;
                Shop = shop;
                Settings = settings;
            }
        }

        private static Sprite RequireHillSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("Required Hill Adventurer UI sprite is missing or not imported: " + path);
            return sprite;
        }

        private static HillNavigationWidgets CreateHillTopNavigation(Transform safe,
            TopNavigationSection selectedSection, bool followGarageSection = false)
        {
            TopNavigationUI navigation = new GameObject("Top Navigation Controller")
                .AddComponent<TopNavigationUI>();
            Button adventure = CreateSpriteButton("ADVENTURE", safe,
                RequireHillSprite(HillAdventureButtonPath), new Vector2(.5f, 1f),
                new Vector2(-325f, -73f), new Vector2(286f, 143f));
            Button garage = CreateSpriteButton("GARAGE", safe,
                RequireHillSprite(HillGarageButtonPath), new Vector2(.5f, 1f),
                new Vector2(-20f, -73f), new Vector2(286f, 143f));
            Button shop = CreateSpriteButton("SHOP", safe,
                RequireHillSprite(HillShopButtonPath), new Vector2(.5f, 1f),
                new Vector2(285f, -73f), new Vector2(286f, 143f));
            Button settings = CreateSpriteButton("Settings", safe,
                RequireHillSprite(HillSettingsIconPath), Vector2.one,
                new Vector2(-62f, -62f), new Vector2(92f, 92f));

            GameObject adventureMarker = CreateHillNavigationMarker(adventure.transform, "ADVENTURE");
            GameObject garageMarker = CreateHillNavigationMarker(garage.transform, "GARAGE");
            GameObject shopMarker = CreateHillNavigationMarker(shop.transform, "SHOP");
            navigation.Assign(selectedSection, adventureMarker, garageMarker, shopMarker,
                followGarageSection);
            Bind(adventure, navigation.Adventure);
            Bind(garage, navigation.Garage);
            Bind(shop, navigation.Shop);
            Bind(settings, navigation.Settings);
            return new HillNavigationWidgets(navigation, adventure, garage, shop, settings);
        }

        private static GameObject CreateHillNavigationMarker(Transform parent, string englishLabel)
        {
            if (englishLabel == "GARAGE" || englishLabel == "SHOP")
            {
                GameObject mask = CreateImage("Embedded Text Mask", parent,
                    new Color(.018f, .030f, .052f, .985f), new Vector2(.27f, .20f),
                    new Vector2(.93f, .79f), Vector2.zero, Vector2.zero);
                mask.GetComponent<Image>().raycastTarget = false;
                Text dominantLabel = CreateText("Dominant English Label", mask.transform,
                    englishLabel, 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero);
                dominantLabel.fontStyle = FontStyle.Bold;
                dominantLabel.color = Color.white;
                dominantLabel.raycastTarget = false;
            }

            GameObject strip = CreateImage("English Navigation Label", parent,
                new Color(.008f, .015f, .035f, .86f), new Vector2(.10f, .02f),
                new Vector2(.90f, .25f), Vector2.zero, Vector2.zero);
            strip.GetComponent<Image>().raycastTarget = false;
            Text label = CreateText("Label", strip.transform, englishLabel, 18,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.fontStyle = FontStyle.Bold;
            GameObject marker = CreateImage("Selected Marker", parent,
                new Color(.14f, .90f, 1f, 1f), new Vector2(.20f, 0f),
                new Vector2(.80f, 0f), new Vector2(0f, -2f), new Vector2(0f, 7f));
            marker.GetComponent<Image>().raycastTarget = false;
            return marker;
        }

        private static void BuildMainMenuHillAdventurer(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.008f, .012f, .025f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            Image background = CreateAspectCoverBackground(canvas.transform,
                RequireHillSprite(HillDefaultBackgroundPath));
            background.name = "Main Menu Default Background";
            CreateImage("Readability Overlay", canvas.transform,
                new Color(.003f, .009f, .025f, .22f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            MainMenuUI menu = new GameObject("Main Menu Controller").AddComponent<MainMenuUI>();
            CreateHillTopNavigation(safe, TopNavigationSection.None);

            GameObject coinCard = CreateHudCard("Coins Card", safe, new Vector2(0f, 1f),
                new Vector2(168f, -51f), new Vector2(292f, 68f));
            CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01,
                new Vector2(.5f, .5f), new Vector2(-112f, 0f), new Vector2(46f, 46f));
            Text coins = CreateText("Coins", coinCard.transform, "COINS  0", 24,
                TextAnchor.MiddleRight, Vector2.zero, Vector2.one,
                new Vector2(-16f, 0f), new Vector2(-34f, 0f));
            GameObject gemCard = CreateHudCard("Gems Card", safe, new Vector2(0f, 1f),
                new Vector2(168f, -122f), new Vector2(292f, 58f));
            Text gemIcon = CreateText("Gem Icon", gemCard.transform, "◆", 31,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(.26f, 1f),
                Vector2.zero, Vector2.zero);
            gemIcon.color = new Color(.18f, .92f, 1f);
            Text gems = CreateText("Gems", gemCard.transform, "GEMS  0", 22,
                TextAnchor.MiddleRight, new Vector2(.18f, 0f), Vector2.one,
                new Vector2(-16f, 0f), new Vector2(-34f, 0f));

            Text title = CreateText("Game Title", safe, "HILL ADVENTURER", 48,
                TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(0f, 300f), new Vector2(820f, 76f));
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(.78f, .94f, 1f);
            Text subtitle = CreateText("Menu Prompt", safe,
                "CHOOSE YOUR DESTINATION", 20, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(0f, 252f), new Vector2(620f, 38f));
            subtitle.color = new Color(.56f, .76f, .88f);

            Text vehicleName = CreateText("Selected Vehicle Name", safe, "JEEP", 35,
                TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(0f, 198f), new Vector2(620f, 58f));
            vehicleName.fontStyle = FontStyle.Bold;
            RawImage previewImage = UIObject("Selected Vehicle Preview", safe,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -35f),
                new Vector2(920f, 450f)).AddComponent<RawImage>();
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;
            Button previous = CreateGrayNavButton("Previous Vehicle", safe, "<",
                new Vector2(-510f, -35f), new Vector2(82f, 104f));
            Button next = CreateGrayNavButton("Next Vehicle", safe, ">",
                new Vector2(510f, -35f), new Vector2(82f, 104f));
            CreateText("Vehicle Hint", safe,
                "YOUR SAVED VEHICLE, SKIN AND TUNES ARE USED IN EVERY MODE", 18,
                TextAnchor.MiddleCenter, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, 42f), new Vector2(920f, 40f)).color =
                new Color(.60f, .76f, .86f);

            Bind(previous, menu.PreviousVehicle);
            Bind(next, menu.NextVehicle);
            // TopNavigationUI owns destination changes; MainMenuUI remains the real vehicle/currency presenter.
            GaragePreviewRenderer preview = CreateGarageVehiclePreview(assets, previewImage, "Home");
            menu.Assign(assets.catalog, preview, coins, vehicleName, gems);
            VehicleDefinition initial = assets.catalog.FindVehicle(SaveManager.SelectedVehicleId) ?? assets.jeep;
            VehicleDefinition.CustomizationOption initialSkin =
                initial.FindSkin(SaveManager.GetSelectedSkinId(initial));
            VehicleDefinition.CustomizationOption initialWheel =
                initial.FindWheel(SaveManager.GetSelectedWheelId(initial));
            preview.Apply(initial, initialSkin, initialWheel, assets.stickmanBlack);

            // Keep generated scene data independent from the developer machine's PlayerPrefs.
            // MainMenuUI replaces these neutral placeholders with the active save on enable.
            coins.text = "COINS  0";
            gems.text = "GEMS  0";

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        private static void BuildTrackSelectionHillAdventurer(BuiltAssets assets)
        {
            Scene scene = NewScene();
            CreateMenuCamera(new Color(.006f, .009f, .025f));
            Canvas canvas = CreateCanvas(Color.clear, false);
            Sprite defaultMenuBackground = RequireHillSprite(HillDefaultBackgroundPath);
            Sprite neonMenuBackground = RequireHillSprite(HillNeonBackgroundPath);
            Image background = CreateAspectCoverBackground(canvas.transform,
                defaultMenuBackground);
            background.name = "Adventure Shared Background";
            CreateImage("Track Selection Overlay", canvas.transform,
                new Color(.002f, .006f, .022f, .48f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero).GetComponent<Image>().raycastTarget = false;
            Transform safe = CreateSafeArea(canvas.transform);
            CreateHillTopNavigation(safe, TopNavigationSection.Adventure);
            PreStartMenuUI menu = new GameObject("Track Selection Controller")
                .AddComponent<PreStartMenuUI>();

            GameObject coinCard = CreateHudCard("Coins Card", safe, new Vector2(0f, 1f),
                new Vector2(166f, -53f), new Vector2(290f, 68f));
            CreateSpriteImage("Coin Icon", coinCard.transform, assets.coin01,
                new Vector2(.5f, .5f), new Vector2(-110f, 0f), new Vector2(46f, 46f));
            Text coins = CreateText("Coins", coinCard.transform, "COINS  0", 24,
                TextAnchor.MiddleRight, Vector2.zero, Vector2.one,
                new Vector2(-14f, 0f), new Vector2(-32f, 0f));
            Text heading = CreateText("Track Selection Heading", safe, "TRACK SELECTION", 38,
                TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -166f), new Vector2(680f, 58f));
            heading.fontStyle = FontStyle.Bold;

            GameObject chestRoot = UIObject("Adventure Chest Slots", safe,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -326f), new Vector2(1650f, 300f));
            Sprite chestSlotFrame = RequireHillSubSprite(HillChestSlotsPath, "ChestSlotFrame");
            ChestPanelUI chestPanel = chestRoot.AddComponent<ChestPanelUI>();
            Button[] chestButtons = new Button[ChestPanelUI.SlotsPerPage];
            Image[] chestImages = new Image[ChestPanelUI.SlotsPerPage];
            Text[] chestLabels = new Text[ChestPanelUI.SlotsPerPage];
            UnityAction[] slotActions =
            {
                chestPanel.SelectSlot0,
                chestPanel.SelectSlot1,
                chestPanel.SelectSlot2,
                chestPanel.SelectSlot3
            };
            Vector2[] slotPositions =
            {
                new Vector2(-570f, 0f),
                new Vector2(-260f, 0f),
                new Vector2(50f, 0f),
                new Vector2(360f, 0f)
            };
            for (int i = 0; i < chestButtons.Length; i++)
            {
                chestButtons[i] = CreateButton("Chest Slot " + (i + 1), chestRoot.transform,
                    string.Empty, slotPositions[i], new Vector2(292f, 110f));
                chestButtons[i].image.sprite = chestSlotFrame;
                chestButtons[i].image.type = Image.Type.Simple;
                chestButtons[i].image.preserveAspect = true;
                chestButtons[i].image.color = Color.white;
                chestImages[i] = CreateImage("Chest", chestButtons[i].transform, Color.white,
                    new Vector2(.27f, .19f), new Vector2(.73f, .94f),
                    Vector2.zero, Vector2.zero).GetComponent<Image>();
                chestImages[i].raycastTarget = false;
                chestImages[i].preserveAspect = true;
                chestLabels[i] = CreateText("Status", chestButtons[i].transform, "EMPTY", 12,
                    TextAnchor.LowerCenter, new Vector2(.02f, .01f), new Vector2(.98f, .30f),
                    Vector2.zero, Vector2.zero);
                Bind(chestButtons[i], slotActions[i]);
            }
            Button previousChestPage = CreateGrayNavButton("Previous Chest Page", chestRoot.transform,
                "<", new Vector2(-790f, 0f), new Vector2(64f, 92f));
            Button nextChestPage = CreateGrayNavButton("Next Chest Page", chestRoot.transform,
                ">", new Vector2(540f, -30f), new Vector2(64f, 92f));
            Bind(previousChestPage, chestPanel.PreviousPage);
            Bind(nextChestPage, chestPanel.NextPage);

            GameObject progressBack = CreateImage("Next Chest Progress", chestRoot.transform,
                new Color(.015f, .035f, .075f, .95f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(685f, 83f), new Vector2(260f, 24f));
            Image progressFill = CreateImage("Fill", progressBack.transform,
                new Color(.16f, .86f, .32f), Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-6f, -6f)).GetComponent<Image>();
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Text progressText = CreateText("Next Chest Timer", chestRoot.transform,
                "NEXT CHEST  30:00", 16, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(685f, 112f), new Vector2(280f, 34f));
            Text inventoryText = CreateText("Chest Inventory Page", chestRoot.transform,
                "INVENTORY  0", 16, TextAnchor.MiddleCenter,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(685f, 45f), new Vector2(280f, 34f));
            Text feedbackText = CreateText("Chest Feedback", chestRoot.transform, string.Empty, 15,
                TextAnchor.UpperLeft, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(700f, -48f), new Vector2(240f, 130f));
            Image rewardThumbnail = CreateSpriteImage("Chest Blueprint Reward", chestRoot.transform,
                null, new Vector2(.5f, .5f), new Vector2(620f, -45f), new Vector2(80f, 80f));
            rewardThumbnail.enabled = false;
            chestPanel.Assign(assets.catalog, chestButtons, chestImages, chestLabels,
                progressFill, progressText, inventoryText, feedbackText, rewardThumbnail,
                previousChestPage, nextChestPage);

            Text selectTitle = CreateText("Select Track Title", safe, "SELECT A TRACK", 27,
                TextAnchor.MiddleCenter, new Vector2(.5f, .505f), new Vector2(.5f, .505f),
                Vector2.zero, new Vector2(520f, 44f));
            selectTitle.fontStyle = FontStyle.Bold;
            GameObject scrollRoot = CreateImage("Track Scroll", safe,
                new Color(.004f, .012f, .035f, .68f), new Vector2(.05f, .035f),
                new Vector2(.95f, .49f), Vector2.zero, Vector2.zero);
            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.decelerationRate = .135f;
            scroll.scrollSensitivity = 48f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            GameObject viewport = CreateImage("Viewport", scrollRoot.transform,
                new Color(1f, 1f, 1f, .001f), Vector2.zero, Vector2.one,
                new Vector2(8f, 8f), new Vector2(-16f, -16f));
            viewport.AddComponent<RectMask2D>();
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            GameObject contentObject = UIObject("Track Grid Content", viewport.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.pivot = new Vector2(.5f, 1f);
            GridLayoutGroup grid = contentObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(24, 24, 20, 24);
            grid.spacing = new Vector2(28f, 28f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewportRect;
            scroll.content = content;

            TrackSelectionCardUI cardTemplate = CreateHillTrackCardTemplate(content);
            Text emptyState = CreateText("Empty Track State", viewport.transform,
                "NO ADVENTURE TRACKS AVAILABLE", 26, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            emptyState.gameObject.SetActive(false);
            menu.AssignTrackSelectionTemplate(assets.catalog, scroll, grid, content, cardTemplate,
                coins, selectTitle, emptyState, scrollRoot, 2.30f);
            menu.AssignSharedMenuBackground(background, defaultMenuBackground, neonMenuBackground);
            Button back = CreateGrayNavButton("Back To Main Menu", safe, "HOME",
                new Vector2(105f, 35f), new Vector2(170f, 56f), Vector2.zero);
            Bind(back, menu.BackStep);

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, PreStartPath);
        }

        private static TrackSelectionCardUI CreateHillTrackCardTemplate(RectTransform content)
        {
            GameObject cardObject = UIObject("Track Card Template", content,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(760f, 330f));
            Image cardImage = cardObject.AddComponent<Image>();
            cardImage.color = new Color(.025f, .055f, .12f, .97f);
            cardImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            cardImage.type = Image.Type.Sliced;
            Button button = cardObject.AddComponent<Button>();
            cardObject.AddComponent<ButtonPressFeedback>();
            Outline outline = cardObject.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .70f, 1f, .85f);
            outline.effectDistance = new Vector2(2f, -2f);
            GameObject preview = CreateImage("Track Preview", cardObject.transform,
                new Color(.01f, .025f, .06f, 1f), new Vector2(.025f, .30f),
                new Vector2(.975f, .97f), Vector2.zero, Vector2.zero);
            preview.AddComponent<RectMask2D>();
            Image sky = CreateImage("Sky", preview.transform, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
            Image environment = CreateImage("Environment", preview.transform, Color.white,
                new Vector2(0f, 0f), new Vector2(1f, .82f), Vector2.zero, Vector2.zero)
                .GetComponent<Image>();
            Text title = CreateText("Track Name", cardObject.transform, "TRACK", 25,
                TextAnchor.MiddleLeft, new Vector2(.04f, .19f), new Vector2(.70f, .30f),
                Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            Text best = CreateText("Best Distance", cardObject.transform, "BEST  0 m", 17,
                TextAnchor.MiddleLeft, new Vector2(.04f, .06f), new Vector2(.67f, .19f),
                Vector2.zero, Vector2.zero);
            Text status = CreateText("Status", cardObject.transform, "SELECT", 18,
                TextAnchor.MiddleCenter, new Vector2(.70f, .055f), new Vector2(.96f, .25f),
                Vector2.zero, Vector2.zero);
            status.fontStyle = FontStyle.Bold;
            GameObject selected = CreateImage("Selected Track Marker",
                cardObject.transform, new Color(.12f, .96f, 1f, 1f),
                new Vector2(.02f, .015f), new Vector2(.98f, .03f),
                Vector2.zero, Vector2.zero);
            selected.GetComponent<Image>().raycastTarget = false;
            TrackSelectionCardUI card = cardObject.AddComponent<TrackSelectionCardUI>();
            card.Assign(button, environment, title, best, status, selected, sky);
            cardObject.SetActive(false);
            return card;
        }

        private static void BuildGarageHillAdventurer(BuiltAssets assets)
        {
            Scene scene = EditorSceneManager.OpenScene(GaragePath, OpenSceneMode.Single);
            GarageUI[] garageControllers = SceneComponents<GarageUI>(scene);
            if (garageControllers.Length != 1)
                throw new InvalidOperationException("The generated Garage must contain exactly one GarageUI before Hill Adventurer integration.");
            GarageUI menu = garageControllers[0];
            Transform safe = FindHillTransform(scene, "Safe Area");
            if (safe == null) throw new InvalidOperationException("Garage Safe Area is missing.");

            Image garageBackground = FindHillComponent<Image>(scene, "Garage Background");
            if (garageBackground == null)
                throw new InvalidOperationException("Garage background Image is missing.");
            Sprite defaultGarage = RequireHillSprite(HillDefaultGaragePath);
            Sprite neonGarage = RequireHillSprite(HillNeonGaragePath);
            Sprite animeGarage = RequireHillSprite(HillAnimeGaragePath);
            Sprite defaultMenu = RequireHillSprite(HillDefaultBackgroundPath);
            Sprite neonMenu = RequireHillSprite(HillNeonBackgroundPath);
            garageBackground.sprite = defaultGarage;
            garageBackground.color = Color.white;
            Image garageOverlay = FindHillComponent<Image>(scene, "Garage Overlay");
            if (garageOverlay != null)
                garageOverlay.color = new Color(.005f, .015f, .04f, .10f);
            AspectRatioFitter backgroundFitter = garageBackground.GetComponent<AspectRatioFitter>();
            if (backgroundFitter != null)
                backgroundFitter.aspectRatio = defaultGarage.rect.width / defaultGarage.rect.height;

            GameObject oldHome = FindHillObject(scene, "Home");
            if (oldHome != null) oldHome.SetActive(false);
            HillNavigationWidgets navigation = CreateHillTopNavigation(safe,
                TopNavigationSection.Garage, true);
            CreateHillGarageSubNavigation(safe, menu);

            MoveHillRect(scene, "Coins Card", new Vector2(0f, 1f), new Vector2(168f, -52f),
                new Vector2(292f, 68f));
            MoveHillRect(scene, "Gems Card", new Vector2(0f, 1f), new Vector2(168f, -122f),
                new Vector2(292f, 58f));
            MoveHillRect(scene, "Section Title", new Vector2(1f, 1f), new Vector2(-320f, -168f),
                new Vector2(500f, 52f));
            RectTransform vehicleNameRect = FindHillComponent<RectTransform>(scene, "Vehicle Name");
            if (vehicleNameRect != null)
            {
                vehicleNameRect.anchorMin = new Vector2(.08f, .43f);
                vehicleNameRect.anchorMax = new Vector2(.55f, .49f);
                vehicleNameRect.anchoredPosition = Vector2.zero;
                vehicleNameRect.sizeDelta = Vector2.zero;
            }
            RectTransform livePreviewRect = FindHillComponent<RectTransform>(scene, "Live Composed Preview");
            if (livePreviewRect != null)
            {
                livePreviewRect.anchorMin = new Vector2(.08f, .08f);
                livePreviewRect.anchorMax = new Vector2(.55f, .42f);
                livePreviewRect.anchoredPosition = Vector2.zero;
                livePreviewRect.sizeDelta = Vector2.zero;
            }
            MoveHillRect(scene, "Previous Vehicle", new Vector2(.5f, .5f),
                new Vector2(-840f, -120f), new Vector2(76f, 100f));
            MoveHillRect(scene, "Next Vehicle", new Vector2(.5f, .5f),
                new Vector2(130f, -120f), new Vector2(76f, 100f));
            Button previousVehicle = FindHillComponent<Button>(scene, "Previous Vehicle");
            Button nextVehicle = FindHillComponent<Button>(scene, "Next Vehicle");
            if (previousVehicle == null || nextVehicle == null)
                throw new InvalidOperationException("Garage vehicle navigation arrows are missing.");
            Button paintButton = CreateSpriteButton("Paint and Skins", safe,
                RequireHillSprite(HillSkinsIconPath), new Vector2(.80f, .68f),
                Vector2.zero, new Vector2(132f, 98f));
            CreateHillNavigationMarker(paintButton.transform, "PAINT / SKINS")
                .SetActive(false);
            Bind(paintButton, menu.ShowSkins);

            GameObject vehiclePanel = FindHillObject(scene, "Vehicle Panel");
            if (vehiclePanel == null) throw new InvalidOperationException("Garage Vehicle Panel is missing.");
            RectTransform vehiclePanelRect = vehiclePanel.GetComponent<RectTransform>();
            vehiclePanelRect.anchorMin = new Vector2(.30f, .02f);
            vehiclePanelRect.anchorMax = new Vector2(.70f, .11f);
            vehiclePanelRect.anchoredPosition = Vector2.zero;
            vehiclePanelRect.sizeDelta = Vector2.zero;
            Image vehiclePanelImage = vehiclePanel.GetComponent<Image>();
            if (vehiclePanelImage != null) vehiclePanelImage.color = new Color(.025f, .06f, .12f, .72f);
            Text vehiclePanelTitle = FindHillChildComponent<Text>(vehiclePanel.transform, "Title");
            if (vehiclePanelTitle != null)
            {
                vehiclePanelTitle.fontSize = 18;
                vehiclePanelTitle.alignment = TextAnchor.MiddleCenter;
                vehiclePanelTitle.rectTransform.anchorMin = new Vector2(.02f, .48f);
                vehiclePanelTitle.rectTransform.anchorMax = new Vector2(.98f, .96f);
            }
            Text vehicleDescription = FindHillChildComponent<Text>(vehiclePanel.transform, "Description");
            if (vehicleDescription != null)
            {
                vehicleDescription.text = "USE THE ARROWS TO SELECT A VEHICLE";
                vehicleDescription.fontSize = 14;
                RectTransform descriptionRect = vehicleDescription.rectTransform;
                descriptionRect.anchorMin = new Vector2(.02f, .04f);
                descriptionRect.anchorMax = new Vector2(.98f, .50f);
            }

            Button start = CreateButton("Adventure Start", safe, "START",
                new Vector2(-205f, 64f), new Vector2(330f, 82f), new Vector2(1f, 0f));
            start.image.color = new Color(.08f, .66f, .32f, .98f);
            Text startText = start.GetComponentInChildren<Text>(true);
            if (startText != null) startText.fontSize = 34;
            Bind(start, menu.StartAdventure);
            Text pendingTrack = CreateText("Pending Adventure Track", safe,
                "SELECTED TRACK", 18, TextAnchor.MiddleCenter,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-205f, 126f),
                new Vector2(390f, 38f));

            GameObject tuningPanel = FindHillObject(scene, "Garage Tuning Panel");
            if (tuningPanel == null) throw new InvalidOperationException("Garage tuning panel is missing.");
            RectTransform tuningPanelRect = tuningPanel.GetComponent<RectTransform>();
            tuningPanelRect.anchorMin = new Vector2(.025f, .51f);
            tuningPanelRect.anchorMax = new Vector2(.975f, .80f);
            tuningPanelRect.anchoredPosition = Vector2.zero;
            tuningPanelRect.sizeDelta = Vector2.zero;
            Image[] tuneIcons = new Image[6];
            Image[] tuneCoinIcons = new Image[6];
            Button[] tuneCardButtons = new Button[6];
            UnityAction[] tuneCardActions =
            {
                menu.UpgradeTune0,
                menu.UpgradeTune1,
                menu.UpgradeTune2,
                menu.UpgradeTune3,
                menu.UpgradeTune4,
                menu.UpgradeTune5
            };
            for (int i = 0; i < tuneIcons.Length; i++)
            {
                Transform row = tuningPanel.transform.Find("Tune Row " + (i + 1));
                if (row == null) throw new InvalidOperationException("Garage tune row is missing: " + (i + 1));
                RectTransform rowRect = row as RectTransform;
                rowRect.anchorMin = rowRect.anchorMax = new Vector2(.5f, .5f);
                rowRect.anchoredPosition = new Vector2(-600f + i * 240f, 0f);
                rowRect.sizeDelta = new Vector2(205f, 273f);
                tuneIcons[i] = CreateSpriteImage("Tune Icon", row, null,
                    new Vector2(.5f, 1f), new Vector2(0f, -76f), new Vector2(64f, 64f));
                Transform name = row.Find("Name");
                Transform level = row.Find("Level");
                if (name is RectTransform nameRect)
                {
                    nameRect.anchorMin = new Vector2(.04f, .44f);
                    nameRect.anchorMax = new Vector2(.96f, .55f);
                    nameRect.anchoredPosition = Vector2.zero;
                    nameRect.sizeDelta = Vector2.zero;
                    Text nameText = nameRect.GetComponent<Text>();
                    if (nameText != null) nameText.alignment = TextAnchor.MiddleCenter;
                }
                if (level is RectTransform levelRect)
                {
                    levelRect.anchorMin = new Vector2(.04f, .34f);
                    levelRect.anchorMax = new Vector2(.96f, .44f);
                    levelRect.anchoredPosition = Vector2.zero;
                    levelRect.sizeDelta = Vector2.zero;
                    Text levelText = levelRect.GetComponent<Text>();
                    if (levelText != null) levelText.alignment = TextAnchor.MiddleCenter;
                }
                RectTransform valueRect = row.Find("Value") as RectTransform;
                if (valueRect != null)
                {
                    valueRect.anchorMin = new Vector2(.03f, .17f);
                    valueRect.anchorMax = new Vector2(.97f, .34f);
                    valueRect.anchoredPosition = Vector2.zero;
                    valueRect.sizeDelta = Vector2.zero;
                    Text valueText = valueRect.GetComponent<Text>();
                    if (valueText != null) valueText.alignment = TextAnchor.MiddleCenter;
                }
                RectTransform decreaseRect = row.Find("Decrease") as RectTransform;
                if (decreaseRect != null)
                {
                    decreaseRect.anchorMin = decreaseRect.anchorMax = new Vector2(.5f, 0f);
                    decreaseRect.anchoredPosition = new Vector2(-72f, 21f);
                    decreaseRect.sizeDelta = new Vector2(42f, 38f);
                }
                Button upgrade = row.Find("Upgrade")?.GetComponent<Button>();
                if (upgrade == null) throw new InvalidOperationException("Tune Upgrade button is missing.");
                RectTransform upgradeRect = upgrade.transform as RectTransform;
                upgradeRect.anchorMin = upgradeRect.anchorMax = new Vector2(.5f, 0f);
                upgradeRect.anchoredPosition = new Vector2(26f, 21f);
                upgradeRect.sizeDelta = new Vector2(128f, 38f);
                tuneCoinIcons[i] = CreateSpriteImage("Coin Icon", upgrade.transform, assets.coin01,
                    new Vector2(0f, .5f), new Vector2(15f, 0f), new Vector2(21f, 21f));
                Text price = upgrade.GetComponentInChildren<Text>(true);
                if (price != null)
                {
                    price.rectTransform.offsetMin = new Vector2(34f, 0f);
                    price.rectTransform.offsetMax = new Vector2(-4f, 0f);
                    price.alignment = TextAnchor.MiddleCenter;
                }
            }

            GameObject storePanel = FindHillObject(scene, "Store Panel");
            if (storePanel == null) throw new InvalidOperationException("Garage Shop panel is missing.");
            Image storePanelImage = storePanel.GetComponent<Image>();
            if (storePanelImage != null)
                storePanelImage.color = new Color(.025f, .06f, .12f, .78f);
            Image storeBackground = CreateAspectCoverBackground(storePanel.transform, defaultMenu);
            storeBackground.name = "Shop Shared Background (Compatibility Layer)";
            storeBackground.color = Color.white;
            storeBackground.enabled = false;
            storeBackground.transform.SetAsFirstSibling();
            Text premiumHeading = FindHillChildComponent<Text>(storePanel.transform, "Special Offers Title");
            if (premiumHeading != null) premiumHeading.text = "PREMIUM CONTENT";
            Text blueprintHeading = FindHillChildComponent<Text>(storePanel.transform, "Blueprint Shop Title");
            if (blueprintHeading != null) blueprintHeading.text = "COINS, BLUEPRINTS & ACCESSORIES";

            GameObject settingsPanel = FindHillObject(scene, "Settings Panel");
            if (settingsPanel == null) throw new InvalidOperationException("Garage Settings panel is missing.");
            Text oldSettings = FindHillChildComponent<Text>(settingsPanel.transform, "Settings Placeholder");
            if (oldSettings != null)
            {
                oldSettings.name = "Settings Title";
                oldSettings.text = "SETTINGS";
                oldSettings.rectTransform.anchorMin = new Vector2(.10f, .88f);
                oldSettings.rectTransform.anchorMax = new Vector2(.90f, .98f);
            }
            CreateText("Audio Label", settingsPanel.transform, "MASTER AUDIO", 28,
                TextAnchor.MiddleLeft, new Vector2(.12f, .76f), new Vector2(.54f, .86f),
                Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Toggle audioToggle = CreateHillToggle("Master Audio Toggle", settingsPanel.transform,
                new Vector2(.72f, .81f));
            UnityEventTools.AddPersistentListener(audioToggle.onValueChanged,
                menu.SetMasterAudioEnabled);
            Text audioStatus = CreateText("Master Audio Status", settingsPanel.transform, "ON", 23,
                TextAnchor.MiddleCenter, new Vector2(.62f, .69f), new Vector2(.84f, .75f),
                Vector2.zero, Vector2.zero);
            Button[] menuBackgroundButtons = CreateHillMenuBackgroundSelector(settingsPanel.transform,
                menu, defaultMenu, neonMenu);
            Button[] backgroundButtons = CreateHillGarageBackgroundSelector(settingsPanel.transform,
                menu, defaultGarage, neonGarage, animeGarage);
            CreateText("Settings Note", settingsPanel.transform,
                "AUDIO AND BACKGROUNDS ARE SAVED ON THIS DEVICE.", 16, TextAnchor.MiddleCenter,
                new Vector2(.08f, .015f), new Vector2(.92f, .065f),
                Vector2.zero, Vector2.zero).color = new Color(.62f, .78f, .88f);

            GameObject modal = FindHillObject(scene, "Purchase Confirmation");
            if (modal == null) throw new InvalidOperationException("Garage purchase modal is missing.");
            Image modalBackground = modal.GetComponent<Image>();
            if (modalBackground != null) modalBackground.color = new Color(.20f, .21f, .24f, .99f);
            RectTransform modalRect = modal.GetComponent<RectTransform>();
            modalRect.sizeDelta = new Vector2(820f, 520f);
            Image confirmationIcon = CreateSpriteImage("Tune Icon", modal.transform, null,
                new Vector2(.22f, .60f), Vector2.zero, new Vector2(150f, 150f));
            Text confirmationDescription = CreateText("Tune Description", modal.transform,
                "TUNE EFFECT", 21, TextAnchor.MiddleLeft,
                new Vector2(.40f, .44f), new Vector2(.91f, .70f),
                Vector2.zero, Vector2.zero);
            Image confirmationCoinIcon = CreateSpriteImage("Coin Icon", modal.transform,
                assets.coin01, new Vector2(.35f, .31f), Vector2.zero, new Vector2(38f, 38f));
            Text modalValues = FindHillChildComponent<Text>(modal.transform, "Values");
            if (modalValues != null)
            {
                modalValues.rectTransform.anchorMin = new Vector2(.40f, .68f);
                modalValues.rectTransform.anchorMax = new Vector2(.92f, .80f);
                modalValues.alignment = TextAnchor.MiddleLeft;
            }
            Text modalCost = FindHillChildComponent<Text>(modal.transform, "Cost");
            if (modalCost != null)
            {
                modalCost.rectTransform.anchorMin = new Vector2(.40f, .25f);
                modalCost.rectTransform.anchorMax = new Vector2(.90f, .39f);
                modalCost.alignment = TextAnchor.MiddleLeft;
            }

            Sprite affordableState = RequireHillSubSprite(
                HillTuneRoot + "/UpgradeTuneStates.png", "UpgradeAffordable");
            Sprite unaffordableState = RequireHillSubSprite(
                HillTuneRoot + "/UpgradeTuneStates.png", "UpgradeUnaffordable");
            for (int i = 0; i < tuneIcons.Length; i++)
            {
                Image card = tuneIcons[i] != null && tuneIcons[i].transform.parent != null
                    ? tuneIcons[i].transform.parent.GetComponent<Image>() : null;
                if (card == null) continue;
                card.sprite = affordableState;
                card.type = Image.Type.Simple;
                card.preserveAspect = false;
                card.color = Color.white;
                card.raycastTarget = true;
                Button cardButton = card.GetComponent<Button>();
                if (cardButton == null) cardButton = card.gameObject.AddComponent<Button>();
                cardButton.targetGraphic = card;
                ColorBlock colors = cardButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
                colors.pressedColor = new Color(.78f, .88f, 1f, 1f);
                colors.disabledColor = new Color(.65f, .65f, .65f, .72f);
                colors.fadeDuration = .06f;
                cardButton.colors = colors;
                Bind(cardButton, tuneCardActions[i]);
                tuneCardButtons[i] = cardButton;
            }
            var bindings = new HillAdventurerGarageBindings
            {
                startButton = start,
                pendingTrackLabel = pendingTrack,
                topNavigation = navigation.Controller,
                garageBackground = garageBackground,
                defaultGarageBackground = defaultGarage,
                neonGarageBackground = neonGarage,
                animeGarageBackground = animeGarage,
                backgroundButtons = backgroundButtons,
                shopMenuBackground = storeBackground,
                defaultMenuBackground = defaultMenu,
                neonMenuBackground = neonMenu,
                menuBackgroundButtons = menuBackgroundButtons,
                previousVehicleButton = previousVehicle,
                nextVehicleButton = nextVehicle,
                masterAudioToggle = audioToggle,
                masterAudioStatus = audioStatus,
                tuneIcons = tuneIcons,
                tuneCoinIcons = tuneCoinIcons,
                tuneCardButtons = tuneCardButtons,
                engineTuneIcon = RequireHillSprite(HillTuneRoot + "/MotorTune.png"),
                accelerationTuneIcon = RequireHillSprite(HillTuneRoot + "/AccelerationTune.png"),
                suspensionTuneIcon = RequireHillSprite(HillTuneRoot + "/SuspensionTune.png"),
                gripTuneIcon = RequireHillSprite(HillTuneRoot + "/GripTune.png"),
                fuelTuneIcon = RequireHillSprite(HillTuneRoot + "/FuelTune.png"),
                downforceTuneIcon = RequireHillSprite(HillTuneRoot + "/DownforceTune.png"),
                thrustersTuneIcon = RequireHillSprite(HillTuneRoot + "/ThrustersTune.png"),
                buggyInclinationTuneIcon = RequireHillSprite(HillTuneRoot + "/InclinationBuggyTune.png"),
                tuneAffordableBackground = affordableState,
                tuneUnaffordableBackground = unaffordableState,
                confirmationIcon = confirmationIcon,
                confirmationCoinIcon = confirmationCoinIcon,
                confirmationPanel = modalBackground,
                confirmationDescription = confirmationDescription
            };
            menu.AssignHillAdventurer(bindings);

            // GarageUI refreshes these from the real save at runtime. Its editor-time Assign calls
            // must not serialize a developer profile's balances into source-controlled scenes.
            Text tuningCoins = FindHillComponent<Text>(scene, "Tuning Coins");
            Text gemsBalance = FindHillComponent<Text>(scene, "Gems Balance");
            Text blueprintCoins = FindHillComponent<Text>(scene, "Blueprint Coins");
            if (tuningCoins != null) tuningCoins.text = "COINS  0";
            if (gemsBalance != null) gemsBalance.text = "GEMS  0";
            if (blueprintCoins != null) blueprintCoins.text = "COINS  0";
            EditorSceneManager.SaveScene(scene, GaragePath);
        }

        private static void CreateHillGarageSubNavigation(Transform safe, GarageUI menu)
        {
            Button vehicle = CreateButton("Garage Vehicle Tab", safe, "VEHICLE",
                new Vector2(-330f, -171f), new Vector2(160f, 50f), new Vector2(.5f, 1f));
            Button tunes = CreateButton("Garage Tunes Tab", safe, "TUNES",
                new Vector2(-155f, -171f), new Vector2(160f, 50f), new Vector2(.5f, 1f));
            Button skins = CreateButton("Garage Paint Tab", safe, "PAINT / SKINS",
                new Vector2(40f, -171f), new Vector2(210f, 50f), new Vector2(.5f, 1f));
            Button stats = CreateButton("Garage Stats Tab", safe, "STATS",
                new Vector2(240f, -171f), new Vector2(160f, 50f), new Vector2(.5f, 1f));
            Bind(vehicle, menu.ShowVehicle);
            Bind(tunes, menu.ShowTuning);
            Bind(skins, menu.ShowSkins);
            Bind(stats, menu.ShowUpgrade);
        }

        private static Button[] CreateHillGarageBackgroundSelector(Transform parent, GarageUI menu,
            Sprite defaultGarage, Sprite neonGarage, Sprite animeGarage)
        {
            CreateText("Garage Background Title", parent, "GARAGE BACKGROUND", 22,
                TextAnchor.MiddleCenter, new Vector2(.10f, .31f), new Vector2(.90f, .38f),
                Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Sprite[] sprites = { defaultGarage, neonGarage, animeGarage };
            string[] labels = { "DEFAULT", "NEON", "ANIME" };
            UnityAction[] actions =
            {
                menu.SelectGarageBackground0,
                menu.SelectGarageBackground1,
                menu.SelectGarageBackground2
            };
            Button[] buttons = new Button[3];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateSpriteButton("Garage Background " + labels[i], parent,
                    sprites[i], new Vector2(.5f, .23f),
                    new Vector2(-180f + i * 180f, 0f), new Vector2(158f, 108f));
                buttons[i].image.color = Color.white;
                Outline outline = buttons[i].gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.14f, .90f, 1f, 1f);
                outline.effectDistance = new Vector2(4f, -4f);
                outline.useGraphicAlpha = false;
                outline.enabled = false;
                GameObject labelBack = CreateImage("Label Background", buttons[i].transform,
                    new Color(.005f, .012f, .03f, .88f), new Vector2(0f, 0f),
                    new Vector2(1f, .25f), Vector2.zero, Vector2.zero);
                labelBack.GetComponent<Image>().raycastTarget = false;
                Text label = CreateText("Label", labelBack.transform, labels[i], 15,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero);
                label.fontStyle = FontStyle.Bold;
                GameObject selectedMarker = CreateImage("Selected Marker", buttons[i].transform,
                    new Color(.14f, .90f, 1f, .98f), new Vector2(.17f, .73f),
                    new Vector2(.83f, .98f), Vector2.zero, Vector2.zero);
                selectedMarker.GetComponent<Image>().raycastTarget = false;
                Text selectedLabel = CreateText("Label", selectedMarker.transform, "SELECTED", 12,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                selectedLabel.color = new Color(.01f, .04f, .08f, 1f);
                selectedLabel.fontStyle = FontStyle.Bold;
                selectedMarker.SetActive(false);
                Bind(buttons[i], actions[i]);
            }
            return buttons;
        }

        private static Button[] CreateHillMenuBackgroundSelector(Transform parent, GarageUI menu,
            Sprite defaultBackground, Sprite neonBackground)
        {
            CreateText("Menu Background Title", parent, "BACKGROUND  —  ADVENTURE / SHOP", 20,
                TextAnchor.MiddleCenter, new Vector2(.08f, .59f), new Vector2(.92f, .66f),
                Vector2.zero, Vector2.zero).fontStyle = FontStyle.Bold;
            Sprite[] sprites = { defaultBackground, neonBackground };
            string[] labels = { "DEFAULT", "NEON" };
            UnityAction[] actions = { menu.SelectMenuBackground0, menu.SelectMenuBackground1 };
            Button[] buttons = new Button[2];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateSpriteButton("Menu Background " + labels[i], parent,
                    sprites[i], new Vector2(.5f, .51f),
                    new Vector2(-105f + i * 210f, 0f), new Vector2(190f, 100f));
                buttons[i].image.color = Color.white;
                Outline outline = buttons[i].gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.14f, .90f, 1f, 1f);
                outline.effectDistance = new Vector2(4f, -4f);
                outline.useGraphicAlpha = false;
                outline.enabled = false;
                GameObject labelBack = CreateImage("Label Background", buttons[i].transform,
                    new Color(.005f, .012f, .03f, .88f), Vector2.zero,
                    new Vector2(1f, .25f), Vector2.zero, Vector2.zero);
                labelBack.GetComponent<Image>().raycastTarget = false;
                Text label = CreateText("Label", labelBack.transform, labels[i], 15,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                label.fontStyle = FontStyle.Bold;
                GameObject selectedMarker = CreateImage("Selected Marker", buttons[i].transform,
                    new Color(.14f, .90f, 1f, .98f), new Vector2(.17f, .73f),
                    new Vector2(.83f, .98f), Vector2.zero, Vector2.zero);
                selectedMarker.GetComponent<Image>().raycastTarget = false;
                Text selectedLabel = CreateText("Label", selectedMarker.transform, "SELECTED", 12,
                    TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                selectedLabel.color = new Color(.01f, .04f, .08f, 1f);
                selectedLabel.fontStyle = FontStyle.Bold;
                selectedMarker.SetActive(false);
                Bind(buttons[i], actions[i]);
            }
            return buttons;
        }

        private static Toggle CreateHillToggle(string name, Transform parent, Vector2 anchor)
        {
            GameObject root = UIObject(name, parent, anchor, anchor, Vector2.zero,
                new Vector2(154f, 66f));
            Image background = root.AddComponent<Image>();
            background.color = new Color(.07f, .15f, .25f, .98f);
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;
            Toggle toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            Image check = CreateImage("Checkmark", root.transform,
                new Color(.16f, .88f, .38f, 1f), new Vector2(.54f, .14f),
                new Vector2(.90f, .86f), Vector2.zero, Vector2.zero).GetComponent<Image>();
            check.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            check.preserveAspect = true;
            check.raycastTarget = false;
            toggle.graphic = check;
            root.AddComponent<ButtonPressFeedback>();
            return toggle;
        }

        private static Sprite RequireHillSubSprite(string path, string spriteName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
                if (assets[i] is Sprite sprite && sprite.name == spriteName) return sprite;
            throw new InvalidOperationException($"Required sprite slice '{spriteName}' was not imported from {path}.");
        }

        private static Transform FindHillTransform(Scene scene, string objectName) =>
            FindHillObject(scene, objectName)?.transform;

        private static GameObject FindHillObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                    if (transforms[j] != null && transforms[j].name == objectName)
                        return transforms[j].gameObject;
            }
            return null;
        }

        private static T FindHillComponent<T>(Scene scene, string objectName) where T : Component =>
            FindHillObject(scene, objectName)?.GetComponent<T>();

        private static T FindHillChildComponent<T>(Transform root, string objectName) where T : Component
        {
            if (root == null) return null;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i] != null && transforms[i].name == objectName)
                    return transforms[i].GetComponent<T>();
            return null;
        }

        private static void MoveHillRect(Scene scene, string objectName, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            RectTransform rect = FindHillComponent<RectTransform>(scene, objectName);
            if (rect == null) return;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static bool ValidateHillAdventurerMenuScene(Scene scene)
        {
            bool valid = true;
            valid &= ValidateHillCount(scene, SceneComponents<Canvas>(scene).Length, 1, "Canvas");
            valid &= ValidateHillCount(scene, SceneComponents<SafeArea>(scene).Length, 1, "SafeArea");
            valid &= ValidateHillCount(scene, SceneComponents<UnityEngine.EventSystems.EventSystem>(scene).Length,
                1, "EventSystem");
            valid &= ValidateHillCount(scene, SceneComponents<TopNavigationUI>(scene).Length,
                1, "TopNavigationUI");

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(roots[i]);
                if (missing <= 0) continue;
                Debug.LogError($"{scene.name}: {roots[i].name} contains {missing} missing script(s).");
                valid = false;
            }

            CanvasScaler[] scalers = SceneComponents<CanvasScaler>(scene);
            if (scalers.Length != 1 || scalers[0].uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                Vector2.Distance(scalers[0].referenceResolution, new Vector2(1920f, 1080f)) > .1f)
            {
                Debug.LogError($"{scene.name}: responsive CanvasScaler 1920x1080 is missing.");
                valid = false;
            }

            Image settingsImage = FindHillComponent<Image>(scene, "Settings");
            if (settingsImage == null || settingsImage.sprite != RequireHillSprite(HillSettingsIconPath) ||
                settingsImage.rectTransform.rect.width < 48f || settingsImage.rectTransform.rect.height < 48f)
            {
                Debug.LogError($"{scene.name}: supplied Settings touch control is missing or undersized.");
                valid = false;
            }

            if (scene.name == "MainMenu")
            {
                valid &= ValidateHillCount(scene, SceneComponents<MainMenuUI>(scene).Length, 1, "MainMenuUI");
                valid &= ValidateHillCount(scene, SceneComponents<GaragePreviewRenderer>(scene).Length,
                    1, "selected vehicle preview");
                Image mainBackground = FindHillComponent<Image>(scene, "Main Menu Default Background");
                if (mainBackground == null || mainBackground.sprite != RequireHillSprite(HillDefaultBackgroundPath))
                {
                    Debug.LogError("MainMenu: supplied Default_Background is not assigned.");
                    valid = false;
                }
            }
            else if (scene.name == "PreStart")
            {
                valid &= ValidateHillCount(scene, SceneComponents<PreStartMenuUI>(scene).Length,
                    1, "Track Selection controller");
                GridLayoutGroup[] grids = SceneComponents<GridLayoutGroup>(scene);
                if (grids.Length != 1 || grids[0].constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
                    grids[0].constraintCount != 2)
                {
                    Debug.LogError("PreStart: Track Selection must have exactly one FixedColumnCount=2 grid.");
                    valid = false;
                }
                ScrollRect[] scrolls = SceneComponents<ScrollRect>(scene);
                if (scrolls.Length != 1 || !scrolls[0].vertical || scrolls[0].horizontal ||
                    scrolls[0].content == null || scrolls[0].viewport == null)
                {
                    Debug.LogError("PreStart: vertical Track Selection ScrollRect is not fully assigned.");
                    valid = false;
                }
                TrackSelectionCardUI[] cards = SceneComponents<TrackSelectionCardUI>(scene);
                if (cards.Length != 1 || cards[0].gameObject.activeSelf)
                {
                    Debug.LogError("PreStart: one inactive data-driven Track Card template is required.");
                    valid = false;
                }
                ChestPanelUI[] chests = SceneComponents<ChestPanelUI>(scene);
                if (chests.Length != 1)
                {
                    Debug.LogError("PreStart: exactly one ChestPanelUI is required.");
                    valid = false;
                }
                else
                {
                    SerializedObject chest = new SerializedObject(chests[0]);
                    SerializedProperty slots = chest.FindProperty("slotButtons");
                    SerializedProperty previous = chest.FindProperty("previousPageButton");
                    SerializedProperty next = chest.FindProperty("nextPageButton");
                    if (slots == null || slots.arraySize != ChestPanelUI.SlotsPerPage ||
                        previous?.objectReferenceValue == null || next?.objectReferenceValue == null)
                    {
                        Debug.LogError("PreStart: four Chest Slots plus previous/next navigation are required.");
                        valid = false;
                    }
                }
                Sprite chestFrame = RequireHillSubSprite(HillChestSlotsPath, "ChestSlotFrame");
                Image[] chestFrames = SceneComponents<Image>(scene);
                int suppliedFrameCount = 0;
                for (int i = 0; i < chestFrames.Length; i++)
                    if (chestFrames[i] != null && chestFrames[i].sprite == chestFrame)
                        suppliedFrameCount++;
                if (suppliedFrameCount != ChestPanelUI.SlotsPerPage)
                {
                    Debug.LogError("PreStart: exactly four visual frames sliced from the supplied ChestSlots artwork are required.");
                    valid = false;
                }
            }
            else if (scene.name == "Garage")
            {
                GarageUI[] garages = SceneComponents<GarageUI>(scene);
                valid &= ValidateHillCount(scene, garages.Length, 1, "GarageUI");
                valid &= ValidateHillCount(scene, SceneComponents<GaragePreviewRenderer>(scene).Length,
                    1, "GaragePreviewRenderer");
                if (garages.Length != 1) return false;
                GarageUI garage = garages[0];
                SerializedObject serialized = new SerializedObject(garage);
                string[] references =
                {
                    "startButton", "pendingTrackLabel", "topNavigation", "garageBackground",
                    "defaultGarageBackground", "neonGarageBackground", "animeGarageBackground",
                    "previousVehicleButton", "nextVehicleButton",
                    "masterAudioToggle", "masterAudioStatus", "confirmationIcon",
                    "confirmationCoinIcon", "confirmationPanel", "confirmationDescription"
                };
                for (int i = 0; i < references.Length; i++)
                {
                    SerializedProperty property = serialized.FindProperty(references[i]);
                    if (property != null && property.objectReferenceValue != null) continue;
                    Debug.LogError($"Garage: required Hill Adventurer reference is missing: {references[i]}.");
                    valid = false;
                }
                string[] arrays = { "backgroundButtons", "tuningIcons", "tuningCoinIcons" };
                int[] expected = { 3, 6, 6 };
                for (int i = 0; i < arrays.Length; i++)
                {
                    SerializedProperty property = serialized.FindProperty(arrays[i]);
                    if (property != null && property.isArray && property.arraySize == expected[i]) continue;
                    Debug.LogError($"Garage: {arrays[i]} must contain {expected[i]} assigned elements.");
                    valid = false;
                }
                GameObject settingsPanel = FindHillObject(scene, "Settings Panel");
                GameObject vehiclePanel = FindHillObject(scene, "Vehicle Panel");
                GameObject backgroundTitle = FindHillObject(scene, "Garage Background Title");
                if (settingsPanel == null || vehiclePanel == null || backgroundTitle == null ||
                    !backgroundTitle.transform.IsChildOf(settingsPanel.transform) ||
                    backgroundTitle.transform.IsChildOf(vehiclePanel.transform))
                {
                    Debug.LogError("Garage: GARAGE BACKGROUND must exist only inside Settings.");
                    valid = false;
                }
                SerializedProperty backgroundOptions = serialized.FindProperty("backgroundButtons");
                int selectedMarkers = 0;
                if (backgroundOptions != null && backgroundOptions.isArray)
                {
                    for (int i = 0; i < backgroundOptions.arraySize; i++)
                    {
                        Button button = backgroundOptions.GetArrayElementAtIndex(i).objectReferenceValue as Button;
                        if (button == null || settingsPanel == null ||
                            !button.transform.IsChildOf(settingsPanel.transform))
                        {
                            Debug.LogError("Garage: background option " + i + " is outside Settings.");
                            valid = false;
                            continue;
                        }
                        if (button.image != null && button.image.color != Color.white)
                        {
                            Debug.LogError("Garage: background option " + i + " darkens its original artwork.");
                            valid = false;
                        }
                        Transform marker = button.transform.Find("Selected Marker");
                        if (marker == null)
                        {
                            Debug.LogError("Garage: background option " + i + " has no selected marker.");
                            valid = false;
                        }
                        else if (marker.gameObject.activeSelf) selectedMarkers++;
                    }
                }
                if (selectedMarkers != 1)
                {
                    Debug.LogError("Garage: exactly one background option must be visibly selected.");
                    valid = false;
                }
                RectTransform centeredPreview = FindHillComponent<RectTransform>(scene, "Live Composed Preview");
                if (centeredPreview == null ||
                    Mathf.Abs((centeredPreview.anchorMin.x + centeredPreview.anchorMax.x) * .5f - .5f) > .01f ||
                    centeredPreview.anchorMax.x - centeredPreview.anchorMin.x < .60f)
                {
                    Debug.LogError("Garage: Vehicle Selection preview is not centered over the stand.");
                    valid = false;
                }
                Image garageOverlay = FindHillComponent<Image>(scene, "Garage Overlay");
                if (garageOverlay == null || garageOverlay.color.a > .16f)
                {
                    Debug.LogError("Garage: overlay darkens the selected background excessively.");
                    valid = false;
                }
                ScrollRect[] shopScrolls = SceneComponents<ScrollRect>(scene);
                bool verticalShop = false;
                for (int i = 0; i < shopScrolls.Length; i++)
                    verticalShop |= shopScrolls[i] != null && shopScrolls[i].vertical &&
                                    !shopScrolls[i].horizontal && shopScrolls[i].content != null;
                if (!verticalShop)
                {
                    Debug.LogError("Garage: Shop requires a vertical ScrollRect.");
                    valid = false;
                }
                if (FindHillObject(scene, "Settings Placeholder") != null)
                {
                    Debug.LogError("Garage: legacy Settings placeholder still exists.");
                    valid = false;
                }
            }

            if (valid) Debug.Log($"{scene.name}: Hill Adventurer menu validation passed.");
            return valid;
        }

        private static bool ValidateHillCount(Scene scene, int actual, int expected, string label)
        {
            if (actual == expected) return true;
            Debug.LogError($"{scene.name}: expected {expected} {label}, found {actual}.");
            return false;
        }
    }
}
#endif
