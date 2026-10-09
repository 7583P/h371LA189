using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingHoldControl:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public System.Action<bool> Action;
        int pointer=int.MinValue;
        public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;Action?.Invoke(true);}
        public void OnPointerUp(PointerEventData e){if(e.pointerId!=pointer)return;pointer=int.MinValue;Action?.Invoke(false);}
        public void OnPointerExit(PointerEventData e){OnPointerUp(e);}
        void OnDisable(){pointer=int.MinValue;Action?.Invoke(false);}
    }
}
