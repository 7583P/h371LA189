using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingShowroom:MonoBehaviour,IDragHandler,IScrollHandler
    {
        public GameObject PreviewVehicle{get;private set;}
        Camera previewCamera;RenderTexture texture;GameObject stage;float yaw=30,zoom=6.2f,idleTime;
        readonly List<Light> sceneLights=new List<Light>();readonly List<int> lightMasks=new List<int>();
        public static RacingShowroom Create(Transform parent,float x,float y,float w,float h,string vehicle,string wheel,Color? paint=null)
        {
            var rt=RacingDesign.Area(parent,"Vehicle studio",x,y,w,h);var image=rt.gameObject.AddComponent<RawImage>();image.raycastTarget=true;
            var preview=rt.gameObject.AddComponent<RacingShowroom>();preview.Initialize(image);preview.Show(vehicle,wheel,paint);return preview;
        }
        void Initialize(RawImage image)
        {
            foreach(var sceneLight in FindObjectsByType<Light>(FindObjectsSortMode.None))if(sceneLight.gameObject.layer!=29)
            {sceneLights.Add(sceneLight);lightMasks.Add(sceneLight.cullingMask);sceneLight.cullingMask&=~(1<<29);}
            stage=new GameObject("Racing studio world");stage.transform.position=new Vector3(5000,0,5000);
            var baseMat=RacingArt.Material(new Color(.08f,.14f,.18f),.55f);
            RacingArt.Box(stage.transform,"Display platform",new Vector3(0,-.68f,0),new Vector3(5.8f,.12f,5.8f),baseMat,.6f);
            RacingArt.Part(stage.transform,"Platform light ring",RacingArt.Ring(2.6f,2.565f,.035f,48),RacingArt.Material(RacingDesign.Cyan,.5f,true),new Vector3(0,-.602f,0),Quaternion.Euler(0,0,90));
            var cameraRoot=new GameObject("Studio camera");cameraRoot.transform.SetParent(stage.transform,false);previewCamera=cameraRoot.AddComponent<Camera>();previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=RacingDesign.Background;previewCamera.fieldOfView=36;previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=35;previewCamera.depth=-5;
            Canvas.ForceUpdateCanvases();
            int width=Application.isMobilePlatform?768:1024;
            Rect rect=image.rectTransform.rect;
            int height=Mathf.Clamp(Mathf.RoundToInt(width*rect.height/Mathf.Max(1,rect.width)),384,1024);
            int samples=RacingQualityScope.Current==RacingQuality.High?4:RacingQualityScope.Current==RacingQuality.Medium?2:1;
            texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="Racing preview",antiAliasing=samples};texture.Create();previewCamera.targetTexture=texture;image.texture=texture;
            var lightRoot=new GameObject("Studio softbox");lightRoot.transform.SetParent(stage.transform,false);lightRoot.transform.localPosition=new Vector3(-3,5,4);var light=lightRoot.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.88f,.95f,1);light.intensity=.65f;light.range=14;light.cullingMask=1<<29;
            var keyRoot=new GameObject("Studio keylight");keyRoot.transform.SetParent(stage.transform,false);var keyLight=keyRoot.AddComponent<Light>();keyLight.type=LightType.Directional;keyLight.color=Color.white;keyLight.intensity=.65f;keyLight.cullingMask=1<<29;keyLight.transform.rotation=Quaternion.Euler(45,-30,0);
            previewCamera.cullingMask=1<<29;
            foreach(var part in stage.GetComponentsInChildren<Transform>())part.gameObject.layer=29;
        }
        public void Show(string id,string wheels,Color? paint=null,string character="cobalt-driver",string accessory="none")
        {
            if(PreviewVehicle!=null)Destroy(PreviewVehicle);
            PreviewVehicle=new GameObject("Studio "+id);PreviewVehicle.transform.SetParent(stage.transform,false);
            bool isMeta=(id=="formula-01"||id==MetaProgressionConfig.MetaVehicleId);
            Color c = paint ?? (isMeta ? RacingVehicleFactory.MetaNavy : (id=="car-01" ? new Color(0.95f, 0.45f, 0.10f) : RacingDesign.Cyan));
            string w = (wheels==MetaProgressionConfig.MetaWheelId || wheels=="wheel-meta-cyan") ? MetaProgressionConfig.MetaWheelVisualId : wheels;
            var visual=RacingVehicleVisuals.Build(PreviewVehicle,id,w,c,preview:false,isHuman:isMeta);
            RacingLoadoutVisuals.Apply(visual,character,accessory);
            foreach(var part in PreviewVehicle.GetComponentsInChildren<Transform>())part.gameObject.layer=29;
        }
        void LateUpdate()
        {
            if(previewCamera==null||PreviewVehicle==null)return;
            bool visible=!RacingMenuOverlay.CoversShowroom&&!RacingProgressionService.IsRaceActive;
            previewCamera.enabled=visible;
            if(stage.activeSelf!=visible)stage.SetActive(visible);
            if(!visible)return;
            if(Time.unscaledTime-idleTime>2)yaw+=Time.unscaledDeltaTime*6;
            PreviewVehicle.transform.localRotation=Quaternion.Euler(0,yaw,0);
            previewCamera.transform.localPosition=new Vector3(0,zoom*.38f,zoom);previewCamera.transform.LookAt(stage.transform.position+Vector3.up*.15f);
        }
        public void OnDrag(PointerEventData e){yaw+=e.delta.x*.3f;idleTime=Time.unscaledTime;}
        public void OnScroll(PointerEventData e){zoom=Mathf.Clamp(zoom-e.scrollDelta.y*.35f,4.5f,8.5f);idleTime=Time.unscaledTime;}
        void OnDisable(){if(stage!=null)stage.SetActive(false);}
        void OnDestroy(){for(int i=0;i<sceneLights.Count;i++)if(sceneLights[i]!=null)sceneLights[i].cullingMask=lightMasks[i];if(texture!=null){texture.Release();Destroy(texture);}if(stage!=null)Destroy(stage);}
    }

}
