using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;

namespace AnimeHillRacing.Pickups
{
    public sealed class CoinPickup : PickupBase
    {
        [UnityEngine.SerializeField, UnityEngine.Min(1)] private int value = 1;
        private GameManager manager;
        public override PickupKind Kind => PickupKind.Coin;
        public int Value => value;

        private void Awake() => manager = FindFirstObjectByType<GameManager>();

        public void AssignValue(int coinValue) => value = UnityEngine.Mathf.Max(1, coinValue);

        protected override void Collect(VehicleRuntimeBase vehicle)
        {
            manager ??= UnityEngine.Object.FindFirstObjectByType<GameManager>();
            manager?.AddCoin(value);
        }
    }
}
