using AnimeHillRacing.Pickups;
using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Road
{
    public sealed class RoadTrackGenerator : MonoBehaviour
    {
        [SerializeField] private RoadMapProfile profile;
        [SerializeField] private VehicleRuntimeBase vehicle;
        [SerializeField] private PickupPool pickupPool;
        [SerializeField, Range(1, 5)] private int chunksBehind = 2;
        [SerializeField, Range(3, 12)] private int chunksAhead = 7;

        private RoadTrackChunk[] chunks;
        private int minimumIndex;
        private int maximumIndex;
        private readonly ProgressiveFuelSequence fuelSequence = new ProgressiveFuelSequence();
        public int CurrentChunkIndex { get; private set; }
        public float CurrentDifficulty => profile != null && vehicle != null
            ? profile.Difficulty(vehicle.ProgressPosition)
            : 0f;
        public int Seed => profile != null ? profile.seed : 0;
        public float CurrentFuelSpacing => fuelSequence.CurrentSpacing;
        public float NextFuelSequencePosition => fuelSequence.NextPosition;

        private void Start()
        {
            if (profile == null || vehicle == null)
            {
                enabled = false;
                return;
            }
            pickupPool?.Initialize();
            fuelSequence.Reset(0f, profile.initialFuelGap, profile.fuelGapIncrease);
            CurrentChunkIndex = Mathf.FloorToInt(vehicle.ProgressPosition / profile.chunkLength);
            minimumIndex = CurrentChunkIndex - chunksBehind;
            maximumIndex = CurrentChunkIndex + chunksAhead;
            chunks = new RoadTrackChunk[chunksBehind + chunksAhead + 1];
            for (int i = 0; i < chunks.Length; i++)
            {
                RoadTrackChunk chunk = new GameObject().AddComponent<RoadTrackChunk>();
                chunk.transform.SetParent(transform, false);
                chunks[i] = chunk;
                Generate(chunk, minimumIndex + i);
            }
        }

        private void Update()
        {
            int next = Mathf.FloorToInt(vehicle.ProgressPosition / profile.chunkLength);
            while (next > CurrentChunkIndex)
            {
                CurrentChunkIndex++;
                RoadTrackChunk oldest = Find(minimumIndex);
                minimumIndex++;
                maximumIndex++;
                if (oldest != null) Generate(oldest, maximumIndex);
            }
        }

        private RoadTrackChunk Find(int index)
        {
            if (chunks == null) return null;
            for (int i = 0; i < chunks.Length; i++)
                if (chunks[i] != null && chunks[i].IsGenerated && chunks[i].Index == index)
                    return chunks[i];
            return null;
        }

        private void Generate(RoadTrackChunk chunk, int index)
        {
            chunk.Generate(index, profile, pickupPool);
            if (pickupPool == null || index < 0) return;
            float start = index * profile.chunkLength;
            float end = start + profile.chunkLength;
            SpawnCoins(chunk, start, end);
            SpawnFuel(chunk, start, end);
        }

        private void SpawnCoins(RoadTrackChunk chunk, float start, float end)
        {
            CoinRouteDefinition route = profile != null ? profile.coinRoute : null;
            if (route == null) return;
            int firstCell = route.FirstCellForDistance(start);
            int lastCell = route.LastCellForDistance(end);
            for (int cell = firstCell; cell <= lastCell; cell++)
            {
                if (!route.TryGetGroup(cell, out CoinRouteGroup group)) continue;
                for (int pointIndex = 0; pointIndex < group.coinCount; pointIndex++)
                {
                    CoinRoutePoint point = route.GetPoint(group, pointIndex);
                    float z = group.anchorDistance + point.distanceOffset;
                    if (z < start || z >= end) continue;
                    float x = SampleCenterX(profile, z) + point.lateralOffset;
                    CoinPickup pickup = pickupPool.SpawnCoin(point.value,
                        new Vector3(x, SampleHeight(profile, z) + route.surfaceOffset + point.heightOffset, z), z,
                        route.GetStableCoinId(group, pointIndex));
                    chunk.Track(pickup);
                }
            }
        }

        private void SpawnFuel(RoadTrackChunk chunk, float start, float end)
        {
            fuelSequence.AdvancePast(start);
            int safety = 0;
            while (fuelSequence.NextPosition < end && safety++ < 128)
            {
                float z = fuelSequence.NextPosition;
                float x = SampleCenterX(profile, z);
                chunk.Track(pickupPool.Spawn(PickupKind.Fuel, new Vector3(x, SampleHeight(profile, z) + .68f, z), z));
                fuelSequence.Advance(z);
            }
        }

        public static float SampleCenterX(RoadMapProfile profile, float z)
        {
            float difficulty = profile.Difficulty(Mathf.Max(0f, z));
            if (profile.curvatureProfile != null)
                return profile.curvatureProfile.GetCenterOffsetAtDistance(z, difficulty);
            float phase = profile.seed * 0.017f;
            return (Mathf.Sin(z * profile.curveFrequency + phase) * 0.68f +
                    Mathf.Sin(z * profile.curveFrequency * 0.43f + phase * 1.7f) * 0.32f) *
                   profile.curveIntensity * difficulty;
        }

        public static float SampleHeight(RoadMapProfile profile, float z)
        {
            if (profile.elevationProfile != null)
                return profile.elevationProfile.GetHeightAtDistance(z);
            float difficulty = profile.Difficulty(Mathf.Max(0f, z));
            float phase = profile.seed * 0.011f;
            return (Mathf.Sin(z * profile.elevationFrequency + phase) * 0.7f +
                    Mathf.Sin(z * profile.elevationFrequency * 0.37f + phase * 2.1f) * 0.3f) *
                   profile.elevationIntensity * difficulty;
        }

        public void Assign(RoadMapProfile mapProfile, VehicleRuntimeBase target, PickupPool pool)
        {
            profile = mapProfile;
            vehicle = target;
            pickupPool = pool;
        }
    }
}
