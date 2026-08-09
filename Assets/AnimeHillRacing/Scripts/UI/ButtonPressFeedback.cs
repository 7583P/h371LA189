using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler
    {
        [SerializeField, Range(.90f, 1f)] private float pressedScale = .96f;
        private Vector3 restingScale;
        private Button button;

        private void Awake()
        {
            restingScale = transform.localScale;
            button = GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null && button.interactable) transform.localScale = restingScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Restore();
        public void OnPointerExit(PointerEventData eventData) => Restore();
        private void OnDisable() => Restore();

        private void Restore()
        {
            if (restingScale == Vector3.zero) restingScale = Vector3.one;
            transform.localScale = restingScale;
        }
    }
}
