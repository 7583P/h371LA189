using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimeHillRacing.Racing3D
{
    public sealed class PlayerRacingInput : MonoBehaviour, IVehicleCommandProvider
    {
        [Header("Configuration")]
        [SerializeField] private bool inputEnabled = true;
        [SerializeField, Range(0.5f, 5f)] private float steeringSensitivity = 2.5f;

        // Mobile touch flags
        private bool touchGas;
        private bool touchBrake;
        private bool touchDrift;
        private bool touchItem;
        private float touchSteer;

        private float smoothSteer;
        private bool itemTriggeredThisFrame;

        public bool InputEnabled => inputEnabled;

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                smoothSteer = 0f;
                itemTriggeredThisFrame = false;
            }
        }

        public VehicleCommands GetCommands()
        {
            if (!inputEnabled) return VehicleCommands.Idle;

            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            // 1. Throttle (W, UpArrow, Gamepad R2 / South Button)
            float rawThrottle = 0f;
            if (touchGas) rawThrottle = 1f;
            else if (kb != null && (kb.wKey.isPressed || kb.upArrowKey.isPressed)) rawThrottle = 1f;
            else if (pad != null) rawThrottle = Mathf.Max(pad.rightTrigger.ReadValue(), pad.buttonSouth.isPressed ? 1f : 0f);

            // 2. Brake / Reverse (S, DownArrow, Gamepad L2 / West Button)
            float rawBrake = 0f;
            if (touchBrake) rawBrake = 1f;
            else if (kb != null && (kb.sKey.isPressed || kb.downArrowKey.isPressed)) rawBrake = 1f;
            else if (pad != null) rawBrake = Mathf.Max(pad.leftTrigger.ReadValue(), pad.buttonWest.isPressed ? 1f : 0f);

            // 3. Steering (A/D, Left/Right, Left Stick, Touch)
            float rawSteer = touchSteer;
            if (Mathf.Abs(rawSteer) < 0.01f)
            {
                if (kb != null)
                {
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) rawSteer += 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) rawSteer -= 1f;
                }
                if (pad != null)
                {
                    float stickX = pad.leftStick.x.ReadValue();
                    if (Mathf.Abs(stickX) > 0.1f) rawSteer = stickX;
                }
            }

            smoothSteer = Mathf.MoveTowards(smoothSteer, Mathf.Clamp(rawSteer, -1f, 1f), Time.deltaTime * steeringSensitivity);

            // 4. Drift (Space, Left Shift, Gamepad R1 / Shoulder, Touch)
            bool drift = touchDrift ||
                (kb != null && (kb.spaceKey.isPressed || kb.leftShiftKey.isPressed)) ||
                (pad != null && (pad.rightShoulder.isPressed || pad.buttonEast.isPressed));

            // 5. Use Item (E, Left Ctrl, Enter, Gamepad L1 / North Button, Touch)
            bool useItem = itemTriggeredThisFrame || touchItem ||
                (kb != null && (kb.eKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                (pad != null && (pad.leftShoulder.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame));

            itemTriggeredThisFrame = false;

            // 6. In-air pitch
            float pitch = 0f;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) pitch += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) pitch -= 1f;
            }
            if (pad != null)
            {
                pitch = pad.leftStick.y.ReadValue();
            }

            return new VehicleCommands
            {
                Steering = smoothSteer,
                Throttle = Mathf.Clamp01(rawThrottle),
                Brake = Mathf.Clamp01(rawBrake),
                Drift = drift,
                UseItem = useItem,
                Pitch = Mathf.Clamp(pitch, -1f, 1f)
            };
        }

        // On-screen UI Touch Bindings
        public void SetTouchGas(bool pressed) => touchGas = pressed;
        public void SetTouchBrake(bool pressed) => touchBrake = pressed;
        public void SetTouchDrift(bool pressed) => touchDrift = pressed;
        public void TriggerTouchItem() => touchItem = true;
        public void ReleaseTouchItem() => touchItem = false;
        public void SetTouchSteer(float axis) => touchSteer = Mathf.Clamp(axis, -1f, 1f);

        private void LateUpdate()
        {
            // Clear single-frame touch triggers
            touchItem = false;
        }
    }
}
