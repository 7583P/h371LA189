using UnityEngine;
using AnimeHillRacing.Ads;

namespace AnimeHillRacing.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private float lastBannerInset;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height || lastBannerInset != AdMobManager.ReservedBottomPixels)
                Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastBannerInset = AdMobManager.ReservedBottomPixels;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            Vector2 min = safe.position;
            min.y = Mathf.Max(min.y, lastBannerInset);
            Vector2 max = safe.position + safe.size;
            min.x /= Mathf.Max(1f, Screen.width);
            min.y /= Mathf.Max(1f, Screen.height);
            max.x /= Mathf.Max(1f, Screen.width);
            max.y /= Mathf.Max(1f, Screen.height);
            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
