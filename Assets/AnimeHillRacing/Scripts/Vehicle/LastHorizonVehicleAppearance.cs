using System.Collections.Generic;
using AnimeHillRacing.Terrain;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>
    /// Non-destructive per-instance silhouette override. It never touches shared sprites or materials,
    /// and restores every renderer color as soon as the object is disabled or leaves the track.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LastHorizonVehicleAppearance : MonoBehaviour
    {
        private SpriteRenderer[] renderers;
        private Color[] originalColors;
        private bool applied;

        public static void Apply(GameObject vehicleRoot, MapProfile profile)
        {
            if (vehicleRoot == null) return;
            LastHorizonVehicleAppearance appearance =
                vehicleRoot.GetComponent<LastHorizonVehicleAppearance>();
            if (profile == null || !profile.useLastHorizonTerrain)
            {
                if (appearance != null) appearance.Restore();
                return;
            }
            if (appearance == null) appearance = vehicleRoot.AddComponent<LastHorizonVehicleAppearance>();
            appearance.CaptureAndApply();
        }

        private void CaptureAndApply()
        {
            if (!applied)
            {
                SpriteRenderer[] candidates = GetComponentsInChildren<SpriteRenderer>(true);
                VehicleVisual2D visual = GetComponentInChildren<VehicleVisual2D>(true);
                Transform driverRoot = visual != null ? visual.DriverAnchor : null;
                var vehicleRenderers = new List<SpriteRenderer>(candidates.Length);
                for (int i = 0; i < candidates.Length; i++)
                {
                    SpriteRenderer candidate = candidates[i];
                    if (candidate == null) continue;
                    if (driverRoot != null &&
                        (candidate.transform == driverRoot || candidate.transform.IsChildOf(driverRoot)))
                        continue;
                    vehicleRenderers.Add(candidate);
                }
                renderers = vehicleRenderers.ToArray();
                originalColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) originalColors[i] = renderers[i].color;
                applied = true;
            }
            ApplyBlack();
        }

        private void LateUpdate()
        {
            // Animated/custom skin effects can write their tint during Update. Last Horizon owns the
            // final presentation only for this spawned instance, so enforce the silhouette afterwards.
            if (applied) ApplyBlack();
        }

        private void ApplyBlack()
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null) continue;
                float alpha = originalColors != null && i < originalColors.Length
                    ? originalColors[i].a : renderer.color.a;
                renderer.color = new Color(0f, 0f, 0f, alpha);
            }
        }

        public void Restore()
        {
            if (!applied || renderers == null || originalColors == null) return;
            for (int i = 0; i < renderers.Length && i < originalColors.Length; i++)
                if (renderers[i] != null) renderers[i].color = originalColors[i];
            applied = false;
        }

        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
