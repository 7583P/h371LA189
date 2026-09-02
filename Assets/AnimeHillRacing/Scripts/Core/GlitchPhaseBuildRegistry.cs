using UnityEngine;

namespace AnimeHillRacing.Core
{
    /// <summary>
    /// Resources-backed dependency root. Its generated asset prevents Android build stripping from
    /// omitting Glitch Phase sprites that are selected only through data-driven customization.
    /// </summary>
    public sealed class GlitchPhaseBuildRegistry : ScriptableObject
    {
        public Sprite[] sprites;
        public Shader crystalTideShader;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RetainBuildDependencies()
        {
            Resources.Load<GlitchPhaseBuildRegistry>("GlitchPhaseBuildRegistry");
        }
    }
}
