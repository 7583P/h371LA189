using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>Reusable runtime geometry for one exclusive Last Horizon/Ragnarok phase.</summary>
    public sealed class LastHorizonSpecialPhaseEvent : MonoBehaviour
    {
        public const float RoofTransportDurationSeconds = 2f;

        private GameObject roofObject;
        private MeshFilter roofFilter;
        private MeshRenderer roofRenderer;
        private EdgeCollider2D roofCollider;
        private Mesh roofMesh;

        private GameObject transportObject;
        private Rigidbody2D transportBody;
        private BoxCollider2D transportCollider;
        private MeshFilter transportFilter;
        private MeshRenderer transportRenderer;
        private Mesh transportMesh;
        private Vector2 transportStartPosition;
        private Vector2 transportEndPosition;
        private float transportElapsed;
        private bool transportStarted;

        private readonly List<GameObject> crusherObjects = new List<GameObject>(5);
        private readonly List<Rigidbody2D> crusherBodies = new List<Rigidbody2D>(5);
        private readonly List<Vector2> crusherStartPositions = new List<Vector2>(5);
        private readonly List<Vector2> crusherEndPositions = new List<Vector2>(5);
        private Mesh crusherMesh;

        private GameObject platformRoot;
        private readonly List<GameObject> playablePlatforms = new List<GameObject>(48);
        private readonly List<GameObject> decorativePlatforms = new List<GameObject>(16);

        private GameObject movingObject;
        private Rigidbody2D movingBody;
        private EdgeCollider2D movingCollider;
        private MeshFilter movingFilter;
        private MeshRenderer movingRenderer;
        private Mesh movingMesh;
        private Vector2 movingBasePosition;

        private GameObject voidTriggerObject;
        private BoxCollider2D voidTrigger;
        private LethalObstacleContact voidLethal;

        private MapProfile profile;
        private LastHorizonSettings settings;
        private LastHorizonSpecialPhaseLayout layout;
        private GameManager gameManager;
        private float trackWidth;

        public long Key => layout.key;
        public LastHorizonSpecialPhaseType Type => layout.type;

        public void Configure(MapProfile map, LastHorizonSpecialPhaseLayout phaseLayout,
            float surfaceWidth, GameManager manager)
        {
            if (map == null || map.lastHorizon == null || !phaseLayout.IsValid)
            {
                Deactivate();
                return;
            }

            profile = map;
            settings = map.lastHorizon;
            layout = phaseLayout;
            gameManager = manager;
            trackWidth = Mathf.Max(2f, surfaceWidth);
            EnsureObjects();
            gameObject.name = $"Last Horizon {layout.type} - {layout.start:0}-{layout.end:0}m";
            gameObject.SetActive(true);
            DisableAllGeometry();
            switch (layout.type)
            {
                case LastHorizonSpecialPhaseType.RoofTunnel:
                    ConfigureRoof();
                    break;
                case LastHorizonSpecialPhaseType.RoofTransportCrusher:
                    ConfigureRoof();
                    ConfigureRoofTransportCrusher();
                    break;
                case LastHorizonSpecialPhaseType.DescendingVoidPlatforms:
                    ConfigureDescendingPlatforms();
                    break;
                case LastHorizonSpecialPhaseType.MovingExtremeTerrain:
                    ConfigureMovingExtremeTerrain();
                    break;
            }
        }

        private void EnsureObjects()
        {
            if (roofObject == null)
            {
                roofObject = new GameObject("Lethal Track-Material Roof");
                roofObject.transform.SetParent(transform, false);
                roofFilter = roofObject.AddComponent<MeshFilter>();
                roofRenderer = roofObject.AddComponent<MeshRenderer>();
                roofCollider = roofObject.AddComponent<EdgeCollider2D>();
                roofMesh = new Mesh { name = "Last Horizon Roof Mesh" };
                roofMesh.MarkDynamic();
                roofFilter.sharedMesh = roofMesh;
            }

            if (transportObject == null)
            {
                transportObject = new GameObject("Black Two-Second Transport Platform");
                transportObject.transform.SetParent(transform, false);
                transportBody = transportObject.AddComponent<Rigidbody2D>();
                transportBody.bodyType = RigidbodyType2D.Kinematic;
                transportBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                transportBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                transportBody.useFullKinematicContacts = true;
                transportCollider = transportObject.AddComponent<BoxCollider2D>();
                transportFilter = transportObject.AddComponent<MeshFilter>();
                transportRenderer = transportObject.AddComponent<MeshRenderer>();
                transportMesh = new Mesh { name = "Last Horizon Black Transport Platform Mesh" };
                transportFilter.sharedMesh = transportMesh;
                LastHorizonTransportPlatformContact contact =
                    transportObject.AddComponent<LastHorizonTransportPlatformContact>();
                contact.Assign(this, transportCollider);
            }

            if (platformRoot == null)
            {
                platformRoot = new GameObject("Descending Platforms");
                platformRoot.transform.SetParent(transform, false);
            }

            if (movingObject == null)
            {
                movingObject = new GameObject("Moving Extreme Terrain");
                movingObject.transform.SetParent(transform, false);
                movingBody = movingObject.AddComponent<Rigidbody2D>();
                movingBody.bodyType = RigidbodyType2D.Kinematic;
                movingBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                movingBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                movingCollider = movingObject.AddComponent<EdgeCollider2D>();
                movingFilter = movingObject.AddComponent<MeshFilter>();
                movingRenderer = movingObject.AddComponent<MeshRenderer>();
                movingMesh = new Mesh { name = "Last Horizon Moving Extreme Terrain Mesh" };
                movingMesh.MarkDynamic();
                movingFilter.sharedMesh = movingMesh;
            }

            if (voidTriggerObject == null)
            {
                voidTriggerObject = new GameObject("Lethal Void");
                voidTriggerObject.transform.SetParent(transform, false);
                voidTrigger = voidTriggerObject.AddComponent<BoxCollider2D>();
                voidTrigger.isTrigger = true;
                voidLethal = voidTriggerObject.AddComponent<LethalObstacleContact>();
            }
        }

        private void DisableAllGeometry()
        {
            roofObject.SetActive(false);
            platformRoot.SetActive(false);
            movingObject.SetActive(false);
            transportObject.SetActive(false);
            voidTriggerObject.SetActive(false);
            for (int i = 0; i < crusherObjects.Count; i++) crusherObjects[i].SetActive(false);
            SetPlatformCount(playablePlatforms, 0, false);
            SetPlatformCount(decorativePlatforms, 0, true);
        }

        private void ConfigureRoof()
        {
            roofObject.SetActive(true);
            roofObject.transform.position = new Vector3(layout.start, 0f, 0f);
            roofRenderer.sharedMaterial = profile.terrainMaterial;

            float length = layout.Length;
            int sampleCount = Mathf.Max(3, Mathf.CeilToInt(length /
                Mathf.Max(1f, settings.roofSampleSpacing)) + 1);
            var points = new Vector2[sampleCount];
            // Extend the visual well above the driveable underside so the ceiling is a filled mass,
            // not a thin floating ribbon. Only the lower contour owns collision.
            float thickness = Mathf.Max(settings.roofThickness,
                settings.roofVehicleClearance * 3f, 24f);
            float z = -trackWidth * .5f - .055f;
            var vertices = new Vector3[sampleCount * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(sampleCount - 1) * 6];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                float localX = length * t;
                float worldX = layout.start + localX;
                float terrainY = TerrainGenerator.SampleLastHorizonHeight(profile, worldX);
                float entranceEnvelope = Mathf.Pow(
                    Mathf.Max(0f, Mathf.Sin(Mathf.PI * t)), .55f);
                float roofRelief = entranceEnvelope *
                    (Mathf.Sin(t * Mathf.PI * 4f + layout.phase) * .8f +
                     Mathf.Sin(t * Mathf.PI * 9f + layout.phase * .37f) * .24f);
                float ceilingY = terrainY + Mathf.Max(settings.safeVehicleHeight + .8f,
                    settings.roofVehicleClearance) + roofRelief;
                points[i] = new Vector2(localX, ceilingY);
                int vertex = i * 2;
                vertices[vertex] = new Vector3(localX, ceilingY, z);
                vertices[vertex + 1] = new Vector3(localX, ceilingY + thickness, z);
                float u = worldX / Mathf.Max(1f, profile.terrainTextureRepeatWidth);
                uvs[vertex] = new Vector2(u, profile.terrainSurfaceUvRange.y);
                uvs[vertex + 1] = new Vector2(u, profile.terrainSurfaceUvRange.x);
                if (i >= sampleCount - 1) continue;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }
            roofCollider.points = points;
            roofCollider.sharedMaterial = profile.groundPhysicsMaterial2D;
            WriteMesh(roofMesh, vertices, uvs, triangles);
        }

        private void ConfigureRoofTransportCrusher()
        {
            ConfigureVoidTrigger();
            transportObject.transform.rotation = Quaternion.identity;
            transportRenderer.sharedMaterial = profile.terrainMaterial;

            float gapStart = layout.GapStart;
            float gapEnd = layout.GapEnd;
            float gapLength = Mathf.Max(24f, gapEnd - gapStart);
            float platformWidth = Mathf.Clamp(settings.roofTransportPlatformWidth, 6f,
                gapLength - 4f);
            float platformThickness = Mathf.Max(.2f,
                settings.roofTransportPlatformThickness);
            float halfWidth = platformWidth * .5f;
            float entrySurfaceY = TerrainGenerator.SampleLastHorizonHeight(profile, gapStart);
            float exitSurfaceY = TerrainGenerator.SampleLastHorizonHeight(profile, gapEnd);
            transportStartPosition = new Vector2(gapStart + halfWidth - .35f,
                entrySurfaceY - platformThickness * .5f);
            transportEndPosition = new Vector2(gapEnd - halfWidth + .35f,
                exitSurfaceY - platformThickness * .5f);
            transportBody.position = transportStartPosition;
            transportBody.rotation = 0f;
            transportBody.linearVelocity = Vector2.zero;
            transportBody.angularVelocity = 0f;
            transportCollider.size = new Vector2(platformWidth, platformThickness);
            transportCollider.offset = Vector2.zero;
            transportCollider.sharedMaterial = profile.groundPhysicsMaterial2D;
            WriteRectangleMesh(transportMesh, platformWidth, platformThickness,
                -trackWidth * .5f - .065f);

            transportElapsed = 0f;
            transportStarted = false;
            ConfigureCrushers(gapEnd, exitSurfaceY);
            transportObject.SetActive(true);
        }

        private void ConfigureCrushers(float exitX, float exitSurfaceY)
        {
            int count = Mathf.Clamp(settings.roofCrusherBlockCount, 2, 5);
            EnsureCrusherCount(count);
            float size = Mathf.Max(1.5f, settings.roofCrusherBlockSize);
            float clearance = Mathf.Max(settings.safeVehicleHeight + .8f,
                settings.roofVehicleClearance);
            crusherStartPositions.Clear();
            crusherEndPositions.Clear();
            WriteRectangleMesh(crusherMesh, size, size, -trackWidth * .5f - .075f);

            for (int i = 0; i < crusherObjects.Count; i++)
            {
                bool active = i < count;
                GameObject block = crusherObjects[i];
                if (!active)
                {
                    block.SetActive(false);
                    continue;
                }

                float x = exitX + size * (.55f + i * .92f);
                float localSurface = TerrainGenerator.SampleLastHorizonHeight(profile, x);
                Vector2 start = new Vector2(x, localSurface - size * .55f);
                Vector2 end = new Vector2(x,
                    Mathf.Max(exitSurfaceY, localSurface) + clearance - size * .55f);
                crusherStartPositions.Add(start);
                crusherEndPositions.Add(end);
                Rigidbody2D body = crusherBodies[i];
                body.position = start;
                body.rotation = 0f;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                block.GetComponent<BoxCollider2D>().size = new Vector2(size, size);
                block.GetComponent<MeshRenderer>().sharedMaterial = profile.terrainMaterial;
                block.GetComponent<LethalObstacleContact>().Configure(gameManager, false);
                block.SetActive(true);
            }
        }

        private void EnsureCrusherCount(int count)
        {
            if (crusherMesh == null)
                crusherMesh = new Mesh { name = "Last Horizon Rising Crusher Block Mesh" };
            while (crusherObjects.Count < count)
            {
                int index = crusherObjects.Count;
                GameObject block = new GameObject($"Rising Crusher Block {index + 1}");
                block.transform.SetParent(transform, false);
                Rigidbody2D body = block.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.useFullKinematicContacts = true;
                block.AddComponent<BoxCollider2D>();
                block.AddComponent<MeshFilter>().sharedMesh = crusherMesh;
                block.AddComponent<MeshRenderer>();
                block.AddComponent<LethalObstacleContact>();
                crusherObjects.Add(block);
                crusherBodies.Add(body);
                block.SetActive(false);
            }
        }

        internal void NotifyTransportPlatformContact(Collider2D other)
        {
            if (transportStarted || settings == null ||
                layout.type != LastHorizonSpecialPhaseType.RoofTransportCrusher || other == null)
                return;
            VehicleRuntimeBase candidate = other.GetComponentInParent<VehicleRuntimeBase>();
            if (candidate == null || candidate.Body2D == null) return;
            transportStarted = true;
            transportElapsed = 0f;
        }

        private void ConfigureDescendingPlatforms()
        {
            platformRoot.SetActive(true);
            platformRoot.transform.position = Vector3.zero;
            ConfigureVoidTrigger();
            float start = layout.GapStart;
            float end = layout.GapEnd;
            float span = Mathf.Max(20f, end - start);
            float targetWidth = Mathf.Max(4f, settings.voidPlatformWidth);
            float targetGap = Mathf.Max(1f, settings.voidPlatformGap);
            int count = Mathf.Max(8, Mathf.FloorToInt(span / (targetWidth + targetGap)));
            float gap = targetGap;
            float width = Mathf.Max(4f, (span - gap * (count + 1)) / count);
            if (width > targetWidth)
            {
                width = targetWidth;
                gap = Mathf.Max(1f, (span - width * count) / (count + 1));
            }
            SetPlatformCount(playablePlatforms, count, false);
            float entryY = TerrainGenerator.SampleLastHorizonHeight(profile, start);
            float exitY = TerrainGenerator.SampleLastHorizonHeight(profile, end);
            float maximumDrop = Mathf.Max(.5f, settings.voidPlatformMaximumDrop);
            float[] platformY = new float[count];
            for (int i = 0; i < count; i++)
            {
                float progress = (i + 1f) / (count + 1f);
                float bridgeY = Mathf.Lerp(entryY, exitY, progress);
                float descent = Mathf.Pow(
                    Mathf.Max(0f, Mathf.Sin(Mathf.PI * progress)), .78f) * maximumDrop;
                platformY[i] = bridgeY - descent;
            }
            for (int i = 0; i < count; i++)
            {
                float x = start + gap + width * .5f + i * (width + gap);
                float previousY = i > 0 ? platformY[i - 1] : entryY;
                float nextY = i + 1 < count ? platformY[i + 1] : exitY;
                float angle = Mathf.Clamp(Mathf.Atan2(nextY - previousY,
                    (width + gap) * 2f) * Mathf.Rad2Deg, -18f, 18f);
                ConfigurePlatform(playablePlatforms[i], x, platformY[i], width,
                    Mathf.Max(.2f, settings.voidPlatformThickness), angle, true);
            }

            int decorativeCount = Mathf.Clamp(settings.voidDecorativePlatformCount, 4, 16);
            SetPlatformCount(decorativePlatforms, decorativeCount, true);
            float decorativeCentre = Mathf.Lerp(start, end, .58f);
            for (int i = 0; i < decorativeCount; i++)
            {
                float direction = i % 2 == 0 ? -1f : 1f;
                float x = decorativeCentre + direction * (6f + i * 4.5f);
                float y = Mathf.Min(entryY, exitY) - maximumDrop - 5f - i * 4.2f;
                ConfigurePlatform(decorativePlatforms[i], x, y, width * .82f,
                    settings.voidPlatformThickness, direction * 7f, false);
            }
        }

        private void ConfigureMovingExtremeTerrain()
        {
            movingObject.SetActive(true);
            ConfigureVoidTrigger();
            float overlap = Mathf.Max(2.2f, settings.movingHorizontalTravel + 1.2f);
            float worldStart = layout.GapStart - overlap;
            float worldEnd = layout.GapEnd + overlap;
            float length = Mathf.Max(20f, worldEnd - worldStart);
            movingBasePosition = new Vector2(worldStart, 0f);
            movingBody.position = movingBasePosition;
            movingBody.rotation = 0f;
            movingBody.linearVelocity = Vector2.zero;
            movingBody.angularVelocity = 0f;
            movingRenderer.sharedMaterial = profile.terrainMaterial;

            var points = new List<Vector2>(Mathf.CeilToInt(length / 2f) * 5);
            float spacing = Mathf.Max(4f, settings.extremeBumpSpacing);
            float angle = Mathf.Clamp(settings.extremeBumpAngle, 65f, 80f);
            float bumpHeight = Mathf.Max(.1f, settings.extremeBumpHeight);
            AddExtremePoint(points, worldStart, worldStart, 0f);
            int cells = Mathf.CeilToInt(length / spacing);
            for (int cell = 0; cell < cells; cell++)
            {
                float cellStart = worldStart + cell * spacing;
                float cellEnd = Mathf.Min(worldEnd, cellStart + spacing);
                if (cellEnd <= cellStart + .2f) continue;
                float cellLength = cellEnd - cellStart;
                float direction = cell % 2 == 0 ? 1f : -.72f;
                float variedHeight = bumpHeight * Mathf.Lerp(.72f, 1.28f,
                    Hash01(layout.phase, cell * 17 + 3));
                float steepRun = variedHeight / Mathf.Max(.01f, Mathf.Tan(angle * Mathf.Deg2Rad));
                float spikeStart = cellStart + cellLength * .44f;
                AddExtremePoint(points, worldStart, cellStart + cellLength * .18f,
                    direction * variedHeight * .18f);
                AddExtremePoint(points, worldStart, spikeStart, 0f);
                AddExtremePoint(points, worldStart, Mathf.Min(cellEnd, spikeStart + steepRun),
                    direction * variedHeight);
                AddExtremePoint(points, worldStart, cellStart + cellLength * .73f,
                    -direction * variedHeight * .20f);
                AddExtremePoint(points, worldStart, cellEnd, 0f);
            }
            AddExtremePoint(points, worldStart, worldEnd, 0f);
            movingCollider.points = points.ToArray();
            movingCollider.sharedMaterial = profile.groundPhysicsMaterial2D;
            BuildMovingSurfaceMesh(points, worldStart);
        }

        private void AddExtremePoint(List<Vector2> points, float worldStart, float worldX,
            float shapeOffset)
        {
            float localX = Mathf.Max(0f, worldX - worldStart);
            if (points.Count > 0 && localX <= points[points.Count - 1].x + .01f) return;
            float baseY = TerrainGenerator.SampleLastHorizonHeight(profile, worldX);
            float micro = Mathf.Sin(worldX * .91f + layout.phase) * .13f +
                          Mathf.Sin(worldX * 2.17f + layout.phase * .41f) * .07f;
            points.Add(new Vector2(localX, baseY + shapeOffset + micro));
        }

        private static float Hash01(float phase, int salt)
        {
            float value = Mathf.Sin(phase * 17.17f + salt * 12.9898f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        private void BuildMovingSurfaceMesh(List<Vector2> points, float worldStart)
        {
            float depth = 3.4f;
            float z = -trackWidth * .5f - .055f;
            var vertices = new Vector3[points.Count * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[Mathf.Max(0, points.Count - 1) * 6];
            for (int i = 0; i < points.Count; i++)
            {
                int vertex = i * 2;
                vertices[vertex] = new Vector3(points[i].x, points[i].y, z);
                vertices[vertex + 1] = new Vector3(points[i].x, points[i].y - depth, z);
                float u = (worldStart + points[i].x) /
                          Mathf.Max(1f, profile.terrainTextureRepeatWidth);
                uvs[vertex] = new Vector2(u, profile.terrainSurfaceUvRange.y);
                uvs[vertex + 1] = new Vector2(u, profile.terrainSurfaceUvRange.x);
                if (i >= points.Count - 1) continue;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }
            WriteMesh(movingMesh, vertices, uvs, triangles);
        }

        private void ConfigureVoidTrigger()
        {
            voidTriggerObject.SetActive(true);
            float start = layout.GapStart;
            float end = layout.GapEnd;
            float entryY = TerrainGenerator.SampleLastHorizonHeight(profile, start);
            float exitY = TerrainGenerator.SampleLastHorizonHeight(profile, end);
            float top = Mathf.Min(entryY, exitY) - 18f;
            const float depth = 260f;
            voidTriggerObject.transform.position = new Vector3((start + end) * .5f,
                top - depth * .5f, 0f);
            voidTrigger.offset = Vector2.zero;
            voidTrigger.size = new Vector2(Mathf.Max(4f, end - start), depth);
            voidLethal.Configure(gameManager, false);
        }

        private void SetPlatformCount(List<GameObject> collection, int count, bool decorative)
        {
            while (collection.Count < count)
            {
                GameObject platform = new GameObject(decorative
                    ? $"Infinite Descent Visual {collection.Count + 1}"
                    : $"Playable Descending Platform {collection.Count + 1}");
                platform.transform.SetParent(platformRoot.transform, false);
                platform.AddComponent<MeshFilter>();
                platform.AddComponent<MeshRenderer>();
                if (!decorative)
                {
                    platform.AddComponent<BoxCollider2D>();
                    platform.AddComponent<LethalObstacleContact>();
                }
                collection.Add(platform);
            }
            for (int i = 0; i < collection.Count; i++) collection[i].SetActive(i < count);
        }

        private void ConfigurePlatform(GameObject platform, float x, float y, float width,
            float thickness, float angle, bool collidable)
        {
            platform.SetActive(true);
            platform.transform.position = new Vector3(x, y, 0f);
            platform.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            MeshFilter filter = platform.GetComponent<MeshFilter>();
            MeshRenderer renderer = platform.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = profile.terrainMaterial;
            Mesh mesh = filter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh { name = "Last Horizon Void Platform Mesh" };
                filter.sharedMesh = mesh;
            }
            float halfWidth = width * .5f;
            float halfHeight = thickness * .5f;
            float z = -trackWidth * .5f - .055f;
            Vector3[] vertices =
            {
                new Vector3(-halfWidth, halfHeight, z), new Vector3(halfWidth, halfHeight, z),
                new Vector3(-halfWidth, -halfHeight, z), new Vector3(halfWidth, -halfHeight, z)
            };
            Vector2[] uvs =
            {
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f)
            };
            int[] triangles = { 0, 1, 2, 2, 1, 3 };
            WriteMesh(mesh, vertices, uvs, triangles);
            BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.enabled = collidable;
                collider.size = new Vector2(width, thickness);
                collider.sharedMaterial = profile.groundPhysicsMaterial2D;
            }
            LethalObstacleContact lethal = platform.GetComponent<LethalObstacleContact>();
            if (lethal != null) lethal.Configure(gameManager, true);
        }

        private static void WriteMesh(Mesh mesh, Vector3[] vertices, Vector2[] uvs, int[] triangles)
        {
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static void WriteRectangleMesh(Mesh mesh, float width, float height, float z)
        {
            float halfWidth = width * .5f;
            float halfHeight = height * .5f;
            Vector3[] vertices =
            {
                new Vector3(-halfWidth, halfHeight, z), new Vector3(halfWidth, halfHeight, z),
                new Vector3(-halfWidth, -halfHeight, z), new Vector3(halfWidth, -halfHeight, z)
            };
            Vector2[] uvs =
            {
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f)
            };
            int[] triangles = { 0, 1, 2, 2, 1, 3 };
            WriteMesh(mesh, vertices, uvs, triangles);
        }

        private void FixedUpdate()
        {
            if (settings == null) return;
            if (layout.type == LastHorizonSpecialPhaseType.MovingExtremeTerrain &&
                movingBody != null && movingObject.activeSelf)
            {
                float period = Mathf.Max(1f, settings.movingTerrainPeriod);
                float time = Time.fixedTime * Mathf.PI * 2f / period + layout.phase;
                Vector2 offset = new Vector2(Mathf.Sin(time) * settings.movingHorizontalTravel,
                    Mathf.Sin(time * .73f + 1.1f) * settings.movingVerticalTravel);
                movingBody.MovePosition(movingBasePosition + offset);
                return;
            }

            if (layout.type != LastHorizonSpecialPhaseType.RoofTransportCrusher ||
                !transportStarted || transportBody == null || !transportObject.activeSelf) return;
            transportElapsed = Mathf.Min(RoofTransportDurationSeconds,
                transportElapsed + Time.fixedDeltaTime);
            float progress = Mathf.Clamp01(transportElapsed / RoofTransportDurationSeconds);
            float easedProgress = progress * progress * (3f - 2f * progress);
            transportBody.MovePosition(Vector2.Lerp(transportStartPosition,
                transportEndPosition, easedProgress));

            float crusherProgress = Mathf.Clamp01(Mathf.InverseLerp(.55f, .92f, progress));
            crusherProgress = crusherProgress * crusherProgress * (3f - 2f * crusherProgress);
            for (int i = 0; i < crusherBodies.Count && i < crusherStartPositions.Count; i++)
                if (crusherObjects[i].activeSelf)
                    crusherBodies[i].MovePosition(Vector2.Lerp(crusherStartPositions[i],
                        crusherEndPositions[i], crusherProgress));
        }

        public void Deactivate()
        {
            profile = null;
            settings = null;
            gameManager = null;
            layout = default;
            if (roofObject != null) roofObject.SetActive(false);
            if (platformRoot != null) platformRoot.SetActive(false);
            if (movingObject != null) movingObject.SetActive(false);
            if (transportObject != null)
            {
                transportBody.linearVelocity = Vector2.zero;
                transportBody.angularVelocity = 0f;
                transportObject.SetActive(false);
            }
            for (int i = 0; i < crusherObjects.Count; i++) crusherObjects[i].SetActive(false);
            if (voidTriggerObject != null) voidTriggerObject.SetActive(false);
            transportStarted = false;
            transportElapsed = 0f;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (roofMesh != null) Destroy(roofMesh);
            if (movingMesh != null) Destroy(movingMesh);
            if (transportMesh != null) Destroy(transportMesh);
            if (crusherMesh != null) Destroy(crusherMesh);
            for (int i = 0; i < playablePlatforms.Count; i++) DestroyPlatformMesh(playablePlatforms[i]);
            for (int i = 0; i < decorativePlatforms.Count; i++) DestroyPlatformMesh(decorativePlatforms[i]);
        }

        private static void DestroyPlatformMesh(GameObject platform)
        {
            Mesh mesh = platform != null ? platform.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (mesh != null) Destroy(mesh);
        }
    }

    public sealed class LastHorizonTransportPlatformContact : MonoBehaviour
    {
        private LastHorizonSpecialPhaseEvent owner;
        private Collider2D platformCollider;

        public void Assign(LastHorizonSpecialPhaseEvent phase, Collider2D collider)
        {
            owner = phase;
            platformCollider = collider;
        }

        private void OnCollisionEnter2D(Collision2D collision) => Forward(collision);
        private void OnCollisionStay2D(Collision2D collision) => Forward(collision);

        private void Forward(Collision2D collision)
        {
            if (owner == null || collision == null) return;
            Collider2D other = collision.collider == platformCollider
                ? collision.otherCollider : collision.collider;
            owner.NotifyTransportPlatformContact(other);
        }
    }
}
