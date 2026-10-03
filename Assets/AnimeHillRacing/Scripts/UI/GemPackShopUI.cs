using System;
using System.Collections.Generic;
using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>Runtime Google Play gem store, displayed as four products per row.</summary>
    public sealed class GemPackShopUI : MonoBehaviour
    {
        private readonly List<Sprite> runtimeSprites = new List<Sprite>();
        private Button[] buyButtons;
        private Text[] priceLabels;
        private Text balanceText;
        private Text statusText;
        private IStorePurchaseProvider subscribedProvider;
        private bool built;
        private bool purchasing;
        private int pendingPackIndex = -1;
        private string feedbackMessage = string.Empty;

        public static GemPackShopUI Attach(GameObject storePanel)
        {
            if (storePanel == null || !Application.isPlaying) return null;
            Transform content = FindDescendant(storePanel.transform, "Shop Content");
            if (content == null) return null;

            Transform existing = content.Find("Gems Shop Runtime");
            GameObject root = existing != null ? existing.gameObject :
                new GameObject("Gems Shop Runtime", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            if (existing == null)
            {
                root.transform.SetParent(content, false);
                Transform blueprints = content.Find("Blue Prints Accesories");
                if (blueprints != null) root.transform.SetSiblingIndex(blueprints.GetSiblingIndex());
            }

            GemPackShopUI presenter = root.GetComponent<GemPackShopUI>();
            if (presenter == null) presenter = root.AddComponent<GemPackShopUI>();
            presenter.BuildOnce();
            presenter.Subscribe();
            presenter.Refresh();
            return presenter;
        }

        private void OnEnable()
        {
            BuildOnce();
            Subscribe();
            Refresh();
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy()
        {
            Unsubscribe();
            if (buyButtons != null)
                for (int i = 0; i < buyButtons.Length; i++)
                    if (buyButtons[i] != null) buyButtons[i].onClick.RemoveAllListeners();
            for (int i = 0; i < runtimeSprites.Count; i++)
                if (runtimeSprites[i] != null) Destroy(runtimeSprites[i]);
            runtimeSprites.Clear();
        }

        public void Refresh()
        {
            if (this == null || !built || buyButtons == null || priceLabels == null) return;
            Subscribe();
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;
            IReadOnlyList<GemPackProduct> packs = GemPackCatalog.Products;
            balanceText.text = $"YOUR GEMS  {SaveManager.TotalGems:N0}";

            for (int i = 0; i < packs.Count && i < buyButtons.Length; i++)
            {
                GemPackProduct pack = packs[i];
                bool available = provider != null && provider.IsProductAvailable(pack.ProductId);
                if (provider != null && provider.TryGetLocalizedPrice(pack.ProductId,
                        out string localizedPrice) && !string.IsNullOrWhiteSpace(localizedPrice))
                    priceLabels[i].text = localizedPrice;
                else if (Application.isEditor)
                    priceLabels[i].text = pack.ReferenceUsdPrice;
                else if (provider != null && (provider.State == ExternalStoreState.Initializing ||
                                               provider.State == ExternalStoreState.NotInitialized))
                    priceLabels[i].text = "LOADING...";
                else
                    priceLabels[i].text = "UNAVAILABLE";
                buyButtons[i].interactable = !purchasing && available;
            }

            if (!string.IsNullOrWhiteSpace(feedbackMessage))
                statusText.text = feedbackMessage;
            else if (purchasing)
                statusText.text = "CONTACTING GOOGLE PLAY...";
            else if (provider != null && provider.IsReady)
                statusText.text = "PRICES AND PAYMENTS BY GOOGLE PLAY";
            else if (Application.isEditor)
                statusText.text = "REFERENCE USD PRICES - GOOGLE PLAY LOCALIZES ON ANDROID";
            else
                statusText.text = "CONNECTING TO GOOGLE PLAY...";
        }

        private void Buy(int packIndex)
        {
            IReadOnlyList<GemPackProduct> packs = GemPackCatalog.Products;
            if (purchasing || packIndex < 0 || packIndex >= packs.Count) return;
            GemPackProduct pack = packs[packIndex];
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;
            if (provider == null || !provider.IsProductAvailable(pack.ProductId))
            {
                feedbackMessage = "PRODUCT UNAVAILABLE IN GOOGLE PLAY";
                Refresh();
                return;
            }

            purchasing = true;
            pendingPackIndex = packIndex;
            feedbackMessage = string.Empty;
            Refresh();
            provider.Purchase(pack.ProductId, CompletePurchase);
        }

        private void CompletePurchase(StorePurchaseResult result)
        {
            purchasing = false;
            int completedIndex = pendingPackIndex;
            pendingPackIndex = -1;
            if (this == null) return;

            if (result.Succeeded && completedIndex >= 0 && completedIndex < GemPackCatalog.Products.Count)
                feedbackMessage = $"+{GemPackCatalog.Products[completedIndex].GemAmount:N0} GEMS ADDED";
            else if (result.Status == StorePurchaseStatus.Cancelled)
                feedbackMessage = "PURCHASE CANCELLED";
            else if (result.Status == StorePurchaseStatus.Pending)
                feedbackMessage = "PURCHASE PENDING APPROVAL";
            else
                feedbackMessage = string.IsNullOrWhiteSpace(result.Error)
                    ? "PURCHASE NOT COMPLETED" : result.Error.ToUpperInvariant();
            Refresh();
        }

        private void Subscribe()
        {
            // Attach also builds inactive panels. They must not retain static
            // events if the scene unloads before their first OnEnable.
            if (!isActiveAndEnabled) return;
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;
            if (!ReferenceEquals(subscribedProvider, provider))
            {
                if (subscribedProvider != null) subscribedProvider.StateChanged -= Refresh;
                subscribedProvider = provider;
                if (subscribedProvider != null) subscribedProvider.StateChanged += Refresh;
            }
            SaveManager.CurrencyChanged -= Refresh;
            SaveManager.CurrencyChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (subscribedProvider != null) subscribedProvider.StateChanged -= Refresh;
            subscribedProvider = null;
            SaveManager.CurrencyChanged -= Refresh;
        }

        private void BuildOnce()
        {
            if (built) return;
            IReadOnlyList<GemPackProduct> packs = GemPackCatalog.Products;
            Image background = GetComponent<Image>();
            background.color = new Color(.025f, .075f, .14f, .98f);
            background.raycastTarget = true;
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.preferredHeight = 680f;
            layout.flexibleHeight = 0f;

            Text title = CreateText("Gems Title", transform, "GEMS", 32, TextAnchor.MiddleLeft,
                new Vector2(.035f, .89f), new Vector2(.48f, .985f));
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(.25f, .9f, 1f);
            balanceText = CreateText("Gems Balance", transform, "YOUR GEMS  0", 21,
                TextAnchor.MiddleRight, new Vector2(.50f, .90f), new Vector2(.965f, .985f));
            statusText = CreateText("Gems Store Status", transform, "CONNECTING TO GOOGLE PLAY...", 13,
                TextAnchor.MiddleCenter, new Vector2(.16f, .855f), new Vector2(.84f, .91f));
            statusText.color = new Color(.65f, .82f, .94f);

            buyButtons = new Button[packs.Count];
            priceLabels = new Text[packs.Count];
            var atlases = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
            for (int i = 0; i < packs.Count; i++)
            {
                int index = i;
                int column = i % GemPackCatalog.Columns;
                int row = i / GemPackCatalog.Columns;
                float xMin = .025f + column * .24375f;
                float xMax = xMin + .21875f;
                float yMin = row == 0 ? .465f : .035f;
                float yMax = row == 0 ? .85f : .42f;

                GameObject card = new GameObject("Gem Pack " + packs[i].GemAmount,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.SetParent(transform, false);
                SetRect(cardRect, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                Image cardImage = card.GetComponent<Image>();
                cardImage.color = new Color(.045f, .16f, .27f, .98f);
                buyButtons[i] = card.GetComponent<Button>();
                buyButtons[i].targetGraphic = cardImage;
                buyButtons[i].onClick.AddListener(() => Buy(index));

                Image artwork = CreateImage("Artwork", card.transform,
                    new Vector2(.015f, .20f), new Vector2(.985f, .995f));
                artwork.sprite = CreatePackSprite(packs[i], atlases);
                artwork.enabled = artwork.sprite != null;
                artwork.preserveAspect = true;
                artwork.raycastTarget = false;

                Image priceBar = CreateImage("Price Bar", card.transform,
                    new Vector2(.04f, .025f), new Vector2(.96f, .235f));
                priceBar.color = new Color(.015f, .04f, .075f, .96f);
                priceLabels[i] = CreateText("Localized Price", priceBar.transform,
                    packs[i].ReferenceUsdPrice, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
                priceLabels[i].fontStyle = FontStyle.Bold;
                priceLabels[i].color = new Color(1f, .86f, .28f);
            }
            built = true;
        }

        private Sprite CreatePackSprite(GemPackProduct pack, Dictionary<string, Texture2D> atlases)
        {
            if (!atlases.TryGetValue(pack.AtlasResourcePath, out Texture2D atlas))
            {
                atlas = Resources.Load<Texture2D>(pack.AtlasResourcePath);
                atlases[pack.AtlasResourcePath] = atlas;
            }
            if (atlas == null) return null;
            float halfWidth = atlas.width * .5f;
            float halfHeight = atlas.height * .5f;
            int column = pack.AtlasSlot % 2;
            int rowFromTop = pack.AtlasSlot / 2;
            float x = column * halfWidth;
            float y = rowFromTop == 0 ? halfHeight : 0f;
            Sprite sprite = Sprite.Create(atlas, new Rect(x, y, halfWidth, halfHeight),
                new Vector2(.5f, .5f), 100f, 0u, SpriteMeshType.FullRect);
            sprite.name = "Gem Pack " + pack.GemAmount;
            runtimeSprites.Add(sprite);
            return sprite;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
                if (descendants[i].name == objectName) return descendants[i];
            return null;
        }

        private static Image CreateImage(string objectName, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject instance = new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, anchorMin, anchorMax);
            return instance.GetComponent<Image>();
        }

        private static Text CreateText(string objectName, Transform parent, string value, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject instance = new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, anchorMin, anchorMax);
            Text text = instance.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
