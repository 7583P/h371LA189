using System;
using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingCoverInteractionMotion : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        private Graphic targetGraphic;
        private Color hoverColor;
        private Color pressColor;
        private bool isHovered;
        private bool isFocused;
        private bool isPressed;
        private float currentGlowAlpha;
        private Vector3 initialScale;

        public void Configure(Graphic graphic, Color hoverGlow, Color pressGlow)
        {
            targetGraphic = graphic;
            hoverColor = hoverGlow;
            pressColor = pressGlow;
            initialScale = transform.localScale;
            if (targetGraphic != null)
            {
                var clearColor = hoverColor;
                clearColor.a = 0f;
                targetGraphic.color = clearColor;
            }
        }

        private void Update()
        {
            bool active = isHovered || isFocused;
            float targetAlpha = isPressed ? pressColor.a : (active ? hoverColor.a : 0f);
            float scaleMultiplier = isPressed ? 0.975f : (active ? 1.018f : 1.0f);

            currentGlowAlpha = Mathf.Lerp(currentGlowAlpha, targetAlpha, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 22f));

            if (targetGraphic != null)
            {
                Color baseCol = isPressed ? pressColor : hoverColor;
                baseCol.a = currentGlowAlpha;
                targetGraphic.color = baseCol;
            }

            Vector3 targetScale = initialScale * scaleMultiplier;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 22f));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            RacingFeedback.Play(RacingCue.UiFocus);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            isFocused = true;
            RacingFeedback.Play(RacingCue.UiFocus);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isFocused = false;
            isPressed = false;
        }
    }
}
