using System.Collections.Generic;
using AnimeHillRacing.UI;
using UnityEngine;

namespace AnimeHillRacing.Pickups
{
    public sealed class PickupPool : MonoBehaviour
    {
        [System.Serializable]
        public sealed class CoinPoolEntry
        {
            [Min(1)] public int value = 1;
            public CoinPickup prefab;
            [Min(1)] public int initialSize = 32;
        }

        [SerializeField] private CoinPoolEntry[] coinPrefabs;
        [SerializeField] private FuelPickup fuelPrefab;
        [SerializeField, Min(1)] private int initialFuel = 20;
        [SerializeField] private GemPickup gemPrefab;
        [SerializeField, Min(1)] private int initialGems = 4;

        private readonly Dictionary<int, Queue<CoinPickup>> coinsByValue = new Dictionary<int, Queue<CoinPickup>>();
        private readonly Dictionary<int, CoinPickup> coinTemplates = new Dictionary<int, CoinPickup>();
        private readonly HashSet<long> collectedCoinIds = new HashSet<long>();
        private readonly HashSet<long> collectedGemIds = new HashSet<long>();
        private readonly Queue<FuelPickup> fuels = new Queue<FuelPickup>();
        private readonly Queue<GemPickup> gems = new Queue<GemPickup>();
        private readonly List<FuelPickup> activeFuel = new List<FuelPickup>();
        private bool initialized;
        private bool warnedAboutMissingGemArtwork;
        private Texture2D runtimeGemFallbackTexture;
        private Sprite runtimeGemFallbackSprite;

        private void Awake() => Initialize();

        public void Initialize()
        {
            RepairMissingTemplatesFromChildren();
            EnsureGemTemplate();
            if (initialized || coinPrefabs == null || coinPrefabs.Length == 0 || fuelPrefab == null)
                return;
            initialized = true;
            fuelPrefab.gameObject.SetActive(false);
            for (int entryIndex = 0; entryIndex < coinPrefabs.Length; entryIndex++)
            {
                CoinPoolEntry entry = coinPrefabs[entryIndex];
                if (entry == null || entry.prefab == null) continue;
                int value = Mathf.Max(1, entry.value);
                entry.prefab.AssignValue(value);
                entry.prefab.gameObject.SetActive(false);
                coinTemplates[value] = entry.prefab;
                var queue = new Queue<CoinPickup>(Mathf.Max(1, entry.initialSize));
                coinsByValue[value] = queue;
                for (int i = 0; i < entry.initialSize; i++) queue.Enqueue(Create(entry.prefab));
            }
            for (int i = 0; i < initialFuel; i++)
                fuels.Enqueue(Create(fuelPrefab));
            if (gemPrefab != null)
            {
                gemPrefab.gameObject.SetActive(false);
                for (int i = 0; i < initialGems; i++) gems.Enqueue(Create(gemPrefab));
            }
        }

        private void RepairMissingTemplatesFromChildren()
        {
            bool hasCoinOne = false;
            bool hasCoinFive = false;
            if (coinPrefabs != null)
            {
                for (int i = 0; i < coinPrefabs.Length; i++)
                {
                    CoinPoolEntry entry = coinPrefabs[i];
                    if (entry == null || entry.prefab == null) continue;
                    if (entry.value == 1) hasCoinOne = true;
                    else if (entry.value == 5) hasCoinFive = true;
                }
            }
            if (gemPrefab == null) gemPrefab = GetComponentInChildren<GemPickup>(true);
            if (hasCoinOne && hasCoinFive && fuelPrefab != null) return;

            CoinPickup coinOne = null;
            CoinPickup coinFive = null;
            CoinPickup[] coins = GetComponentsInChildren<CoinPickup>(true);
            for (int i = 0; i < coins.Length; i++)
            {
                if (coins[i].Value == 1 && coinOne == null) coinOne = coins[i];
                else if (coins[i].Value == 5 && coinFive == null) coinFive = coins[i];
            }
            if (fuelPrefab == null) fuelPrefab = GetComponentInChildren<FuelPickup>(true);
            if (coinOne != null && coinFive != null)
            {
                coinPrefabs = new[]
                {
                    new CoinPoolEntry { value = 1, prefab = coinOne, initialSize = 96 },
                    new CoinPoolEntry { value = 5, prefab = coinFive, initialSize = 24 }
                };
            }
        }

