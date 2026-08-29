using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;

namespace AnimeHillRacing.Pickups
{
    public sealed class GemPickup : PickupBase
    {
        private GameManager manager;

        public override PickupKind Kind => PickupKind.Gem;

        private void Awake() => manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();

        protected override void Collect(VehicleRuntimeBase vehicle)
        {
            if (manager == null) manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (manager != null)
                manager.AddGem(1);
            else
                SaveManager.AddGems(1);
        }
    }
}
