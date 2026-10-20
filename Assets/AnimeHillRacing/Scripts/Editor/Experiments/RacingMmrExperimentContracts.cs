#if UNITY_EDITOR
using System;
using System.IO;
using AnimeHillRacing.Racing3D;
using AnimeHillRacing.Racing3D.Experiments;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingMmrExperimentContracts
    {
        public static void Run()
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));Directory.CreateDirectory(output);
            var report=new ClosureContractReport();var scene=EditorSceneManager.NewPreviewScene();RenderTexture target=null;Texture2D pixels=null;
            try{
                var cameraNode=new GameObject("Isolated prestige preview camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraNode,scene);
                var camera=cameraNode.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=240;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.075f);camera.cullingMask=1<<31;
                target=new RenderTexture(1200,480,24);camera.targetTexture=target;
                var canvasNode=new GameObject("Experimental prestige only",typeof(RectTransform),typeof(Canvas));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasNode,scene);
                var canvas=canvasNode.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;canvas.GetComponent<RectTransform>().sizeDelta=new Vector2(1200,480);
                int[] cases={2999,3000,3099,3100,3199,3200,3999,4000,5000,10000,100000};int[] expected={-1,0,0,1,1,2,9,10,20,70,970};
                var root=new GameObject("Live mesh under test",typeof(RectTransform),typeof(AfterPreviewBadge));root.transform.SetParent(canvas.transform,false);var badge=root.GetComponent<AfterPreviewBadge>();
                report.Check(root.GetComponent<CanvasRenderer>()!=null,"Experimental UI mesh has a real CanvasRenderer");
                using(var vertices=new VertexHelper())for(int i=0;i<cases.Length;i++){
                    badge.Configure(cases[i],true,true,AfterScale.Unified());AfterPreviewBadge.Populate(vertices,new Rect(-100,-100,200,200),badge.ShowsLevel);
                    report.Check(badge.ShowsLevel==(expected[i]>=0)&&badge.Counter==(expected[i]<0?"":expected[i].ToString())&&vertices.currentVertCount==(expected[i]<0?0:5),"Real UI dynamic diamond at "+cases[i]);
                }
                badge.Configure(100000,true,false,AfterScale.Unified());report.Check(!badge.ShowsLevel&&badge.Counter=="","Season Placement never displays an experimental current diamond");
                badge.Configure(100000,false,true,AfterScale.Unified());report.Check(!badge.ShowsLevel&&badge.Counter=="","Career Placement never displays an experimental current diamond");
                UnityEngine.Object.DestroyImmediate(root);
                for(int i=0;i<3;i++){
                    int rating=i==0?2999:i==1?3000:100000;float x=(i-1)*380;
                    var tile=new GameObject("Boundary "+rating,typeof(RectTransform),typeof(AfterPreviewBadge));tile.transform.SetParent(canvas.transform,false);
                    var rect=tile.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(240,260);rect.anchoredPosition=new Vector2(x,15);tile.GetComponent<AfterPreviewBadge>().Configure(rating,true,true,AfterScale.Unified());
                    var textNode=new GameObject("Caption",typeof(RectTransform),typeof(Text));textNode.transform.SetParent(canvas.transform,false);var text=textNode.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=26;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.rectTransform.sizeDelta=new Vector2(360,120);text.rectTransform.anchoredPosition=new Vector2(x,-170);text.text=rating+" MMR\n"+(i==0?"NO AFTER":i==1?"AFTER 0":"AFTER 970");
                }
                void Layer(Transform node){node.gameObject.layer=31;foreach(Transform child in node)Layer(child);}Layer(canvas.transform);
                Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;
                try{RenderTexture.active=target;pixels=new Texture2D(1200,480,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1200,480),0,0);pixels.Apply();}
                finally{RenderTexture.active=previous;}
                File.WriteAllBytes(Path.Combine(output,"after-experimental-preview.png"),pixels.EncodeToPNG());
                var colors=pixels.GetPixels32();int currentInk=0,belowInk=0;
                for(int y=150;y<330;y++)for(int x=510;x<690;x++){
                    var shown=colors[y*1200+x];var absent=colors[y*1200+x-380];
                    if(shown.g>50&&shown.b>80)currentInk++;if(absent.g>50&&absent.b>80)belowInk++;
                }
                report.Check(currentInk>5000&&belowInk<500,"Rendered preview contains the current AFTER diamond and no diamond below the threshold");
                var original=AfterExperimentJournal.Create(CompetitiveCoordinator.HumanId,1,AfterScale.Unified());
                var loaded=JsonUtility.FromJson<AfterExperimentJournal>(JsonUtility.ToJson(original));loaded.Validate();
                report.Check(loaded.profileId==original.profileId&&loaded.scale.threshold==3000&&loaded.scale.step==100&&loaded.titles.Count==0,"Unity JSON round-trip preserves the isolated scale without granting titles");
                var state=LocalLeagueEngine.Create(GlobalSeasonCalendar.EpochTicks,4207,24);var engine=new LocalLeagueEngine(state);int skill=engine.View(0).drivingRating;
                state.bots[0].placementGames=3;state.bots[0].currentMmr=100000;report.Check(engine.View(0).drivingRating==skill,"Actual persistent view separates visible MMR from driving skill");
                report.Check(MmrTierUtility.GetTier(3000)==MmrTier.ProI&&MmrTierUtility.GetTier(10000)==MmrTier.ApexII&&MmrTierUtility.GetTier(10001)==MmrTier.ApexBeyond,"Production Hill ranks remain compatible and AFTER stays editor-only");
            }
            catch(Exception exception){report.Check(false,exception.ToString());}
            finally{EditorSceneManager.ClosePreviewScene(scene);if(target!=null)UnityEngine.Object.DestroyImmediate(target);if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
            File.WriteAllText(Path.Combine(output,"mmr-ui-contracts.json"),JsonUtility.ToJson(report,true));
            if(report.failed>0)throw new InvalidOperationException("MMR experiment UI contracts failed: "+report.failed);
            Debug.Log("MMR_EXPERIMENT_UI passed="+report.passed+" failed="+report.failed);
        }
    }
}
#endif
