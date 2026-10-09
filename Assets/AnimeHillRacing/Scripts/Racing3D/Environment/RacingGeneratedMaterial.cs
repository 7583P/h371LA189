using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingGeneratedMaterial : MonoBehaviour
    {
        public Material Material;
        void OnDestroy(){if(Material!=null)Destroy(Material);}
    }
}
