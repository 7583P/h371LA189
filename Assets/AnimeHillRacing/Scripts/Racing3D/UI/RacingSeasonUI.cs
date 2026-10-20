using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingSeasonUI : MonoBehaviour
    {
        RectTransform root,history; LeagueCompetitor owner; int page;
        public static RacingSeasonUI Open(LeagueCompetitor competitor=null)
        {
            var root=RacingDesign.Canvas("Local season history",120);root.gameObject.AddComponent<RacingMenuOverlay>();var view=root.gameObject.AddComponent<RacingSeasonUI>();view.root=root;view.owner=competitor??RacingProgressionService.State.human;view.Build();return view;
        }
        void Build()
        {
            var state=RacingProgressionService.State;
            var bg=RacingDesign.Panel(root,"Season backdrop",0,0,1,1,RacingDesign.Background,0);bg.raycastTarget=true;
            RacingDesign.Label(root,GlobalSeasonCalendar.Label(state.currentSeasonNumber),RacingType.Title,.05f,.88f,.7f,.08f,RacingDesign.Cyan);
            RacingDesign.Button(root,"CLOSE",.83f,.885f,.12f,.06f,()=>Destroy(root.GetComponentInParent<UnityEngine.Canvas>().gameObject));
            string start=new DateTime(state.seasonStartTimestamp,DateTimeKind.Utc).ToString("dd MMM yyyy"),end=new DateTime(state.seasonEndTimestamp,DateTimeKind.Utc).ToString("dd MMM yyyy");
            var left=GlobalSeasonCalendar.Remaining(state.currentSeasonNumber,state.lastKnownUtcTimestamp);
            bool clockNotice=RacingProgressionService.Coordinator.Clock.Frozen;
            foreach(var anomaly in state.ledger.anomalies)if(anomaly.kind!=null&&anomaly.kind.Contains("clock"))clockNotice=true;
            RacingDesign.Label(root,start+" → "+end+" UTC   ·   "+(clockNotice?"CLOCK CHECK REQUIRED":("ENDS IN "+(left.Days>0?left.Days+"D "+left.Hours+"H":left.Hours+"H "+left.Minutes+"M"))),RacingType.Body,.05f,.81f,.70f,.045f,RacingDesign.Ink);
            RacingDesign.Label(root,"Global season: three calendar months in UTC, the same for every player. Career peak and earned titles stay with you.",RacingType.Caption,.05f,.735f,.90f,.06f,RacingDesign.Muted);
            if(owner.Ranked){
                int next=RacingProgressionService.Engine.Configuration.ResetMmr(owner.currentMmr);
                RacingDesign.RankBadge(root,next,.76f,.74f,.16f,.16f);
                RacingDesign.Label(root,$"{owner.displayName}  /  NEXT SEASON START: {next:N0} MMR",RacingType.Section,.05f,.65f,.72f,.06f,RacingDesign.TierColor(next));
                RacingDesign.Label(root,owner.currentMmr>10000?"Beyond 10k follows the season reset curve, with a 10,000 ceiling. Your historical diamond peak is preserved.":"Next season starts from your final rating on the reset curve. Keep climbing before the season closes.",RacingType.Caption,.05f,.585f,.90f,.05f,RacingDesign.Muted);
            } else {
                RacingDesign.PlacementPips(root,owner.placementGames,.76f,.80f,.16f,.03f);
                RacingDesign.Label(root,$"{owner.displayName}  /  {MmrTierUtility.UnrankedName}  ·  {MmrTierUtility.PlacementLabel(owner.placementGames)}",RacingType.Section,.05f,.65f,.72f,.06f,RacingDesign.Accent);
                RacingDesign.Label(root,"Finish three Placement races to reveal your rank. Placement progress carries across the season boundary.",RacingType.Caption,.05f,.585f,.90f,.05f,RacingDesign.Muted);
            }
            history=RacingDesign.Area(root,"Capped season records",.05f,.19f,.90f,.36f);RefreshHistory();
            RacingDesign.Button(root,"PREVIOUS",.05f,.085f,.18f,.065f,()=>{page=Mathf.Max(0,page-1);RefreshHistory();});
            RacingDesign.Button(root,"NEXT",.25f,.085f,.18f,.065f,()=>{page=Mathf.Min(Mathf.Max(0,(owner.history.Count-1)/4),page+1);RefreshHistory();});
            RacingDesign.Button(root,"RANK LADDER",.73f,.085f,.22f,.065f,ShowRanks);
        }
        void RefreshHistory()
        {
            foreach(Transform child in history){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            if(owner.history.Count==0){RacingDesign.Label(history,"YOUR FIRST SEASON IS IN PROGRESS",RacingType.Section,0,.35f,1,.20f,RacingDesign.Muted);return;}
            for(int i=0;i<4;i++) {
                int index=owner.history.Count-1-page*4-i;if(index<0)break;var record=owner.history[index];
                var panel=RacingDesign.CompetitivePanel(history,"Season record "+record.seasonNumber,0,.75f-i*.25f,1,.23f,RacingDesign.TierColor(record.peakMmr));
                if(!record.qualificationKnown||record.officiallyQualified)RacingDesign.RankBadge(panel.transform,record.peakMmr,.01f,.015f,.075f,.97f);
                RacingDesign.Label(panel.transform,$"SEASON {record.seasonNumber}  ·  {record.endingRank}  ·  {record.endingMmr:N0}",RacingType.Body,.10f,.47f,.70f,.48f);
                RacingDesign.Label(panel.transform,$"START {record.startingMmr:N0}  /  PEAK {record.peakMmr:N0} {record.peakRank}  /  {record.wins} WINS · {record.podiums} PODIUMS · {record.races} RACES",RacingType.Caption,.10f,.03f,.88f,.42f,RacingDesign.Muted);
            }
        }
        public static void ShowRanks()
        {
            var root=RacingDesign.Canvas("Approved rank ladder",130);
            root.gameObject.AddComponent<RacingMenuOverlay>();
            var bg=RacingDesign.Panel(root,"Rank gallery backdrop",0,0,1,1,RacingDesign.Background,0);bg.raycastTarget=true;
            RacingDesign.Label(root,"HIGHER SKILLS · HIGHER PEAKS",RacingType.Title,.04f,.92f,.75f,.065f,RacingDesign.Cyan);
            RacingDesign.Button(root,"CLOSE",.84f,.925f,.12f,.055f,()=>UnityEngine.Object.Destroy(root.GetComponentInParent<Canvas>().gameObject));
            for(int band=0;band<20;band++) {
                int mmr=MmrTierUtility.MinimumRating((MmrTier)band);float x=.04f+(band%5)*.185f,y=.73f-(band/5)*.19f;
                var panel=RacingDesign.CompetitivePanel(root,"Rank band "+band,x,y,.172f,.181f,RacingDesign.TierColor(mmr));
                RacingDesign.RankBadge(panel.transform,mmr,.08f,.29f,.84f,.70f);
                RacingDesign.Label(panel.transform,MmrTierUtility.GetDisplayName(mmr),RacingType.Caption,.02f,.12f,.96f,.16f,RacingDesign.TierColor(mmr),TextAnchor.MiddleCenter);
                RacingDesign.Label(panel.transform,$"{mmr:N0}–{(band==19?10000:mmr+MmrTierUtility.DivisionWidth-1):N0}",RacingType.Caption,.02f,.01f,.96f,.12f,RacingDesign.Muted,TextAnchor.MiddleCenter);
            }
            RacingDesign.RankBadge(root,12000,.40f,.008f,.09f,.12f);
            RacingDesign.Label(root,"APEX BEYOND 10K   ·   12,000 → DIAMOND 2",RacingType.Caption,.50f,.025f,.46f,.05f,RacingDesign.Apex);
        }
    }
}
