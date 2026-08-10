using AnimeHillRacing.Vehicle;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public enum HoldControl
    {
        Gas,
        Brake,
        Left,
        Right
    }

    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private VehicleInputController input;
        [SerializeField] private HoldControl control;
        [SerializeField] private Image visual;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite pressedSprite;

        private readonly HashSet<int> activePointers = new HashSet<int>();
        private bool pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            activePointers.Add(eventData.pointerId);
            ApplyState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            activePointers.Remove(eventData.pointerId);
            ApplyState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            activePointers.Remove(eventData.pointerId);
            ApplyState();
        }

        private void OnDisable()
        {
            activePointers.Clear();
            Apply(false);
        }

        private void ApplyState() => Apply(activePointers.Count > 0);

        private void Apply(bool value)
        {
            if (pressed == value && (visual == null || visual.sprite == (value ? pressedSprite : normalSprite)))
                return;
            pressed = value;
            if (visual != null)
                visual.sprite = value && pressedSprite != null ? pressedSprite : normalSprite;
            if (input == null) return;
            switch (control)
            {
                case HoldControl.Gas: input.SetTouchGas(value); break;
                case HoldControl.Brake: input.SetTouchBrake(value); break;
                case HoldControl.Left: input.SetTouchLeft(value); break;
                case HoldControl.Right: input.SetTouchRight(value); break;
            }
        }

        public void Assign(VehicleInputController targetInput, HoldControl targetControl,
            Image targetVisual = null, Sprite released = null, Sprite pushed = null)
        {
            input = targetInput;
            control = targetControl;
            visual = targetVisual;
            normalSprite = released;
            pressedSprite = pushed;
            if (visual != null && normalSprite != null) visual.sprite = normalSprite;
        }
    }
}
