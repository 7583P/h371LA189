using System;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>Reusable, single-action coin purchase modal with a full-screen input blocker.</summary>
    public sealed class CoinPurchaseConfirmationUI : MonoBehaviour
    {
        private Text titleText;
        private Text itemText;
        private Text costText;
        private Text statusText;
        private Button yesButton;
        private Button noButton;
        private Action confirmationAction;
        private bool confirmationInFlight;

        public bool IsVisible => gameObject.activeSelf;

        public static CoinPurchaseConfirmationUI GetOrCreate(Transform parent)
        {
            if (parent == null) return null;
            CoinPurchaseConfirmationUI existing = parent.GetComponentInChildren<CoinPurchaseConfirmationUI>(true);
            if (existing != null) return existing;

            GameObject root = new GameObject("Coin Purchase Confirmation", typeof(RectTransform),
                typeof(Image), typeof(CoinPurchaseConfirmationUI));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, Vector2.zero, Vector2.one);
            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, .01f, .03f, .78f);
            blocker.raycastTarget = true;
            CoinPurchaseConfirmationUI modal = root.GetComponent<CoinPurchaseConfirmationUI>();
            modal.BuildVisualTree();
            root.SetActive(false);
            return modal;
        }

        private void Awake()
        {
            if (titleText == null) BuildVisualTree();
        }

        public void Show(string itemType, string displayName, int coinCost, bool affordable,
            Action onConfirmed)
        {
            if (titleText == null) BuildVisualTree();
            confirmationAction = onConfirmed;
            confirmationInFlight = false;
            titleText.text = $"UNLOCK FOR {coinCost:N0} COINS?";
            itemText.text = $"{itemType.ToUpperInvariant()}\n{displayName.ToUpperInvariant()}";
            costText.text = "PERMANENT UNLOCK";
            statusText.text = affordable ? string.Empty : "NOT ENOUGH COINS";
            yesButton.interactable = affordable;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void ShowCoinPack(int coinAmount, int gemCost, bool affordable, Action onConfirmed)
        {
            if (titleText == null) BuildVisualTree();
            confirmationAction = onConfirmed;
            confirmationInFlight = false;
            titleText.text = $"BUY +{coinAmount:N0} COINS?";
            itemText.text = "COIN PACK";
            costText.text = $"COST  {gemCost:N0} GEMS";
            statusText.text = affordable ? string.Empty : "NOT ENOUGH GEMS";
            yesButton.interactable = affordable;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void SetFailure(string message, bool canRetry)
        {
            confirmationInFlight = false;
            statusText.text = message ?? "UNAVAILABLE";
            yesButton.interactable = canRetry;
        }

        public void Hide()
        {
            confirmationAction = null;
            confirmationInFlight = false;
            gameObject.SetActive(false);
        }

        private void Confirm()
        {
            if (confirmationInFlight || confirmationAction == null) return;
            confirmationInFlight = true;
            yesButton.interactable = false;
            confirmationAction.Invoke();
        }

        private void BuildVisualTree()
        {
            if (transform.Find("Panel") != null) return;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject panelObject = CreateImage("Panel", transform, new Color(.025f, .07f, .13f, .99f),
                new Vector2(.18f, .20f), new Vector2(.82f, .80f));
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .72f, 1f, .95f);
            outline.effectDistance = new Vector2(3f, -3f);

            titleText = CreateText("Title", panelObject.transform, font, 30, FontStyle.Bold,
                new Vector2(.07f, .77f), new Vector2(.93f, .94f));
            itemText = CreateText("Item", panelObject.transform, font, 25, FontStyle.Bold,
                new Vector2(.08f, .48f), new Vector2(.92f, .76f));
            costText = CreateText("Cost", panelObject.transform, font, 23, FontStyle.Bold,
                new Vector2(.08f, .33f), new Vector2(.92f, .48f));
            costText.color = new Color(1f, .84f, .23f, 1f);
            statusText = CreateText("Status", panelObject.transform, font, 19, FontStyle.Bold,
                new Vector2(.08f, .23f), new Vector2(.92f, .34f));
            statusText.color = new Color(1f, .28f, .22f, 1f);

            noButton = CreateButton("No", panelObject.transform, font, "NO",
                new Vector2(.10f, .06f), new Vector2(.46f, .23f), new Color(.30f, .32f, .36f, 1f));
            yesButton = CreateButton("Yes", panelObject.transform, font, "YES",
                new Vector2(.54f, .06f), new Vector2(.90f, .23f), new Color(.08f, .55f, .28f, 1f));
            noButton.onClick.AddListener(Hide);
            yesButton.onClick.AddListener(Confirm);
        }

        private static GameObject CreateImage(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, anchorMin, anchorMax);
            result.GetComponent<Image>().color = color;
            return result;
        }

        private static Text CreateText(string name, Transform parent, Font font, int size,
            FontStyle style, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(Text));
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, anchorMin, anchorMax);
            Text text = result.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject result = CreateImage(name, parent, color, anchorMin, anchorMax);
            Button button = result.AddComponent<Button>();
            Text text = CreateText("Label", result.transform, font, 24, FontStyle.Bold,
                Vector2.zero, Vector2.one);
            text.text = label;
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
