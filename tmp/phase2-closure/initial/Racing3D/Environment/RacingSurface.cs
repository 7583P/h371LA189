using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RacingSurfaceKind { Road, Dirt, Grass, Metal, Boost, Danger }
    public sealed class RacingSurface : MonoBehaviour
    {
        public RacingSurfaceKind Kind = RacingSurfaceKind.Road;
        public bool IsOffroad => Kind == RacingSurfaceKind.Dirt || Kind == RacingSurfaceKind.Grass;
    }
}
