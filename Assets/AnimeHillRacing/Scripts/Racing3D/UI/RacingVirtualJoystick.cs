using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private PlayerRacingInput input;
        private RectTransform baseRect;
        private RectTransform knobRect;
        private Image knobGraphic;
        private Image baseGraphic;
        private Vector2 center;
        private float radius = 75f;
        private Vector2 currentAxis;
        private int activePointer=int.MinValue;

        public void Initialize(PlayerRacingInput playerInput)
        {
            input = playerInput;

            // Background Base Ring
            var baseGo = new GameObject("Joystick Base", typeof(RectTransform), typeof(Image));
            baseRect = (RectTransform)baseGo.transform;
            baseRect.SetParent(transform, false);
            baseRect.anchorMin = new Vector2(0.5f, 0.5f);
            baseRect.anchorMax = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            baseRect.anchoredPosition = Vector2.zero;

            baseGraphic = baseGo.GetComponent<Image>();
            baseGraphic.color = new Color(0.02f, 0.07f, 0.12f, 0.65f);
            baseGraphic.raycastTarget = true;

            // Neon Border Accent
            var ringGo = new GameObject("Joystick Neon Ring", typeof(RectTransform), typeof(Outline));
            var ringRt = (RectTransform)ringGo.transform;
            ringRt.SetParent(baseRect, false);
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = ringRt.offsetMax = Vector2.zero;
            var outline = ringGo.GetComponent<Outline>();
            outline.effectColor = RacingDesign.Cyan;
            outline.effectDistance = new Vector2(2f, -2f);

            // Central Thumb Knob
            var knobGo = new GameObject("Joystick Knob", typeof(RectTransform), typeof(Image));
            knobRect = (RectTransform)knobGo.transform;
            knobRect.SetParent(baseRect, false);
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(58f, 58f);
            knobRect.anchoredPosition = Vector2.zero;

            knobGraphic = knobGo.GetComponent<Image>();
            knobGraphic.color = RacingDesign.Cyan;
            knobGraphic.raycastTarget = false;

            // Label
            var lbl = RacingDesign.Label(baseRect, "STEER / AIR", RacingType.Caption, 0.1f, 0.05f, 0.8f, 0.25f, RacingDesign.Muted, TextAnchor.MiddleCenter);
            lbl.raycastTarget = false;
        }

        public void Bind(PlayerRacingInput playerInput) => input = playerInput;

        public void OnPointerDown(PointerEventData eventData)
        {
            if(activePointer!=int.MinValue)return;
            activePointer=eventData.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out center);
            ProcessDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(activePointer!=eventData.pointerId)return;
            ProcessDrag(eventData);
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
            currentAxis = Vector2.zero;
            if (knobRect != null) knobRect.anchoredPosition = Vector2.zero;
            if (knobGraphic != null) knobGraphic.color = RacingDesign.Cyan;
            if (input != null)
            {
                input.SetTouchSteer(0f);
                input.SetTouchPitch(0f);
                input.SetTouchGas(false);
                input.SetTouchBrake(false);
                input.SetTouchDrift(false);
            }
        }

        private void ProcessDrag(PointerEventData eventData)
        {
            if (baseRect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            Vector2 delta = localPoint;
            float maxDist = radius * 0.95f;
            if (delta.magnitude > maxDist)
            {
                delta = delta.normalized * maxDist;
            }

            if (knobRect != null) knobRect.anchoredPosition = delta;
            currentAxis = new Vector2(delta.x / maxDist, delta.y / maxDist);

            if (knobGraphic != null)
            {
                knobGraphic.color = Color.Lerp(RacingDesign.Cyan, Color.white, currentAxis.magnitude * 0.5f);
            }

            if (input != null)
            {
                input.SetTouchSteer(currentAxis.x);
                input.SetTouchPitch(currentAxis.y);
                input.SetTouchDrift(Mathf.Abs(currentAxis.x)>.72f&&currentAxis.y>.25f);
                // Dual axis: pushing joystick forward applies throttle; pulling back applies brake
                input.SetTouchGas(currentAxis.y > 0.25f);
                input.SetTouchBrake(currentAxis.y < -0.30f);
            }
        }
    }
}
