using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class PreStartMenuUI : MonoBehaviour
    {
        private enum Step { Mode, Vehicle }

        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text gemsText;
        [SerializeField] private Image gemIcon;
        [SerializeField] private Text progressText;
        [SerializeField] private GameObject[] stepPanels;
        [SerializeField] private Image[] modeCards;
        [SerializeField] private RectTransform[] modeCardRects;
        [SerializeField] private Text[] modeCardStatusTexts;
        [SerializeField] private Text modeText;
        [SerializeField] private Text mapText;
        [SerializeField] private Image mapSkyPreview;
        [SerializeField] private Image mapBackgroundPreview;
        [SerializeField] private Image mapGroundPreview;
        [SerializeField] private Text trackTitleText;
        [SerializeField] private Text trackBestDistanceText;
        [SerializeField] private GameObject adventureTrackSelector;
        [SerializeField] private GameObject comingSoonPanel;
        [SerializeField] private Text vehicleText;
        [SerializeField] private Image vehiclePreviewImage;
        [SerializeField] private GaragePreviewRenderer vehiclePreviewRenderer;
        [SerializeField] private GameObject vehiclePreviewFallback;
        [SerializeField] private Text vehicleStatsText;
        [SerializeField] private Text tuningSummaryText;
        [SerializeField] private Text transmissionText;
        [SerializeField] private GameObject tuningPanel;
        [SerializeField] private Text[] tuningNameTexts;
        [SerializeField] private Text[] tuningLevelTexts;
        [SerializeField] private Text[] tuningValueTexts;
        [SerializeField] private Text[] tuningCostTexts;
        [SerializeField] private Button[] tuningButtons;
        [SerializeField] private Text tuningCoinsText;

        [Header("Shared Menu Background")]
        [SerializeField] private Image menuBackground;
        [SerializeField] private Sprite defaultMenuBackground;
        [SerializeField] private Sprite neonMenuBackground;

        [Header("Adventure Track Selection")]
        [SerializeField] private bool trackSelectionLayout;
        [SerializeField] private GameObject trackSelectionRoot;
        [SerializeField] private ScrollRect trackScroll;
        [SerializeField] private RectTransform trackContent;
        [SerializeField] private GridLayoutGroup trackGrid;
        [SerializeField] private TrackSelectionCardUI trackCardTemplate;
        [SerializeField] private TrackSelectionCardUI[] trackCards;
        [SerializeField] private Text trackSelectionTitleText;
        [SerializeField] private Text trackSelectionEmptyText;
        [SerializeField, Min(.5f)] private float trackCardAspectRatio = 2.30f;
        [SerializeField] private CoinPurchaseConfirmationUI purchaseConfirmation;

        private Step step;
        private GameModeType mode = GameModeType.Adventure2_5D;
        private int mapIndex;
        private int vehicleIndex;
        private bool outlinesConfigured;
        private readonly List<TrackSelectionCardUI> generatedTrackCards = new List<TrackSelectionCardUI>();
        private float lastTrackViewportWidth = -1f;
        private int displayedTrackCoins = int.MinValue;
        private int displayedTrackGems = int.MinValue;

        private VehicleDefinition CurrentVehicle => catalog != null && catalog.vehicles != null && catalog.vehicles.Length > 0
            ? catalog.vehicles[Mathf.Clamp(vehicleIndex, 0, catalog.vehicles.Length - 1)] : null;
        private MapDefinition CurrentMap => catalog?.GetCompatibleMap(mode, mapIndex);

        private void Awake()
        {
            RefreshMenuBackground();
            EnsureGemCounter();
            if (trackSelectionLayout) EnsurePurchaseConfirmation();
            if (!trackSelectionLayout) EnsureModernLayout();
            EconomyCounterIconBinder.RefreshActiveScene();
        }

        private void Start()
        {
            if (trackSelectionLayout)
            {
                InitializeTrackSelection();
                return;
            }

            EnsureModernLayout();
            ResolveMapPreviewImages();
            mode = GameModeType.Adventure2_5D;
            FindSavedVehicle();
            FindSavedMap();
            GameSession.OpenTuningOnPreStart = false;
            GameSession.SelectedTransmission = TransmissionMode.Auto;
            ShowStep(Step.Mode);
        }

        private void Update()
        {
            if (trackSelectionLayout) ConfigureTwoColumnGrid();
            if (displayedTrackCoins != SaveManager.TotalCoins || displayedTrackGems != SaveManager.TotalGems)
                RefreshTrackSelectionCurrency();
        }

        private void EnsureModernLayout()
        {
            if (stepPanels == null || stepPanels.Length == 0 || stepPanels[0] == null) return;
            Transform step1 = stepPanels[0].transform;

            // 1. If old Mode Card Grid exists:
            Transform gridTransform = step1.Find("Mode Card Grid");
            if (gridTransform != null)
            {
                var gridLayout = gridTransform.GetComponent<GridLayoutGroup>();
                if (gridLayout != null) gridLayout.enabled = false;

                // Hide 3D and Time Trial cards
                for (int i = 1; i < gridTransform.childCount; i++)
                {
                    gridTransform.GetChild(i).gameObject.SetActive(false);
                }

                // Reshape Adventure card to compact horizontal badge
                if (gridTransform.childCount > 0)
                {
                    Transform advCard = gridTransform.GetChild(0);
                    var advRt = advCard.GetComponent<RectTransform>();
                    if (advRt != null)
                    {
                        advRt.anchorMin = new Vector2(0.5f, 1f);
                        advRt.anchorMax = new Vector2(0.5f, 1f);
                        advRt.anchoredPosition = new Vector2(0f, -95f);
                        advRt.sizeDelta = new Vector2(320f, 48f);
                    }
                    var cardImg = advCard.GetComponent<Image>();
                    if (cardImg != null) cardImg.color = new Color(0.08f, 0.22f, 0.42f, 0.95f);
                    var cardOutline = advCard.GetComponent<Outline>();
                    if (cardOutline != null) cardOutline.effectColor = new Color(0.30f, 0.85f, 1f, 0.90f);

                    // Update inner texts
                    Text[] cardTexts = advCard.GetComponentsInChildren<Text>(true);
                    for (int t = 0; t < cardTexts.Length; t++)
                    {
                        if (t == 0)
                        {
                            cardTexts[t].text = "ADVENTURE";
                            cardTexts[t].fontSize = 24;
                            cardTexts[t].fontStyle = FontStyle.Bold;
                            cardTexts[t].color = new Color(0.45f, 0.92f, 1f, 1f);
                            cardTexts[t].alignment = TextAnchor.MiddleCenter;
                            var textRt = cardTexts[t].GetComponent<RectTransform>();
                            if (textRt != null)
                            {
                                textRt.anchorMin = Vector2.zero;
                                textRt.anchorMax = Vector2.one;
                                textRt.anchoredPosition = Vector2.zero;
                                textRt.sizeDelta = Vector2.zero;
                            }
                        }
                        else
                        {
                            cardTexts[t].gameObject.SetActive(false);
                        }
                    }
                }
            }

            // 2. Coming Soon Badge (Top-Right)
            if (comingSoonPanel == null && step1.Find("Coming Soon Panel") == null)
            {
                var csGo = new GameObject("Coming Soon Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
                csGo.transform.SetParent(step1, false);
                var csRt = csGo.GetComponent<RectTransform>();
                csRt.anchorMin = new Vector2(1f, 1f);
                csRt.anchorMax = new Vector2(1f, 1f);
                csRt.anchoredPosition = new Vector2(-165f, -50f);
                csRt.sizeDelta = new Vector2(240f, 62f);
                var csImg = csGo.GetComponent<Image>();
                csImg.color = new Color(0.05f, 0.09f, 0.16f, 0.85f);
                var csOut = csGo.GetComponent<Outline>();
                csOut.effectColor = new Color(0.35f, 0.42f, 0.52f, 0.65f);
                csOut.effectDistance = new Vector2(1.5f, -1.5f);

                var headerGo = new GameObject("CS Header", typeof(RectTransform), typeof(Text));
                headerGo.transform.SetParent(csGo.transform, false);
                var hRt = headerGo.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0f, 0.45f);
                hRt.anchorMax = new Vector2(1f, 1f);
                hRt.anchoredPosition = Vector2.zero;
                hRt.sizeDelta = Vector2.zero;
                var hTxt = headerGo.GetComponent<Text>();
                hTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                hTxt.text = "COMING SOON...";
                hTxt.fontSize = 15;
                hTxt.fontStyle = FontStyle.Bold;
                hTxt.alignment = TextAnchor.MiddleCenter;
                hTxt.color = new Color(0.90f, 0.85f, 0.45f, 1f);

                var subGo = new GameObject("CS Sub", typeof(RectTransform), typeof(Text));
                subGo.transform.SetParent(csGo.transform, false);
                var sRt = subGo.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0f, 0f);
                sRt.anchorMax = new Vector2(1f, 0.52f);
                sRt.anchoredPosition = Vector2.zero;
                sRt.sizeDelta = Vector2.zero;
                var sTxt = subGo.GetComponent<Text>();
                sTxt.font = hTxt.font;
                sTxt.text = "3D  •  TIME TRIAL";
                sTxt.fontSize = 13;
                sTxt.alignment = TextAnchor.MiddleCenter;
                sTxt.color = new Color(0.55f, 0.65f, 0.78f, 0.90f);

                comingSoonPanel = csGo;
            }

            // 3. Central Track Preview Frame
            Transform frameTransform = step1.Find("Track Preview Frame");
            if (frameTransform == null)
            {
                var frameGo = new GameObject("Track Preview Frame", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(Outline), typeof(Shadow));
                frameGo.transform.SetParent(step1, false);
                var frameRt = frameGo.GetComponent<RectTransform>();
                frameRt.anchorMin = new Vector2(0.5f, 0.5f);
                frameRt.anchorMax = new Vector2(0.5f, 0.5f);
                frameRt.anchoredPosition = new Vector2(0f, 30f);
                frameRt.sizeDelta = new Vector2(620f, 340f);
                var frameImg = frameGo.GetComponent<Image>();
                frameImg.color = new Color(0.02f, 0.05f, 0.10f, 0.95f);
                var frameOutline = frameGo.GetComponent<Outline>();
                frameOutline.effectColor = new Color(0.25f, 0.72f, 1f, 0.85f);
                frameOutline.effectDistance = new Vector2(2f, -2f);
                var frameShadow = frameGo.GetComponent<Shadow>();
                frameShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                frameShadow.effectDistance = new Vector2(0f, -6f);
                frameTransform = frameGo.transform;
            }
            else
            {
                if (frameTransform.GetComponent<RectMask2D>() == null)
                    frameTransform.gameObject.AddComponent<RectMask2D>();
            }

            Transform skyTr = frameTransform.Find("Sky Preview");
            if (skyTr == null)
            {
                var skyGo = new GameObject("Sky Preview", typeof(RectTransform), typeof(Image));
                skyGo.transform.SetParent(frameTransform, false);
                var skyRt = skyGo.GetComponent<RectTransform>();
                skyRt.anchorMin = Vector2.zero;
                skyRt.anchorMax = Vector2.one;
                skyRt.anchoredPosition = Vector2.zero;
                skyRt.sizeDelta = Vector2.zero;
                mapSkyPreview = skyGo.GetComponent<Image>();
                mapSkyPreview.color = Color.clear;
                mapSkyPreview.preserveAspect = false;
                mapSkyPreview.raycastTarget = false;
                mapSkyPreview.enabled = false;
            }
            else
            {
                mapSkyPreview = skyTr.GetComponent<Image>();
            }

            Transform bgTr = frameTransform.Find("Mountains Preview");
            if (bgTr == null)
            {
                var bgGo = new GameObject("Mountains Preview", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(frameTransform, false);
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = new Vector2(0f, 0.08f);
                bgRt.anchorMax = new Vector2(1f, 0.92f);
                bgRt.anchoredPosition = Vector2.zero;
                bgRt.sizeDelta = Vector2.zero;
                mapBackgroundPreview = bgGo.GetComponent<Image>();
                mapBackgroundPreview.color = Color.clear;
                mapBackgroundPreview.preserveAspect = false;
                mapBackgroundPreview.raycastTarget = false;
                mapBackgroundPreview.enabled = false;
            }
            else
            {
                mapBackgroundPreview = bgTr.GetComponent<Image>();
            }

            Transform groundTr = frameTransform.Find("Ground Preview");
            if (groundTr == null)
            {
                var groundGo = new GameObject("Ground Preview", typeof(RectTransform), typeof(Image));
                groundGo.transform.SetParent(frameTransform, false);
                var groundRt = groundGo.GetComponent<RectTransform>();
                groundRt.anchorMin = new Vector2(0f, 0f);
                groundRt.anchorMax = new Vector2(1f, 0.40f);
                groundRt.anchoredPosition = Vector2.zero;
                groundRt.sizeDelta = Vector2.zero;
                mapGroundPreview = groundGo.GetComponent<Image>();
                mapGroundPreview.color = Color.clear;
                mapGroundPreview.preserveAspect = false;
                mapGroundPreview.raycastTarget = false;
                mapGroundPreview.enabled = false;
            }
            else
            {
                mapGroundPreview = groundTr.GetComponent<Image>();
            }

            // 4. Style Track Selector & Gray Arrows
            Transform selectorTr = step1.Find("Adventure Track Selector");
            if (selectorTr != null)
            {
                Transform prevBtnTr = selectorTr.Find("Previous Adventure Track");
                if (prevBtnTr != null)
                {
                    var rt = prevBtnTr.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(-365f, 30f);
                        rt.sizeDelta = new Vector2(68f, 85f);
                    }
                    ApplyGrayButtonStyle(prevBtnTr.gameObject);
                }

                Transform nextBtnTr = selectorTr.Find("Next Adventure Track");
                if (nextBtnTr != null)
                {
                    var rt = nextBtnTr.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(365f, 30f);
                        rt.sizeDelta = new Vector2(68f, 85f);
                    }
                    ApplyGrayButtonStyle(nextBtnTr.gameObject);
                }

                Transform selectedTrackTr = selectorTr.Find("Selected Adventure Track");
                if (selectedTrackTr != null)
                {
                    selectedTrackTr.gameObject.SetActive(false);
                }
            }

            // 5. Track Title & Best Distance
            Transform titleTr = step1.Find("Track Title");
            if (titleTr == null)
            {
                var titleGo = new GameObject("Track Title", typeof(RectTransform), typeof(Text), typeof(Outline));
                titleGo.transform.SetParent(step1, false);
                var rt = titleGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -180f);
                rt.sizeDelta = new Vector2(500f, 44f);
                trackTitleText = titleGo.GetComponent<Text>();
                trackTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                trackTitleText.fontSize = 32;
                trackTitleText.fontStyle = FontStyle.Bold;
                trackTitleText.color = Color.white;
                trackTitleText.alignment = TextAnchor.MiddleCenter;
                AddBlackOutline(trackTitleText);
            }
            else
            {
                trackTitleText = titleTr.GetComponent<Text>();
                AddBlackOutline(trackTitleText);
            }

            Transform bestTr = step1.Find("Track Best Distance");
            if (bestTr == null)
            {
                var bestGo = new GameObject("Track Best Distance", typeof(RectTransform), typeof(Text), typeof(Outline));
                bestGo.transform.SetParent(step1, false);
                var rt = bestGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -224f);
                rt.sizeDelta = new Vector2(400f, 36f);
                trackBestDistanceText = bestGo.GetComponent<Text>();
                trackBestDistanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                trackBestDistanceText.fontSize = 22;
                trackBestDistanceText.fontStyle = FontStyle.Bold;
                trackBestDistanceText.color = new Color(1f, 0.88f, 0.28f, 1f);
                trackBestDistanceText.alignment = TextAnchor.MiddleCenter;
                AddBlackOutline(trackBestDistanceText);
            }
            else
            {
                trackBestDistanceText = bestTr.GetComponent<Text>();
                AddBlackOutline(trackBestDistanceText);
            }

            // 6. Vehicle Step Arrows
            if (stepPanels.Length > 1 && stepPanels[1] != null)
            {
                Transform step2 = stepPanels[1].transform;
                Transform prevVeh = step2.Find("Previous Vehicle");
                if (prevVeh != null)
                {
                    var rt = prevVeh.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(-710f, -20f);
                        rt.sizeDelta = new Vector2(76f, 95f);
                    }
                    ApplyGrayButtonStyle(prevVeh.gameObject);
                }
                Transform nextVeh = step2.Find("Next Vehicle");
                if (nextVeh != null)
                {
                    var rt = nextVeh.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(710f, -20f);
                        rt.sizeDelta = new Vector2(76f, 95f);
                    }
                    ApplyGrayButtonStyle(nextVeh.gameObject);
                }
            }
        }

        private static void ApplyGrayButtonStyle(GameObject buttonGo)
        {
            if (buttonGo == null) return;
            var img = buttonGo.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.18f, 0.22f, 0.28f, 0.82f);
            }
            var outline = buttonGo.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0.45f, 0.52f, 0.62f, 0.75f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            var btn = buttonGo.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                cb.pressedColor = new Color(0.62f, 0.68f, 0.76f, 1f);
                cb.selectedColor = Color.white;
                btn.colors = cb;
            }
            var text = buttonGo.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 36;
                text.fontStyle = FontStyle.Bold;
                text.color = new Color(0.92f, 0.94f, 0.97f, 1f);
                AddBlackOutline(text);
            }
        }

        private void InitializeTrackSelection()
        {
            mode = GameModeType.Adventure2_5D;
            GameSession.BeginAdventureTrackSelection();
            if (trackSelectionRoot != null) trackSelectionRoot.SetActive(true);
            if (stepPanels != null)
                for (int i = 0; i < stepPanels.Length; i++)
                    if (stepPanels[i] != null && stepPanels[i] != trackSelectionRoot)
                        stepPanels[i].SetActive(false);

            ConfigureTwoColumnGrid(true);
            RebuildTrackCards();
            RefreshTrackSelectionCurrency();
            if (trackSelectionTitleText != null) trackSelectionTitleText.text = "SELECT A TRACK";
            if (trackScroll != null) trackScroll.verticalNormalizedPosition = 1f;
        }

        private void RebuildTrackCards()
        {
            int mapCount = catalog != null ? catalog.CountMaps(GameModeType.Adventure2_5D) : 0;
            bool useTemplate = trackCardTemplate != null && trackContent != null;
            if (useTemplate)
            {
                for (int i = 0; i < generatedTrackCards.Count; i++)
                    if (generatedTrackCards[i] != null)
                    {
                        generatedTrackCards[i].gameObject.SetActive(false);
                        Destroy(generatedTrackCards[i].gameObject);
                    }
                generatedTrackCards.Clear();
                trackCardTemplate.gameObject.SetActive(false);
                for (int i = 0; i < mapCount; i++)
                {
                    TrackSelectionCardUI card = Instantiate(trackCardTemplate, trackContent);
                    card.name = $"Track Card {i + 1}";
                    card.gameObject.SetActive(true);
                    card.Bind(catalog.GetCompatibleMap(GameModeType.Adventure2_5D, i), i, SelectAdventureTrack);
                    generatedTrackCards.Add(card);
                }
            }
            else
            {
                int cardCount = trackCards != null ? trackCards.Length : 0;
                for (int i = 0; i < cardCount; i++)
                {
                    if (trackCards[i] == null) continue;
                    bool visible = i < mapCount;
                    trackCards[i].gameObject.SetActive(visible);
                    if (visible)
                        trackCards[i].Bind(catalog.GetCompatibleMap(GameModeType.Adventure2_5D, i), i,
                            SelectAdventureTrack);
                }
                if (mapCount > cardCount)
                    Debug.LogError($"Track Selection needs {mapCount} cards but only {cardCount} were assigned.", this);
            }

            if (trackSelectionEmptyText != null)
            {
                trackSelectionEmptyText.gameObject.SetActive(mapCount == 0);
                trackSelectionEmptyText.text = mapCount == 0 ? "NO ADVENTURE TRACKS AVAILABLE" : string.Empty;
            }
        }

        private void ConfigureTwoColumnGrid(bool force = false)
        {
            if (trackGrid == null) return;
            trackGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            trackGrid.constraintCount = 2;
            trackGrid.startAxis = GridLayoutGroup.Axis.Horizontal;

            RectTransform viewport = trackScroll != null && trackScroll.viewport != null
                ? trackScroll.viewport : trackGrid.GetComponent<RectTransform>();
            float width = viewport != null ? viewport.rect.width : 0f;
            if (width <= 1f || (!force && Mathf.Abs(width - lastTrackViewportWidth) < .5f)) return;
            lastTrackViewportWidth = width;

            float available = width - trackGrid.padding.left - trackGrid.padding.right - trackGrid.spacing.x;
            float cardWidth = Mathf.Max(1f, available * .5f);
            float cardHeight = cardWidth / Mathf.Max(.5f, trackCardAspectRatio);
            trackGrid.cellSize = new Vector2(cardWidth, cardHeight);
            if (trackContent != null) LayoutRebuilder.MarkLayoutForRebuild(trackContent);
        }

        private void RefreshTrackSelectionCurrency()
        {
            displayedTrackCoins = SaveManager.TotalCoins;
            displayedTrackGems = SaveManager.TotalGems;
            if (coinsText != null) coinsText.text = $"COINS  {displayedTrackCoins:N0}";
            if (gemsText != null) gemsText.text = $"GEMS  {displayedTrackGems:N0}";
        }

        private void EnsureGemCounter()
        {
            if (gemsText != null || coinsText == null || coinsText.transform.parent == null) return;
            RectTransform coinCard = coinsText.transform.parent as RectTransform;
            if (coinCard == null || coinCard.parent == null) return;

            Transform existing = coinCard.parent.Find("Gems Card");
            GameObject gemCard = existing != null
                ? existing.gameObject
                : Object.Instantiate(coinCard.gameObject, coinCard.parent, false);
            gemCard.name = "Gems Card";
            RectTransform gemCardRect = gemCard.GetComponent<RectTransform>();
            // Currency balances form one vertical stack in every menu.
            gemCardRect.anchoredPosition = coinCard.anchoredPosition +
                                           Vector2.down * (coinCard.rect.height + 4f);

            Image[] images = gemCard.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null || images[i].transform == gemCard.transform) continue;
                gemIcon = images[i];
                gemIcon.name = "Gem Icon";
                break;
            }

            Text[] labels = gemCard.GetComponentsInChildren<Text>(true);
            if (labels.Length > 0) gemsText = labels[0];
            HillAdventurerUiCatalog ui = HillAdventurerUiCatalog.Instance;
            if (gemIcon != null)
            {
                gemIcon.sprite = ui != null ? ui.gemCounterIcon : null;
                gemIcon.enabled = gemIcon.sprite != null;
                gemIcon.preserveAspect = true;
                gemIcon.raycastTarget = false;
            }
            displayedTrackGems = int.MinValue;
        }

        public void SelectAdventureTrack(int compatibleMapIndex)
        {
            MapDefinition map = catalog?.GetCompatibleMap(GameModeType.Adventure2_5D, compatibleMapIndex);
            if (map == null) return;
            if (!SaveManager.IsMapUnlocked(map.mapId, map.unlockedByDefault))
            {
                ShowMapPurchase(map);
                return;
            }
            CompleteAdventureTrackSelection(map);
        }

        private void CompleteAdventureTrackSelection(MapDefinition map)
        {
            if (!GameSession.PrepareAdventureGarage(map)) return;
            SceneManager.LoadScene("Garage");
        }

        private void ShowMapPurchase(MapDefinition map)
        {
            EnsurePurchaseConfirmation();
            if (purchaseConfirmation == null || map == null) return;
            bool affordable = map.coinPrice > 0 && SaveManager.TotalCoins >= map.coinPrice;
            purchaseConfirmation.Show("TRACK", map.displayName, map.coinPrice, affordable, () =>
            {
                bool purchased = SaveManager.TryPurchaseMap(map, out CoinUnlockPurchaseResult result);
                if (purchased || result == CoinUnlockPurchaseResult.AlreadyUnlocked)
                {
                    purchaseConfirmation.Hide();
                    CompleteAdventureTrackSelection(map);
                    return;
                }
                purchaseConfirmation.SetFailure(result == CoinUnlockPurchaseResult.NotEnoughCoins
                    ? "NOT ENOUGH COINS" : "UNAVAILABLE", false);
                RefreshTrackSelectionCurrency();
                RebuildTrackCards();
            });
        }

        private void EnsurePurchaseConfirmation()
        {
            if (purchaseConfirmation != null) return;
            Canvas canvas = trackSelectionRoot != null
                ? trackSelectionRoot.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform.root;
            purchaseConfirmation = CoinPurchaseConfirmationUI.GetOrCreate(parent);
        }

        public void Adventure()
        {
            GameSession.BeginAdventureTrackSelection();
            if (trackSelectionLayout)
            {
                RebuildTrackCards();
                if (trackScroll != null) trackScroll.verticalNormalizedPosition = 1f;
                return;
            }
            SceneManager.LoadScene("PreStart");
        }

        public void Garage()
        {
            GameSession.RequestDirectGarage(GarageSection.Vehicle);
            SceneManager.LoadScene("Garage");
        }

        public void Shop()
        {
            GameSession.RequestDirectGarage(GarageSection.Store);
            SceneManager.LoadScene("Garage");
        }

        public void Settings()
        {
            GameSession.RequestDirectGarage(GarageSection.Settings);
            SceneManager.LoadScene("Garage");
        }

        public void SelectAdventure2_5D() => SelectMode(GameModeType.Adventure2_5D);
        public void SelectAdventure3D() => SelectMode(GameModeType.Adventure3D);
        public void SelectTimeTrial3D() => SelectMode(GameModeType.TimeTrial3D);
        public void SelectRacing3D() => SceneManager.LoadScene("RacingSelect");

        public void SelectClimb() => SelectAdventure2_5D();
        public void SelectRoad() => SelectAdventure3D();

        private void SelectMode(GameModeType selected)
        {
            mode = selected;
            GameSession.SelectedGameMode = mode;
            FindSavedMap();
            if (CurrentMap == null) mapIndex = 0;
            Refresh();
        }

        public void ContinueStep()
        {
            if (trackSelectionLayout) return;
            if (step == Step.Vehicle) { StartGame(); return; }
            ShowStep(Step.Vehicle);
        }

        public void BackStep()
        {
            if (trackSelectionLayout)
            {
                GameSession.ClearGarageContext();
                SceneManager.LoadScene("PreStart");
                return;
            }
            if (step == Step.Mode) { SceneManager.LoadScene("PreStart"); return; }
            ShowStep(Step.Mode);
        }

        private void RefreshMenuBackground()
        {
            string storedId = Application.isPlaying
                ? SaveManager.SelectedMenuBackgroundId : SaveManager.DefaultMenuBackgroundId;
            bool neon = storedId == "neon";
            string selectedId = neon || storedId == SaveManager.DefaultMenuBackgroundId
                ? storedId : SaveManager.DefaultMenuBackgroundId;
            Sprite selectedSprite = selectedId == "neon" ? neonMenuBackground : defaultMenuBackground;
            if (selectedSprite == null)
            {
                selectedId = SaveManager.DefaultMenuBackgroundId;
                selectedSprite = defaultMenuBackground ?? neonMenuBackground;
            }
            if (Application.isPlaying && selectedId != storedId)
                SaveManager.SelectedMenuBackgroundId = selectedId;
            if (menuBackground == null) return;
            menuBackground.sprite = selectedSprite;
            menuBackground.enabled = selectedSprite != null;
            AspectRatioFitter fitter = menuBackground.GetComponent<AspectRatioFitter>();
            if (fitter != null && selectedSprite != null && selectedSprite.rect.height > 0f)
                fitter.aspectRatio = selectedSprite.rect.width / selectedSprite.rect.height;
        }

        public void Back() => BackStep();
        public void PreviousMap() { CycleMap(-1); }
        public void NextMap() { CycleMap(1); }
        public void PreviousVehicle() { CycleVehicle(-1); }
        public void NextVehicle() { CycleVehicle(1); }

        public void ToggleTransmission()
        {
            GameSession.SelectedTransmission = TransmissionMode.Auto;
            Refresh();
        }

        public void OpenTuning()
        {
            GameSession.RequestDirectGarage(GarageSection.Tuning);
            SceneManager.LoadScene("Garage");
        }

        public void CloseTuning()
        {
            if (tuningPanel != null) tuningPanel.SetActive(false);
            ShowStep(Step.Vehicle);
        }

        public void UpgradeEngine() => Purchase(VehicleUpgradeType.Engine);
        public void UpgradeAcceleration() => Purchase(VehicleUpgradeType.Acceleration);
        public void UpgradeSuspension() => Purchase(VehicleUpgradeType.Suspension);
        public void UpgradeGrip() => Purchase(VehicleUpgradeType.Grip);
        public void UpgradeThird() { if (CurrentVehicle != null) Purchase(CurrentVehicle.GetTuneTypeForSlot(2)); }
        public void UpgradeFourth() { if (CurrentVehicle != null) Purchase(CurrentVehicle.GetTuneTypeForSlot(3)); }
        public void UpgradeFuel() => Purchase(VehicleUpgradeType.Fuel);

        public void StartGame()
        {
            if (trackSelectionLayout)
            {
                SelectAdventureTrack(mapIndex);
                return;
            }
            MapDefinition map = CurrentMap;
            VehicleDefinition vehicle = CurrentVehicle;
            if (map == null || vehicle == null || !SaveManager.IsVehicleUnlocked(vehicle.vehicleId,
                    vehicle.unlockedByDefault)) return;
            GameSession.SelectedGameMode = mode;
            GameSession.SelectedMapId = map.mapId;
            if (mode == GameModeType.TimeTrial3D) GameSession.SelectedCircuitId = map.mapId;
            GameSession.SelectedVehicleId = vehicle.vehicleId;
            SceneManager.LoadScene(mode == GameModeType.Adventure2_5D ? "Adventure2_5D" :
                mode == GameModeType.Adventure3D ? "Adventure3D" : "TimeTrial3D");
        }

        private void ShowStep(Step target)
        {
            step = target;
            // UnityEngine.Object uses a custom null state; null-conditional access can still throw an
            // UnassignedReferenceException for an intentionally empty serialized field.
            if (tuningPanel != null) tuningPanel.SetActive(false);
            SetStepPanelsVisible(true);
            Refresh();
        }

        private void SetStepPanelsVisible(bool showCurrent)
        {
            if (stepPanels == null) return;
            for (int i = 0; i < stepPanels.Length; i++)
                if (stepPanels[i] != null) stepPanels[i].SetActive(showCurrent && i == (int)step);
        }

        private void CycleMap(int direction)
        {
            int count = catalog != null ? catalog.CountMaps(mode) : 0;
            if (count > 0) mapIndex = PositiveModulo(mapIndex + direction, count);
            if (CurrentMap != null) GameSession.SelectedMapId = CurrentMap.mapId;
            Refresh();
        }

        private void CycleVehicle(int direction)
        {
            int count = catalog != null && catalog.vehicles != null ? catalog.vehicles.Length : 0;
            if (count <= 0) return;

            int stepDirection = direction < 0 ? -1 : 1;
            int candidateIndex = vehicleIndex;
            for (int inspected = 0; inspected < count; inspected++)
            {
                candidateIndex = PositiveModulo(candidateIndex + stepDirection, count);
                VehicleDefinition candidate = catalog.vehicles[candidateIndex];
                if (candidate == null || !SaveManager.IsVehicleUnlocked(candidate.vehicleId,
                        candidate.unlockedByDefault)) continue;
                vehicleIndex = candidateIndex;
                GameSession.SelectedVehicleId = candidate.vehicleId;
                Refresh();
                return;
            }
            Refresh();
        }

        private void Purchase(VehicleUpgradeType type)
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null || catalog?.upgradeEconomy == null) return;
            SaveManager.TryIncreaseEquippedTune(vehicle, type, catalog.upgradeEconomy, out _, out _);
            Refresh();
        }

        private static readonly System.Collections.Generic.Dictionary<Texture2D, Sprite> _textureSpriteCache = new System.Collections.Generic.Dictionary<Texture2D, Sprite>();

        private Sprite GetOrCreateSpriteFromTexture(Texture2D texture)
        {
            if (texture == null) return null;
            if (_textureSpriteCache.TryGetValue(texture, out var cached) && cached != null)
                return cached;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _textureSpriteCache[texture] = sprite;
            return sprite;
        }

        private void Refresh()
        {
            ResolveMapPreviewImages();

            MapDefinition map = CurrentMap;
            VehicleDefinition vehicle = CurrentVehicle;
            if (coinsText != null) coinsText.text = $"COINS  {SaveManager.TotalCoins:N0}";
            if (progressText != null) progressText.text = ProgressLabel();
            if (modeText != null) modeText.text = GameModeNames.DisplayName(mode);
            if (adventureTrackSelector != null)
                adventureTrackSelector.SetActive(true);

            string bestDist = map != null
                ? (mode == GameModeType.TimeTrial3D
                    ? $"BEST  {FormatTime(SaveManager.GetBestTime(map.mapId))}"
                    : $"BEST: {SaveManager.GetBestDistance(mode, map.mapId):N0}m")
                : "BEST: 0m";

            if (trackTitleText != null)
            {
                trackTitleText.text = map != null ? map.displayName.ToUpperInvariant() : "NO TRACK";
                trackTitleText.gameObject.SetActive(true);
            }
            if (trackBestDistanceText != null)
            {
                trackBestDistanceText.text = bestDist;
                trackBestDistanceText.gameObject.SetActive(true);
            }

            if (mapText != null)
            {
                if (trackTitleText != null)
                {
                    mapText.gameObject.SetActive(false);
                }
                else
                {
                    mapText.text = map == null ? "NO COMPATIBLE MAP" :
                        mode == GameModeType.Adventure2_5D
                            ? $"TRACK\n{map.displayName.ToUpperInvariant()}\n{bestDist}"
                            : $"{map.displayName.ToUpperInvariant()}\nDIFFICULTY  NORMAL\n{bestDist}";
                }
            }

            ConfigureTrackTextOutlines();

            MapProfile mapProfile = map as MapProfile;
            Sprite skySprite = null;
            Sprite bgSprite = null;
            Sprite groundSprite = null;

            if (mapProfile != null)
            {
                if (mapProfile.environment != null)
                {
                    skySprite = mapProfile.environment.sky;
                    bgSprite = mapProfile.environment.mountains;
                }

                if (mapProfile.terrainEdgeMaterial != null && mapProfile.terrainEdgeMaterial.mainTexture is Texture2D edgeTex)
                {
                    groundSprite = GetOrCreateSpriteFromTexture(edgeTex);
                }
                else if (mapProfile.terrainMaterial != null && mapProfile.terrainMaterial.mainTexture is Texture2D terrainTex)
                {
                    groundSprite = GetOrCreateSpriteFromTexture(terrainTex);
                }
                else if (mapProfile.liquidChallenges != null && mapProfile.liquidChallenges.liquidSprite != null)
                {
                    groundSprite = mapProfile.liquidChallenges.liquidSprite;
                }
            }
            else if (map is AnimeHillRacing.Road.RoadMapProfile roadMap)
            {
                if (roadMap.roadMaterial != null && roadMap.roadMaterial.mainTexture is Texture2D roadTex)
                    groundSprite = GetOrCreateSpriteFromTexture(roadTex);
            }

            if (mapSkyPreview != null)
            {
                mapSkyPreview.sprite = skySprite;
                mapSkyPreview.enabled = skySprite != null;
                mapSkyPreview.color = skySprite != null ? Color.white : Color.clear;
            }
            if (mapBackgroundPreview != null)
            {
                mapBackgroundPreview.sprite = bgSprite;
                mapBackgroundPreview.enabled = bgSprite != null;
                mapBackgroundPreview.color = bgSprite != null ? Color.white : Color.clear;
            }
            if (mapGroundPreview != null)
            {
                mapGroundPreview.sprite = groundSprite;
                mapGroundPreview.enabled = groundSprite != null;
                mapGroundPreview.color = groundSprite != null ? Color.white : Color.clear;
            }

            if (vehicleText != null) vehicleText.text = vehicle != null ? vehicle.displayName.ToUpperInvariant() : "NO VEHICLE";
            if (vehiclePreviewImage != null)
            {
                vehiclePreviewImage.sprite = vehicle != null ? vehicle.previewSprite : null;
                vehiclePreviewImage.enabled = vehiclePreviewRenderer == null && vehiclePreviewImage.sprite != null;
                vehiclePreviewImage.preserveAspect = true;
            }
            if (vehiclePreviewFallback != null)
                vehiclePreviewFallback.SetActive(vehiclePreviewRenderer == null && vehicle != null &&
                                                 vehicle.previewSprite == null);
            if (vehiclePreviewRenderer != null && vehicle != null)
            {
                VehicleDefinition.CustomizationOption skin = vehicle.FindSkin(SaveManager.GetSelectedSkinId(vehicle));
                VehicleDefinition.CustomizationOption wheel = vehicle.FindWheel(SaveManager.GetSelectedWheelId(vehicle));
                string fallbackDriver = catalog?.drivers != null && catalog.drivers.Length > 0
                    ? catalog.drivers[0].driverId : string.Empty;
                DriverDefinition driver = catalog?.FindDriver(SaveManager.GetSelectedDriverId(fallbackDriver));
                vehiclePreviewRenderer.Apply(vehicle, skin, wheel, driver);
            }
            if (transmissionText != null) transmissionText.text = "AUTOMATIC";
            RefreshModeCards();
            if (vehicle != null)
            {
                VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
                ResolvedVehicleStats stats = vehicle.Resolve(state);
                string tuneLines = $"SPEED   {stats.topSpeedMps * 3.6f:0} KM/H\n" +
                                   $"0-100 KM/H   {stats.zeroToHundredTargetSeconds:0.0} SEC\n" +
                                   $"SUSPENSION   {stats.suspensionTravel:0.00} M\n" +
                                   $"GRIP   {stats.gripRating} / {VehicleDefinition.GlobalGripMaximum}\n" +
                                   $"FUEL   {stats.totalRunFuelDuration:0} SEC";
                if (vehicleStatsText != null)
                    vehicleStatsText.text = tuneLines;
                if (tuningSummaryText != null)
                    tuningSummaryText.text = $"MODE   {GameModeNames.DisplayName(mode)}\n" +
                                             $"MAP   {(map != null ? map.displayName.ToUpperInvariant() : "-")}\n" +
                                             $"VEHICLE   {vehicle.displayName.ToUpperInvariant()}\n\n" +
                                             tuneLines;
            }
        }

        private void ConfigureTrackTextOutlines()
        {
            if (outlinesConfigured) return;
            outlinesConfigured = true;
            AddBlackOutline(trackTitleText);
            AddBlackOutline(trackBestDistanceText);
            AddBlackOutline(mapText);
        }

        private static void AddBlackOutline(Text target)
        {
            if (target == null) return;
            if (target.GetComponent<Outline>() == null)
            {
                Outline outline = target.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private void ResolveMapPreviewImages()
        {
            if (mapSkyPreview != null && mapBackgroundPreview != null && mapGroundPreview != null && trackTitleText != null && trackBestDistanceText != null) return;
            Transform searchRoot = stepPanels != null && stepPanels.Length > 0 && stepPanels[0] != null ? stepPanels[0].transform.root : transform.root;
            Image[] images = searchRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (mapSkyPreview == null && images[i].name.Contains("Sky Preview")) mapSkyPreview = images[i];
                if (mapBackgroundPreview == null && (images[i].name.Contains("Mountains Preview") || images[i].name.Contains("Background Preview")))
                    mapBackgroundPreview = images[i];
                if (mapGroundPreview == null && images[i].name.Contains("Ground Preview"))
                    mapGroundPreview = images[i];
            }
            Text[] texts = searchRoot.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (trackTitleText == null && texts[i].name.Contains("Track Title")) trackTitleText = texts[i];
                if (trackBestDistanceText == null && texts[i].name.Contains("Track Best Distance")) trackBestDistanceText = texts[i];
            }
        }

        private void RefreshModeCards()
        {
            if (modeCards == null || modeCards.Length == 0) return;
            if (comingSoonPanel != null || (stepPanels != null && stepPanels.Length > 0 && stepPanels[0] != null && stepPanels[0].transform.Find("Coming Soon Panel") != null))
            {
                if (modeCards.Length > 0 && modeCards[0] != null)
                {
                    modeCards[0].color = new Color(0.08f, 0.22f, 0.42f, 0.95f);
                    Outline outline = modeCards[0].GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = new Color(0.30f, 0.85f, 1f, 0.90f);
                        outline.effectDistance = new Vector2(1.5f, -1.5f);
                    }
                }
                return;
            }

            int count = modeCards.Length;
            for (int i = 0; i < count; i++)
            {
                bool selected = i == (int)mode;
                if (modeCards[i] != null)
                {
                    modeCards[i].color = selected ? new Color(0.18f, 0.48f, 0.76f, 1f) : new Color(0.055f, 0.10f, 0.18f, 0.98f);
                    Outline outline = modeCards[i].GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = selected ? new Color(0.38f, 0.88f, 1f, 1f) : new Color(0.18f, 0.30f, 0.42f, 0.75f);
                        outline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(1.5f, -1.5f);
                    }
                }
                if (modeCardRects != null && i < modeCardRects.Length && modeCardRects[i] != null)
                    modeCardRects[i].localScale = selected ? Vector3.one * 1.025f : Vector3.one;
                if (modeCardStatusTexts != null && i < modeCardStatusTexts.Length && modeCardStatusTexts[i] != null)
                    modeCardStatusTexts[i].text = selected ? "✓  SELECTED" : "SELECT";
            }
        }

        private void RefreshTuning()
        {
            VehicleDefinition vehicle = CurrentVehicle;
            if (vehicle == null) return;
            VehicleUpgradeState state = SaveManager.LoadUpgrades(vehicle.vehicleId, vehicle);
            VehicleUpgradeState purchasedState = SaveManager.LoadPurchasedUpgrades(vehicle.vehicleId, vehicle);
            int visibleSlots = vehicle.GetTuneSlotCount();
            int rowCount = tuningNameTexts != null ? tuningNameTexts.Length : 0;
            for (int i = 0; i < rowCount; i++)
            {
                bool visible = i < visibleSlots;
                if (tuningNameTexts != null && i < tuningNameTexts.Length && tuningNameTexts[i] != null)
                    tuningNameTexts[i].transform.parent.gameObject.SetActive(visible);
                if (!visible) continue;
                VehicleUpgradeType type = vehicle.GetTuneTypeForSlot(i);
                int current = state.GetTune(type);
                int purchased = purchasedState.GetTune(type);
                int maximum = vehicle.GetMaxTunes(type);
                bool atMax = current >= maximum;
                bool purchasedMax = purchased >= maximum;
                if (tuningNameTexts != null && i < tuningNameTexts.Length && tuningNameTexts[i] != null)
                    tuningNameTexts[i].text = vehicle.GetTuneDisplayName(type);
                if (tuningLevelTexts != null && i < tuningLevelTexts.Length && tuningLevelTexts[i] != null)
                    tuningLevelTexts[i].text = $"EQUIPPED {current} / {maximum}   BOUGHT {purchased}";
                if (tuningValueTexts != null && i < tuningValueTexts.Length && tuningValueTexts[i] != null)
                    tuningValueTexts[i].text = TuneComparison(vehicle, state, type, atMax);
                int cost = purchasedMax || catalog?.upgradeEconomy == null ? -1 :
                    catalog.upgradeEconomy.GetCostForNextTune(vehicle, type, purchased);
                if (tuningCostTexts != null && i < tuningCostTexts.Length && tuningCostTexts[i] != null)
                    tuningCostTexts[i].text = current < purchased ? "↑  EQUIP" : purchasedMax
                        ? "↑  MAX" : $"↑  {cost:N0} COINS";
                if (tuningButtons != null && i < tuningButtons.Length && tuningButtons[i] != null)
                    tuningButtons[i].interactable = current < maximum && (current < purchased || cost >= 0);
            }
            if (tuningCoinsText != null) tuningCoinsText.text = $"COINS  {SaveManager.TotalCoins:N0}";
        }

        private static string TuneComparison(VehicleDefinition vehicle, VehicleUpgradeState current,
            VehicleUpgradeType type, bool atMax)
        {
            ResolvedVehicleStats currentStats = vehicle.Resolve(current);
            var next = new VehicleUpgradeState();
            next.CopyFrom(current);
            next.SetTune(type, current.GetTune(type) + (atMax ? 0 : 1));
            next.ClampTo(vehicle);
            ResolvedVehicleStats nextStats = vehicle.Resolve(next);
            string arrow = atMax ? "" : "  →  ";
            switch (type)
            {
                case VehicleUpgradeType.Engine:
                    return atMax ? $"{currentStats.topSpeedMps * 3.6f:0} KM/H   MAX"
                        : $"MAX SPEED  {currentStats.topSpeedMps * 3.6f:0}{arrow}{nextStats.topSpeedMps * 3.6f:0} KM/H\n" +
                          $"POWER  {currentStats.peakPowerKw:0.0}{arrow}{nextStats.peakPowerKw:0.0} KW";
                case VehicleUpgradeType.Acceleration:
                    return atMax ? $"0-100  {currentStats.zeroToHundredTargetSeconds:0.00} SEC   MAX"
                        : $"0-100  {currentStats.zeroToHundredTargetSeconds:0.00}{arrow}{nextStats.zeroToHundredTargetSeconds:0.00} SEC";
                case VehicleUpgradeType.Suspension:
                    return atMax ? $"TRAVEL  {currentStats.suspensionTravel:0.000} M   MAX"
                        : $"TRAVEL  {currentStats.suspensionTravel:0.000}{arrow}{nextStats.suspensionTravel:0.000} M\n" +
                          $"SPRING  {currentStats.spring:0}{arrow}{nextStats.spring:0}   DAMPER  {currentStats.damping:0}{arrow}{nextStats.damping:0}";
                case VehicleUpgradeType.Grip:
                    return atMax ? $"GRIP  {currentStats.gripRating}/{VehicleDefinition.GlobalGripMaximum}   MAX"
                        : $"GRIP  {currentStats.gripRating}/{VehicleDefinition.GlobalGripMaximum}{arrow}{nextStats.gripRating}/{VehicleDefinition.GlobalGripMaximum}";
                case VehicleUpgradeType.AirControl:
                    return atMax ? $"AIR TORQUE  {currentStats.airControlTorque:0} NM   MAX"
                        : $"AIR TORQUE  {currentStats.airControlTorque:0}{arrow}{nextStats.airControlTorque:0} NM";
                case VehicleUpgradeType.Downforce:
                    return atMax ? $"AIR DOWNFORCE  {currentStats.airborneDownforceAcceleration:0.0} M/S²   MAX"
                        : $"AIR DOWNFORCE  {currentStats.airborneDownforceAcceleration:0.0}{arrow}{nextStats.airborneDownforceAcceleration:0.0} M/S²";
                case VehicleUpgradeType.Turbo:
                    if (vehicle.archetype == VehicleArchetype.AerialSatelite)
                    {
                        float currentThrust = Mathf.Lerp(50f, 100f,
                            VehicleDefinition.UpgradeProgress(current.turboTune, vehicle.turboMaxTunes));
                        float nextThrust = Mathf.Lerp(50f, 100f,
                            VehicleDefinition.UpgradeProgress(next.turboTune, vehicle.turboMaxTunes));
                        return atMax ? $"THRUST  {currentThrust:0}%   MAX"
                            : $"THRUST  {currentThrust:0}%{arrow}{nextThrust:0}%";
                    }
                    if (vehicle.archetype == VehicleArchetype.Buggy)
                        return atMax ? $"MAX SLOPE  {currentStats.maximumClimbAngle:0}°   MAX"
                            : $"MAX SLOPE  {currentStats.maximumClimbAngle:0}°{arrow}{nextStats.maximumClimbAngle:0}°";
                    return atMax ? $"0-100  {currentStats.zeroToHundredTargetSeconds:0.00} SEC   MAX"
                        : $"0-100  {currentStats.zeroToHundredTargetSeconds:0.00}{arrow}{nextStats.zeroToHundredTargetSeconds:0.00} SEC";
                default:
                    return atMax ? $"TOTAL RUN  {currentStats.totalRunFuelDuration:0.0} SEC   MAX"
                        : $"TOTAL RUN  {currentStats.totalRunFuelDuration:0.0}{arrow}{nextStats.totalRunFuelDuration:0.0} SEC";
            }
        }

        private string ProgressLabel()
        {
            string[] labels = { "1 MODE", "2 VEHICLE" };
            for (int i = 0; i < labels.Length; i++)
                if (i == (int)step) labels[i] = $"<color=#64D8FF>[{labels[i]}]</color>";
            return string.Join("   >   ", labels);
        }

        private void FindSavedVehicle()
        {
            vehicleIndex = 0;
            if (catalog == null || catalog.vehicles == null) return;
            int firstUnlocked = -1;
            for (int i = 0; i < catalog.vehicles.Length; i++)
            {
                VehicleDefinition candidate = catalog.vehicles[i];
                if (candidate == null || !SaveManager.IsVehicleUnlocked(candidate.vehicleId,
                        candidate.unlockedByDefault)) continue;
                if (firstUnlocked < 0) firstUnlocked = i;
                if (candidate.vehicleId == GameSession.SelectedVehicleId)
                {
                    vehicleIndex = i;
                    return;
                }
            }
            if (firstUnlocked >= 0)
            {
                vehicleIndex = firstUnlocked;
                GameSession.SelectedVehicleId = catalog.vehicles[firstUnlocked].vehicleId;
            }
        }

        private void FindSavedMap()
        {
            mapIndex = 0;
            int count = catalog != null ? catalog.CountMaps(mode) : 0;
            for (int i = 0; i < count; i++)
                if (catalog.GetCompatibleMap(mode, i)?.mapId == GameSession.SelectedMapId) mapIndex = i;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static string FormatTime(float seconds) => seconds <= 0f ? "--:--.---" :
            $"{Mathf.FloorToInt(seconds / 60f):00}:{seconds % 60f:00.000}";

        public void AssignTrackSelection(GameCatalog gameCatalog, ScrollRect scroll, GridLayoutGroup grid,
            TrackSelectionCardUI[] cards, Text coins = null, Text title = null, Text emptyState = null,
            GameObject selectionRoot = null, float cardAspectRatio = 2.30f)
        {
            catalog = gameCatalog;
            trackSelectionLayout = true;
            trackSelectionRoot = selectionRoot;
            trackScroll = scroll;
            trackGrid = grid;
            trackContent = grid != null ? grid.GetComponent<RectTransform>() : scroll?.content;
            trackCards = cards;
            trackCardTemplate = null;
            coinsText = coins;
            EnsureGemCounter();
            trackSelectionTitleText = title;
            trackSelectionEmptyText = emptyState;
            trackCardAspectRatio = Mathf.Max(.5f, cardAspectRatio);
            ConfigureTwoColumnGrid(true);
        }

        public void AssignTrackSelectionTemplate(GameCatalog gameCatalog, ScrollRect scroll,
            GridLayoutGroup grid, RectTransform content, TrackSelectionCardUI cardTemplate,
            Text coins = null, Text title = null, Text emptyState = null,
            GameObject selectionRoot = null, float cardAspectRatio = 2.30f)
        {
            catalog = gameCatalog;
            trackSelectionLayout = true;
            trackSelectionRoot = selectionRoot;
            trackScroll = scroll;
            trackGrid = grid;
            trackContent = content != null ? content : grid != null ? grid.GetComponent<RectTransform>() :
                scroll?.content;
            trackCards = null;
            trackCardTemplate = cardTemplate;
            coinsText = coins;
            EnsureGemCounter();
            trackSelectionTitleText = title;
            trackSelectionEmptyText = emptyState;
            trackCardAspectRatio = Mathf.Max(.5f, cardAspectRatio);
            ConfigureTwoColumnGrid(true);
        }

        public void AssignSharedMenuBackground(Image target, Sprite defaultBackground,
            Sprite neonBackground)
        {
            menuBackground = target;
            defaultMenuBackground = defaultBackground;
            neonMenuBackground = neonBackground;
            RefreshMenuBackground();
        }

        public void Assign(GameCatalog gameCatalog, Text coins, Text progress, GameObject[] panels,
            Image[] cards, RectTransform[] cardRects, Text[] cardStatuses, Text modeLabel, Text mapLabel,
            Text vehicleLabel, Image vehiclePreview, GameObject previewFallback, Text stats, Text summary,
            Text transmission, GameObject tuningRoot,
            Text[] tuneNames, Text[] tuneLabels, Text[] tuneValues, Text[] tuneCosts, Button[] tuneButtons, Text tuningCoins,
            GaragePreviewRenderer composedPreview = null, GameObject trackSelector = null)
        {
            catalog = gameCatalog; coinsText = coins; EnsureGemCounter(); progressText = progress; stepPanels = panels;
            modeCards = cards; modeCardRects = cardRects; modeCardStatusTexts = cardStatuses;
            modeText = modeLabel; mapText = mapLabel; vehicleText = vehicleLabel; vehiclePreviewImage = vehiclePreview;
            vehiclePreviewFallback = previewFallback;
            vehicleStatsText = stats;
            tuningSummaryText = summary; transmissionText = transmission; tuningPanel = tuningRoot;
            tuningNameTexts = tuneNames; tuningLevelTexts = tuneLabels; tuningValueTexts = tuneValues; tuningCostTexts = tuneCosts;
            tuningButtons = tuneButtons; tuningCoinsText = tuningCoins;
            vehiclePreviewRenderer = composedPreview;
            adventureTrackSelector = trackSelector;
        }
    }
}
