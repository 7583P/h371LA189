using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>Runtime extension of the existing Garage Shop's Special Offers section.</summary>
    public sealed class StarterPackShopUI : MonoBehaviour
    {
        private GameCatalog catalog;
        private Image artwork;
        private Text artworkPending;
        private Image coinsIcon;
        private Image gemsIcon;
        private Text coinsText;
        private Text gemsText;
        private Text priceText;
        private Text statusText;
        private Button buyButton;
        private Text buyFallbackText;
        private bool built;
        private bool purchasing;

        public static StarterPackShopUI Attach(GameObject storePanel, GameCatalog gameCatalog)
        {
            if (storePanel == null) return null;
            Transform[] descendants = storePanel.GetComponentsInChildren<Transform>(true);
            Transform offers = null;
            for (int i = 0; i < descendants.Length; i++)
                if (descendants[i].name == "Special Offers")
                {
                    offers = descendants[i];
                    break;
                }
            if (offers == null) return null;

            Transform existing = offers.Find("Starter Pack Offer Runtime");
            GameObject root;
            if (existing != null)
                root = existing.gameObject;
            else
            {
                root = new GameObject("Starter Pack Offer Runtime", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.SetParent(offers, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(8f, 8f);
                rect.offsetMax = new Vector2(-8f, -8f);
            }
            root.transform.SetAsLastSibling();
            StarterPackShopUI presenter = root.GetComponent<StarterPackShopUI>();
            if (presenter == null) presenter = root.AddComponent<StarterPackShopUI>();
            presenter.Initialize(gameCatalog);
            return presenter;
        }

        public void Initialize(GameCatalog gameCatalog)
        {
            catalog = gameCatalog;
            BuildOnce();
            Refresh();
        }

        private void OnEnable()
        {
            BuildOnce();
            Refresh();
        }

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(Buy);
        }

        public void Refresh()
        {
            if (!built) return;
            HillAdventurerUiCatalog ui = HillAdventurerUiCatalog.Instance;
            StarterPackConfiguration config = StarterPackConfiguration.Instance;
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;

            Sprite artworkSprite = ui != null ? ui.starterPackArtwork : null;
            artwork.sprite = artworkSprite;
            artwork.enabled = artworkSprite != null;
            artworkPending.enabled = artworkSprite == null;
            coinsIcon.sprite = ui != null ? ui.coinsRewardIcon : null;
            coinsIcon.enabled = coinsIcon.sprite != null;
            gemsIcon.sprite = ui != null ? ui.gemsRewardIcon : null;
            gemsIcon.enabled = gemsIcon.sprite != null;
            coinsText.text = $"+{(config != null ? config.coinReward : 100000):N0} COINS";
            gemsText.text = $"+{(config != null ? config.gemReward : 1000):N0} GEMS";

            string productId = config != null ? config.googlePlayProductId : string.Empty;
            if (provider != null && provider.TryGetLocalizedPrice(productId, out string localizedPrice) &&
                !string.IsNullOrWhiteSpace(localizedPrice))
                priceText.text = localizedPrice;
            else
                priceText.text = (config != null && !string.IsNullOrWhiteSpace(config.conceptualPrice)
                    ? config.conceptualPrice : "$2.99") + "  CONCEPT";

            Sprite buySprite = ui != null ? ui.buyStarterPackButton : null;
            buyButton.image.sprite = buySprite;
            buyButton.image.preserveAspect = buySprite != null;
            buyButton.image.color = buySprite != null ? Color.white : new Color(.08f, .54f, .84f, 1f);
            buyFallbackText.enabled = buySprite == null;

            bool productConfigured = config != null && !string.IsNullOrWhiteSpace(productId);
            bool bundleConfigured = config != null && catalog != null &&
                                    PremiumBundleCatalog.Find(catalog, config.exclusiveBundleId) != null;
            bool providerConfigured = provider != null && provider.IsConfigured;
            bool owned = SaveManager.IsStarterPackOwned;
            buyButton.interactable = !purchasing && !owned && productConfigured && bundleConfigured &&
                                     providerConfigured;
            statusText.text = owned ? "OWNED  •  REWARDS CLAIMED" : purchasing ? "CONTACTING STORE..." :
                !bundleConfigured ? "EXCLUSIVE SKIN BUNDLE NOT CONFIGURED" :
                !productConfigured ? "GOOGLE PLAY PRODUCT ID NOT CONFIGURED" :
                !providerConfigured ? "GOOGLE PLAY BILLING NOT CONNECTED" : "PURCHASE THROUGH GOOGLE PLAY";
        }

        private void Buy()
        {
            if (purchasing || SaveManager.IsStarterPackOwned) return;
            StarterPackConfiguration config = StarterPackConfiguration.Instance;
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;
            if (config == null || provider == null || !provider.IsConfigured ||
                string.IsNullOrWhiteSpace(config.googlePlayProductId))
            {
                Refresh();
                return;
            }

            purchasing = true;
            Refresh();
            provider.Purchase(config.googlePlayProductId, CompletePurchase);
        }

        private void CompletePurchase(StorePurchaseResult result)
        {
            purchasing = false;
            if (!result.Succeeded)
            {
                statusText.text = string.IsNullOrWhiteSpace(result.Error)
                    ? "PURCHASE NOT COMPLETED" : result.Error.ToUpperInvariant();
                RefreshButtonOnly();
                return;
            }

            if (!SaveManager.TryGrantStarterPack(catalog, StarterPackConfiguration.Instance,
                    out string error))
                statusText.text = error;
            Refresh();
        }

        private void RefreshButtonOnly()
        {
            StarterPackConfiguration config = StarterPackConfiguration.Instance;
            buyButton.interactable = !SaveManager.IsStarterPackOwned && config != null &&
                                     !string.IsNullOrWhiteSpace(config.googlePlayProductId) &&
                                     ExternalServiceProviders.Store.IsConfigured;
        }

        private void BuildOnce()
        {
            if (built) return;
            Image background = GetComponent<Image>();
            background.color = new Color(.035f, .045f, .09f, .99f);
            background.raycastTarget = true;

            artwork = CreateImage("Starter Pack Artwork", transform,
                new Vector2(.015f, .06f), new Vector2(.59f, .94f));
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            artworkPending = CreateText("Artwork Pending", artwork.transform,
                "STARTER_PACK\nARTWORK PENDING", 18, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one);

            Text title = CreateText("Title", transform, "STARTER PACK", 28, TextAnchor.MiddleCenter,
                new Vector2(.61f, .76f), new Vector2(.98f, .94f));
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, .82f, .22f);

            coinsIcon = CreateImage("Coins Reward", transform,
                new Vector2(.615f, .59f), new Vector2(.685f, .75f));
            coinsIcon.preserveAspect = true;
            coinsIcon.raycastTarget = false;
            coinsText = CreateText("Coins Amount", transform, "+100,000 COINS", 19,
                TextAnchor.MiddleLeft, new Vector2(.69f, .59f), new Vector2(.98f, .75f));

            gemsIcon = CreateImage("Gems Reward", transform,
                new Vector2(.615f, .43f), new Vector2(.685f, .59f));
            gemsIcon.preserveAspect = true;
            gemsIcon.raycastTarget = false;
            gemsText = CreateText("Gems Amount", transform, "+1,000 GEMS", 19,
                TextAnchor.MiddleLeft, new Vector2(.69f, .43f), new Vector2(.98f, .59f));

            priceText = CreateText("Price", transform, "$2.99  CONCEPT", 22,
                TextAnchor.MiddleCenter, new Vector2(.61f, .31f), new Vector2(.98f, .43f));
            statusText = CreateText("Status", transform, "GOOGLE PLAY BILLING NOT CONNECTED", 13,
                TextAnchor.MiddleCenter, new Vector2(.60f, .22f), new Vector2(.99f, .32f));
            statusText.color = new Color(.68f, .78f, .9f);

            GameObject buttonObject = new GameObject("Buy Starter Pack", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(transform, false);
            buttonRect.anchorMin = new Vector2(.62f, .035f);
            buttonRect.anchorMax = new Vector2(.97f, .22f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            buyButton = buttonObject.GetComponent<Button>();
            buyButton.targetGraphic = buttonObject.GetComponent<Image>();
            buyButton.onClick.AddListener(Buy);
            buyFallbackText = CreateText("Buy Label", buttonObject.transform, "BUY STARTER PACK", 17,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            buyFallbackText.fontStyle = FontStyle.Bold;
            built = true;
        }

        private static Image CreateImage(string objectName, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject instance = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return instance.GetComponent<Image>();
        }

        private static Text CreateText(string objectName, Transform parent, string value, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject instance = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Text));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = instance.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
