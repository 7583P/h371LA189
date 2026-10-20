using System;
using System.Collections.Generic;
// Minimal Unity-free stand-ins for types referenced by the linked pure sources.
namespace AnimeHillRacing.Racing3D
{
    public enum AiRacingStyle { AerialAggressive, DriftPrecision, ShortcutHeavy, ItemTactical, SafeConsistency, Opportunistic, Balanced }
}
namespace UnityEngine
{
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white=>new Color(1,1,1);
    }
}
