using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using AnimeHillRacing.UI;
using AnimeHillRacing.Ads;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingSafeArea:MonoBehaviour
    {
        Rect previous;int width,height;float previousBanner;
        void LateUpdate()
        {
            Rect safe=Screen.safeArea;float banner=AdMobManager.ReservedBottomPixels;if(safe==previous&&width==Screen.width&&height==Screen.height&&banner==previousBanner)return;
            previous=safe;width=Screen.width;height=Screen.height;previousBanner=banner;
            var rt=(RectTransform)transform;rt.anchorMin=new Vector2(safe.x/Mathf.Max(1,width),Mathf.Max(safe.y,banner)/Mathf.Max(1,height));rt.anchorMax=new Vector2(safe.xMax/Mathf.Max(1,width),safe.yMax/Mathf.Max(1,height));rt.offsetMin=rt.offsetMax=Vector2.zero;
        }
    }
}
