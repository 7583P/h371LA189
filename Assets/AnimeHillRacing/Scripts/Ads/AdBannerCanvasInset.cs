using AnimeHillRacing.UI;
using AnimeHillRacing.Racing3D;
using UnityEngine;

namespace AnimeHillRacing.Ads
{
    // Legacy screen canvases without a safe-area owner also reserve the loaded native banner.
    public sealed class AdBannerCanvasInset : MonoBehaviour
    {
        RectTransform content;
        float previous = -1;
        public static void InstallOnSceneCanvases()
        {
            if (AdMobManager.ReservedBottomPixels <= 0) return;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace || canvas.GetComponent<AdBannerCanvasInset>() != null ||
                    canvas.GetComponentInChildren<SafeArea>(true) != null || canvas.GetComponentInChildren<RacingSafeArea>(true) != null) continue;
                canvas.gameObject.AddComponent<AdBannerCanvasInset>();
            }
        }
        void Awake()
        {
            var go = new GameObject("Banner-safe screen content", typeof(RectTransform));
            content = (RectTransform)go.transform; content.SetParent(transform, false);
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one; content.offsetMin = content.offsetMax = Vector2.zero;
            for (int i = transform.childCount - 1; i >= 0; i--) {
                var child = transform.GetChild(i); if (child != content) child.SetParent(content, false);
            }
        }
        void LateUpdate()
        {
            float reserve = AdMobManager.ReservedBottomPixels;
            if (reserve == previous) return;
            previous = reserve;
            content.anchorMin = new Vector2(0, Mathf.Clamp01(reserve / Mathf.Max(1, Screen.height)));
            content.anchorMax = Vector2.one; content.offsetMin = content.offsetMax = Vector2.zero;
        }
    }
}
