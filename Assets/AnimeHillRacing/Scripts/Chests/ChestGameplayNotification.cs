using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Chests
{
    public sealed class ChestGameplayNotification : MonoBehaviour
    {
        private static ChestGameplayNotification instance;
        private CanvasGroup group;
        private Image chestImage;
        private Text message;
        private Coroutine hideRoutine;

        public static void Show(ChestRecord chest, ChestCatalog catalog)
        {
            if (chest == null || catalog == null) return;
            EnsureInstance();
            ChestDefinition definition = catalog.Find(chest.type);
            instance.chestImage.sprite = definition?.closedSprite;
            instance.chestImage.enabled = instance.chestImage.sprite != null;
            instance.message.text = $"NEW CHEST!\n{(definition?.displayName ?? chest.type + " CHEST").ToUpperInvariant()}";
            instance.group.alpha = 1f;
            if (instance.hideRoutine != null) instance.StopCoroutine(instance.hideRoutine);
            instance.hideRoutine = instance.StartCoroutine(instance.HideAfterDelay());
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            GameObject root = new GameObject("Chest Gameplay Notification");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<ChestGameplayNotification>();
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();
            instance.group = root.AddComponent<CanvasGroup>();
            instance.group.blocksRaycasts = false;

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .82f);
            panelRect.sizeDelta = new Vector2(520f, 170f);
            panel.GetComponent<Image>().color = new Color(.015f, .035f, .075f, .96f);

            GameObject imageObject = new GameObject("Chest", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(panel.transform, false);
            RectTransform imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = new Vector2(.02f, .08f); imageRect.anchorMax = new Vector2(.30f, .92f);
            imageRect.offsetMin = imageRect.offsetMax = Vector2.zero;
            instance.chestImage = imageObject.GetComponent<Image>();
            instance.chestImage.preserveAspect = true;

            GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = new Vector2(.31f, .08f); textRect.anchorMax = new Vector2(.98f, .92f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            instance.message = textObject.GetComponent<Text>();
            instance.message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instance.message.fontSize = 30;
            instance.message.fontStyle = FontStyle.Bold;
            instance.message.alignment = TextAnchor.MiddleCenter;
            instance.message.color = Color.white;
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(4f);
            float started = Time.unscaledTime;
            while (Time.unscaledTime - started < .35f)
            {
                group.alpha = 1f - (Time.unscaledTime - started) / .35f;
                yield return null;
            }
            group.alpha = 0f;
            hideRoutine = null;
        }
    }
}
