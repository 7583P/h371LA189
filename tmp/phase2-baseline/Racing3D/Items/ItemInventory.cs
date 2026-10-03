using System;
using System.Collections;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    [RequireComponent(typeof(RacingVehiclePhysics))]
    public sealed class ItemInventory : MonoBehaviour
    {
        [Header("Prefabs (Optional placeholders created procedurally if unassigned)")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject trapPrefab;

        private RacingVehiclePhysics vehiclePhysics;
        private RaceItemType heldItem = RaceItemType.None;
        private bool isRolling;
        private bool shieldActive;
        private float shieldTimer;

        public RaceItemType HeldItem => heldItem;
        public bool HasItem => heldItem != RaceItemType.None && !isRolling;
        public bool IsRolling => isRolling;
        public bool HasShield => shieldActive;

        public event Action<RaceItemType> OnItemReceived;
        public event Action<RaceItemType> OnItemUsed;

        private void Awake()
        {
            vehiclePhysics = GetComponent<RacingVehiclePhysics>();
        }

        private void Update()
        {
            if (shieldActive)
            {
                shieldTimer -= Time.deltaTime;
                if (shieldTimer <= 0f)
                {
                    shieldActive = false;
                }
            }

            // Check if command provider wants to use item
            if (vehiclePhysics != null && vehiclePhysics.LastCommands.UseItem)
            {
                UseItem();
            }
        }

        public void AcquireItem(RaceItemType itemType, bool rollRoulette = true)
        {
            if (heldItem != RaceItemType.None || isRolling) return;

            if (rollRoulette)
            {
                StartCoroutine(RouletteRoutine(itemType));
            }
            else
            {
                heldItem = itemType;
                OnItemReceived?.Invoke(heldItem);
            }
        }

        private IEnumerator RouletteRoutine(RaceItemType finalItem)
        {
            isRolling = true;
            yield return new WaitForSeconds(0.85f);
            heldItem = finalItem;
            isRolling = false;
            OnItemReceived?.Invoke(heldItem);
        }

        public bool UseItem()
        {
            if (!HasItem || vehiclePhysics == null) return false;

            RaceItemType used = heldItem;
            heldItem = RaceItemType.None;

            switch (used)
            {
                case RaceItemType.TurboPulse:
                    vehiclePhysics.ApplySpeedBoost(1.4f, 2.5f);
                    break;

                case RaceItemType.HyperCharge:
                    vehiclePhysics.ApplySpeedBoost(1.75f, 4.0f);
                    shieldActive = true;
                    shieldTimer = 4.0f;
                    break;

                case RaceItemType.MagneticShield:
                    shieldActive = true;
                    shieldTimer = 6.0f;
                    break;

                case RaceItemType.ElectroSpike:
                    SpawnTrap();
                    break;

                case RaceItemType.KineticMissile:
                    SpawnMissile();
                    break;

                case RaceItemType.ShockwavePulse:
                    TriggerShockwave();
                    break;
            }

            OnItemUsed?.Invoke(used);
            return true;
        }

        private void SpawnTrap()
        {
            Vector3 spawnPos = transform.position - transform.forward * 2.2f + Vector3.up * 0.3f;
            GameObject trapObj;
            if (trapPrefab != null)
            {
                trapObj = Instantiate(trapPrefab, spawnPos, transform.rotation);
            }
            else
            {
                // Procedural placeholder trap
                trapObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trapObj.transform.position = spawnPos;
                trapObj.transform.localScale = new Vector3(1.2f, 0.25f, 1.2f);
                var renderer = trapObj.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.9f, 0.2f, 0.1f, 1f);
                trapObj.AddComponent<TrapItemEntity>();
            }

            var entity = trapObj.GetComponent<TrapItemEntity>();
            if (entity != null) entity.Initialize(vehiclePhysics);
        }

        private void SpawnMissile()
        {
            Vector3 spawnPos = transform.position + transform.forward * 2.2f + Vector3.up * 0.6f;
            GameObject missileObj;
            if (projectilePrefab != null)
            {
                missileObj = Instantiate(projectilePrefab, spawnPos, transform.rotation);
            }
            else
            {
                // Procedural placeholder missile
                missileObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                missileObj.transform.position = spawnPos;
                missileObj.transform.rotation = transform.rotation * Quaternion.Euler(90f, 0f, 0f);
                missileObj.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
                var renderer = missileObj.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.1f, 0.8f, 1f, 1f);
                missileObj.AddComponent<ProjectileItemEntity>();
            }

            var proj = missileObj.GetComponent<ProjectileItemEntity>();
            if (proj != null) proj.Initialize(vehiclePhysics);
        }

        private void TriggerShockwave()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 8.0f);
            for (int i = 0; i < hits.Length; i++)
            {
                var rival = hits[i].GetComponentInParent<RacingVehiclePhysics>();
                if (rival != null && rival != vehiclePhysics)
                {
                    Vector3 pushDir = (rival.transform.position - transform.position).normalized + Vector3.up * 0.3f;
                    rival.Body.AddForce(pushDir * 12f, ForceMode.VelocityChange);
                }
            }
        }
    }
}
