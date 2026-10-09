using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public sealed class TrapItemEntity : ItemEntityBase
    {
        private Vector3 velocity;
        private bool isRolling;
        private static readonly Collider[] sphereHits = new Collider[16];

        public void LaunchForward(Vector3 forwardDir)
        {
            isRolling = true;
            velocity = forwardDir * 36f + Vector3.up * 1.5f;
        }

        public void DropStationary()
        {
            isRolling = false;
            velocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (!gameObject.activeSelf) return;

            if (ItemType == RaceItemType.PulseMine && Time.time - spawnTime > .40f) {
                int near = Physics.OverlapSphereNonAlloc(transform.position, 2f, sphereHits, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < near; i++) {
                    var racer = sphereHits[i].GetComponentInParent<RacingVehiclePhysics>();
                    if (racer != null && racer != owner) { Pulse(); return; }
                }
            }
            if (isRolling)
            {
                float dt = Time.fixedDeltaTime;
                velocity.y -= 22f * dt;
                velocity.x *= Mathf.Clamp01(1f - dt * 0.6f);
                velocity.z *= Mathf.Clamp01(1f - dt * 0.6f);

                Vector3 nextPos = transform.position + velocity * dt;
                if (Physics.Raycast(nextPos + Vector3.up * 1.5f, Vector3.down, out var groundHit, 3.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (nextPos.y <= groundHit.point.y + 0.48f)
                    {
                        nextPos.y = groundHit.point.y + 0.48f;
                        velocity.y = Mathf.Max(0f, -velocity.y * 0.25f);
                    }
                }

                transform.position = nextPos;
                transform.Rotate(Vector3.right, 580f * dt, Space.Self);

                if (velocity.sqrMagnitude < 3f)
                {
                    isRolling = false;
                }

                int count = Physics.OverlapSphereNonAlloc(transform.position, 0.7f, sphereHits, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var racer = sphereHits[i].GetComponentInParent<RacingVehiclePhysics>();
                    if (racer != null && racer != owner)
                    {
                        ApplyHit(racer);
                        return;
                    }
                }
            }
        }

        private readonly RacingVehiclePhysics[] pulseVictims = new RacingVehiclePhysics[12];
        void Pulse() {
            int count = Physics.OverlapSphereNonAlloc(transform.position, 4.5f, sphereHits, ~0, QueryTriggerInteraction.Ignore), visited=0;
            for (int i=0;i<count;i++) {
                var racer=sphereHits[i].GetComponentInParent<RacingVehiclePhysics>();
                if (racer==null || racer==owner || !racer.RaceEnabled) continue;
                bool duplicate=false;for(int j=0;j<visited;j++)duplicate|=pulseVictims[j]==racer;
                if(duplicate)continue;if(visited<pulseVictims.Length)pulseVictims[visited++]=racer;
                var shield=racer.GetComponent<ItemInventory>();
                if(shield==null || !shield.AbsorbHit())racer.Trigger360Spin(.75f,false);
            }
            System.Array.Clear(pulseVictims,0,pulseVictims.Length);
            RacingVfxPool.ImpactBurst(transform.position,RaceItemUtility.ColorFor(ItemType),14);Release();
        }

        private void OnTriggerStay(Collider other)
        {
            if (Time.time - spawnTime < 0.25f || ItemType == RaceItemType.PulseMine) return;
            var racer = other.GetComponentInParent<RacingVehiclePhysics>();
            if (racer != null && racer != owner) ApplyHit(racer);
        }
    }}
