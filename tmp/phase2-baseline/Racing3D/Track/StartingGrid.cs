using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class StartingGrid : MonoBehaviour
    {
        [Header("Authored Slots (12 Required)")]
        [SerializeField] private Transform[] gridSlots = new Transform[12];

        [Header("Procedural Fallback Parameters")]
        [SerializeField] private float rowSpacingMeters = 7.0f;
        [SerializeField] private float columnSpacingMeters = 3.2f;

        public int SlotCount => 12;

        public Transform GetSlotTransform(int gridPosition)
        {
            int index = Mathf.Clamp(gridPosition - 1, 0, 11);
            if (gridSlots != null && index < gridSlots.Length && gridSlots[index] != null)
            {
                return gridSlots[index];
            }

            // Fallback: procedural transform relative to this StartingGrid object
            return transform;
        }

        public void GetSlotSpawn(int gridPosition, out Vector3 position, out Quaternion rotation)
        {
            int index = Mathf.Clamp(gridPosition - 1, 0, 11);
            if (gridSlots != null && index < gridSlots.Length && gridSlots[index] != null)
            {
                position = gridSlots[index].position;
                rotation = gridSlots[index].rotation;
                return;
            }

            // Procedural 2-column staggered grid:
            // Row 0: pos 1 (left), pos 2 (right staggered)
            int row = index / 2;
            int col = index % 2; // 0 = left, 1 = right
            float lateralOffset = (col == 0 ? -1f : 1f) * (columnSpacingMeters * 0.5f);
            float longitudinalOffset = -row * rowSpacingMeters - (col == 1 ? (rowSpacingMeters * 0.5f) : 0f);

            position = transform.position + (transform.right * lateralOffset) + (transform.forward * longitudinalOffset) + Vector3.up * 0.3f;
            rotation = transform.rotation;
        }
    }
}
