using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingMobileControls : MonoBehaviour
    {
        [SerializeField] private PlayerRacingInput targetInput;
        private RacingVirtualJoystick joystick;
        private RacingMobileTouchButton jumpButton;
        private RacingMobileTouchButton useButton;
        private RacingMobileTouchButton rearButton;
        private RacingMobileTouchButton driftButton;

        public static RacingMobileControls Create(Transform parent, PlayerRacingInput input)
        {
            var go = new GameObject("Mobile Racing Controls", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var controls = go.AddComponent<RacingMobileControls>();
            controls.targetInput = input;
            controls.BuildUI(rt);
            return controls;
        }

        private void BuildUI(RectTransform root)
        {
            // LEFT SIDE: Virtual Steering & Throttle Joystick
            var joyContainer = RacingDesign.Area(root, "Joystick Container", 0.04f, 0.05f, 0.22f, 0.35f);
            joystick = joyContainer.gameObject.AddComponent<RacingVirtualJoystick>();
            joystick.Initialize(targetInput);

            // RIGHT SIDE: JUMP & USE Action Buttons
            // JUMP Button (Bottom-Right outer: hops on ground, triggers aerial boost when airborne)
            var jumpArea = RacingDesign.Area(root, "Jump Button Container", 0.85f, 0.06f, 0.11f, 0.18f);
            jumpButton = jumpArea.gameObject.AddComponent<RacingMobileTouchButton>();
            jumpButton.Configure("JUMP", RacingDesign.Gold, 
                pressed => targetInput?.SetTouchJump(pressed));

            // USE Button (Inner right: activates items)
            var useArea = RacingDesign.Area(root, "Use Button Container", 0.72f, 0.06f, 0.11f, 0.18f);
            useButton = useArea.gameObject.AddComponent<RacingMobileTouchButton>();
            useButton.Configure("USE", RacingDesign.Cyan,
                pressed => { if (pressed) targetInput?.TriggerTouchItem(); else targetInput?.ReleaseTouchItem(); });
            var rearArea=RacingDesign.Area(root,"Rear view button",.87f,.31f,.085f,.085f);
            rearButton=rearArea.gameObject.AddComponent<RacingMobileTouchButton>();
            rearButton.Configure("REAR",RacingDesign.Cyan,pressed=>targetInput?.SetTouchLookBack(pressed));
            var driftArea=RacingDesign.Area(root,"Drift button",.72f,.30f,.11f,.12f);
            driftButton=driftArea.gameObject.AddComponent<RacingMobileTouchButton>();
            driftButton.Configure("HOP / DRIFT",RacingDesign.Gold,pressed=>targetInput?.SetTouchDrift(pressed));
        }

        public void Bind(PlayerRacingInput input)
        {
            targetInput = input;
            if (joystick != null) joystick.Bind(input);
            if (jumpButton != null) jumpButton.BindAction(pressed => targetInput?.SetTouchJump(pressed));
            if (useButton != null) useButton.BindAction(pressed => { if (pressed) targetInput?.TriggerTouchItem(); else targetInput?.ReleaseTouchItem(); });
            if (rearButton != null) rearButton.BindAction(pressed=>targetInput?.SetTouchLookBack(pressed));
            if (driftButton != null) driftButton.BindAction(pressed=>targetInput?.SetTouchDrift(pressed));
        }
    }




}