        private void EnsureGemTemplate()
        {
            if (gemPrefab != null) return;
            HillAdventurerUiCatalog uiAssets = HillAdventurerUiCatalog.Instance;
            Sprite sprite = uiAssets != null ? uiAssets.gemCounterIcon : null;
            if (sprite == null)
            {
                if (!warnedAboutMissingGemArtwork)
                {
                    warnedAboutMissingGemArtwork = true;
                    Debug.LogWarning("Exact 'Gem' artwork is not imported/linked yet; using the runtime " +
                                     "fallback until HillAdventurerUiAssets is populated.");
                }
                sprite = CreateRuntimeGemFallbackSprite();
            }

            GameObject templateObject = new GameObject("Gem Pickup Template");
            templateObject.transform.SetParent(transform, false);
            templateObject.SetActive(false);
            SphereCollider trigger = templateObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = .55f;
            gemPrefab = templateObject.AddComponent<GemPickup>();

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(templateObject.transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 100;
            Vector2 size = sprite.bounds.size;
            float largestSide = Mathf.Max(.01f, size.x, size.y);
            visualObject.transform.localScale = Vector3.one * (1.1f / largestSide);
        }

        private Sprite CreateRuntimeGemFallbackSprite()
        {
            if (runtimeGemFallbackSprite != null) return runtimeGemFallbackSprite;

            const int size = 32;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float normalizedY = (y + .5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float diamondX = Mathf.Abs((x + .5f) / size * 2f - 1f);
                    float diamondY = Mathf.Abs(normalizedY * 2f - 1f);
                    float edgeDistance = diamondX + diamondY;
                    if (edgeDistance > .92f) continue;

                    Color color = Color.Lerp(new Color(.22f, .28f, 1f),
                        new Color(.20f, 1f, .92f), normalizedY);
                    if (x < size / 2 && y > size / 2 && edgeDistance < .58f)
                        color = Color.Lerp(color, Color.white, .62f);
                    color.a = Mathf.InverseLerp(.92f, .72f, edgeDistance);
                    pixels[y * size + x] = color;
                }
            }

            runtimeGemFallbackTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Gem Fallback Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            runtimeGemFallbackTexture.SetPixels32(pixels);
            runtimeGemFallbackTexture.Apply(false, true);
            runtimeGemFallbackSprite = Sprite.Create(runtimeGemFallbackTexture,
                new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size, 0,
                SpriteMeshType.FullRect);
            runtimeGemFallbackSprite.name = "Runtime Gem Fallback";
            runtimeGemFallbackSprite.hideFlags = HideFlags.DontSave;
            return runtimeGemFallbackSprite;
        }

        private void OnDestroy()
        {
            if (runtimeGemFallbackSprite != null)
            {
                if (Application.isPlaying) Destroy(runtimeGemFallbackSprite);
                else DestroyImmediate(runtimeGemFallbackSprite);
            }
            if (runtimeGemFallbackTexture != null)
            {
                if (Application.isPlaying) Destroy(runtimeGemFallbackTexture);
                else DestroyImmediate(runtimeGemFallbackTexture);
            }
        }

