using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingEnvironmentMotion:MonoBehaviour
    {
        public bool Water;Quaternion rotation;Vector3 scale;
        void Awake(){rotation=transform.localRotation;scale=transform.localScale;gameObject.AddComponent<RacingAnimatedPart>();}
        void Update(){if(Water)transform.localScale=new Vector3(scale.x*(1+Mathf.Sin(Time.time*2.7f)*.015f),scale.y,scale.z);else transform.localRotation=rotation*Quaternion.Euler(Mathf.Sin(Time.time*2.1f+transform.position.x)*3,0,Mathf.Sin(Time.time*3.3f+transform.position.z)*4);}
    }
}
