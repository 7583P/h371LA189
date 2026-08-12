using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Core
{
    public static class FrameRateBootstrap
    {
        public const int MinimumTargetFrameRate = 60;
        public const int MaximumTargetFrameRate = 120;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            // Mobile ignores vSyncCount and commonly defaults to 30 FPS unless targetFrameRate
            // is explicit. Requesting 120 lets Android select 60/90/120 frame pacing according
            // to the display mode while keeping the game capped on faster panels.
            QualitySettings.vSyncCount = 0;
            OnDemandRendering.renderFrameInterval = 1;
            Application.targetFrameRate = MaximumTargetFrameRate;
        }
    }
}
