using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Opaque full-screen menu panels suspend the showroom hidden underneath them.
    public sealed class RacingMenuOverlay : MonoBehaviour
    {
        static int active;
        bool registered;
        public static bool CoversShowroom => active > 0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => active = 0;
        void OnEnable() { if (!registered) { registered = true; active++; } }
        void OnDisable() { if (registered) { registered = false; active = Mathf.Max(0, active - 1); } }
    }
}
