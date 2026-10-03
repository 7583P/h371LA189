using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingFrontEndArt
    {
        public static RawImage Background(Transform parent)
        {
            var image=RacingDesign.Area(parent,"Blackline city key art",0,0,1,1).gameObject.AddComponent<RawImage>();
            image.texture=Resources.Load<Texture2D>("RacingArt/BlacklineMenu");image.raycastTarget=false;
            image.color=image.texture!=null?Color.white:RacingDesign.Background;
            return image;
        }
    }
    public sealed class RacingLoadingScreen : MonoBehaviour
    {
        static RacingLoadingScreen instance;
        RectTransform canvasRoot,fill,car;
        GameObject canvasObject;
        Text status;
        public static bool IsLoading=>instance!=null;
        public static void Load(string scene,string caption=null)
        {
            if(instance!=null)return;
            instance=new GameObject("Themed scene loader").AddComponent<RacingLoadingScreen>();
            instance.Begin(scene,caption??scene.ToUpperInvariant());
        }
        void Begin(string scene,string caption)
        {
            DontDestroyOnLoad(gameObject);
            canvasRoot=RacingDesign.Canvas("Hill Adventurer loading",200);
            canvasObject=canvasRoot.GetComponentInParent<Canvas>().gameObject;
            DontDestroyOnLoad(canvasObject);
            RacingFrontEndArt.Background(canvasRoot);
            var plate=RacingDesign.Panel(canvasRoot,"Loading console",.055f,.055f,.89f,.16f,RacingDesign.Surface,12);
            RacingDesign.Label(plate.transform,caption,RacingType.Section,.03f,.56f,.74f,.28f,RacingDesign.Ink);
            status=RacingDesign.Label(plate.transform,"0%",RacingType.Caption,.80f,.56f,.17f,.28f,RacingDesign.Cyan,TextAnchor.MiddleRight);
            var bar=RacingDesign.Panel(plate.transform,"Loading road",.03f,.20f,.94f,.12f,RacingDesign.Highlight,0).rectTransform;
            fill=RacingDesign.Panel(bar,"Progress",0,0,0,1,RacingDesign.Cyan,0).rectTransform;
            car=RacingDesign.Area(bar,"Moving Formula",0,.35f,0,0);
            car.sizeDelta=new Vector2(76,32);car.pivot=new Vector2(.5f,0);
            car.gameObject.AddComponent<RacingLoadingCarGraphic>().color=RacingDesign.Platinum;
            StartCoroutine(Transition(scene));
        }
        void SetProgress(float progress)
        {
            float t=Mathf.Clamp01(progress);
            fill.anchorMax=new Vector2(t,1);fill.offsetMax=Vector2.zero;
            car.anchorMin=car.anchorMax=new Vector2(t,.38f);car.anchoredPosition=Vector2.zero;
            status.text=Mathf.RoundToInt(t*100)+"%";
        }
        IEnumerator Transition(string scene)
        {
            yield return null;
            if(Application.isBatchMode)yield return null;else yield return new WaitForEndOfFrame();
            var op=SceneManager.LoadSceneAsync(scene);op.allowSceneActivation=false;
            float shown=0;
            while(op.progress<.9f||shown<.995f)
            {
                shown=Mathf.MoveTowards(shown,Mathf.Clamp01(op.progress/.9f),Time.unscaledDeltaTime*1.7f);
                SetProgress(shown);yield return null;
            }
            SetProgress(1);op.allowSceneActivation=true;
            while(!op.isDone)yield return null;
            yield return null;
            if(Application.isBatchMode)yield return null;else yield return new WaitForEndOfFrame();
            Destroy(gameObject);
        }
        void OnDestroy(){if(canvasObject!=null)Destroy(canvasObject);if(instance==this)instance=null;}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RacingLoadingCarGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;Color body=new Color(.075f,.10f,.15f);
            Quad(vh,r,.10f,.27f,.79f,.56f,body);
            Quad(vh,r,.40f,.53f,.57f,.85f,RacingVehicleFactory.MetaBlueHelmet);
            Quad(vh,r,.03f,.20f,.25f,.54f,body);Quad(vh,r,.75f,.20f,.95f,.54f,body);
            Quad(vh,r,.15f,.34f,.87f,.42f,RacingDriftFeedback.Cyan);
            Quad(vh,r,.74f,.47f,.96f,.53f,RacingVehicleFactory.MetaRed);
            Quad(vh,r,.04f,.59f,.24f,.68f,RacingVehicleFactory.MetaGreen);
            Quad(vh,r,.01f,.23f,.08f,.73f,color);
        }
        static void Quad(VertexHelper vh,Rect r,float x,float y,float x2,float y2,Color c)
        {
            int i=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin+x*r.width,r.yMin+y*r.height),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMin+x*r.width,r.yMin+y2*r.height),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMin+x2*r.width,r.yMin+y2*r.height),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMin+x2*r.width,r.yMin+y*r.height),c,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
