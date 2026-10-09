using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingGeneratedMesh : MonoBehaviour
    {
        public Mesh Mesh;
        private void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }
}
