using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    /// <summary>Replaces only balance-counter artwork; reward and purchase icons are untouched.</summary>
    public static class EconomyCounterIconBinder
    {
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (subscribed) SceneManager.sceneLoaded -= OnSceneLoaded;
            subscribed = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Subscribe()
        {
            if (subscribed) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribed = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BindActiveScene() => RefreshActiveScene();

        public static void RefreshActiveScene() => Bind(SceneManager.GetActiveScene());

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Bind(scene);

        private static void Bind(Scene scene)
        {
            HillAdventurerUiCatalog catalog = HillAdventurerUiCatalog.Instance;
            if (!scene.IsValid() || !scene.isLoaded || catalog == null) return;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Image[] images = roots[rootIndex].GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i];
                    if (image == null || image.name != "Coin Icon" ||
                        !HasCounterAncestor(image.transform, true) || catalog.coinCounterIcon == null)
                        continue;
                    image.sprite = catalog.coinCounterIcon;
                    image.preserveAspect = true;
                }

                Text[] texts = roots[rootIndex].GetComponentsInChildren<Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    Text text = texts[i];
                    if (text == null || (text.name != "Gem Icon" && text.name != "Gem Mark") ||
                        !HasCounterAncestor(text.transform, false) || catalog.gemCounterIcon == null)
                        continue;
                    ReplaceGemText(text, catalog.gemCounterIcon);
                }
            }
        }

        private static bool HasCounterAncestor(Transform source, bool coins)
        {
            for (Transform current = source != null ? source.parent : null;
                 current != null; current = current.parent)
            {
                string objectName = current.name;
                if (coins && (objectName == "Coins Card" || objectName == "Coin Balance Card" ||
                              objectName == "Total Coins Card")) return true;
                if (!coins && (objectName == "Gems Card" || objectName == "Gem Balance Card" ||
                               objectName == "Total Gems Card")) return true;
            }
            return false;
        }

        private static void ReplaceGemText(Text source, Sprite sprite)
        {
            Transform parent = source.transform.parent;
            if (parent == null) return;
            string imageName = source.name + " Image";
            Transform existing = parent.Find(imageName);
            Image image;
            RectTransform imageRect;
            if (existing != null)
            {
                image = existing.GetComponent<Image>();
                imageRect = existing as RectTransform;
            }
            else
            {
                GameObject imageObject = new GameObject(imageName, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                imageRect = imageObject.GetComponent<RectTransform>();
                imageRect.SetParent(parent, false);
                imageRect.SetSiblingIndex(source.transform.GetSiblingIndex());
                image = imageObject.GetComponent<Image>();
            }

            RectTransform sourceRect = source.rectTransform;
            imageRect.anchorMin = sourceRect.anchorMin;
            imageRect.anchorMax = sourceRect.anchorMax;
            imageRect.pivot = sourceRect.pivot;
            imageRect.anchoredPosition = sourceRect.anchoredPosition;
            imageRect.sizeDelta = sourceRect.sizeDelta;
            imageRect.localScale = Vector3.one;
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = true;
            source.enabled = false;
        }
    }
}
