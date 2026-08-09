using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class ShifterUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private VehicleRuntimeBase vehicle;
        [SerializeField] private VehicleInputController input;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Text directionLabel;
        [SerializeField] private GameModeType mode;
        [SerializeField, Min(10f)] private float horizontalShiftThreshold = 65f;
        [SerializeField, Min(10f)] private float verticalShiftThreshold = 65f;
        [SerializeField, Min(1f)] private float centerReturnSpeed = 650f;

        private Vector2 pointerStart;
        private Vector2 visualOffset;
        private bool dragging;
        private bool shiftedThisGesture;
        private int lastGear = 1;
        private float autoKick;

        private bool Horizontal => mode == GameModeType.Adventure2_5D;
        private bool Interactive => input != null && input.InputEnabled && vehicle != null &&
                                    vehicle.Transmission != null && vehicle.Transmission.Mode == TransmissionMode.Manual;

        private void Start()
        {
            if (vehicle == null) vehicle = FindFirstObjectByType<VehicleRuntimeBase>();
            if (input == null) input = FindFirstObjectByType<VehicleInputController>();
            lastGear = vehicle != null && vehicle.Transmission != null ? vehicle.Transmission.CurrentGear : 1;
            if (directionLabel != null)
                directionLabel.text = Horizontal ? "←  SHIFT  →" : "↑\nSHIFT\n↓";
        }

        private void Update()
        {
            if (vehicle == null || vehicle.Transmission == null) return;
            Keyboard keyboard = Keyboard.current;
            if (Interactive && keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame) vehicle.Transmission.RequestShift(1);
                if (keyboard.qKey.wasPressedThisFrame) vehicle.Transmission.RequestShift(-1);
            }

            int gear = vehicle.Transmission.CurrentGear;
            if (gear != lastGear)
            {
                autoKick = gear > lastGear ? 1f : -1f;
                lastGear = gear;
            }
            if (!dragging)
            {
                float axis = autoKick * (Horizontal ? horizontalShiftThreshold : verticalShiftThreshold) * 0.72f;
                Vector2 target = Horizontal ? new Vector2(axis, 0f) : new Vector2(0f, axis);
                visualOffset = Vector2.MoveTowards(visualOffset, target, centerReturnSpeed * Time.unscaledDeltaTime);
                autoKick = Mathf.MoveTowards(autoKick, 0f, Time.unscaledDeltaTime * 7f);
                if (Mathf.Abs(autoKick) < 0.03f)
                    visualOffset = Vector2.MoveTowards(visualOffset, Vector2.zero, centerReturnSpeed * Time.unscaledDeltaTime);
            }
            if (knob != null) knob.anchoredPosition = visualOffset;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactive) return;
            pointerStart = eventData.position;
            shiftedThisGesture = false;
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || !Interactive) return;
            Vector2 delta = eventData.position - pointerStart;
            float axis = Horizontal ? delta.x : delta.y;
            float threshold = Horizontal ? horizontalShiftThreshold : verticalShiftThreshold;
            axis = Mathf.Clamp(axis, -threshold * 1.25f, threshold * 1.25f);
            visualOffset = Horizontal ? new Vector2(axis, 0f) : new Vector2(0f, axis);
            if (!shiftedThisGesture && Mathf.Abs(axis) >= threshold)
            {
                vehicle.Transmission.RequestShift(axis > 0f ? 1 : -1);
                shiftedThisGesture = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
            shiftedThisGesture = false;
        }

        private void OnDisable()
        {
            dragging = false;
            visualOffset = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        public void Assign(VehicleRuntimeBase targetVehicle, VehicleInputController targetInput,
            RectTransform targetKnob, Text label, GameModeType targetMode)
        {
            vehicle = targetVehicle;
            input = targetInput;
            knob = targetKnob;
            directionLabel = label;
            mode = targetMode;
        }

        public void SetVehicle(VehicleRuntimeBase targetVehicle) => vehicle = targetVehicle;
    }
}
