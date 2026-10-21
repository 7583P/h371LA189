using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingLeaderboardUI : MonoBehaviour
    {
        const float RowHeight = 64;
        const int PageSize = 1000;
        sealed class Row {
            public RectTransform area; public Text position,name,rating,rank,peak,movement,title; public RacingTitleView titleView; public RacingRankBadge badge;
            public LeagueCompetitor competitor;
            public RacingComboThumbnail combo; public int positionNumber;
        }
        RectTransform root, viewport, content;
        ScrollRect scroll;
        Text status,searchFeedback;
        LeagueIdentityLookup identityLookup;
        LocalLeagueEngine lookupEngine;
        readonly List<Row> pool = new List<Row>();
        int pageStart, pageCount;
        int lastFirst = -1, observedRevision = -1;
        float nextRefresh;
        public int PooledRowCount => pool.Count;
        public int EntityCount => RacingProgressionService.Engine.Index.Count;
        public static RacingLeaderboardUI Open()
        {
            var root = RacingDesign.Canvas("LOCAL LEAGUE",90);
            root.gameObject.AddComponent<RacingMenuOverlay>();
            var view = root.gameObject.AddComponent<RacingLeaderboardUI>(); view.root=root; view.Build(); return view;
        }
        void Build()
        {
            var background=RacingDesign.Panel(root,"League backdrop",0,0,1,1,RacingDesign.Background,0);background.raycastTarget=true;
            RacingDesign.Label(root,"NIGHT CITY LEAGUE",RacingType.Title,.05f,.88f,.72f,.08f,RacingDesign.Cyan);
            status=RacingDesign.Label(root,"",RacingType.Caption,.05f,.825f,.70f,.045f,RacingDesign.Muted);
            RacingDesign.Button(root,"CLOSE",.83f,.885f,.12f,.06f,Close);
            RacingDesign.Button(root,"TOP",.05f,.735f,.11f,.06f,()=>GoTo(0),RacingDesign.Highlight);
            RacingDesign.Button(root,"NEAR ME",.17f,.735f,.16f,.06f,NearHuman);
            RacingDesign.Button(root,"< PAGE",.34f,.735f,.12f,.06f,()=>GoTo(pageStart-PageSize));
            RacingDesign.Button(root,"PAGE >",.47f,.735f,.12f,.06f,()=>GoTo(pageStart+PageSize));
            var search=RacingDesign.Panel(root,"Indexed racer lookup",.61f,.735f,.34f,.06f,RacingDesign.Surface,6);
            var input=search.gameObject.AddComponent<InputField>();input.targetGraphic=search;search.raycastTarget=true;
            input.textComponent=RacingDesign.Label(search.transform,"",RacingType.Body,.035f,.05f,.93f,.90f,RacingDesign.Ink);
            var placeholder=RacingDesign.Label(search.transform,"#position / exact name / racer ID",RacingType.Caption,.035f,.05f,.93f,.90f,RacingDesign.Muted);input.placeholder=placeholder;
            searchFeedback=RacingDesign.Label(root,"",RacingType.Caption,.61f,.700f,.34f,.03f,RacingDesign.Muted);
            input.onEndEdit.AddListener(Locate);
            string[] headers={"POS","RACER / TITLE","MMR / RANK","LOADOUT","PEAK","MOVE"};float[] xs={.05f,.175f,.445f,.645f,.835f,.91f},ws={.075f,.26f,.19f,.185f,.07f,.05f};
            for(int i=0;i<headers.Length;i++)RacingDesign.Label(root,headers[i],RacingType.Caption,xs[i],.66f,ws[i],.045f,RacingDesign.Muted);
            viewport=RacingDesign.Area(root,"Virtualized leaderboard viewport",.05f,.15f,.90f,.505f);
            viewport.gameObject.AddComponent<Image>().color=RacingDesign.Surface; viewport.gameObject.AddComponent<RectMask2D>();
            scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;
            content=RacingDesign.Rect(viewport,"Indexed league page",new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);content.pivot=new Vector2(.5f,1);
            scroll.content=content;scroll.onValueChanged.AddListener(_=>RefreshRows());
            // Fourteen reusable rows cover the viewport plus overscan at the 1600x900 reference size.
            for(int i=0;i<14;i++)CreateRow();
            RacingDesign.Label(root,"LOCAL OFFLINE COMMUNITY",RacingType.Caption,.05f,.075f,.35f,.04f,RacingDesign.Muted);
            RacingDesign.Button(root,"HISTORY",.49f,.065f,.24f,.06f,()=>RacingHistoryUI.Open());
            RacingDesign.Button(root,"MY PROFILE",.77f,.065f,.18f,.06f,()=>RacingProfileUI.Open());
            RefreshStandings();
        }
        void CreateRow()
        {
            var area=RacingDesign.Rect(content,"Pooled league row "+pool.Count,new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);area.pivot=new Vector2(.5f,1);area.sizeDelta=new Vector2(0,RowHeight);
            var fill=area.gameObject.AddComponent<Image>();fill.color=RacingDesign.Surface;
            var row=new Row {area=area};
            row.position=RacingDesign.Label(area,"",RacingType.Body,.005f,.12f,.075f,.74f);
            row.badge=RacingDesign.RankBadge(area,0,.081f,.025f,.055f,.95f);
            row.name=RacingDesign.Label(area,"",RacingType.Body,.14f,.44f,.295f,.48f);
            row.title=RacingDesign.Label(area,"",RacingType.Caption,.14f,.04f,.295f,.38f);row.titleView=row.title.gameObject.AddComponent<RacingTitleView>();
            row.rating=RacingDesign.Label(area,"",RacingType.Body,.44f,.44f,.21f,.48f);
            row.rank=RacingDesign.Label(area,"",RacingType.Caption,.44f,.04f,.21f,.38f);
            row.combo=RacingComboThumbnail.Create(area,.655f,.07f,.207f,.86f);
            row.peak=RacingDesign.Label(area,"",RacingType.Body,.868f,.12f,.072f,.74f);
            row.movement=RacingDesign.Label(area,"",RacingType.Caption,.946f,.12f,.05f,.74f);
            var button=area.gameObject.AddComponent<Button>();button.targetGraphic=fill;
            button.onClick.AddListener(()=>{if(row.competitor!=null)RacingProfileUI.Open(row.competitor);});
            pool.Add(row);
        }
        void RefreshStandings()
        {
            var engine=RacingProgressionService.Engine;engine.RefreshHumanIndex();
            pageCount=Mathf.Min(PageSize,EntityCount-pageStart);content.sizeDelta=new Vector2(0,pageCount*RowHeight);
            observedRevision=RacingProgressionService.Revision;lastFirst=-1;
            int humanPosition=engine.Rank("player_human")+1;var you=engine.State.human;
            string standing=SeasonQualification.IsOfficial(you,engine.State.currentSeasonNumber)?$"YOU #{humanPosition:N0}":"YOU "+SeasonQualification.DisplayRank(you,engine.State.currentSeasonNumber);
            status.text=$"{GlobalSeasonCalendar.Label(engine.State.currentSeasonNumber)} / {standing} / {engine.RankedCount():N0} RANKED OF {EntityCount:N0} / {pageStart+1:N0}–{pageStart+pageCount:N0}";
            RefreshRows();
        }
        void RefreshRows()
        {
            int first=Mathf.Clamp(Mathf.FloorToInt(content.anchoredPosition.y/RowHeight),0,Mathf.Max(0,pageCount-1));
            if(first==lastFirst)return;lastFirst=first;
            var engine=RacingProgressionService.Engine;
            for(int i=0;i<pool.Count;i++) {
                var row=pool[i];Remember(row);int index=first+i;row.area.gameObject.SetActive(index<pageCount);
                if(index>=pageCount){row.competitor=null;continue;}
                int position=pageStart+index+1;var competitor=engine.View(engine.Index.AtRank(position-1));row.competitor=competitor;row.positionNumber=position;
                row.area.anchoredPosition=new Vector2(0,-index*RowHeight);
                row.area.GetComponent<Image>().color=competitor==RacingProgressionService.State.human?RacingDesign.Highlight:(index%2==0?RacingDesign.Surface:RacingDesign.Background);
                bool ranked=competitor.Ranked;
                bool official=SeasonQualification.IsOfficial(competitor,engine.State.currentSeasonNumber);
                row.position.text=official?position.ToString("N0"):"—";row.name.text=competitor.displayName+(competitor==RacingProgressionService.State.human?" / YOU":"");
                // CPPS: no visible rating or rank before Placement 3/3.
                row.rating.text=ranked?competitor.currentMmr.ToString("N0"):MmrTierUtility.UnrankedName;row.rank.text=SeasonQualification.DisplayRank(competitor,engine.State.currentSeasonNumber);row.rank.color=official?RacingDesign.TierColor(competitor.currentMmr):RacingDesign.Muted;
                row.peak.text=ranked?competitor.seasonPeakMmr.ToString("N0"):"—";row.badge.Configure(competitor.currentMmr);row.badge.gameObject.SetActive(official);
                row.combo.Configure(competitor);
                int moved=competitor.lastLeaderboardPosition<=0?0:competitor.lastLeaderboardPosition-position;
                row.movement.text=moved>0?"↑"+moved:moved<0?"↓"+(-moved):"—";row.movement.color=moved>0?RacingDesign.Success:moved<0?RacingDesign.Danger:RacingDesign.Muted;
                row.titleView.Configure(row.title,competitor.selectedTitleId,RacingProgressionService.State.currentSeasonNumber,competitor.seasonPeakMmr,official?position:0);
            }
        }
        void Remember(Row row){if(row.competitor!=null&&SeasonQualification.IsOfficial(row.competitor,RacingProgressionService.State.currentSeasonNumber))RacingProgressionService.RememberLeaderboardPosition(row.competitor.id,row.positionNumber);}
        void GoTo(int rank) {
            if(searchFeedback!=null)searchFeedback.text="";
            pageStart=Mathf.Clamp(rank,0,Mathf.Max(0,EntityCount-1));content.anchoredPosition=Vector2.zero;
            lastFirst=-1;RefreshStandings();scroll.verticalNormalizedPosition=1;
        }
        void Locate(string value) {
            value=(value??"").Trim();
            // #position is explicit; a bare number may be an existing name.
            if(value.StartsWith("#",StringComparison.Ordinal)&&int.TryParse(value.Substring(1),out int position)){GoTo(position-1);return;}
            if(value.Equals("PLAYER",StringComparison.OrdinalIgnoreCase)){NearHuman();return;}
            var engine=RacingProgressionService.Engine;
            int rank=engine.Rank(value.ToLowerInvariant()),matches=rank>=0?1:0;
            if(rank<0) {
                if(identityLookup==null||!ReferenceEquals(lookupEngine,engine)){lookupEngine=engine;identityLookup=new LeagueIdentityLookup(engine);}
                identityLookup.TryLocate(value,out rank,out matches);
            }
            if(rank>=0){GoTo(Mathf.Max(0,rank-3));searchFeedback.text=matches>1?matches+" MATCHES / USE RACER ID TO DISAMBIGUATE":"";}
            else if(int.TryParse(value,out position))GoTo(position-1);
            else searchFeedback.text=value.Length==0?"":"NO EXACT NAME OR ID MATCH";
        }
        public void NearHuman()=>GoTo(Mathf.Max(0,RacingProgressionService.Engine.Rank("player_human")-3));
        void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.5f;if(observedRevision!=RacingProgressionService.Revision)RefreshStandings();}
        public void Close(){Destroy(root.GetComponentInParent<Canvas>().gameObject);}
        void OnDestroy(){foreach(var row in pool)Remember(row);RacingProgressionService.Save();}
    }
}
