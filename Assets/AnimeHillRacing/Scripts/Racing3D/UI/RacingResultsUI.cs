using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingResultsUI:MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        RaceResults results;RectTransform root;Text mmrText,tierText;Image progress;float age;RacerResultEntry player;RacingRankBadge badge;
        void Start(){if(raceManager==null)raceManager=FindFirstObjectByType<RaceManager>();if(raceManager!=null)raceManager.OnRaceCompleted+=ShowResults;}
        public void ShowResults(RaceResults raceResults)
        {
            if(root!=null)return;results=raceResults;player=results.GetPlayerEntry();if(player==null)return;
            int displayRating=raceManager.PersistResults?player.newMmr:player.oldMmr;
            root=RacingDesign.Canvas("Ranked results",50);root.gameObject.AddComponent<RacingScreenMotion>();RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            RacingDesign.Label(root,(raceManager.PersistResults?"RANKED / ":"TEST RACE / ")+results.trackDisplayName,RacingType.Section,.05f,.87f,.86f,.065f,RacingDesign.Cyan);
            RacingDesign.Label(root,RacingHUD.Ordinal(player.finishPosition)+" PLACE",RacingType.Display,.05f,.72f,.39f,.13f,RacingDesign.Ink,display:true);
            badge=RacingDesign.RankBadge(root,displayRating,.335f,.70f,.08f,.12f);
            var summary=RacingDesign.CompetitivePanel(root,"Progression",.05f,.25f,.33f,.43f,RacingDesign.Apex);
            RacingDesign.Label(summary.transform,raceManager.PersistResults?"RATING":"TEST RATING",RacingType.Caption,.08f,.81f,.84f,.08f,RacingDesign.Muted);
            mmrText=RacingDesign.Label(summary.transform,player.oldMmr.ToString("N0"),RacingType.Mmr,.08f,.53f,.84f,.26f,RacingDesign.Ink);
            RacingDesign.Label(summary.transform,raceManager.PersistResults?$"{player.oldMmr:N0}  {player.mmrDelta:+0;-0;0}  =  {player.newMmr:N0}":"RANK AND REWARDS PRESERVED",RacingType.Body,.08f,.40f,.84f,.11f,player.mmrDelta>=0?RacingDesign.Success:RacingDesign.Danger);
            tierText=RacingDesign.Label(summary.transform,"",RacingType.Section,.08f,.23f,.84f,.13f,RacingDesign.TierColor(displayRating));
            var bg=RacingDesign.Area(summary.transform,"Rank progress",.08f,.15f,.84f,.02f).gameObject.AddComponent<Image>();bg.color=RacingDesign.Highlight;
            progress=RacingDesign.Area(bg.transform,"Progress",0,0,1,1).gameObject.AddComponent<Image>();progress.color=RacingDesign.TierColor(displayRating);
            RacingDesign.Label(summary.transform,displayRating>MmrTierUtility.MaxTargetMmr?"APEX BEYOND 10K":"NEXT DIVISION / KEEP CLIMBING",RacingType.Caption,.08f,.015f,.84f,.09f,RacingDesign.Muted);
            RacingDesign.Title(root,player.titleId,displayRating,.05f,.18f,.33f,.05f);
            var table=RacingDesign.CompetitivePanel(root,"Race classification",.42f,.18f,.53f,.64f,RacingDesign.Cyan);
            Row(table.transform,"POS","RACER / TITLE","MMR / RANK","VEHICLE","TIME",.91f,RacingDesign.Muted);
            for(int i=0;i<results.entries.Length;i++)
            {
                var entry=results.entries[i];float y=.84f-i*.062f;
                if(entry.isHuman)RacingDesign.Panel(table.transform,"Player highlight",.015f,y-.01f,.97f,.055f,RacingDesign.Highlight,6);
                Row(table.transform,entry.finishPosition.ToString("00"),entry.displayName+(entry.isHuman?" / YOU":""),entry.oldMmr.ToString("N0"),RacingDesign.VehicleName(entry.vehicleId),entry.finished?RacerResultEntry.FormatTime(entry.totalTime):"DNF",y,entry.isHuman?RacingDesign.Cyan:RacingDesign.Ink);
                RacingDesign.RankBadge(table.transform,entry.oldMmr,.061f,y,.036f,.048f);
                var title=RacingDesign.Title(table.transform,entry.titleId,entry.oldMmr,.10f,y-.025f,.26f,.027f);title.fontSize=11;
                var rank=RacingDesign.Label(table.transform,MmrTierUtility.GetDisplayName(entry.oldMmr),RacingType.Caption,.37f,y-.022f,.13f,.023f,RacingDesign.TierColor(entry.oldMmr),TextAnchor.MiddleRight);rank.fontSize=9;
            }
            var retry=RacingDesign.Button(root,"RACE AGAIN",.42f,.055f,.25f,.085f,()=>raceManager.RetryRace(),RacingDesign.Highlight);
            RacingDesign.Button(root,"RACING HUB",.70f,.055f,.25f,.085f,()=>raceManager.ExitToMenu());
            RacingDesign.Button(root,"GARAGE",.05f,.055f,.24f,.085f,()=>RacingLoadingScreen.Load("RacingGarage","TUNE YOUR NEXT RACE"));RacingDesign.Focus(retry);
        }
        static void Row(Transform table,string pos,string name,string mmr,string vehicle,string time,float y,Color color)
        {
            RacingDesign.Label(table,pos,RacingType.Caption,.025f,y,.06f,.05f,color);
            RacingDesign.Label(table,name,RacingType.Caption,.10f,y,.26f,.05f,color);
            RacingDesign.Label(table,mmr,RacingType.Caption,.37f,y,.13f,.05f,color,TextAnchor.MiddleRight);
            RacingDesign.Label(table,vehicle,RacingType.Caption,.53f,y,.25f,.05f,color);
            RacingDesign.Label(table,time,RacingType.Caption,.80f,y,.18f,.05f,color,TextAnchor.MiddleRight);
        }
        void Update()
        {
            if(player==null||mmrText==null)return;age+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.3f)/1f));int mmr=Mathf.RoundToInt(Mathf.Lerp(player.oldMmr,raceManager.PersistResults?player.newMmr:player.oldMmr,t));
            mmrText.text=mmr.ToString("N0");tierText.text=MmrTierUtility.GetDisplayName(mmr);tierText.color=RacingDesign.TierColor(mmr);badge.Configure(mmr);progress.rectTransform.anchorMax=new Vector2(MmrTierUtility.GetDivisionProgress(mmr),1);progress.rectTransform.offsetMax=Vector2.zero;
            if(age>1.4f)enabled=false;
        }
        void OnDestroy(){if(raceManager!=null)raceManager.OnRaceCompleted-=ShowResults;}
    }
}
