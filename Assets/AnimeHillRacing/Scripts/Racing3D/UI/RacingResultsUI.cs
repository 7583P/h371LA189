using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingResultsUI:MonoBehaviour
    {
        [SerializeField] RaceManager raceManager;
        RaceResults results;RectTransform root;Text mmrText,tierText,revealText;Image progress;float age;RacerResultEntry player;RacingRankBadge badge;
        Image[] pips;bool placementRace,reveal,seasonPending;
        void Start(){if(raceManager==null)raceManager=FindFirstObjectByType<RaceManager>();if(raceManager!=null)raceManager.OnRaceCompleted+=ShowResults;}

        // Settled CPPS entries carry placementBefore/After; unrated races carry -1.
        static bool Unranked(RacerResultEntry e,bool after)=>(after?e.placementAfter:e.placementBefore)>=0&&(after?e.placementAfter:e.placementBefore)<CompetitiveRating.PlacementRaces;

        public void ShowResults(RaceResults raceResults)
        {
            if(root!=null)return;results=raceResults;player=results.GetPlayerEntry();if(player==null)return;
            bool rated=raceManager.PersistResults&&!string.IsNullOrEmpty(results.rankedRaceId);
            var human=RacingProgressionService.State.human;seasonPending=human.Ranked&&!SeasonQualification.IsOfficial(human,RacingProgressionService.State.currentSeasonNumber);
            placementRace=rated&&Unranked(player,false);reveal=placementRace&&!Unranked(player,true)&&!seasonPending;
            int displayRating=rated?player.newMmr:player.oldMmr;
            root=RacingDesign.Canvas("Ranked results",50);root.gameObject.AddComponent<RacingScreenMotion>();RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            RacingDesign.Label(root,(rated?(placementRace?"PLACEMENT / ":"RANKED / "):"TEST RACE / ")+results.trackDisplayName,RacingType.Section,.05f,.87f,.86f,.065f,placementRace?RacingDesign.Accent:RacingDesign.Cyan);
            RacingDesign.Label(root,RacingHUD.Ordinal(player.finishPosition)+" PLACE",RacingType.Display,.05f,.72f,.39f,.13f,RacingDesign.Ink,display:true);
            badge=RacingDesign.RankBadge(root,displayRating,.285f,.505f,.075f,.115f);
            var summary=RacingDesign.CompetitivePanel(root,"Progression",.05f,.25f,.33f,.43f,placementRace?RacingDesign.Accent:RacingDesign.Apex);
            if(seasonPending){BuildRanked(summary.transform,rated,displayRating);badge.gameObject.SetActive(false);}
            else if(placementRace&&!reveal)BuildPlacement(summary.transform);
            else if(reveal)BuildReveal(summary.transform);
            else BuildRanked(summary.transform,rated,displayRating);
            if(!placementRace||reveal)RacingDesign.Title(root,player.titleId,displayRating,.05f,.18f,.33f,.05f,RacingProgressionService.Engine.OfficialPosition(player.racerId));
            var table=RacingDesign.CompetitivePanel(root,"Race classification",.42f,.18f,.53f,.64f,RacingDesign.Cyan);
            Row(table.transform,"POS","RACER / TITLE","MMR / RANK","VEHICLE","TIME",.91f,RacingDesign.Muted);
            for(int i=0;i<results.entries.Length;i++)
            {
                var entry=results.entries[i];float y=.84f-i*.062f;bool hidden=Unranked(entry,false);
                if(entry.isHuman)RacingDesign.Panel(table.transform,"Player highlight",.015f,y-.01f,.97f,.055f,RacingDesign.Highlight,6);
                Row(table.transform,entry.finishPosition.ToString("00"),entry.displayName+(entry.isHuman?" / YOU":""),hidden?MmrTierUtility.UnrankedName:entry.oldMmr.ToString("N0"),RacingDesign.VehicleName(entry.vehicleId),entry.finished?RacerResultEntry.FormatTime(entry.totalTime):"DNF",y,entry.isHuman?RacingDesign.Cyan:RacingDesign.Ink);
                int officialPosition=RacingProgressionService.Engine.OfficialPosition(entry.racerId);
                if(!hidden&&officialPosition>0)RacingDesign.RankBadge(table.transform,entry.oldMmr,.061f,y,.036f,.048f);
                var title=RacingDesign.Title(table.transform,entry.titleId,entry.oldMmr,.10f,y-.025f,.26f,.027f,officialPosition);title.fontSize=11;
                var rank=RacingDesign.Label(table.transform,hidden?MmrTierUtility.PlacementLabel(entry.placementBefore):officialPosition>0?MmrTierUtility.GetDisplayName(entry.oldMmr):SeasonQualification.PendingLabel,RacingType.Caption,.37f,y-.022f,.13f,.023f,hidden?RacingDesign.Muted:RacingDesign.TierColor(entry.oldMmr),TextAnchor.MiddleRight);rank.fontSize=9;
            }
            var retry=RacingDesign.Button(root,"RACE AGAIN",.42f,.055f,.25f,.085f,()=>raceManager.RetryRace(),RacingDesign.Highlight);
            RacingDesign.Button(root,"RACING HUB",.70f,.055f,.25f,.085f,()=>raceManager.ExitToMenu());
            RacingDesign.Button(root,"GARAGE",.05f,.055f,.24f,.085f,()=>RacingLoadingScreen.Load("RacingGarage","TUNE YOUR NEXT RACE"));RacingDesign.Focus(retry);
        }

        void BuildRanked(Transform summary,bool rated,int displayRating)
        {
            RacingDesign.Label(summary,rated?"RATING":"TEST RATING",RacingType.Caption,.08f,.81f,.84f,.08f,RacingDesign.Muted);
            mmrText=RacingDesign.Label(summary,player.oldMmr.ToString("N0"),RacingType.Mmr,.08f,.53f,.84f,.26f,RacingDesign.Ink);
            RacingDesign.Label(summary,rated?$"{player.oldMmr:N0}  {player.mmrDelta:+0;-0;0}  =  {player.newMmr:N0}":"RANK AND REWARDS PRESERVED",RacingType.Body,.08f,.40f,.84f,.11f,player.mmrDelta>=0?RacingDesign.Success:RacingDesign.Danger);
            tierText=RacingDesign.Label(summary,"",RacingType.Section,.08f,.23f,.84f,.13f,RacingDesign.TierColor(displayRating));
            var bg=RacingDesign.Area(summary,"Rank progress",.08f,.15f,.84f,.02f).gameObject.AddComponent<Image>();bg.color=RacingDesign.Highlight;
            progress=RacingDesign.Area(bg.transform,"Progress",0,0,1,1).gameObject.AddComponent<Image>();progress.color=RacingDesign.TierColor(displayRating);
            RacingDesign.Label(summary,displayRating>MmrTierUtility.MaxTargetMmr?"APEX BEYOND 10K":"NEXT DIVISION / KEEP CLIMBING",RacingType.Caption,.08f,.015f,.84f,.09f,RacingDesign.Muted);
        }

        void BuildPlacement(Transform summary)
        {
            badge.gameObject.SetActive(false);
            RacingDesign.Label(summary,MmrTierUtility.UnrankedName,RacingType.Caption,.08f,.81f,.84f,.08f,RacingDesign.Muted);
            RacingDesign.Label(summary,MmrTierUtility.PlacementLabel(player.placementAfter),RacingType.Section,.08f,.58f,.84f,.18f,RacingDesign.Ink);
            pips=RacingDesign.PlacementPips(summary,player.placementBefore,.08f,.43f,.84f,.07f);
            int left=CompetitiveRating.PlacementRaces-player.placementAfter;
            RacingDesign.Label(summary,left==1?"1 RACE TO YOUR RANK":left+" RACES TO YOUR RANK",RacingType.Body,.08f,.25f,.84f,.11f,RacingDesign.Accent);
            RacingDesign.Label(summary,"Your rating stays hidden until Placement 3/3.",RacingType.Caption,.08f,.04f,.84f,.12f,RacingDesign.Muted);
        }

        void BuildReveal(Transform summary)
        {
            revealText=RacingDesign.Label(summary,"RANK REVEALED",RacingType.Section,.08f,.80f,.84f,.12f,RacingDesign.Accent);
            mmrText=RacingDesign.Label(summary,"0",RacingType.Mmr,.08f,.53f,.84f,.26f,RacingDesign.Ink);
            tierText=RacingDesign.Label(summary,"",RacingType.Section,.08f,.36f,.84f,.13f,RacingDesign.TierColor(player.newMmr));
            pips=RacingDesign.PlacementPips(summary,player.placementBefore,.08f,.22f,.84f,.05f);
            RacingDesign.Label(summary,"PLACEMENT COMPLETE  /  "+MmrTierUtility.GetDisplayName(player.newMmr),RacingType.Caption,.08f,.04f,.84f,.12f,RacingDesign.Muted);
            badge.transform.localScale=Vector3.zero;
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
            if(player==null)return;age+=Time.unscaledDeltaTime;
            if(pips!=null&&player.placementAfter>player.placementBefore&&player.placementBefore>=0&&player.placementBefore<pips.Length)
            {
                // The newly earned Placement segment fills in.
                var pip=pips[player.placementBefore];float fill=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.25f)/.6f));
                pip.color=Color.Lerp(RacingDesign.Highlight,RacingDesign.Accent,fill);
            }
            if(placementRace&&!reveal&&!seasonPending){if(age>1.2f)enabled=false;return;}
            if(mmrText==null)return;
            if(reveal)
            {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.9f)/1.6f));int mmr=Mathf.RoundToInt(player.newMmr*t);
                mmrText.text=mmr.ToString("N0");
                float pop=Mathf.Clamp01((age-.8f)/.45f);badge.transform.localScale=Vector3.one*(pop<1?Mathf.SmoothStep(0,1.15f,pop):1f+.04f*Mathf.Sin(age*5f)*Mathf.Clamp01(3.2f-age));
                badge.Configure(player.newMmr);tierText.text=t>.98f?MmrTierUtility.GetDisplayName(player.newMmr):"";
                if(revealText!=null)revealText.color=Color.Lerp(RacingDesign.Accent,RacingDesign.Ink,.5f+.5f*Mathf.Sin(age*4f));
                if(age>3.2f){badge.transform.localScale=Vector3.one;if(revealText!=null)revealText.color=RacingDesign.Accent;enabled=false;}
                return;
            }
            float k=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.3f)/1f));bool rated=raceManager.PersistResults&&!string.IsNullOrEmpty(results.rankedRaceId);
            int value=Mathf.RoundToInt(Mathf.Lerp(player.oldMmr,rated?player.newMmr:player.oldMmr,k));
            mmrText.text=value.ToString("N0");tierText.text=seasonPending?SeasonQualification.PendingLabel:MmrTierUtility.GetDisplayName(value);tierText.color=RacingDesign.TierColor(value);badge.Configure(value);progress.rectTransform.anchorMax=new Vector2(MmrTierUtility.GetDivisionProgress(value),1);progress.rectTransform.offsetMax=Vector2.zero;
            if(age>1.4f)enabled=false;
        }
        void OnDestroy(){if(raceManager!=null)raceManager.OnRaceCompleted-=ShowResults;}
    }
}
