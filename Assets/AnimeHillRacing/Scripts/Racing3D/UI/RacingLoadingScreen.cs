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
            var image=RacingDesign.Area(parent,"DAPEX city key art",0,0,1,1).gameObject.AddComponent<RawImage>();
            image.texture=Resources.Load<Texture2D>("RacingArt/MainMenuCover") ?? Resources.Load<Texture2D>("RacingArt/DAPEXMenu") ?? Resources.Load<Texture2D>("RacingArt/NeonDepthCity");image.raycastTarget=false;
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
            if(Application.isPlaying)DontDestroyOnLoad(gameObject);
            canvasRoot=RacingDesign.Canvas("Hill Adventurer loading",200);
            canvasObject=canvasRoot.GetComponentInParent<Canvas>().gameObject;
            if(Application.isPlaying)DontDestroyOnLoad(canvasObject);
            RacingFrontEndArt.Background(canvasRoot);
            int rating=RacingTestSession.Active?RacingTestSession.Mmr:RacingSaveManager.CurrentMmr;
            RacingDesign.RankBadge(canvasRoot,rating,.875f,.24f,.075f,.115f);
            RacingDesign.Label(canvasRoot,MmrTierUtility.GetDisplayName(rating),RacingType.Caption,.61f,.275f,.25f,.05f,RacingDesign.TierColor(rating),TextAnchor.MiddleRight);
            var plate=RacingDesign.Panel(canvasRoot,"Loading console",.055f,.055f,.89f,.16f,RacingDesign.Surface,12);
            RacingDesign.Label(plate.transform,caption,RacingType.Section,.03f,.56f,.74f,.28f,RacingDesign.Ink);
            status=RacingDesign.Label(plate.transform,"0%",RacingType.Caption,.80f,.56f,.17f,.28f,RacingDesign.Cyan,TextAnchor.MiddleRight);
            var bar=RacingDesign.Panel(plate.transform,"Loading road",.03f,.20f,.94f,.12f,RacingDesign.Highlight,0).rectTransform;
            fill=RacingDesign.Panel(bar,"Progress",0,0,0,1,RacingDesign.Cyan,0).rectTransform;
            car=RacingDesign.Area(bar,"Moving Jeep",0,.45f,0,0);
            car.sizeDelta=new Vector2(68,36);car.pivot=new Vector2(.5f,0);
            car.gameObject.AddComponent<RacingLoadingCarGraphic>().color=RacingDesign.Platinum;
            StartCoroutine(Transition(scene));
        }
        void SetProgress(float progress)
        {
            float t=Mathf.Clamp01(progress);
            fill.anchorMax=new Vector2(t,1);fill.offsetMax=Vector2.zero;
            car.anchorMin=car.anchorMax=new Vector2(t,.42f);car.anchoredPosition=Vector2.zero;
            status.text=Mathf.RoundToInt(t*100)+"%";
        }
        IEnumerator Transition(string scene)
        {
            yield return null;
            if(Application.isBatchMode)yield return null;else yield return new WaitForEndOfFrame();
            if(scene=="Racing3D") {
                RacingProgressionService.Resume(System.DateTime.UtcNow.Ticks);
                while(RacingProgressionService.HasPendingSeasonTransition) {
                    status.text="UPDATING SEASON";
                    RacingProgressionService.Pump();
                    yield return null;
                }
                RacingProgressionService.Save(true);
            }
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

}
