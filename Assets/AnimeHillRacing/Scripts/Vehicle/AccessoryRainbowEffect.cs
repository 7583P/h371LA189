using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    /// <summary>Supplies the shared animated neon-lightning material used by premium accessories.</summary>
    public static class AccessoryRainbowEffect
    {
        private const string ShaderResourceName = "AccessoryRainbow";
        private static readonly int SpeedId = Shader.PropertyToID("_RainbowSpeed");
        private static readonly int FrequencyId = Shader.PropertyToID("_RainbowFrequency");
        private static readonly int NeonGlowId = Shader.PropertyToID("_NeonGlow");
        private static readonly int DirectionId = Shader.PropertyToID("_RainbowDirection");
        private static Material rainbowMaterial;

        public static Material GetMaterial(float speed, float frequency, float neonGlow, bool flipX)
        {
            if (rainbowMaterial == null)
            {
                Shader shader = Resources.Load<Shader>(ShaderResourceName);
                if (shader == null) shader = Shader.Find("AnimeHillRacing/Accessory Rainbow");
                if (shader == null) return null;
                rainbowMaterial = new Material(shader)
                {
                    name = "Accessory Rainbow (Runtime)",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            rainbowMaterial.SetFloat(SpeedId, Mathf.Max(.01f, speed));
            rainbowMaterial.SetFloat(FrequencyId, Mathf.Max(.1f, frequency));
            rainbowMaterial.SetFloat(NeonGlowId, Mathf.Clamp(neonGlow, 0f, 2f));
            rainbowMaterial.SetFloat(DirectionId, flipX ? 1f : -1f);
            return rainbowMaterial;
        }
    }
}
