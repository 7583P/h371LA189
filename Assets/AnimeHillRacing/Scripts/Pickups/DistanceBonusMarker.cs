using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Pickups
{
    /// <summary>
    /// Marker for a distance milestone. The normal progress claim remains authoritative, while a
    /// small physics proximity check lets a visible chassis/wheel touch claim it just before the
    /// vehicle origin crosses ProgressPosition.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DistanceBonusMarker : MonoBehaviour
    {
        private const string SpriteResourcePath = "UI/DistanceBonusBag";
        private const string ShaderResourcePath = "UI/WhiteBackdropSpriteCutout";
        private const string CutoutShaderName = "AnimeHillRacing/White Backdrop Sprite Cutout";
        private static Material sharedCutoutMaterial;
        private static bool missingSpriteReported;

        private float progressPosition;
        private Vector3 restingPosition;
        private Vector3 restingScale;
        private float animationPhase;
        private bool billboard;
        private GameManager manager;

        public float ProgressPosition => progressPosition;

        public static DistanceBonusMarker Create(Transform owner, Vector3 worldPosition,
            float milestoneDistance, bool faceCamera)
        {
            Sprite sprite = Resources.Load<Sprite>(SpriteResourcePath);
            if (sprite == null)
            {
                if (!missingSpriteReported)
                {
                    missingSpriteReported = true;
                    Debug.LogError($"Distance bonus sprite is missing at Resources/{SpriteResourcePath}.");
                }
                return null;
            }

            GameObject markerObject = new GameObject(
                $"Distance Bonus {milestoneDistance:0}m (+{DistanceBonusTracker.GetAwardAtDistance(milestoneDistance)})");
            if (owner != null) markerObject.transform.SetParent(owner, true);
            if (!faceCamera) worldPosition.z = PickupBase.AdventureForegroundVisualZ;
            markerObject.transform.position = worldPosition;

            SpriteRenderer renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 80; // Always in front of the regular coin sprites.
            Material material = GetCutoutMaterial();
            if (material != null) renderer.sharedMaterial = material;

            DistanceBonusMarker marker = markerObject.AddComponent<DistanceBonusMarker>();
            marker.progressPosition = milestoneDistance;
            marker.restingPosition = worldPosition;
            marker.animationPhase = milestoneDistance * .017f;
            marker.billboard = faceCamera;
            markerObject.transform.localScale = Vector3.one * .82f;
            marker.restingScale = markerObject.transform.localScale;
            return marker;
        }

        private static Material GetCutoutMaterial()
        {
            if (sharedCutoutMaterial != null) return sharedCutoutMaterial;
            Shader shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null) shader = Shader.Find(CutoutShaderName);
            if (shader == null) return null;
            sharedCutoutMaterial = new Material(shader)
            {
                name = "Distance Bonus Bag Cutout (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return sharedCutoutMaterial;
        }

        private void Update()
        {
            if (manager == null) manager = GameManager.Instance;
            if (manager != null &&
                (progressPosition <= manager.BestDistance + .001f ||
                 manager.Distance + .001f >= progressPosition))
            {
                gameObject.SetActive(false);
                return;
            }

            float wave = Mathf.Sin(Time.unscaledTime * 2.4f + animationPhase);
            transform.position = restingPosition + Vector3.up * (.10f * wave);
            transform.localScale = restingScale * (1f + .035f * wave);
        }

        private void FixedUpdate()
        {
            if (manager == null) manager = GameManager.Instance;
            if (manager == null || manager.Vehicle == null ||
                Mathf.Abs(manager.Vehicle.ProgressPosition - progressPosition) > 6f ||
                !PickupBase.TryFindTouchingVehicle(transform.position, .72f,
                    out VehicleRuntimeBase touchingVehicle)) return;

            if (manager.TryCollectDistanceBonusMarker(progressPosition, touchingVehicle))
                gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!billboard || Camera.main == null) return;
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
