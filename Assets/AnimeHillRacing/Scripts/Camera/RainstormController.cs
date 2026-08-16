using AnimeHillRacing.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class RainstormController : MonoBehaviour
    {
        private Camera targetCamera;
        private RainstormSettings settings;
        private ParticleSystem rain;
        private ParticleSystemRenderer rainRenderer;
        private Material rainMaterial;
        private Canvas flashCanvas;
        private Image flashImage;
        private LineRenderer lightningBolt;
        private Material lightningMaterial;
        private readonly Vector3[] lightningPoints = new Vector3[11];
        private float nextLightningTime;
        private float flashStartedAt = float.NegativeInfinity;
        private bool configured;

        public bool IsRaining => rain != null && rain.isPlaying;
        public int ActiveParticleCount => rain != null ? rain.particleCount : 0;
        public float ConfiguredEventsPerMinute => settings != null ? settings.lightningEventsPerMinute : 0f;
        public float SurfaceGripMultiplier => settings != null ? settings.surfaceGripMultiplier : 1f;
        public float CurrentFlashAlpha => flashImage != null ? flashImage.color.a : 0f;

        public static void ApplyForMap(MapProfile profile, Camera camera)
        {
            RainstormController existing = FindFirstObjectByType<RainstormController>();
            bool shouldRain = profile != null && profile.rainstorm != null && profile.rainstorm.permanentRain;
            if (!shouldRain)
            {
                if (existing != null) Destroy(existing.gameObject);
                return;
            }

            if (camera == null) camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("Rainyer: gameplay camera was not found for the permanent rainstorm.");
                return;
            }
            if (existing == null)
            {
                GameObject root = new GameObject("Rainyer Permanent Storm");
                existing = root.AddComponent<RainstormController>();
            }
            existing.Configure(profile.rainstorm, camera);
        }

        public void Configure(RainstormSettings rainSettings, Camera camera)
        {
            settings = rainSettings;
            targetCamera = camera;
            BuildRain();
            BuildFlash();
            BuildLightningBolt();
            ScheduleNextLightning();
            configured = true;
        }

        private void BuildRain()
        {
            GameObject rainObject = new GameObject("Pooled Diagonal Rain");
            rainObject.transform.SetParent(transform, false);
            rain = rainObject.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = rain.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = settings.maximumParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.25f, 1.85f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(settings.streakWidth * .8f,
                settings.streakWidth * 1.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(.52f, .72f, .92f, .42f), new Color(.72f, .84f, 1f, .72f));

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = settings.emissionRate;
            ParticleSystem.ShapeModule shape = rain.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 1f, .2f);
            ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = settings.fallVelocity.x;
            velocity.y = settings.fallVelocity.y;

            rainRenderer = rain.GetComponent<ParticleSystemRenderer>();
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.velocityScale = settings.streakLength * .055f;
            rainRenderer.lengthScale = settings.streakLength * .55f;
            rainRenderer.cameraVelocityScale = 0f;
            rainRenderer.sortingOrder = 90;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            rainMaterial = new Material(shader) { name = "Rainyer Rain (Runtime)" };
            rainMaterial.mainTexture = Texture2D.whiteTexture;
            rainRenderer.sharedMaterial = rainMaterial;
            rain.Play(true);
        }

        private void BuildFlash()
        {
            GameObject canvasObject = new GameObject("Lightning Flash Canvas");
            canvasObject.transform.SetParent(transform, false);
            flashCanvas = canvasObject.AddComponent<Canvas>();
            flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            flashCanvas.sortingOrder = -50;
            GameObject imageObject = new GameObject("Storm Sky Flash");
            imageObject.transform.SetParent(canvasObject.transform, false);
            flashImage = imageObject.AddComponent<Image>();
            flashImage.raycastTarget = false;
            RectTransform rect = flashImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            flashImage.color = new Color(.58f, .72f, 1f, 0f);
        }

        private void BuildLightningBolt()
        {
            GameObject boltObject = new GameObject("Occasional Lightning Bolt");
            boltObject.transform.SetParent(transform, false);
            lightningBolt = boltObject.AddComponent<LineRenderer>();
            lightningBolt.useWorldSpace = true;
            lightningBolt.positionCount = lightningPoints.Length;
            lightningBolt.widthMultiplier = .055f;
            lightningBolt.numCornerVertices = 2;
            lightningBolt.numCapVertices = 2;
            lightningBolt.startColor = new Color(.72f, .86f, 1f, .95f);
            lightningBolt.endColor = new Color(.48f, .66f, 1f, .25f);
            lightningBolt.sortingOrder = 89;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            lightningMaterial = new Material(shader) { name = "Rainyer Lightning (Runtime)" };
            lightningMaterial.mainTexture = Texture2D.whiteTexture;
            lightningBolt.sharedMaterial = lightningMaterial;
            lightningBolt.enabled = false;
        }

        private void LateUpdate()
        {
            if (!configured || targetCamera == null) return;
            float halfHeight = targetCamera.orthographic ? targetCamera.orthographicSize : 7f;
            float halfWidth = halfHeight * targetCamera.aspect;
            transform.position = targetCamera.transform.position;
            if (rain != null)
            {
                rain.transform.position = targetCamera.transform.position +
                                          new Vector3(-settings.fallVelocity.x * .18f, halfHeight + 2f, 8f);
                ParticleSystem.ShapeModule shape = rain.shape;
                shape.scale = new Vector3(halfWidth * 2f + 10f, 1f, .2f);
            }

            float now = Time.unscaledTime;
            if (now >= nextLightningTime) TriggerLightning();
            UpdateFlash(now);
        }

        public void ForceLightningForValidation() => TriggerLightning();

        private void TriggerLightning()
        {
            flashStartedAt = Time.unscaledTime;
            if (flashImage != null)
                flashImage.color = new Color(.58f, .72f, 1f, settings.lightningFlashAlpha);
            bool showBolt = Random.value < .65f;
            lightningBolt.enabled = showBolt;
            if (showBolt) GenerateBolt();
            ScheduleNextLightning();
        }

        private void GenerateBolt()
        {
            float x = Random.Range(.16f, .84f);
            float depth = Mathf.Abs(targetCamera.transform.position.z - 2f);
            Vector3 start = targetCamera.ViewportToWorldPoint(new Vector3(x, .98f, depth));
            Vector3 end = targetCamera.ViewportToWorldPoint(new Vector3(x + Random.Range(-.10f, .10f), .47f, depth));
            for (int i = 0; i < lightningPoints.Length; i++)
            {
                float t = i / (lightningPoints.Length - 1f);
                Vector3 point = Vector3.Lerp(start, end, t);
                if (i > 0 && i < lightningPoints.Length - 1)
                    point.x += Random.Range(-.28f, .28f);
                point.z = 2f;
                lightningPoints[i] = point;
            }
            lightningBolt.SetPositions(lightningPoints);
        }

        private void UpdateFlash(float now)
        {
            float elapsed = now - flashStartedAt;
            float duration = Mathf.Max(.05f, settings.lightningFlashDuration);
            if (elapsed < 0f || elapsed >= duration)
            {
                if (flashImage != null) flashImage.color = new Color(.58f, .72f, 1f, 0f);
                if (lightningBolt != null) lightningBolt.enabled = false;
                return;
            }
            float normalized = elapsed / duration;
            float firstPulse = Mathf.Clamp01(1f - normalized * 4.2f);
            float secondPulse = normalized > .34f
                ? Mathf.Clamp01(1f - Mathf.Abs(normalized - .48f) * 7f) * .38f : 0f;
            float alpha = Mathf.Max(firstPulse, secondPulse) * settings.lightningFlashAlpha;
            flashImage.color = new Color(.58f, .72f, 1f, alpha);
            if (lightningBolt != null && elapsed > duration * .48f) lightningBolt.enabled = false;
        }

        private void ScheduleNextLightning()
        {
            float average = 60f / Mathf.Max(.5f, settings.lightningEventsPerMinute);
            nextLightningTime = Time.unscaledTime + Random.Range(average * .8f, average * 1.2f);
        }

        private void OnDestroy()
        {
            if (rainMaterial != null) Destroy(rainMaterial);
            if (lightningMaterial != null) Destroy(lightningMaterial);
        }
    }
}