        private T Create<T>(T prefab) where T : PickupBase
        {
            T instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public PickupBase Spawn(PickupKind kind, Vector3 position, float progressPosition = float.NaN)
        {
            if (kind == PickupKind.Coin) return SpawnCoin(1, position, progressPosition, 0L);
            if (kind == PickupKind.Gem) return SpawnGem(position, progressPosition, 0L);
            Initialize();
            if (fuelPrefab == null) return null;
            FuelPickup pickup = fuels.Count > 0 ? fuels.Dequeue() : Create(fuelPrefab);
            float progress = float.IsNaN(progressPosition) ? position.x : progressPosition;
            pickup.Spawn(this, position, progress, 0L);
            activeFuel.Add(pickup);
            return pickup;
        }

        public GemPickup SpawnGem(Vector3 position, float progressPosition, long deterministicId)
        {
            Initialize();
            if (gemPrefab == null) return null;
            if (deterministicId != 0L && collectedGemIds.Contains(deterministicId)) return null;
            GemPickup pickup = gems.Count > 0 ? gems.Dequeue() : Create(gemPrefab);
            float progress = float.IsNaN(progressPosition) ? position.x : progressPosition;
            pickup.Spawn(this, position, progress, deterministicId);
            return pickup;
        }

        public CoinPickup SpawnCoin(int value, Vector3 position, float progressPosition, long deterministicId)
        {
            Initialize();
            if (deterministicId != 0L && collectedCoinIds.Contains(deterministicId)) return null;
            int safeValue = Mathf.Max(1, value);
            if (!coinTemplates.TryGetValue(safeValue, out CoinPickup template) || template == null)
            {
                Debug.LogError($"Coin value {safeValue} has no pooled template.");
                return null;
            }
            if (!coinsByValue.TryGetValue(safeValue, out Queue<CoinPickup> queue))
            {
                queue = new Queue<CoinPickup>();
                coinsByValue[safeValue] = queue;
            }
            CoinPickup pickup = queue.Count > 0 ? queue.Dequeue() : Create(template);
            float progress = float.IsNaN(progressPosition) ? position.x : progressPosition;
            pickup.Spawn(this, position, progress, deterministicId);
            return pickup;
        }

        public void NotifyCollected(PickupBase pickup)
        {
            if (pickup != null && pickup.Kind == PickupKind.Coin && pickup.SpawnId != 0L)
                collectedCoinIds.Add(pickup.SpawnId);
            else if (pickup != null && pickup.Kind == PickupKind.Gem && pickup.SpawnId != 0L)
                collectedGemIds.Add(pickup.SpawnId);
        }

        public bool TryGetNextFuelDistance(float vehicleProgress, out float distance)
        {
            distance = float.PositiveInfinity;
            for (int i = activeFuel.Count - 1; i >= 0; i--)
            {
                FuelPickup fuel = activeFuel[i];
                if (fuel == null || !fuel.IsAvailable)
                {
                    activeFuel.RemoveAt(i);
                    continue;
                }
                float delta = fuel.ProgressPosition - vehicleProgress;
                if (delta >= -.5f && delta < distance) distance = Mathf.Max(0f, delta);
            }
            return !float.IsPositiveInfinity(distance);
        }

        public void Release(PickupBase pickup)
        {
            if (pickup == null) return;
            if (pickup.Kind == PickupKind.Fuel) activeFuel.Remove((FuelPickup)pickup);
            if (!pickup.gameObject.activeSelf) return;
            pickup.gameObject.SetActive(false);
            pickup.transform.SetParent(transform, false);
            if (pickup.Kind == PickupKind.Coin)
            {
                CoinPickup coin = (CoinPickup)pickup;
                if (!coinsByValue.TryGetValue(coin.Value, out Queue<CoinPickup> queue))
                {
                    queue = new Queue<CoinPickup>();
                    coinsByValue[coin.Value] = queue;
                }
                queue.Enqueue(coin);
            }
            else if (pickup.Kind == PickupKind.Fuel)
                fuels.Enqueue((FuelPickup)pickup);
            else
                gems.Enqueue((GemPickup)pickup);
        }

        public void AssignPrefabs(CoinPickup coinOne, CoinPickup coinFive, FuelPickup fuel,
            GemPickup gem = null)
        {
            coinPrefabs = new[]
            {
                new CoinPoolEntry { value = 1, prefab = coinOne, initialSize = 96 },
                new CoinPoolEntry { value = 5, prefab = coinFive, initialSize = 24 }
            };
            fuelPrefab = fuel;
            gemPrefab = gem;
        }
    }

    public sealed class ProgressiveFuelSequence
    {
        public float CurrentSpacing { get; private set; }
        public float NextPosition { get; private set; }
        public int SequenceIndex { get; private set; }

        private float spacingIncrease;

        public void Reset(float startPosition, float initialSpacing, float increase)
        {
            CurrentSpacing = Mathf.Max(.01f, initialSpacing);
            spacingIncrease = Mathf.Max(0f, increase);
            NextPosition = startPosition + CurrentSpacing;
            SequenceIndex = 0;
        }

        public void Advance(float actualFuelPosition)
        {
            SequenceIndex++;
            CurrentSpacing += spacingIncrease;
            NextPosition = actualFuelPosition + CurrentSpacing;
        }

        public void AdvancePast(float progress)
        {
            int safety = 0;
            while (NextPosition < progress && safety++ < 100000)
                Advance(NextPosition);
        }
    }
}
