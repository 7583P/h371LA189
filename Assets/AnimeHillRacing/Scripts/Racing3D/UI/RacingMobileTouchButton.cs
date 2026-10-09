using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingMobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Action<bool> onTouchAction;
        private RacingPanelGraphic panel;
        private Color baseColor;
        private Text buttonLabel;
        private Vector3 initialScale;
        private int activePointer=int.MinValue;

        public void Configure(string label, Color accentColor, Action<bool> action)
        {
            onTouchAction = action;
            baseColor = accentColor;
            initialScale = transform.localScale;

            panel = RacingDesign.Panel(transform, label + " Panel", 0, 0, 1, 1, new Color(0.03f, 0.08f, 0.14f, 0.85f), 14f);
            panel.raycastTarget = true;

            // Neon accent border
            var trim = RacingDesign.Panel(panel.transform, "Trim", 0, 0.94f, 1, 0.06f, accentColor, 0f);
            trim.raycastTarget = false;

            buttonLabel = RacingDesign.Label(panel.transform, label, RacingType.Section, 0.08f, 0.18f, 0.84f, 0.64f, accentColor, TextAnchor.MiddleCenter);
            buttonLabel.raycastTarget = false;

            // Subtitle
            string sub = label == "JUMP" ? "HOP / BOOST" : "FIRE ITEM";
            var subLabel = RacingDesign.Label(panel.transform, sub, RacingType.Caption, 0.05f, 0.06f, 0.90f, 0.22f, RacingDesign.Muted, TextAnchor.MiddleCenter);
            subLabel.raycastTarget = false;
        }

        public void BindAction(Action<bool> action) => onTouchAction = action;

        public void OnPointerDown(PointerEventData eventData)
        {
            if(activePointer!=int.MinValue)return;
            activePointer=eventData.pointerId;
            transform.localScale = initialScale * 0.92f;
            if (panel != null) panel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.40f);
            RacingFeedback.Play(RacingCue.UiConfirm);
            onTouchAction?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(activePointer!=eventData.pointerId)return;
            Release();
        }
        void OnDisable(){Release();}
        void Release()
        {
            activePointer=int.MinValue;
            transform.localScale = initialScale;
            if (panel != null) panel.color = new Color(0.03f, 0.08f, 0.14f, 0.85f);
            onTouchAction?.Invoke(false);
        }
    }
}
