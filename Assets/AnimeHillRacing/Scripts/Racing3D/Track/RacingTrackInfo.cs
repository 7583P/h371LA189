using UnityEngine;
namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingTrackInfo : MonoBehaviour
    {
        public string TrackId = "night_city";
        public string DisplayName = "NIGHT CITY CIRCUIT";
        public string Theme = "Neon city expedition sport";
    }
    public static class RacingLaunchTiming
    {
        // Shared future launch rule. Disabled until timing can be taught and play-tested.
        public static float Evaluate(float heldSeconds)
        {
            if (heldSeconds < .6f || heldSeconds > 1.25f) return 0;
            return Mathf.Clamp01(1f - Mathf.Abs(heldSeconds - .92f) / .32f);
        }
    }
}
