using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeHillRacing.Racing3D
{
    // Race-only art must not change the sky, fog or ambient light of Adventure.
    [ExecuteAlways]
    public sealed class RacingEnvironmentScope : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();
        Material sky; bool fog; FogMode mode; float density; Color fogColor, ambientSky, equator, ground;
        AmbientMode ambientMode;
        Light light,oldSun;LightType lightType;float intensity;Color lightColor;LightShadows lightShadows;Quaternion lightRotation;
        Camera camera;CameraClearFlags clearFlags;Color background;
        bool captured;
        void Awake()=>Capture();
        public void Capture()
        {
            if(captured)return;captured=true;
            sky=RenderSettings.skybox;fog=RenderSettings.fog;mode=RenderSettings.fogMode;
            density=RenderSettings.fogDensity;fogColor=RenderSettings.fogColor;
            ambientMode=RenderSettings.ambientMode;ambientSky=RenderSettings.ambientSkyColor;
            equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;
            oldSun=RenderSettings.sun;light=FindFirstObjectByType<Light>();
            if(light!=null){lightType=light.type;intensity=light.intensity;lightColor=light.color;lightShadows=light.shadows;lightRotation=light.transform.rotation;}
            camera=Camera.main;if(camera!=null){clearFlags=camera.clearFlags;background=camera.backgroundColor;}
        }
        public void Own(Object value) { if(value!=null)owned.Add(value); }
        void OnDestroy()
        {
            if(!captured)return;
            RenderSettings.skybox=sky;RenderSettings.fog=fog;RenderSettings.fogMode=mode;
            RenderSettings.fogDensity=density;RenderSettings.fogColor=fogColor;
            RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=ambientSky;
            RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            RenderSettings.sun=oldSun;
            if(light!=null){light.type=lightType;light.intensity=intensity;light.color=lightColor;light.shadows=lightShadows;light.transform.rotation=lightRotation;}
            if(camera!=null){camera.clearFlags=clearFlags;camera.backgroundColor=background;}
            foreach(var value in owned)if(value!=null)
            {
                if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
            }
        }
    }
}
