using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimeHillRacing.Racing3D
{
    public sealed class PlayerRacingInput : MonoBehaviour, IVehicleCommandProvider
    {
        [Header("Configuration")]
        [SerializeField] private bool inputEnabled = true;
        [SerializeField, Range(2f, 30f)] private float steeringSensitivity = 18f;
        private VehicleCommands sampledCommands;
        private bool pendingItem;
        private bool pendingItemBackward;
        private bool pendingJump;
        private bool pendingTrick;
        private bool pendingMiniHop, wasPreDriftHeld;

        // Mobile touch flags
        private bool touchGas;
        private bool touchBrake;
        private bool touchDrift;
        private bool touchItem;
        private bool touchJump;
        private float touchSteer;
        private float touchPitch;
        private bool touchLookBack;

        private float smoothSteer;
        private bool itemTriggeredThisFrame;
        private bool jumpTriggeredThisFrame;

        public bool InputEnabled => inputEnabled;
        public bool LookBack { get; private set; }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                smoothSteer = 0f;
                itemTriggeredThisFrame = false;
                jumpTriggeredThisFrame = false;
                pendingItem = false;
                pendingItemBackward = false;
                pendingJump = false;
                pendingTrick = false;
                pendingMiniHop = wasPreDriftHeld = false;
                touchGas = touchBrake = touchDrift = touchItem = touchJump = false;
                touchSteer = touchPitch = 0f;
                touchLookBack = false;
                LookBack = false;
            }
        }

        public VehicleCommands GetCommands()
        {
            if (!inputEnabled) return VehicleCommands.Idle;
            if (!Application.isPlaying) return SampleCommands();
            var commands = sampledCommands;
            commands.UseItem = pendingItem;
            commands.ItemBackward = pendingItemBackward;
            commands.Jump = pendingJump;
            commands.Trick = pendingTrick;
            commands.MiniHop = pendingMiniHop;
            pendingItem = false;
            pendingItemBackward = false;
            pendingJump = false;
            pendingTrick = false;
            pendingMiniHop = false;
            return commands;
        }

        private void Update()
        {
            sampledCommands = SampleCommands();
            pendingItem |= sampledCommands.UseItem;
            pendingItemBackward |= sampledCommands.ItemBackward;
            pendingJump |= sampledCommands.Jump;
            pendingTrick |= sampledCommands.Trick;
            pendingMiniHop |= sampledCommands.MiniHop;
        }

        public VehicleCommands SampleCommands()
        {
            if (!inputEnabled) return VehicleCommands.Idle;

            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            Mouse mouse = Mouse.current;
            LookBack = touchLookBack || (mouse != null && mouse.rightButton.isPressed) ||
                (pad != null && pad.rightStickButton.isPressed);

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

            float dt = Time.deltaTime > 0.0001f ? Time.deltaTime : 0.05f;
            smoothSteer = Mathf.MoveTowards(smoothSteer, Mathf.Clamp(rawSteer, -1f, 1f), dt * steeringSensitivity);

            // 4. Shift drift; retain existing keyboard/gamepad alternatives.
            bool drift = touchDrift ||
                (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed || kb.spaceKey.isPressed || kb.cKey.isPressed)) ||
                (pad != null && (pad.rightShoulder.isPressed || pad.buttonEast.isPressed));
            bool preDriftHeld = touchDrift ||
                (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) ||
                (pad != null && pad.buttonEast.isPressed);
            bool miniHop = preDriftHeld && !wasPreDriftHeld;
            wasPreDriftHeld = preDriftHeld;

            var physics = GetComponent<RacingVehiclePhysics>();
            bool isGrounded = physics != null && physics.IsGrounded;

            // 5. Use Item (E on ground, F, Left Ctrl, Enter, Gamepad L1 / North Button, Touch)
            bool useItem = itemTriggeredThisFrame || touchItem || (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                (isGrounded && kb != null && kb.eKey.wasPressedThisFrame) ||
                (kb != null && (kb.fKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                (pad != null && (pad.leftShoulder.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame));

            // RMB+W requests the rear direction without consuming throttle or
            // auto-firing. Only a use-button rising edge activates an item.
            bool itemBackward = useItem && (LookBack || rawBrake > 0.25f ||
                (kb != null && (kb.sKey.isPressed || kb.downArrowKey.isPressed)) ||
                (pad != null && pad.leftStick.y.ReadValue() < -0.3f));

            itemTriggeredThisFrame = false;
            touchItem = false;

            // 6. Jump and Air Trick
            bool jumpOrTrick = jumpTriggeredThisFrame || touchJump ||
                (kb != null && (kb.jKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.leftAltKey.wasPressedThisFrame)) ||
                (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame));

            jumpTriggeredThisFrame = false;

            bool jump = jumpOrTrick && isGrounded;
            bool trick = jumpOrTrick && !isGrounded;

            // 7. In-air pitch (W / S or Left Stick Y)
            float pitch = touchPitch;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) pitch += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) pitch -= 1f;
            }
            if (pad != null)
            {
                if(Mathf.Abs(pad.leftStick.y.ReadValue())>.1f) pitch = pad.leftStick.y.ReadValue();
            }

            // 8. PC Air Roll (Q = Air Roll Left, E = Air Roll Right) - ONLY active while airborne!
            float airRoll = 0f;
            if (!isGrounded)
            {
                airRoll=touchSteer;
                if (kb != null)
                {
                    if (kb.qKey.isPressed) airRoll -= 1f;
                    if (kb.eKey.isPressed) airRoll += 1f;
                }
                if (pad != null)
                {
                    if (pad.leftShoulder.isPressed) airRoll -= 1f;
                    if (pad.rightShoulder.isPressed) airRoll += 1f;
                }
            }

            // 9. Aerial Boost (Space or B, Gamepad South/Trigger, or Touch) - ONLY while airborne!
            bool aerialBoost = !isGrounded && (touchJump ||
                (kb != null && (kb.spaceKey.isPressed || kb.bKey.isPressed)) ||
                (pad != null && (pad.buttonSouth.isPressed || pad.rightTrigger.isPressed || pad.buttonWest.isPressed)));

            return new VehicleCommands
            {
                Steering = smoothSteer,
                Throttle = Mathf.Clamp01(rawThrottle),
                Brake = Mathf.Clamp01(rawBrake),
                Drift = drift,
                MiniHop = miniHop,
                UseItem = useItem,
                ItemBackward = itemBackward,
                Jump = jump,
                Trick = trick,
                AerialBoost = aerialBoost,
                Pitch = Mathf.Clamp(pitch, -1f, 1f),
                AirRoll = Mathf.Clamp(airRoll, -1f, 1f)
            };
        }

        // On-screen UI Touch Bindings
        public void SetTouchGas(bool pressed) => touchGas = pressed;
        public void SetTouchBrake(bool pressed) => touchBrake = pressed;
        public void SetTouchDrift(bool pressed) => touchDrift = pressed;
        public void SetTouchJump(bool pressed) { touchJump = pressed; if (pressed) jumpTriggeredThisFrame = true; }
        public void TriggerTouchJump() => jumpTriggeredThisFrame = inputEnabled;
        public void ReleaseTouchJump() => touchJump = false;
        public void TriggerTouchItem() { itemTriggeredThisFrame = inputEnabled; touchItem = inputEnabled; }
        public void ReleaseTouchItem() => touchItem = false;
        public void SetTouchSteer(float axis) => touchSteer = Mathf.Clamp(axis, -1f, 1f);
        public void SetTouchPitch(float axis) => touchPitch = Mathf.Clamp(axis, -1f, 1f);
        public void SetTouchLookBack(bool pressed) => touchLookBack = pressed && inputEnabled;

        private void LateUpdate()
        {
            // Clear single-frame touch triggers
            touchItem = false;
        }
        private void OnApplicationFocus(bool focused) { if (!focused) SetInputEnabled(false); else SetInputEnabled(GetComponent<RacingVehiclePhysics>()?.RaceEnabled == true); }
    }
}
