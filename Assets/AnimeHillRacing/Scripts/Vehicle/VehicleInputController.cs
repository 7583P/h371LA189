using UnityEngine;
using UnityEngine.InputSystem;
using AnimeHillRacing.Core;

namespace AnimeHillRacing.Vehicle
{
    public struct VehicleInputState
    {
        public float Gas;
        public float BrakeRaw;
        public float BrakeHeld;
        public bool BrakeTap;
        public float Steer;
    }

    [DefaultExecutionOrder(-100)]
    public sealed class VehicleInputController : MonoBehaviour
    {
        [SerializeField, Range(0.05f, 0.4f)] private float tapThreshold = 0.18f;
        [SerializeField] private GameModeType inputMode = GameModeType.Adventure2_5D;

        private bool touchGas;
        private bool touchBrake;
        private bool touchLeft;
        private bool touchRight;
        private bool previousBrake;
        private float brakePressedAt;
        private bool brakeTapPending;
        private bool inputEnabled = true;

        public bool InputEnabled => inputEnabled;
        public float RawThrottleInput => State.Gas;
        public float RawBrakeInput => State.BrakeRaw;
        public float RawSteeringInput => State.Steer;

        public VehicleInputState State { get; private set; }
        public bool RetryPressedThisFrame { get; private set; }
        public bool ToggleModePressedThisFrame { get; private set; }
        public bool ToggleDebugPressedThisFrame { get; private set; }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool road = inputMode != GameModeType.Adventure2_5D;
            bool keyboardGas = keyboard != null && (road
                ? keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed
                : keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed);
            bool keyboardBrake = keyboard != null && (road
                ? keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed
                : keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
            float keyboardSteer = road && keyboard != null
                ? (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                  (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f)
                : 0f;
            bool gas = inputEnabled && (touchGas || keyboardGas);
            bool brake = inputEnabled && (touchBrake || keyboardBrake);

            if (brake && !previousBrake)
                brakePressedAt = Time.unscaledTime;
            else if (!brake && previousBrake && Time.unscaledTime - brakePressedAt <= tapThreshold)
            {
                brakeTapPending = true;
            }

            float heldTime = brake ? Time.unscaledTime - brakePressedAt : 0f;
            State = new VehicleInputState
            {
                Gas = gas ? 1f : 0f,
                BrakeRaw = brake ? 1f : 0f,
                BrakeHeld = brake && heldTime > tapThreshold ? 1f : 0f,
                BrakeTap = brakeTapPending,
                Steer = inputEnabled ? Mathf.Clamp(keyboardSteer + (touchRight ? 1f : 0f) - (touchLeft ? 1f : 0f), -1f, 1f) : 0f
            };

            RetryPressedThisFrame = keyboard != null && keyboard.rKey.wasPressedThisFrame;
            ToggleModePressedThisFrame = keyboard != null && keyboard.mKey.wasPressedThisFrame;
            ToggleDebugPressedThisFrame = keyboard != null && keyboard.f3Key.wasPressedThisFrame;
            previousBrake = brake;
        }

        public void SetTouchGas(bool pressed) => touchGas = pressed;
        public void SetTouchBrake(bool pressed) => touchBrake = pressed;
        public void SetTouchLeft(bool pressed) => touchLeft = pressed;
        public void SetTouchRight(bool pressed) => touchRight = pressed;
        public void SetInputMode(GameModeType mode) => inputMode = mode;

        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
            if (value) return;
            touchGas = touchBrake = touchLeft = touchRight = false;
            brakeTapPending = false;
            State = default;
        }

        public bool ConsumeBrakeTap()
        {
            bool value = brakeTapPending;
            brakeTapPending = false;
            return value;
        }
    }
}
