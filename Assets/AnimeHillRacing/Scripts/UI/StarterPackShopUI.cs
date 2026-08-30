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
        private Text cosmeticsText;
        private Text priceText;
        private Text statusText;
        private Button buyButton;
        private Text buyFallbackText;
        private bool built;
        private bool purchasing;
        private IStorePurchaseProvider subscribedProvider;

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
            SubscribeProvider();
            Refresh();
        }

        private void OnDisable() => UnsubscribeProvider();

        private void OnDestroy()
        {
            UnsubscribeProvider();
            if (buyButton != null) buyButton.onClick.RemoveListener(Buy);
        }

        public void Refresh()
        {
            // Google Play can finish a purchase after the shop hierarchy has been replaced or
            // scheduled for destruction. UnityEvent/delegate callbacks may still reach this
            // presenter during that frame, so never touch stale Unity UI objects.
            if (!HasLiveView())
            {
                UnsubscribeProvider();
                return;
            }
            HillAdventurerUiCatalog ui = HillAdventurerUiCatalog.Instance;
            StarterPackConfiguration config = StarterPackConfiguration.Instance;
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;
            SubscribeProvider();

            Sprite artworkSprite = ui != null ? ui.starterPackArtwork : null;
            artwork.sprite = artworkSprite;
            artwork.enabled = artworkSprite != null;
            artworkPending.enabled = artworkSprite == null;
            coinsIcon.sprite = ui != null ? ui.coinsRewardIcon : null;
            coinsIcon.enabled = coinsIcon.sprite != null;
            gemsIcon.sprite = ui != null ? ui.gemsRewardIcon : null;
            gemsIcon.enabled = gemsIcon.sprite != null;
            coinsText.text = $"+{(config != null ? config.coinReward : 500000):N0} COINS";
            gemsText.text = $"+{(config != null ? config.gemReward : 1000):N0} GEMS";
            cosmeticsText.text = "HILL ADVENTURER GOLD WHEELS\nCAP - NEON LIGHTNING";

            string productId = config != null ? config.googlePlayProductId : string.Empty;
            bool owned = SaveManager.IsStarterPackOwned;
            if (owned)
                priceText.text = "OWNED";
            else if (provider != null && provider.TryGetLocalizedPrice(productId, out string localizedPrice) &&
                !string.IsNullOrWhiteSpace(localizedPrice))
                priceText.text = localizedPrice;
            else if (string.IsNullOrWhiteSpace(productId))
                priceText.text = "NOT CONFIGURED";
            else if (provider != null && (provider.State == ExternalStoreState.Initializing ||
                                           provider.State == ExternalStoreState.NotInitialized))
                priceText.text = "LOADING PRICE...";
            else
                priceText.text = "UNAVAILABLE";

            Sprite buySprite = ui != null ? owned ? ui.ownedBadge : ui.buyStarterPackButton : null;
            buyButton.image.sprite = buySprite;
            buyButton.image.preserveAspect = buySprite != null;
            buyButton.image.color = buySprite != null ? Color.white : new Color(.08f, .54f, .84f, 1f);
            buyFallbackText.enabled = buySprite == null;

            bool productConfigured = config != null && !string.IsNullOrWhiteSpace(productId);
            bool bundleConfigured = SaveManager.IsStarterPackContentConfigured(catalog, config, out _);
            bool providerConfigured = provider != null && provider.IsConfigured;
            bool productAvailable = provider != null && provider.IsProductAvailable(productId);
            buyButton.interactable = !purchasing && !owned && productConfigured && bundleConfigured &&
                                     providerConfigured && productAvailable;
            statusText.text = owned ? "OWNED  -  REWARDS CLAIMED" : purchasing ? "CONTACTING STORE..." :
                !bundleConfigured ? "STARTER PACK REWARDS NOT CONFIGURED" :
                !productConfigured ? "GOOGLE PLAY PRODUCT ID NOT CONFIGURED" :
                !providerConfigured ? "GOOGLE PLAY BILLING NOT CONFIGURED" :
                provider.State == ExternalStoreState.Initializing ? "LOADING GOOGLE PLAY PRICE..." :
                !productAvailable ? "PRODUCT UNAVAILABLE IN GOOGLE PLAY" : "PURCHASE THROUGH GOOGLE PLAY";
        }

        private void Buy()
        {
            if (purchasing || SaveManager.IsStarterPackOwned) return;
            StarterPackConfiguration config = StarterPackConfiguration.Instance;
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;
            if (config == null || provider == null || !provider.IsConfigured ||
                string.IsNullOrWhiteSpace(config.googlePlayProductId) ||
                !provider.IsProductAvailable(config.googlePlayProductId))
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
            if (!HasLiveView())
            {
                UnsubscribeProvider();
                return;
            }
            if (!result.Succeeded)
            {
                statusText.text = string.IsNullOrWhiteSpace(result.Error)
                    ? result.Status == StorePurchaseStatus.Cancelled
                        ? "PURCHASE CANCELLED" : "PURCHASE NOT COMPLETED"
                    : result.Error.ToUpperInvariant();
                RefreshButtonOnly();
                return;
            }

            // The provider persisted the entitlement before reporting success and only then
            // acknowledged the Google Play order.
            Refresh();
        }

        private void RefreshButtonOnly()
        {
            if (!HasLiveView()) return;
            StarterPackConfiguration config = StarterPackConfiguration.Instance;
            buyButton.interactable = !SaveManager.IsStarterPackOwned && config != null &&
                                     !string.IsNullOrWhiteSpace(config.googlePlayProductId) &&
                                     ExternalServiceProviders.Store.IsConfigured &&
                                     ExternalServiceProviders.Store.IsProductAvailable(
                                         config.googlePlayProductId);
        }

        private void SubscribeProvider()
        {
            IStorePurchaseProvider provider = ExternalServiceProviders.Store;
            if (ReferenceEquals(subscribedProvider, provider)) return;
            UnsubscribeProvider();
            subscribedProvider = provider;
            if (subscribedProvider != null) subscribedProvider.StateChanged += Refresh;
        }

        private void UnsubscribeProvider()
        {
            if (subscribedProvider != null) subscribedProvider.StateChanged -= Refresh;
            subscribedProvider = null;
        }

        private bool HasLiveView()
        {
            return this != null && built && artwork != null && artworkPending != null &&
                   coinsIcon != null && gemsIcon != null && coinsText != null && gemsText != null &&
                   cosmeticsText != null && priceText != null && statusText != null && buyButton != null &&
                   buyButton.image != null && buyFallbackText != null;
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
            coinsText = CreateText("Coins Amount", transform, "+500,000 COINS", 19,
                TextAnchor.MiddleLeft, new Vector2(.69f, .59f), new Vector2(.98f, .75f));

            gemsIcon = CreateImage("Gems Reward", transform,
                new Vector2(.615f, .43f), new Vector2(.685f, .59f));
            gemsIcon.preserveAspect = true;
            gemsIcon.raycastTarget = false;
            gemsText = CreateText("Gems Amount", transform, "+1,000 GEMS", 19,
                TextAnchor.MiddleLeft, new Vector2(.69f, .43f), new Vector2(.98f, .59f));

            cosmeticsText = CreateText("Exclusive Cosmetics", transform,
                "HILL ADVENTURER GOLD WHEELS\nCAP - NEON LIGHTNING", 13,
                TextAnchor.MiddleCenter, new Vector2(.61f, .34f), new Vector2(.98f, .44f));
            cosmeticsText.color = new Color(1f, .82f, .22f);

            priceText = CreateText("Price", transform, "LOADING PRICE...", 22,
                TextAnchor.MiddleCenter, new Vector2(.61f, .27f), new Vector2(.98f, .35f));
            statusText = CreateText("Status", transform, "GOOGLE PLAY BILLING NOT CONNECTED", 13,
                TextAnchor.MiddleCenter, new Vector2(.60f, .19f), new Vector2(.99f, .27f));
            statusText.color = new Color(.68f, .78f, .9f);

            GameObject buttonObject = new GameObject("Buy Starter Pack", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(transform, false);
            buttonRect.anchorMin = new Vector2(.62f, .035f);
            buttonRect.anchorMax = new Vector2(.97f, .19f);
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
