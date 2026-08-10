using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    public enum TerrainSectionKind
    {
        Fast,
        Rolling,
        Technical,
        Recovery
    }

    public enum TerrainFeatureType
    {
        FlatRecovery,
        SoftHill,
        MediumHill,
        LongClimb,
        LongDescent,
        Valley,
        Crest,
        RollingSection,
        JumpableCrest
    }

    public static class TerrainGenerator
    {
        private const float TwoPi = Mathf.PI * 2f;

        public static float SampleHeight(MapProfile profile, float worldX)
        {
            return profile != null && profile.elevationProfile != null
                ? profile.elevationProfile.GetHeightAtDistance(worldX)
                : SampleProfileHeight(profile, worldX);
        }

        public static float SampleProfileHeight(MapProfile profile, float worldX)
        {
            if (profile == null) return 0f;
            float distance = Mathf.Max(0f, worldX);
            float difficulty = profile.EvaluateDifficulty(distance);
            float amplitudeProgress = profile.introductoryAmplitude +
                                      difficulty * (1f - profile.introductoryAmplitude);
            float featureLength = Mathf.Max(profile.sectionLength, profile.minimumValleyWidth,
                profile.minimumLandingLength * 1.5f);
            float featurePosition = worldX / featureLength;
            int featureIndex = Mathf.FloorToInt(featurePosition);
            float t = featurePosition - featureIndex;
            float featureDifficulty = profile.EvaluateDifficulty(Mathf.Max(0f, (featureIndex + 0.5f) * featureLength));
            TerrainFeatureType feature = GetFeatureType(profile, featureIndex, featureDifficulty);
            uint seed = CombinedSeed(profile);
            float variant = Hash01(seed, featureIndex * 271 + 37);
            float featureShape = EvaluateFeature(feature, t, variant, Mathf.Clamp01(featureDifficulty),
                profile.technicalDetailStrength);
            float frequency = profile.frequency / Mathf.Max(0.1f, profile.hillSize);
            float phase = Hash01(seed, 73) * TwoPi;
            float broadVariation = Mathf.Sin(worldX * frequency * 0.38f + phase) * 0.12f;
            float perlinVariation = (Mathf.PerlinNoise(seed * 0.000031f + worldX * frequency * 0.24f, 0.417f) * 2f - 1f) * 0.08f;
            float startFlatten = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((worldX + 20f) / 120f));
            return profile.baseHeight + (featureShape + broadVariation + perlinVariation) *
                   profile.amplitude * amplitudeProgress * startFlatten;
        }

        public static TerrainSectionKind GetSectionKind(MapProfile profile, int sectionIndex)
        {
            float distance = Mathf.Max(0f, sectionIndex * profile.sectionLength);
            TerrainFeatureType feature = GetFeatureType(profile, sectionIndex, profile.EvaluateDifficulty(distance));
            if (feature == TerrainFeatureType.FlatRecovery)
                return TerrainSectionKind.Recovery;
            if (feature == TerrainFeatureType.RollingSection || feature == TerrainFeatureType.JumpableCrest)
                return TerrainSectionKind.Technical;
            if (feature == TerrainFeatureType.SoftHill)
                return TerrainSectionKind.Fast;
            return TerrainSectionKind.Rolling;
        }

        public static TerrainFeatureType GetFeatureType(MapProfile profile, int featureIndex, float difficulty)
        {
            uint seed = CombinedSeed(profile);
            int recoveryInterval = Mathf.Max(2, profile.recoveryEverySections);
            int recoveryOffset = (int)(seed % (uint)recoveryInterval);
            if (PositiveModulo(featureIndex + recoveryOffset, recoveryInterval) == recoveryInterval - 1 ||
                (featureIndex > 0 && IsTechnicalFeatureCandidate(profile, seed, featureIndex - 1, difficulty)))
                return TerrainFeatureType.FlatRecovery;

            float difficulty01 = Mathf.Clamp01(difficulty);
            if (IsTechnicalFeatureCandidate(profile, seed, featureIndex, difficulty))
                return Hash01(seed, featureIndex * 431 + 83) < 0.58f
                    ? TerrainFeatureType.RollingSection : TerrainFeatureType.JumpableCrest;
            float flat = Mathf.Max(0f, profile.flatFeatureWeight * Mathf.Lerp(1.15f, 0.72f, difficulty01));
            float hills = Mathf.Max(0f, profile.hillFeatureWeight);
            float valleys = Mathf.Max(0f, profile.valleyFeatureWeight);
            float technical = Mathf.Max(0f, profile.technicalFeatureWeight * Mathf.Lerp(0.45f, 1.35f, difficulty01));
            float total = Mathf.Max(0.001f, flat + hills + valleys + technical);
            float choice = Hash01(seed, featureIndex * 193 + 101) * total;
            if (choice < flat)
                return TerrainFeatureType.FlatRecovery;
            choice -= flat;
            if (choice < hills)
            {
                float variant = Hash01(seed, featureIndex * 389 + 59);
                if (difficulty01 < 0.18f)
                    return variant < 0.58f ? TerrainFeatureType.SoftHill : TerrainFeatureType.MediumHill;
                if (variant < 0.24f) return TerrainFeatureType.SoftHill;
                if (variant < 0.52f) return TerrainFeatureType.MediumHill;
                if (variant < 0.69f) return TerrainFeatureType.LongClimb;
                if (variant < 0.86f) return TerrainFeatureType.LongDescent;
                return TerrainFeatureType.Crest;
            }
            choice -= hills;
            if (choice < valleys)
                return TerrainFeatureType.Valley;
            return Hash01(seed, featureIndex * 431 + 83) < 0.62f
                ? TerrainFeatureType.RollingSection
                : TerrainFeatureType.JumpableCrest;
        }

        private static float EvaluateFeature(TerrainFeatureType feature, float t, float variant, float difficulty,
            float technicalDetailStrength)
        {
            float clampedT = Mathf.Clamp01(t);
            float envelope = Mathf.Sin(clampedT * Mathf.PI);
            envelope *= envelope;
            float shape;
            switch (feature)
            {
                case TerrainFeatureType.FlatRecovery:
                    shape = Mathf.Sin(clampedT * TwoPi) * envelope * 0.08f;
                    break;
                case TerrainFeatureType.SoftHill:
                    shape = envelope * Mathf.Lerp(0.30f, 0.42f, variant);
                    break;
                case TerrainFeatureType.MediumHill:
                    shape = envelope * Mathf.Lerp(0.58f, 0.76f, variant);
                    break;
                case TerrainFeatureType.LongClimb:
                    shape = envelope * (0.42f + clampedT * 0.36f);
                    break;
                case TerrainFeatureType.LongDescent:
                    shape = -envelope * (0.78f - clampedT * 0.34f);
                    break;
                case TerrainFeatureType.Valley:
                    shape = -envelope * Mathf.Lerp(0.55f, 0.78f, variant);
                    break;
                case TerrainFeatureType.Crest:
                    shape = envelope * envelope * Mathf.Lerp(0.72f, 0.9f, variant);
                    break;
                case TerrainFeatureType.RollingSection:
                    shape = Mathf.Sin(clampedT * TwoPi) * envelope * Mathf.Lerp(0.46f, 0.62f, variant);
                    break;
                case TerrainFeatureType.JumpableCrest:
                    shape = Mathf.Pow(envelope, 1.65f) * Mathf.Lerp(0.68f, 0.84f, variant);
                    break;
                default:
                    shape = 0f;
                    break;
            }

            float technicalFactor = feature == TerrainFeatureType.RollingSection ||
                                    feature == TerrainFeatureType.JumpableCrest ||
                                    feature == TerrainFeatureType.Crest || feature == TerrainFeatureType.Valley
                ? 1f : 0.32f;
            float detailCycles = Mathf.Lerp(1.15f, 2.25f, difficulty);
            float detail = Mathf.Sin((clampedT * detailCycles + variant * 0.17f) * TwoPi) * envelope *
                           technicalDetailStrength * technicalFactor * Mathf.Lerp(0f, 0.20f, difficulty);
            return shape + detail;
        }

        public static void BuildMesh(Mesh mesh, MapProfile profile, float chunkStartX, float chunkLength,
            int samples, float width, bool hasBoundary, float boundaryHeight, float boundarySlope,
            out float[] surfaceHeights, out float endHeight, out float endSlope)
        {
            int sampleCount = Mathf.Max(2, samples + 1);
            float dx = chunkLength / (sampleCount - 1);
            int padding = Mathf.Max(2, profile.smoothingPasses + 2);
            float[] working = new float[sampleCount + padding * 2];
            float[] scratch = new float[working.Length];
            for (int i = 0; i < working.Length; i++)
                working[i] = SampleHeight(profile, chunkStartX + (i - padding) * dx);

            int passes = Mathf.Clamp(profile.smoothingPasses, 0, 8);
            for (int pass = 0; pass < passes; pass++)
            {
                scratch[0] = working[0];
                scratch[working.Length - 1] = working[working.Length - 1];
                for (int i = 1; i < working.Length - 1; i++)
                    scratch[i] = working[i - 1] * 0.25f + working[i] * 0.5f + working[i + 1] * 0.25f;
                float[] swap = working;
                working = scratch;
                scratch = swap;
            }

            surfaceHeights = new float[sampleCount];
            float maxSlopeDelta = Mathf.Tan(profile.maxSlopeAngle * Mathf.Deg2Rad) * dx;
            float maxDelta = Mathf.Min(maxSlopeDelta, profile.maxHeightDeltaPerSample);
            float previousHeight = hasBoundary ? boundaryHeight : working[padding];
            float previousSlope = hasBoundary ? boundarySlope :
                (working[padding + 1] - working[padding]) / dx;
            surfaceHeights[0] = previousHeight;

            int firstGeneratedSample = 1;
            if (hasBoundary && sampleCount > 1)
            {
                // Preserve both C0 and C1 continuity at a recycled chunk boundary. The former code reused
                // the exact height but immediately changed the outgoing slope, leaving a hidden normal step
                // at every seam even though the vertices visually met.
                float boundaryDelta = Mathf.Clamp(boundarySlope * dx, -maxDelta, maxDelta);
                surfaceHeights[1] = previousHeight + boundaryDelta;
                previousHeight = surfaceHeights[1];
                previousSlope = boundaryDelta / dx;
                firstGeneratedSample = 2;
            }

            for (int i = firstGeneratedSample; i < sampleCount; i++)
            {
                float desiredHeight = working[i + padding];
                float desiredDelta = Mathf.Clamp(desiredHeight - previousHeight, -maxDelta, maxDelta);
                float desiredSlope = desiredDelta / dx;
                float previousAngle = Mathf.Atan(previousSlope) * Mathf.Rad2Deg;
                float desiredAngle = Mathf.Atan(desiredSlope) * Mathf.Rad2Deg;
                float safeAngle = Mathf.MoveTowards(previousAngle, desiredAngle, profile.maximumCrestSharpness);
                float safeSlope = Mathf.Tan(safeAngle * Mathf.Deg2Rad);
                float height = previousHeight + safeSlope * dx;
                surfaceHeights[i] = height;
                previousHeight = height;
                previousSlope = safeSlope;
            }

            endHeight = previousHeight;
            endSlope = previousSlope;
            float skirtBottom = profile.baseHeight - profile.amplitude *
                (Mathf.Max(1f, profile.maximumDifficulty) + 0.5f) - profile.terrainVisualDepth;
            Vector3[] vertices = new Vector3[sampleCount * 3];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[(sampleCount - 1) * 12];
            for (int i = 0; i < sampleCount; i++)
            {
                float localX = i * dx;
                int vertex = i * 3;
                vertices[vertex] = new Vector3(localX, surfaceHeights[i], -width * 0.5f);
                vertices[vertex + 1] = new Vector3(localX, surfaceHeights[i], width * 0.5f);
                vertices[vertex + 2] = new Vector3(localX, skirtBottom, -width * 0.5f);
                uvs[vertex] = new Vector2(localX * 0.1f, 0f);
                uvs[vertex + 1] = new Vector2(localX * 0.1f, 1f);
                uvs[vertex + 2] = new Vector2(localX * 0.1f, Mathf.Abs(surfaceHeights[i] - skirtBottom) * 0.1f);
            }

            int triangle = 0;
            for (int i = 0; i < sampleCount - 1; i++)
            {
                int vertex = i * 3;
                int next = vertex + 3;
                triangles[triangle++] = vertex;
                triangles[triangle++] = vertex + 1;
                triangles[triangle++] = next;
                triangles[triangle++] = vertex + 1;
                triangles[triangle++] = next + 1;
                triangles[triangle++] = next;
                triangles[triangle++] = vertex;
                triangles[triangle++] = next;
                triangles[triangle++] = vertex + 2;
                triangles[triangle++] = next;
                triangles[triangle++] = next + 2;
                triangles[triangle++] = vertex + 2;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static bool IsTechnicalFeatureCandidate(MapProfile profile, uint seed, int index, float difficulty)
        {
            float chance = Mathf.Clamp01(profile.technicalSectionChance *
                                         Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(difficulty)));
            return Hash01(seed, index * 313 + 17) < chance;
        }

        private static uint CombinedSeed(MapProfile profile)
        {
            uint hash = 2166136261u;
            string id = profile.mapId ?? string.Empty;
            for (int i = 0; i < id.Length; i++)
                hash = (hash ^ id[i]) * 16777619u;
            return hash ^ (uint)profile.seed * 2246822519u;
        }

        private static float Hash01(uint seed, int value)
        {
            uint x = seed ^ (uint)value * 374761393u;
            x = (x ^ (x >> 13)) * 1274126177u;
            x ^= x >> 16;
            return (x & 0x00FFFFFFu) / 16777215f;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
