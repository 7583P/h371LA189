using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingModeSelectUI:MonoBehaviour
    {
        [SerializeField] bool openOnStart;
        readonly Button[] qualityButtons=new Button[3];
        void Start()
        {
            RacingTestSession.Clear();
            RacingFeedback.Ensure();gameObject.AddComponent<RacingQualityScope>();
            var root=RacingDesign.Canvas("Racing hub");root.gameObject.AddComponent<RacingScreenMotion>();
            RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            var contour=RacingDesign.Area(root,"Topography",.46f,.06f,.54f,.88f).gameObject.AddComponent<RacingEmblem>();contour.Contours=true;contour.color=new Color(.18f,.4f,.45f,.22f);contour.raycastTarget=false;
            RacingDesign.Label(root,"HILL ADVENTURER",RacingType.Section,.05f,.86f,.48f,.08f,RacingDesign.Ink,display:true);
            RacingDesign.Label(root,"RACING",RacingType.Display,.05f,.75f,.52f,.12f,RacingDesign.Cyan,display:true);
            RacingDesign.Label(root,"CHASE THE CITY LIGHTS.",RacingType.Caption,.05f,.70f,.46f,.04f,RacingDesign.Muted);
            int mmr=RacingSaveManager.CurrentMmr;
            var badge=RacingDesign.Area(root,"Rank badge",.82f,.83f,.1f,.12f).gameObject.AddComponent<RacingEmblem>();badge.Tier=(int)MmrTierUtility.GetTier(mmr);badge.color=RacingDesign.TierColor(mmr);
            RacingDesign.Label(root,mmr.ToString("N0")+" MMR",RacingType.Stat,.61f,.89f,.2f,.055f,RacingDesign.Ink,TextAnchor.MiddleRight);
            RacingDesign.Label(root,MmrTierUtility.GetTierDisplayName(MmrTierUtility.GetTier(mmr)),RacingType.Caption,.62f,.845f,.19f,.04f,RacingDesign.TierColor(mmr),TextAnchor.MiddleRight);
            var ranked=RacingDesign.Button(root,"RANKED",.05f,.34f,.45f,.31f,()=>SceneManager.LoadScene("RacingGarage"),RacingDesign.Surface);
            RacingDesign.Label(ranked.transform,"12 RACERS / 3 LAPS",RacingType.Caption,.08f,.15f,.75f,.14f,RacingDesign.Cyan);
            RacingDesign.Label(ranked.transform,"BUILD YOUR LINE. EARN YOUR RANK.",RacingType.Caption,.08f,.02f,.79f,.15f,RacingDesign.Muted);
            RacingDesign.Panel(ranked.transform,"Gold edge",0,0,.01f,1,RacingDesign.Gold,0);
            RacingDesign.Label(root,"NIGHT CITY CIRCUIT",RacingType.Title,.55f,.67f,.4f,.07f,RacingDesign.Ink);
            var preview=RacingShowroom.Create(root,.53f,.31f,.43f,.35f,RacingSaveManager.SelectedVehicleId,RacingSaveManager.SelectedWheelId);
            RacingDesign.Label(root,RacingDesign.VehicleName(RacingSaveManager.SelectedVehicleId),RacingType.Section,.55f,.25f,.37f,.05f,RacingDesign.Ink);
            RacingDesign.Label(root,$"BEST {RacingSaveManager.HighestMmr:N0}  /  {RacingSaveManager.RacesCount} RACES  /  {RacingSaveManager.WinsCount} WINS",RacingType.Caption,.55f,.20f,.39f,.04f,RacingDesign.Muted);
            RacingDesign.Label(root,"RECENT FORM  "+RacingSaveManager.RecentPerformance,RacingType.Caption,.55f,.15f,.39f,.04f,RacingDesign.Muted);
            string[] modes={"GRAND PRIX","TIME TRIAL","PRACTICE"};
            for(int i=0;i<3;i++)
            {
                var card=RacingDesign.Button(root,modes[i],.05f+i*.155f,.17f,.145f,.125f,null,RacingDesign.Surface);card.interactable=false;
                RacingDesign.Label(card.transform,"IN DEVELOPMENT",RacingType.Caption,.08f,.02f,.86f,.22f,RacingDesign.Muted);
            }
            RacingDesign.Button(root,"MAIN MENU",.05f,.045f,.18f,.075f,()=>SceneManager.LoadScene("MainMenu"));
            RacingDesign.Button(root,"TEST MMR",.24f,.045f,.16f,.075f,()=>OpenMmrTestTool(root));
            qualityButtons[0]=RacingDesign.Button(root,"LOW",.58f,.04f,.1f,.065f,()=>RacingQualityScope.Select(RacingQuality.Low));
            qualityButtons[1]=RacingDesign.Button(root,"MEDIUM",.69f,.04f,.13f,.065f,()=>RacingQualityScope.Select(RacingQuality.Medium));
            qualityButtons[2]=RacingDesign.Button(root,"HIGH",.83f,.04f,.11f,.065f,()=>RacingQualityScope.Select(RacingQuality.High));
            RacingQualityScope.Changed+=ShowQuality;ShowQuality(RacingQualityScope.Current);
            RacingDesign.Focus(ranked);
        }
        void ShowQuality(RacingQuality selected)
        {
            for(int i=0;i<qualityButtons.Length;i++)if(qualityButtons[i]!=null)
                qualityButtons[i].GetComponent<RacingButtonMotion>().SetBaseColor(i==(int)selected?RacingDesign.Cyan:RacingDesign.Highlight);
        }
        void OnDestroy(){RacingQualityScope.Changed-=ShowQuality;}

        GameObject mmrToolPanel;
        int testMmr=1000,testSeed=42,selectedRacer=1;
        RectTransform inspector,detail;
        RacerIdentity[] inspectedLobby;
        static readonly int[] Presets={1000,10000,25000,50000,75000,90000,100000,125000};
        void OpenMmrTestTool(RectTransform root)
        {
            if(mmrToolPanel!=null){CloseInspector();return;}
            testMmr=RacingSaveManager.CurrentMmr;
            inspector=RacingDesign.Area(root,"Test MMR Panel",.03f,.04f,.94f,.92f);mmrToolPanel=inspector.gameObject;
            mmrToolPanel.AddComponent<RacingScreenMotion>();RefreshMmrToolView();
        }
        void CloseInspector(){Destroy(mmrToolPanel);mmrToolPanel=null;}
        void RefreshMmrToolView()
        {
            foreach(Transform child in inspector){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            RacingDesign.Panel(inspector,"Modal backdrop",0,0,1,1,new Color(.035f,.05f,.09f,.99f),8);
            RacingDesign.Label(inspector,"TEST MMR",RacingType.Title,.03f,.91f,.55f,.07f,RacingDesign.Cyan);
            RacingDesign.Button(inspector,"CLOSE",.85f,.92f,.12f,.055f,CloseInspector);
            RacingDesign.Label(inspector,$"{testMmr:N0} MMR / SEED {testSeed} / YOUR SAVED RANK IS PRESERVED",RacingType.Caption,.03f,.855f,.75f,.05f,RacingDesign.Muted);
            RacingDesign.Button(inspector,"REROLL",.83f,.855f,.14f,.05f,()=>{testSeed+=17;RefreshMmrToolView();});
            for(int p=0;p<Presets.Length;p++)
            {
                int value=Presets[p];string label=value==125000?"100K+":(value/1000)+"K";
                RacingDesign.Button(inspector,label,.03f+p*.118f,.775f,.11f,.06f,()=>{testMmr=value;RefreshMmrToolView();},testMmr==value?RacingDesign.Cyan:RacingDesign.Highlight);
            }
            inspectedLobby=MatchmakingService.GenerateRankedLobby(testMmr,RacingSaveManager.SelectedVehicleId,RacingSaveManager.SelectedWheelId,"TEST PLAYER",testSeed);
            int exact=0,near=0;foreach(var racer in inspectedLobby){if(racer.isHuman)continue;if(racer.buildCategory==MetaBuildCategory.Meta)exact++;if(racer.buildCategory==MetaBuildCategory.NearMeta)near++;}
            RacingDesign.Label(inspector,$"11 AI: {exact} EXACT META / {near} NEAR META / {11-exact-near} ALTERNATIVE    •    SELECT A RACER TO INSPECT ITS SKILLS",RacingType.Caption,.03f,.72f,.94f,.045f,RacingDesign.Ink);
            var grid=RacingDesign.Area(inspector,"Lobby rows",.03f,.265f,.94f,.44f);
            string[] heads={"GRID / RACER","AI MMR","BUILD / META CHANCE","VEHICLE / WHEELS"};float[] xs={.01f,.28f,.44f,.68f},ws={.26f,.15f,.23f,.31f};
            for(int c=0;c<4;c++)RacingDesign.Label(grid,heads[c],RacingType.Caption,xs[c],.935f,ws[c],.06f,RacingDesign.Muted);
            for(int i=0;i<inspectedLobby.Length;i++)
            {
                int index=i;var r=inspectedLobby[i];float y=.865f-i*.077f;
                var row=RacingDesign.Panel(grid,"Inspect "+r.displayName,0,y,1,.071f,i==selectedRacer?RacingDesign.Highlight:(i%2==0?RacingDesign.Surface:RacingDesign.Background),3);
                var button=row.gameObject.AddComponent<Button>();button.targetGraphic=row;row.raycastTarget=true;
                button.onClick.AddListener(()=>{selectedRacer=index;RefreshMmrToolView();});
                RacingDesign.Label(row.transform,$"{r.gridPosition:00} / {r.displayName}",RacingType.Caption,.01f,0,.26f,1,r.isHuman?RacingDesign.Cyan:RacingDesign.Ink);
                RacingDesign.Label(row.transform,r.mmr.ToString("N0"),RacingType.Caption,.28f,0,.15f,1,RacingDesign.TierColor(r.mmr));
                RacingDesign.Label(row.transform,$"{r.buildCategory} / {r.metaProbabilityUsed:P0}",RacingType.Caption,.44f,0,.23f,1,r.buildCategory==MetaBuildCategory.Meta?RacingDesign.Gold:RacingDesign.Ink);
                RacingDesign.Label(row.transform,RacingDesign.VehicleName(r.vehicleId)+" / "+r.wheelId.Replace("wheel-","").ToUpperInvariant(),RacingType.Caption,.68f,0,.31f,1,RacingDesign.Ink);
            }
            detail=RacingDesign.Area(inspector,"Selected AI skills",.03f,.065f,.76f,.18f);
            var selected=inspectedLobby[selectedRacer];var skill=AiSkillModel.CreateProfile(selected.mmr,selected.personalitySeed);
            RacingDesign.Label(detail,selected.displayName+" / "+selected.buildName,RacingType.Section,0,.75f,1,.24f,RacingDesign.Cyan);
            string text=selected.isHuman?"HUMAN INPUT / NO AI ASSISTANCE":
                $"LINE {skill.racingLineSkill:P0}  •  BRAKING {skill.brakingSkill:P0}  •  DRIFT {skill.driftSkill:P0}  •  CHAINING {skill.chainingSkill:P0}\n"+
                $"SHORTCUTS {skill.shortcutKnowledge:P0} / EXECUTION {skill.shortcutExecution:P0}  •  ITEMS {skill.itemTiming:P0}\n"+
                $"CONSISTENCY {skill.consistency:P0}  •  AWARENESS {skill.awareness:P0}  •  AGGRESSION {skill.aggression:P0}  •  TRICKS {skill.trickSkill:P0}";
            RacingDesign.Label(detail,text,RacingType.Body,0,0,1,.72f,RacingDesign.Ink);
            RacingDesign.Button(inspector,"DRIVE TEST",.81f,.12f,.16f,.09f,()=>{RacingTestSession.Set(testMmr,testSeed);SceneManager.LoadScene("RacingGarage");});
            RacingDesign.Label(inspector,"TEST RACES DO NOT CHANGE YOUR RATING OR REWARDS.",RacingType.Caption,.03f,.013f,.94f,.04f,RacingDesign.Muted);
        }
    }
    public static class RacingTestSession
    {
        public static bool Active {get;private set;}
        public static int Mmr {get;private set;}
        public static int Seed {get;private set;}
        public static void Set(int mmr,int seed){Mmr=Mathf.Max(0,mmr);Seed=seed;Active=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear(){Active=false;}
    }
}
