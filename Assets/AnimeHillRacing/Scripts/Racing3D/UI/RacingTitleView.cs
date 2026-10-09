using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingTitleView : MonoBehaviour
    {
        Text label; TitleDefinition definition; float nextPulse;
        public void Configure(Text target, string id, int season, int achievedMmr)
        {
            label = target; definition = RacingTitleCatalog.Resolve(id,season,achievedMmr);
            label.text = definition == null ? "" : definition.displayName;
            if (definition == null) return;
            label.color = definition.primaryColor;
            var shadow = label.GetComponent<Shadow>();
            if (definition.style == RacingTitleStyle.Plain) { if(shadow!=null)shadow.enabled=false; return; }
            if(shadow==null)shadow=label.gameObject.AddComponent<Shadow>();
            shadow.enabled=true; shadow.effectColor=new Color(definition.secondaryColor.r,definition.secondaryColor.g,definition.secondaryColor.b,Mathf.Clamp01(definition.emissionIntensity));
            shadow.effectDistance=new Vector2(1.2f,-1.2f);
        }
        void Update()
        {
            if (definition==null||label==null||definition.animationType!=RacingTitleAnimation.SlowPulse||Time.unscaledTime<nextPulse)return;
            nextPulse=Time.unscaledTime+.1f;
            label.color=Color.Lerp(definition.primaryColor,definition.secondaryColor,(Mathf.Sin(Time.unscaledTime*1.4f)+1)*.12f);
        }
    }
}
