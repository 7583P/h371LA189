using AnimeHillRacing.Vehicle;
using UnityEngine;

namespace AnimeHillRacing.Pickups
{
    public sealed class FuelPickup : PickupBase
    {
        public override PickupKind Kind => PickupKind.Fuel;

        protected override void Collect(VehicleRuntimeBase vehicle)
        {
            vehicle.RefillFuelFull();
        }
    }
}
