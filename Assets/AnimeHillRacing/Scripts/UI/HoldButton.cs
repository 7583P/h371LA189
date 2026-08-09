using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.EventSystems;

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

        public void OnPointerDown(PointerEventData eventData) => Set(true);
        public void OnPointerUp(PointerEventData eventData) => Set(false);
        public void OnPointerExit(PointerEventData eventData) => Set(false);

        private void OnDisable() => Set(false);

        private void Set(bool pressed)
        {
            if (input == null)
                return;
            switch (control)
            {
                case HoldControl.Gas: input.SetTouchGas(pressed); break;
                case HoldControl.Brake: input.SetTouchBrake(pressed); break;
                case HoldControl.Left: input.SetTouchLeft(pressed); break;
                case HoldControl.Right: input.SetTouchRight(pressed); break;
            }
        }

        public void Assign(VehicleInputController targetInput, HoldControl targetControl)
        {
            input = targetInput;
            control = targetControl;
        }
    }
}
