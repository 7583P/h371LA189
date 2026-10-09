using UnityEngine;
using Unity.Profiling;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class RacingLancerVisuals : MonoBehaviour
    {
        static readonly ProfilerMarker VisualMarker=new ProfilerMarker("Racing.Lancer.Visual");
        RacingVehiclePhysics vehicle;
        Renderer[] originalRenderers;
        bool[] originallyEnabled;
        Transform form, flame;
        TrailRenderer trail;
        bool transformed;
        public bool IsVisible => transformed && form != null && form.gameObject.activeSelf;

        void Awake()
        {
            vehicle = GetComponent<RacingVehiclePhysics>();
            var visual = GetComponent<RacingVehicleVisuals>();
            Transform original = visual != null && visual.BodyVisual != null ? visual.BodyVisual.parent : transform;
            originalRenderers = original.GetComponentsInChildren<Renderer>(true);
            originallyEnabled = new bool[originalRenderers.Length];
            form = new GameObject("Lancer rocket meteor form").transform;
            form.SetParent(transform, false);
            form.localPosition = new Vector3(0, .32f, -.12f);
            form.localScale = Vector3.one * 1.9f;
            flame = RacingLancerArt.Build(form);
            var exhaust = new GameObject("Lancer cached fire trail");
            exhaust.transform.SetParent(form, false);exhaust.transform.localPosition = new Vector3(0, 0, -.95f);
            trail = exhaust.AddComponent<TrailRenderer>();
            trail.sharedMaterial = RacingArt.EnergyTrailMaterial();
            trail.time = .28f;trail.minVertexDistance = .4f;trail.startWidth = .58f;trail.endWidth = .015f;
            trail.startColor = RaceItemUtility.ColorFor(RaceItemType.Lancer);trail.endColor = new Color(1f, .12f, .015f, 0);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows = false;trail.emitting = false;
            form.gameObject.SetActive(false);
            vehicle.OnLancerChanged += TransformVehicle;
            TransformVehicle(vehicle.IsLancerActive);
        }
        void TransformVehicle(bool active)
        {
            if (active == transformed) return;
            transformed = active;
            for (int i = 0; i < originalRenderers.Length; i++) {
                if (originalRenderers[i] == null) continue;
                if (active) { originallyEnabled[i] = originalRenderers[i].enabled; originalRenderers[i].enabled = false; }
                else originalRenderers[i].enabled = originallyEnabled[i];
            }
            form.gameObject.SetActive(active);
            trail.Clear();trail.emitting = active;
        }
        void LateUpdate()
        {
            if (!transformed) return;
            using(VisualMarker.Auto()) AnimateFlame();
        }
        void AnimateFlame()
        {
            form.localRotation = Quaternion.Euler(0, 0, -vehicle.LastCommands.Steering * 6f);
            flame.localScale = new Vector3(1, 1, 1f + Mathf.Sin(Time.time * 37f) * .12f);
        }
        void OnDisable() { if (vehicle != null) vehicle.CancelLancer(); TransformVehicle(false); }
        void OnDestroy() { if (vehicle != null) vehicle.OnLancerChanged -= TransformVehicle; }
    }
}
