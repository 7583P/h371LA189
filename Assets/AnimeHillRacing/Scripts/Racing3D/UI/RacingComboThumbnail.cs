using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RawImage))]
    public sealed class RacingComboThumbnail : MonoBehaviour
    {
        RawImage image;
        RacingComboThumbnailCache.Entry entry;
        string vehicle, wheel, driver, accessory;
        bool configured;
        float retryAt;
        public Texture Texture => image == null ? null : image.texture;

        public static RacingComboThumbnail Create(Transform parent, float x, float y, float w, float h)
        {
            var area = RacingDesign.Area(parent, "Complete vehicle combo", x, y, w, h);
            var rect = RacingDesign.Area(area, "Cached combo image", 0, 0, 1, 1);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false; image.color = Color.clear;
            var aspect = rect.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)RacingComboThumbnailCache.Width / RacingComboThumbnailCache.Height;
            var view = rect.gameObject.AddComponent<RacingComboThumbnail>();
            view.image = image;
            return view;
        }

        public void Configure(LeagueCompetitor owner) => Configure(owner.vehiclePreference, owner.wheelPreference, owner.characterPreference, owner.accessoryPreference);

        public void Configure(string vehicleId, string wheelId, string driverId, string accessoryId)
        {
            if (configured && vehicle == vehicleId && wheel == wheelId && driver == driverId && accessory == accessoryId && entry != null) return;
            Release();
            vehicle = vehicleId; wheel = wheelId; driver = driverId; accessory = accessoryId; configured = true;
            if (isActiveAndEnabled) Bind();
        }

        void Bind()
        {
            if (image == null) image = GetComponent<RawImage>();
            entry = RacingComboThumbnailCache.Bind(this, vehicle, wheel, driver, accessory);
            Accept(entry == null ? null : entry.texture);
        }

        internal void Accept(Texture texture)
        {
            if (image == null) return;
            image.texture = texture;
            image.color = texture == null ? Color.clear : Color.white;
        }

        void Release()
        {
            RacingComboThumbnailCache.Unbind(entry, this); entry = null;
            Accept(null);
        }

        void OnEnable() { if (configured) Bind(); }
        void Update()
        {
            // A full cache may defer a new view until a menu can safely evict an unused bitmap.
            if(entry!=null || !configured || !RacingComboThumbnailCache.CanCapture || Time.unscaledTime<retryAt)return;
            retryAt=Time.unscaledTime+.25f;Bind();
        }
        void OnDisable() => Release();
        void OnDestroy() => Release();
    }
}
