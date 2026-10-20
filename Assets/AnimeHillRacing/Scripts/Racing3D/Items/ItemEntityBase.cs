using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public abstract class ItemEntityBase : MonoBehaviour
    {
        [SerializeField] protected float lifetime = 8f;
        protected RacingVehiclePhysics owner;
        protected float expiresAt, spawnTime;

        public RacingItemPool Pool { get; set; }
        public RaceItemType ItemType { get; private set; }
        public void ConfigureItem(RaceItemType type) { ItemType = type; GetComponent<RacingItemVisualSet>().Show(type, false); }
        public int PoolUseCount { get; internal set; }
        public RacingVehiclePhysics Owner => owner;

        public virtual void Initialize(RacingVehiclePhysics source)
        {
            owner = source;
            spawnTime = Time.time;
            expiresAt = Time.time + lifetime;
        }

        protected void Release()
        {
            if (Pool != null) Pool.Release(this);
            else gameObject.SetActive(false);
        }

        protected virtual void Update()
        {
            if (Time.time >= expiresAt) Release();
        }

        protected virtual void ApplyHit(RacingVehiclePhysics target)
        {
            if (!gameObject.activeSelf || target == null || target == owner) return;
            var inventory = target.GetComponent<ItemInventory>();
            bool isHeavy = ItemType == RaceItemType.ElectroSpike || ItemType==RaceItemType.GoldenRock;
            float spinDuration = isHeavy ? 1.2f : 0.9f;

            bool shielded = inventory != null && inventory.AbsorbHit();
            if (ItemType == RaceItemType.GoldenRock) Pool?.RecordGoldenContact(shielded);
            if (!shielded)
            {
                target.Trigger360Spin(spinDuration, isHeavy);
            }

            RacingFeedback.Play(isHeavy ? RacingCue.RockHit : RacingCue.DartHit, transform.position);
            RacingVfxPool.ImpactBurst(transform.position, ItemType==RaceItemType.GoldenRock?RaceItemUtility.ColorFor(ItemType):isHeavy ? RacingDesign.Secondary : RacingDesign.Cyan, 14);
            Release();
        }
    }

}
