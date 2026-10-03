using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RacingQuality { Low, Medium, High }
    public sealed class RacingQualityScope : MonoBehaviour
    {
        int aa,pixelLights,vSync,fps;ShadowQuality shadows;float shadowDistance;ShadowResolution resolution;
        static RacingQuality? cached,validationQuality;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){cached=validationQuality=null;}
        public static RacingQuality Current
        {
            get
            {
                if(validationQuality.HasValue)return validationQuality.Value;
                if(!cached.HasValue)cached=(RacingQuality)Mathf.Clamp(PlayerPrefs.GetInt("AHR.Racing.Quality",Application.isMobilePlatform?1:2),0,2);
                return cached.Value;
            }
        }
        public static int ParticleBurstBudget => Current==RacingQuality.Low?5:Current==RacingQuality.Medium?10:16;
        public static event System.Action<RacingQuality> Changed;
        public static void Select(RacingQuality quality){cached=quality;validationQuality=null;PlayerPrefs.SetInt("AHR.Racing.Quality",(int)quality);PlayerPrefs.Save();FindFirstObjectByType<RacingQualityScope>()?.Apply();Changed?.Invoke(Current);}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void OverrideForValidation(RacingQuality quality)
        {validationQuality=quality;foreach(var scope in FindObjectsByType<RacingQualityScope>(FindObjectsSortMode.None))scope.Apply();}
#endif
        void Awake()
        {
            aa=QualitySettings.antiAliasing;pixelLights=QualitySettings.pixelLightCount;shadows=QualitySettings.shadows;shadowDistance=QualitySettings.shadowDistance;resolution=QualitySettings.shadowResolution;vSync=QualitySettings.vSyncCount;fps=Application.targetFrameRate;Apply();
        }
        void Apply()
        {
            var quality=Current;QualitySettings.antiAliasing=quality==RacingQuality.High?4:quality==RacingQuality.Medium?2:0;
            QualitySettings.pixelLightCount=quality==RacingQuality.High?2:1;
            QualitySettings.shadows=quality==RacingQuality.Low?ShadowQuality.Disable:ShadowQuality.HardOnly;
            QualitySettings.shadowDistance=quality==RacingQuality.High?85:45;QualitySettings.shadowResolution=quality==RacingQuality.High?ShadowResolution.Medium:ShadowResolution.Low;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
        }
        void OnDestroy(){QualitySettings.antiAliasing=aa;QualitySettings.pixelLightCount=pixelLights;QualitySettings.shadows=shadows;QualitySettings.shadowDistance=shadowDistance;QualitySettings.shadowResolution=resolution;QualitySettings.vSyncCount=vSync;Application.targetFrameRate=fps;}
    }
}
