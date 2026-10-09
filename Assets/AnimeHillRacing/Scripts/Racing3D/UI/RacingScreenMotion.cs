using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AnimeHillRacing.UI;

namespace AnimeHillRacing.Racing3D
{
    [DisallowMultipleComponent]
    public sealed class RacingScreenMotion:MonoBehaviour
    {
        CanvasGroup group;float age;
        void Awake(){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();Restart();}
        public void Restart(){age=0;if(group!=null)group.alpha=0;enabled=true;}
        void Update(){age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/.24f);group.alpha=1-Mathf.Pow(1-t,3);transform.localScale=Vector3.one*Mathf.Lerp(.985f,1,group.alpha);if(t>=1)enabled=false;}
    }
}
