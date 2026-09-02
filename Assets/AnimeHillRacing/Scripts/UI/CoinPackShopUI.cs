using System.Collections.Generic;
using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>Seven coin packs displayed below Gems; artwork owns the amount and buttons own the Gem cost.</summary>
    public sealed class CoinPackShopUI : MonoBehaviour
    {
        private Button[] buyButtons;
        private Text balanceText;
        private Text statusText;
        private CoinPurchaseConfirmationUI confirmation;
        private bool built;
        private string feedback = string.Empty;

        public static CoinPackShopUI Attach(GameObject storePanel)
        {
            if (storePanel == null || !Application.isPlaying) return null;
            Transform content = FindDescendant(storePanel.transform, "Shop Content");
            if (content == null) return null;

            Transform existing = content.Find("Coins Shop Runtime");
            GameObject root = existing != null ? existing.gameObject :
                new GameObject("Coins Shop Runtime", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(LayoutElement));
            if (existing == null)
            {
                root.transform.SetParent(content, false);
                Transform blueprints = content.Find("Blue Prints Accesories");
                if (blueprints != null) root.transform.SetSiblingIndex(blueprints.GetSiblingIndex());
            }

            CoinPackShopUI presenter = root.GetComponent<CoinPackShopUI>();
            if (presenter == null) presenter = root.AddComponent<CoinPackShopUI>();
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

        private void OnDisable() => SaveManager.CurrencyChanged -= Refresh;

        private void OnDestroy()
        {
            SaveManager.CurrencyChanged -= Refresh;
            if (buyButtons == null) return;
            for (int i = 0; i < buyButtons.Length; i++)
                if (buyButtons[i] != null) buyButtons[i].onClick.RemoveAllListeners();
        }

        public void Refresh()
        {
            if (!built || buyButtons == null) return;
            IReadOnlyList<CoinPackProduct> packs = CoinPackCatalog.Products;
            balanceText.text = $"YOUR GEMS  {SaveManager.TotalGems:N0}";
            for (int i = 0; i < packs.Count && i < buyButtons.Length; i++)
                buyButtons[i].interactable = SaveManager.TotalGems >= packs[i].GemCost;
            statusText.text = feedback;
        }

        private void Buy(int packIndex)
        {
            IReadOnlyList<CoinPackProduct> packs = CoinPackCatalog.Products;
            if (packIndex < 0 || packIndex >= packs.Count) return;
            CoinPackProduct pack = packs[packIndex];
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            confirmation = CoinPurchaseConfirmationUI.GetOrCreate(canvas.transform);
            confirmation.ShowCoinPack(pack.CoinAmount, pack.GemCost,
                SaveManager.TotalGems >= pack.GemCost, () => ConfirmPurchase(pack));
        }

        private void ConfirmPurchase(CoinPackProduct pack)
        {
            if (!SaveManager.TryPurchaseCoinPack(pack.CoinAmount, pack.GemCost, out string error))
            {
                confirmation?.SetFailure(error, SaveManager.TotalGems >= pack.GemCost);
                Refresh();
                return;
            }

            confirmation?.Hide();
            feedback = $"+{pack.CoinAmount:N0} COINS ADDED";
            Refresh();
        }

        private void Subscribe()
        {
            SaveManager.CurrencyChanged -= Refresh;
            SaveManager.CurrencyChanged += Refresh;
        }

        private void BuildOnce()
        {
            if (built) return;
            IReadOnlyList<CoinPackProduct> packs = CoinPackCatalog.Products;
            Image background = GetComponent<Image>();
            background.color = new Color(.035f, .065f, .115f, .98f);
            background.raycastTarget = true;
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.preferredHeight = 680f;
            layout.flexibleHeight = 0f;

            Text title = CreateText("Coins Title", transform, "COINS", 32, TextAnchor.MiddleLeft,
                new Vector2(.035f, .90f), new Vector2(.48f, .985f));
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(1f, .78f, .18f);
            balanceText = CreateText("Gem Balance", transform, "YOUR GEMS  0", 21,
                TextAnchor.MiddleRight, new Vector2(.50f, .90f), new Vector2(.965f, .985f));
            statusText = CreateText("Coin Store Status", transform, string.Empty, 16,
                TextAnchor.MiddleCenter, new Vector2(.14f, .855f), new Vector2(.86f, .91f));
            statusText.color = new Color(.35f, .92f, 1f);

            buyButtons = new Button[packs.Count];
            for (int i = 0; i < packs.Count; i++)
            {
                int index = i;
                int column = i % CoinPackCatalog.Columns;
                int row = i / CoinPackCatalog.Columns;
                float xMin = .025f + column * .24375f;
                float xMax = xMin + .21875f;
                float yMin = row == 0 ? .465f : .035f;
                float yMax = row == 0 ? .85f : .42f;

                GameObject card = new GameObject("Coin Pack " + packs[i].CoinAmount,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.SetParent(transform, false);
                SetRect(cardRect, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                card.GetComponent<Image>().color = new Color(.045f, .14f, .22f, .98f);

                Image artwork = CreateImage("Artwork With Coin Amount", card.transform,
                    new Vector2(.015f, .205f), new Vector2(.985f, .995f));
                artwork.sprite = Resources.Load<Sprite>(packs[i].SpriteResourcePath);
                artwork.enabled = artwork.sprite != null;
                artwork.preserveAspect = true;
                artwork.raycastTarget = false;

                GameObject buttonObject = new GameObject("Buy With Gems", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(Button));
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.SetParent(card.transform, false);
                SetRect(buttonRect, new Vector2(.04f, .025f), new Vector2(.96f, .235f));
                Image buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.color = new Color(.02f, .42f, .72f, 1f);
                buyButtons[i] = buttonObject.GetComponent<Button>();
                buyButtons[i].targetGraphic = buttonImage;
                buyButtons[i].onClick.AddListener(() => Buy(index));
                Text label = CreateText("Gem Cost", buttonObject.transform,
                    $"BUY  {packs[i].GemCost:N0} GEMS", 18, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one);
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
            }
            built = true;
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
