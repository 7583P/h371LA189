using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AnimeHillRacing.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingButtonMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        bool hover,focus,pressed;float value;Graphic graphic;Color baseColor;Button button;Text title;Color titleColor;
        void Awake(){graphic=GetComponent<Graphic>();baseColor=graphic.color;button=GetComponent<Button>();title=GetComponentInChildren<Text>();if(title!=null)titleColor=title.color;}
        public void SetBaseColor(Color color)
        {
            baseColor=color;
            titleColor=color.r*.2126f+color.g*.7152f+color.b*.0722f>.55f?RacingDesign.Background:RacingDesign.Ink;
            if(title!=null)title.color=titleColor;
        }
        void Update()
        {
            float target=button.interactable&&(hover||focus)?1:0;value=Mathf.Lerp(value,target,1-Mathf.Exp(-Time.unscaledDeltaTime*18));
            transform.localScale=Vector3.one*(pressed?.965f:1f+value*.018f);graphic.color=Color.Lerp(baseColor,RacingDesign.Highlight+new Color(.04f,.08f,.08f,0),value);
            if(title!=null)title.color=Color.Lerp(titleColor,RacingDesign.Ink,value);
        }
        public void OnPointerEnter(PointerEventData e){hover=true;RacingFeedback.Play(RacingCue.UiFocus);}
        public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
        public void OnPointerDown(PointerEventData e){pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnSelect(BaseEventData e){focus=true;}
        public void OnDeselect(BaseEventData e){focus=false;pressed=false;}
    }
}
