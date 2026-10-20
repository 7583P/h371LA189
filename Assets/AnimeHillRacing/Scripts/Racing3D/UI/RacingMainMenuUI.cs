using System;
using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingMainMenuUI : MonoBehaviour
    {
        private Button adventureCardBtn;
        private Text coinLabel;
        private Text gemLabel;
        private Text mmrLabel,tierLabel;
        private RacingRankBadge liveRankArtwork;
        private int displayedMmr=-1;
        private float nextRatingRefresh;

        private void Start() => Initialize();

        private void OnEnable()
        {
            SaveManager.CurrencyChanged += RefreshCurrency;
        }

        private void OnDisable()
        {
            SaveManager.CurrencyChanged -= RefreshCurrency;
        }

        private void RefreshCurrency()
        {
            if (coinLabel != null) coinLabel.text = SaveManager.TotalCoins.ToString("N0");
            if (gemLabel != null) gemLabel.text = SaveManager.TotalGems.ToString("N0");
        }
        private void Update()
        {
            if(Time.unscaledTime<nextRatingRefresh)return;nextRatingRefresh=Time.unscaledTime+.5f;
            // CPPS: an UNRANKED player sees Placement progress, never a 0-MMR rank.
            var human=RacingProgressionService.State.human;bool official=SeasonQualification.IsOfficial(human,RacingProgressionService.State.currentSeasonNumber);int rating=human.currentMmr;int key=human.Ranked?rating+(official?0:1000000):-1-human.placementGames;
            if(key==displayedMmr)return;displayedMmr=key;
            if(mmrLabel!=null)mmrLabel.text=human.Ranked?$"{rating:N0} MMR":MmrTierUtility.UnrankedName;
            if(tierLabel!=null){tierLabel.text=SeasonQualification.DisplayRank(human,RacingProgressionService.State.currentSeasonNumber);tierLabel.color=official?RacingDesign.TierColor(rating):RacingDesign.Accent;}
            if(liveRankArtwork!=null){liveRankArtwork.Configure(rating);liveRankArtwork.gameObject.SetActive(official);}
        }

        public void Initialize()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.sortingOrder < 200) canvas.gameObject.SetActive(false);
            }

            RacingFeedback.Ensure();
            gameObject.AddComponent<RacingQualityScope>();

            var root = RacingDesign.Canvas("Hill Adventurer Main Menu", 30);

            // 1. Full-screen backdrop fill to guarantee seamless framing on any aspect ratio (ultrawide/tablet)
            var backdrop = RacingDesign.Area(root, "Cover Backdrop Fill", 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            var coverTex = Resources.Load<Texture2D>("RacingArt/MainMenuCover") 
                           ?? Resources.Load<Texture2D>("RacingArt/DAPEXMenu") 
                           ?? Resources.Load<Texture2D>("RacingArt/NeonDepthCity");
            backdrop.texture = coverTex;
            backdrop.raycastTarget = false;
            backdrop.color = new Color(0.10f, 0.12f, 0.18f, 1f);

            // 2. Master 16:9 Aspect Ratio Container for pixel-perfect artwork and interactive button alignment
            var frameGo = new GameObject("Artwork 16:9 Frame", typeof(RectTransform), typeof(AspectRatioFitter));
            var frameRt = (RectTransform)frameGo.transform;
            frameRt.SetParent(root, false);
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = Vector2.zero;
            frameRt.offsetMax = Vector2.zero;

            var fitter = frameGo.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
            frameGo.AddComponent<RacingScreenMotion>();

            // 3. Foreground High-Definition Cover Art
            var coverImage = RacingDesign.Area(frameRt, "Cover Artwork", 0, 0, 1, 1).gameObject.AddComponent<RawImage>();
            coverImage.texture = coverTex;
            coverImage.raycastTarget = false;
            coverImage.color = coverTex != null ? Color.white : RacingDesign.Background;

            // 4. Interactive Clickable Cards & Buttons mapped directly to the artwork
            // ADVENTURE Card Banner (2D Hill Climb Adventure)
            adventureCardBtn = CreateCoverButton(
                parent: frameRt,
                buttonName: "ADVENTURE",
                x: 0.068f, y: 0.132f, w: 0.430f, h: 0.228f,
                action: Adventure,
                hoverGlow: new Color(1f, 0.82f, 0.25f, 0.18f),
                pressGlow: new Color(1f, 0.82f, 0.25f, 0.35f),
                cornerCut: 16f
            );

            // Also make the upper Adventure illustration (Jeep on hill) clickable
            CreateCoverButton(
                parent: frameRt,
                buttonName: "ADVENTURE_ILLUST",
                x: 0.068f, y: 0.360f, w: 0.430f, h: 0.180f,
                action: Adventure,
                hoverGlow: new Color(1f, 0.82f, 0.25f, 0.12f),
                pressGlow: new Color(1f, 0.82f, 0.25f, 0.25f),
                cornerCut: 12f
            );

            // RANKED Card Banner (3D Night City Circuit)
            CreateCoverButton(
                parent: frameRt,
                buttonName: "RACING",
                x: 0.490f, y: 0.125f, w: 0.448f, h: 0.235f,
                action: EnterRacing,
                hoverGlow: new Color(0.20f, 0.82f, 1f, 0.18f),
                pressGlow: new Color(0.20f, 0.82f, 1f, 0.35f),
                cornerCut: 16f
            );

            // Also make the upper Ranked illustration (DAPEX supercar in city) clickable
            CreateCoverButton(
                parent: frameRt,
                buttonName: "RACING_ILLUST",
                x: 0.490f, y: 0.360f, w: 0.448f, h: 0.180f,
                action: EnterRacing,
                hoverGlow: new Color(0.20f, 0.82f, 1f, 0.12f),
                pressGlow: new Color(0.20f, 0.82f, 1f, 0.25f),
                cornerCut: 12f
            );

            // Bottom Dock: GARAGE
            CreateCoverButton(
                parent: frameRt,
                buttonName: "GARAGE",
                x: 0.140f, y: 0.024f, w: 0.230f, h: 0.088f,
                action: () => OpenGarage(GarageSection.Vehicle),
                hoverGlow: new Color(0.35f, 0.82f, 1f, 0.20f),
                pressGlow: new Color(0.35f, 0.82f, 1f, 0.40f),
                cornerCut: 14f
            );

            // Bottom Dock: SHOP
            CreateCoverButton(
                parent: frameRt,
                buttonName: "SHOP",
                x: 0.382f, y: 0.024f, w: 0.238f, h: 0.088f,
                action: () => OpenGarage(GarageSection.Store),
                hoverGlow: new Color(0.35f, 0.82f, 1f, 0.20f),
                pressGlow: new Color(0.35f, 0.82f, 1f, 0.40f),
                cornerCut: 14f
            );

            // Bottom Dock: SETTINGS
            CreateCoverButton(
                parent: frameRt,
                buttonName: "SETTINGS",
                x: 0.630f, y: 0.024f, w: 0.238f, h: 0.088f,
                action: () => OpenGarage(GarageSection.Settings),
                hoverGlow: new Color(0.35f, 0.82f, 1f, 0.20f),
                pressGlow: new Color(0.35f, 0.82f, 1f, 0.40f),
                cornerCut: 14f
            );

            // Top Header: Modular Independent Assets with Live Values
            BuildHeaderUI(frameRt);

            RacingDesign.Focus(adventureCardBtn);
        }

        private void BuildHeaderUI(Transform parent)
        {
            // 1. COINS Pill (Interactive Button with Dynamic Real Coin Value)
            var coinBtn = CreateCoverButton(
                parent: parent,
                buttonName: "COINS_PILL",
                x: 0.548f, y: 0.880f, w: 0.100f, h: 0.076f,
                action: () => OpenGarage(GarageSection.Store),
                hoverGlow: new Color(1f, 0.85f, 0.30f, 0.22f),
                pressGlow: new Color(1f, 0.85f, 0.30f, 0.42f),
                cornerCut: 10f
            );
            HeaderSurface(coinBtn,"COINS",RacingDesign.Gold);
            coinLabel = AddHeaderLabel(coinBtn.transform, SaveManager.TotalCoins.ToString("N0"), 0.08f, 0.08f, 0.84f, 0.52f, 20, Color.white, TextAnchor.MiddleCenter);

            // 2. GEMS Pill (Interactive Button with Dynamic Real Gem Value)
            var gemBtn = CreateCoverButton(
                parent: parent,
                buttonName: "GEMS_PILL",
                x: 0.652f, y: 0.880f, w: 0.094f, h: 0.076f,
                action: () => OpenGarage(GarageSection.Store),
                hoverGlow: new Color(0.30f, 0.85f, 1f, 0.22f),
                pressGlow: new Color(0.30f, 0.85f, 1f, 0.42f),
                cornerCut: 10f
            );
            HeaderSurface(gemBtn,"GEMS",RacingDesign.Cyan);
            gemLabel = AddHeaderLabel(gemBtn.transform, SaveManager.TotalGems.ToString("N0"), 0.08f, 0.08f, 0.84f, 0.52f, 20, Color.white, TextAnchor.MiddleCenter);

            // 3. MMR Badge (Interactive Button with Dynamic Real MMR & Tier Name)
            int currentMmr = RacingSaveManager.CurrentMmr;
            var mmrBtn = CreateCoverButton(
                parent: parent,
                buttonName: "MMR_BADGE",
                x: 0.752f, y: 0.870f, w: 0.160f, h: 0.088f,
                action: EnterRacing,
                hoverGlow: new Color(0.75f, 0.50f, 1f, 0.22f),
                pressGlow: new Color(0.75f, 0.50f, 1f, 0.42f),
                cornerCut: 10f
            );
            var mmrSurface=RacingDesign.Panel(mmrBtn.transform,"Live rank surface",0,0,1,1,new Color(.015f,.03f,.07f,.95f),10);mmrSurface.raycastTarget=false;mmrSurface.transform.SetAsFirstSibling();
            liveRankArtwork=RacingDesign.RankBadge(mmrBtn.transform,currentMmr,.025f,.04f,.28f,.92f);
            mmrLabel=AddHeaderLabel(mmrBtn.transform, $"{currentMmr:N0} MMR", 0.33f, 0.46f, 0.64f, 0.48f, 21, Color.white, TextAnchor.MiddleLeft);
            string tierName = MmrTierUtility.GetTierDisplayName(MmrTierUtility.GetTier(currentMmr));
            tierLabel=AddHeaderLabel(mmrBtn.transform, tierName, 0.33f, 0.08f, 0.64f, 0.40f, 15, RacingDesign.TierColor(currentMmr), TextAnchor.MiddleLeft);
            displayedMmr=int.MinValue;nextRatingRefresh=0;

            // 4. SETTINGS Gear (Interactive Button)
            CreateCoverButton(
                parent: parent,
                buttonName: "TOP_SETTINGS",
                x: 0.942f, y: 0.880f, w: 0.044f, h: 0.076f,
                action: () => OpenGarage(GarageSection.Settings),
                hoverGlow: new Color(0.80f, 0.90f, 1f, 0.22f),
                pressGlow: new Color(0.80f, 0.90f, 1f, 0.45f),
                cornerCut: 8f
            );

            // INBOX is completely eliminated per user request (no button, no notification icon, no modal)
        }

        private static void HeaderSurface(Button button,string label,Color color)
        {
            var surface=RacingDesign.Panel(button.transform,"Live currency surface",0,0,1,1,new Color(.015f,.03f,.07f,.96f),10);surface.raycastTarget=false;surface.transform.SetAsFirstSibling();
            AddHeaderLabel(button.transform,label,.08f,.61f,.84f,.30f,11,color,TextAnchor.MiddleCenter);
        }
        private static void AddHeaderIcon(Transform parent, string resourcePath, float x, float y, float w, float h)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                var iconArea = RacingDesign.Area(parent, "Cutout Icon", x, y, w, h);
                var raw = iconArea.gameObject.AddComponent<RawImage>();
                raw.texture = tex;
                raw.raycastTarget = false;
                raw.color = Color.white;
            }
        }

        private static Text AddHeaderLabel(Transform parent, string text, float x, float y, float w, float h, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var rt = RacingDesign.Area(parent, "Dynamic Text " + text, x, y, w, h);
            var lbl = rt.gameObject.AddComponent<Text>();
            lbl.font = RacingDesign.Font;
            lbl.fontSize = fontSize;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = color;
            lbl.text = text;
            lbl.alignment = anchor;
            lbl.raycastTarget = false;
            lbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbl.resizeTextForBestFit=true;lbl.resizeTextMinSize=Mathf.Min(12,fontSize);lbl.resizeTextMaxSize=fontSize;
            lbl.verticalOverflow = VerticalWrapMode.Truncate;
            return lbl;
        }

        private static Button CreateCoverButton(
            Transform parent,
            string buttonName,
            float x, float y, float w, float h,
            Action action,
            Color hoverGlow,
            Color pressGlow,
            float cornerCut)
        {
            var panel = RacingDesign.Panel(parent, buttonName, x, y, w, h, Color.clear, cornerCut);
            panel.raycastTarget = true;

            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.None;

            var nav = button.navigation;
            nav.mode = Navigation.Mode.Automatic;
            button.navigation = nav;

            var coverMotion = panel.gameObject.AddComponent<RacingCoverInteractionMotion>();
            coverMotion.Configure(panel, hoverGlow, pressGlow);

            if (action != null)
            {
                button.onClick.AddListener(() =>
                {
                    RacingFeedback.Play(RacingCue.UiConfirm);
                    action();
                });
            }

            return button;
        }

        public static void OpenGarage(GarageSection section)
        {
            GameSession.RequestDirectGarage(section);
            RacingLoadingScreen.Load("Garage", section == GarageSection.Store ? "UPGRADE SHOP" : section == GarageSection.Settings ? "SETTINGS" : "YOUR GARAGE");
        }

        public static void Adventure()
        {
            GameSession.BeginAdventureTrackSelection();
            RacingLoadingScreen.Load("PreStart", "CHOOSE YOUR ADVENTURE");
        }

        public static void EnterRacing()
        {
            GameSession.ClearGarageContext();
            RacingLoadingScreen.Load("RacingSelect", "RACING / NEON CITY");
        }

        // Scene / probe callbacks maintained for backward compatibility
        public void ShowAdventureVehicle() { }
        public void ShowMetaVehicle() { }
        public void ShowPlayChoices() => Adventure();
    }


}
