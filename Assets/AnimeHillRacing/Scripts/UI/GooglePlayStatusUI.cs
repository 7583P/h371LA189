using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>Compact Settings strip for Play Games status, retry and permanent-IAP restore.</summary>
    public sealed class GooglePlayStatusUI : MonoBehaviour
    {
        private Text statusText;
        private Text connectLabel;
        private Button connectButton;
        private Button restoreButton;
        private IAuthenticationProvider authentication;
        private IStorePurchaseProvider store;
        private ICloudSaveProvider cloudSave;
        private bool restoring;
        private bool built;

        public static GooglePlayStatusUI Attach(GameObject settingsPanel)
        {
            if (settingsPanel == null || !Application.isPlaying) return null;
            Transform existing = settingsPanel.transform.Find("Google Play Status Runtime");
            GameObject root = existing != null ? existing.gameObject :
                new GameObject("Google Play Status Runtime", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            if (existing == null) rect.SetParent(settingsPanel.transform, false);
            rect.anchorMin = new Vector2(.08f, .665f);
            rect.anchorMax = new Vector2(.92f, .755f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            GooglePlayStatusUI presenter = root.GetComponent<GooglePlayStatusUI>();
            if (presenter == null) presenter = root.AddComponent<GooglePlayStatusUI>();
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
            if (connectButton != null) connectButton.onClick.RemoveListener(Connect);
            if (restoreButton != null) restoreButton.onClick.RemoveListener(Restore);
        }

        public void Refresh()
        {
            if (!built) return;
            Subscribe();
            bool connected = authentication != null && authentication.IsAuthenticated;
            string games = connected ? "PLAY GAMES: CONNECTED" :
                authentication != null && authentication.State == ExternalAuthenticationState.Authenticating
                    ? "PLAY GAMES: CONNECTING..." : "PLAY GAMES: NOT CONNECTED";
            string billing = store != null && store.IsReady ? "BILLING READY" : "BILLING OFFLINE";
            string cloud = CloudStatus();
            statusText.text = games + cloud + "  -  " + billing;
            statusText.color = connected ? new Color(.36f, 1f, .62f) : new Color(.76f, .84f, .94f);

            connectLabel.text = connected && cloudSave != null && cloudSave.IsEnabled
                ? cloudSave.State == CloudSaveState.Synchronizing ? "SYNCING..." : "SYNC NOW" : connected ? "CONNECTED" :
                authentication != null && authentication.State == ExternalAuthenticationState.Failed
                    ? "RETRY" : "CONNECT";
            connectButton.interactable = connected
                ? cloudSave != null && cloudSave.IsEnabled &&
                  cloudSave.State != CloudSaveState.Synchronizing
                : authentication != null && authentication.IsAvailable &&
                  authentication.IsConfigured &&
                  authentication.State != ExternalAuthenticationState.Authenticating;
            restoreButton.interactable = !restoring && store != null && store.IsReady;
        }

        private void Connect()
        {
            if (authentication != null && authentication.IsAuthenticated)
                cloudSave?.RequestSync();
            else
                authentication?.Authenticate(true);
            Refresh();
        }

        private string CloudStatus()
        {
            if (cloudSave == null || !cloudSave.IsEnabled) return string.Empty;
            switch (cloudSave.State)
            {
                case CloudSaveState.Synchronizing: return "  -  CLOUD: SYNCING";
                case CloudSaveState.Synchronized: return "  -  CLOUD: SYNCED";
                case CloudSaveState.Failed: return "  -  CLOUD: FAILED";
                default: return "  -  CLOUD: WAITING";
            }
        }

        private void Restore()
        {
            if (restoring || store == null || !store.IsReady) return;
            restoring = true;
            Refresh();
            store.RestorePurchases(result =>
            {
                restoring = false;
                statusText.text = result.Succeeded
                    ? $"RESTORE COMPLETE  -  {result.EntitlementsRestored} OWNERSHIP ITEM(S)"
                    : string.IsNullOrWhiteSpace(result.Error) ? "RESTORE FAILED" : result.Error;
                statusText.color = result.Succeeded
                    ? new Color(.36f, 1f, .62f) : new Color(1f, .48f, .36f);
                restoreButton.interactable = store.IsReady;
            });
        }

        private void Subscribe()
        {
            IAuthenticationProvider nextAuthentication = ExternalServiceProviders.Authentication;
            IStorePurchaseProvider nextStore = ExternalServiceProviders.Store;
            ICloudSaveProvider nextCloudSave = ExternalServiceProviders.CloudSave;
            if (ReferenceEquals(authentication, nextAuthentication) && ReferenceEquals(store, nextStore) &&
                ReferenceEquals(cloudSave, nextCloudSave)) return;
            Unsubscribe();
            authentication = nextAuthentication;
            store = nextStore;
            cloudSave = nextCloudSave;
            if (authentication != null) authentication.StateChanged += Refresh;
            if (store != null) store.StateChanged += Refresh;
            if (cloudSave != null) cloudSave.StateChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (authentication != null) authentication.StateChanged -= Refresh;
            if (store != null) store.StateChanged -= Refresh;
            if (cloudSave != null) cloudSave.StateChanged -= Refresh;
            authentication = null;
            store = null;
            cloudSave = null;
        }

        private void BuildOnce()
        {
            if (built) return;
            Image background = GetComponent<Image>();
            background.color = new Color(.025f, .055f, .095f, .92f);
            background.raycastTarget = true;

            statusText = CreateText("Status", transform, "PLAY GAMES: NOT CONNECTED", 14,
                TextAnchor.MiddleLeft, new Vector2(.025f, .08f), new Vector2(.48f, .92f));
            Sprite googlePlayConnectSprite = Resources.Load<Sprite>("UI/GooglePlayConnectButton");
            connectButton = CreateButton("Connect", transform, new Vector2(.50f, .12f),
                new Vector2(.70f, .88f), out connectLabel, googlePlayConnectSprite);
            connectLabel.text = "CONNECT";
            connectLabel.enabled = googlePlayConnectSprite == null;
            connectButton.onClick.AddListener(Connect);
            restoreButton = CreateButton("Restore Purchases", transform, new Vector2(.72f, .12f),
                new Vector2(.98f, .88f), out Text restoreLabel);
            restoreLabel.text = "RESTORE PURCHASES";
            restoreButton.onClick.AddListener(Restore);
            built = true;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, out Text label, Sprite sprite = null)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = instance.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.color = sprite != null ? Color.white : new Color(.08f, .38f, .62f, .96f);
            Button button = instance.GetComponent<Button>();
            button.targetGraphic = image;
            label = CreateText("Label", instance.transform, name.ToUpperInvariant(), 13,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            label.fontStyle = FontStyle.Bold;
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
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
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
