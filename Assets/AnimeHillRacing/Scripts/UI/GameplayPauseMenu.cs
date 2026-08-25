using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>
    /// Runtime pause overlay shared by every gameplay scene that uses GameplayHUD.
    /// </summary>
    internal sealed class GameplayPauseMenu : MonoBehaviour
    {
        private GameObject modalContainer;
        private bool isOpen;
        private float previousTimeScale = 1f;

        public static GameplayPauseMenu Create(Transform parent, Font preferredFont)
        {
            Canvas rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            Transform targetParent = rootCanvas != null ? rootCanvas.transform : parent;
            GameObject root = new GameObject("Gameplay Pause Menu", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(targetParent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 110;

            GameplayPauseMenu menu = root.AddComponent<GameplayPauseMenu>();
            menu.Build(preferredFont);
            return menu;
        }

        private void Build(Font preferredFont)
        {
            Font font = preferredFont != null
                ? preferredFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Exact 50% black screen requested for the paused state.
            modalContainer = CreateImageObject("Pause Backdrop", transform,
                new Color(0f, 0f, 0f, .5f), Vector2.zero, Vector2.one);

            GameObject window = CreateImageObject("Pause Window", modalContainer.transform,
                new Color(.035f, .065f, .13f, .98f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.sizeDelta = new Vector2(420f, 390f);

            Outline outline = window.AddComponent<Outline>();
            outline.effectColor = new Color(.18f, .72f, .92f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            CreateText(window.transform, "Title", "PAUSE", 38, FontStyle.Bold,
                new Vector2(0f, 138f), new Vector2(350f, 62f), Color.white, font);

            CreateButton(window.transform, "Resume", "Resume", new Vector2(0f, 60f),
                new Vector2(300f, 64f), new Color(.08f, .48f, .28f, 1f), font, Resume);
            CreateButton(window.transform, "Reboot", "Reboot", new Vector2(0f, -25f),
                new Vector2(300f, 64f), new Color(.10f, .35f, .66f, 1f), font, Reboot);
            CreateButton(window.transform, "Leave", "Leave", new Vector2(0f, -110f),
                new Vector2(300f, 64f), new Color(.62f, .12f, .16f, 1f), font, Leave);

            modalContainer.SetActive(false);
        }

        public void Open()
        {
            if (isOpen || modalContainer == null) return;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
            modalContainer.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Resume()
        {
            if (!isOpen) return;
            isOpen = false;
            Time.timeScale = previousTimeScale;
            modalContainer.SetActive(false);
        }

        public void Reboot()
        {
            PrepareSceneChange();
            GameManager manager = ResolveGameManager();
            if (manager != null)
                manager.Retry();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Leave()
        {
            PrepareSceneChange();
            GameManager manager = ResolveGameManager();
            if (manager != null)
                manager.MainMenu();
            else
                SceneManager.LoadScene("PreStart");
        }

        private void PrepareSceneChange()
        {
            isOpen = false;
            Time.timeScale = 1f;
            if (modalContainer != null) modalContainer.SetActive(false);
        }

        private static GameManager ResolveGameManager()
        {
            return GameManager.Instance != null
                ? GameManager.Instance
                : FindFirstObjectByType<GameManager>();
        }

        private void OnDestroy()
        {
            if (isOpen) Time.timeScale = previousTimeScale;
        }

        private static GameObject CreateImageObject(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return target;
        }

        private static Text CreateText(Transform parent, string name, string content, int size,
            FontStyle style, Vector2 position, Vector2 dimensions, Color color, Font font)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Text));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = target.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 dimensions, Color color, Font font,
            UnityEngine.Events.UnityAction onClick)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Image image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            Button button = target.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.76f, .76f, .76f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            target.AddComponent<ButtonPressFeedback>();

            CreateText(target.transform, "Label", label, 27, FontStyle.Bold,
                Vector2.zero, dimensions, Color.white, font);
            return button;
        }
    }
}
