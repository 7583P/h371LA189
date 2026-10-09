using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingTrafficFlow:MonoBehaviour
    {
        public Vector3 Direction=Vector3.forward;
        public float Speed=28f;
        public float Length=120f;
        public float Phase;
        Vector3 startLocal;
        void Awake(){startLocal=transform.localPosition;gameObject.AddComponent<RacingAnimatedPart>();}
        void Update()
        {
            float offset=Mathf.Repeat(Time.time*Speed+Phase*Length,Length);
            transform.localPosition=startLocal+Direction*offset;
        }
    }
}
