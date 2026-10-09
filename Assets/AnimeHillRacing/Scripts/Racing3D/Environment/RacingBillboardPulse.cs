using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingBillboardPulse:MonoBehaviour
    {
        MeshRenderer display;MaterialPropertyBlock block;Color color;float next;
        void Awake(){gameObject.AddComponent<RacingAnimatedPart>();display=GetComponent<MeshRenderer>();color=display.sharedMaterial.color;block=new MaterialPropertyBlock();display.shadowCastingMode=ShadowCastingMode.Off;}
        void Update(){if(Time.time<next)return;next=Time.time+.2f;block.SetColor("_EmissionColor",color*(.4f+.2f*Mathf.Sin(Time.time*.8f+transform.position.x)));display.SetPropertyBlock(block);}
    }
}
