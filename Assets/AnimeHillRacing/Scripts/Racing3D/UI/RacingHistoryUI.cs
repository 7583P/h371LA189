using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    // Fourteen recycled rows regardless of archive population. Disk reads,
    // decompression, search, export and lifecycle operations run off-thread.
    public sealed class RacingHistoryUI : MonoBehaviour
    {
        const float RowHeight=66;
        const int PageSize=1000;
        sealed class Row {public RectTransform root;public Text position,name,title,rating,rank;public RacingTitleView titleView;public RacingComboThumbnail combo;}
        readonly List<Row> rows=new List<Row>();
        RectTransform root,content,viewport;
        ScrollRect scroll;
        Text status,storage,heading;
        Button export,delete,restore;
        List<SeasonHistoryInfo> seasons=new List<SeasonHistoryInfo>();
        SeasonHistoryStore store;
        Task<HistoricalStanding[]> pendingRows;
        int selected,filter,pageStart,pageCount,requestedFirst=-1,displayedFirst=-1,selectionVersion,rowVersion;
        bool busy,showDeleted;
        SeasonHistoryInfo Current=>selected>=0&&selected<seasons.Count?seasons[selected]:null;

        public static RacingHistoryUI Open()
        {
            var canvas=RacingDesign.Canvas("LOCAL SEASON ARCHIVES",125);canvas.gameObject.AddComponent<RacingMenuOverlay>();
            var view=canvas.gameObject.AddComponent<RacingHistoryUI>();view.root=canvas;view.store=RacingProgressionService.History;view.Build();return view;
        }
        void Build()
        {
            var bg=RacingDesign.Panel(root,"History backdrop",0,0,1,1,RacingDesign.Background,0);bg.raycastTarget=true;
            heading=RacingDesign.Label(root,"SEASON HISTORY",RacingType.Title,.05f,.89f,.72f,.075f,RacingDesign.Cyan);
            RacingDesign.Button(root,"CLOSE",.83f,.895f,.12f,.06f,()=>Destroy(root.GetComponentInParent<Canvas>().gameObject));
            status=RacingDesign.Label(root,"Loading local archives…",RacingType.Caption,.05f,.825f,.90f,.055f,RacingDesign.Muted);
            RacingDesign.Button(root,"< SEASON",.05f,.75f,.14f,.055f,()=>Select(selected-1));
            RacingDesign.Button(root,"SEASON >",.20f,.75f,.14f,.055f,()=>Select(selected+1));
            int[] bands={10,100,1000,0};string[] labels={"TOP 10","TOP 100","TOP 1000","ALL"};
            for(int i=0;i<bands.Length;i++){int band=bands[i];RacingDesign.Button(root,labels[i],.36f+i*.15f,.75f,.14f,.055f,()=>{filter=band;GoTo(0);});}
            var search=RacingDesign.Panel(root,"Search historical names",.05f,.675f,.37f,.055f,RacingDesign.Surface,4);search.raycastTarget=true;
            var input=search.gameObject.AddComponent<InputField>();input.targetGraphic=search;
            input.textComponent=RacingDesign.Label(search.transform,"",RacingType.Body,.03f,0,.94f,1);
            input.placeholder=RacingDesign.Label(search.transform,"Search historical name or ID",RacingType.Caption,.03f,0,.94f,1,RacingDesign.Muted);
            input.onEndEdit.AddListener(value=>Search(value,false));
            RacingDesign.Button(root,"MY POSITION",.44f,.675f,.20f,.055f,()=>Search("",true));
            RacingDesign.Button(root,"RECENTLY DELETED",.66f,.675f,.29f,.055f,()=>{showDeleted=!showDeleted;StartCoroutine(Reload());});
            viewport=RacingDesign.Area(root,"History viewport",.05f,.21f,.90f,.445f);
            viewport.gameObject.AddComponent<Image>().color=RacingDesign.Surface;viewport.gameObject.AddComponent<RectMask2D>();
            scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=44;
            content=RacingDesign.Rect(viewport,"History page",new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);content.pivot=new Vector2(.5f,1);scroll.content=content;
            for(int i=0;i<14;i++){
                var area=RacingDesign.Rect(content,"Pooled history row "+i,new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);area.pivot=new Vector2(.5f,1);area.sizeDelta=new Vector2(0,RowHeight);
                area.gameObject.AddComponent<Image>().color=i%2==0?RacingDesign.Surface:RacingDesign.Background;
                var row=new Row{root=area,position=RacingDesign.Label(area,"",RacingType.Body,.01f,0,.08f,1),name=RacingDesign.Label(area,"",RacingType.Body,.105f,.48f,.39f,.48f),
                    title=RacingDesign.Label(area,"",RacingType.Caption,.105f,.06f,.39f,.36f),rating=RacingDesign.Label(area,"",RacingType.Body,.50f,.48f,.21f,.48f),rank=RacingDesign.Label(area,"",RacingType.Caption,.50f,.06f,.23f,.36f)};
                row.titleView=row.title.gameObject.AddComponent<RacingTitleView>();row.combo=RacingComboThumbnail.Create(area,.77f,.06f,.22f,.88f);rows.Add(row);
            }
            RacingDesign.Button(root,"< PAGE",.05f,.145f,.15f,.05f,()=>GoTo(pageStart-PageSize));
            RacingDesign.Button(root,"PAGE >",.21f,.145f,.15f,.05f,()=>GoTo(pageStart+PageSize));
            export=RacingDesign.Button(root,"EXPORT SEASON",.39f,.145f,.25f,.05f,Export);
            delete=RacingDesign.Button(root,"DELETE SEASON HISTORY",.66f,.145f,.29f,.05f,ConfirmDelete);
            restore=RacingDesign.Button(root,"RESTORE",.66f,.145f,.29f,.05f,Restore);
            storage=RacingDesign.Label(root,"",RacingType.Caption,.05f,.075f,.90f,.05f,RacingDesign.Muted);
            RacingDesign.Label(root,"LOCAL ONLY · Names captured at season end · Export and delete are separate actions",RacingType.Caption,.05f,.025f,.90f,.04f,RacingDesign.Muted);
            StartCoroutine(Reload());
        }
        IEnumerator Reload()
        {
            busy=true;string careerDirectory=RacingProgressionService.CareerHistoryDirectory;
            var task=Task.Run(()=>{lock(store){long career=0;if(Directory.Exists(careerDirectory))foreach(string path in Directory.GetFiles(careerDirectory,"season-*.bin"))career+=new FileInfo(path).Length;return (entries:store.List(),usage:store.StorageUsage(),careerBytes:career);}});
            while(!task.IsCompleted)yield return null;
            busy=false;if(task.IsFaulted){status.text=task.Exception.GetBaseException().Message;yield break;}
            seasons=task.Result.entries.FindAll(info=>info.deleted==showDeleted);selected=0;
            var usage=task.Result.usage;long career=task.Result.careerBytes;
            storage.text=$"LEADERBOARD ACTIVE {usage.activeBytes/1048576.0:F2} MiB / TRASH {usage.trashBytes/1048576.0:F2} MiB / RECOVERABLE {usage.recoverableBytes/1048576.0:F2} MiB\nCAREER RECORDS RETAINED {career/1048576.0:F2} MiB / HISTORY TOTAL {(usage.TotalBytes+career)/1048576.0:F2} MiB";
            Select(0);
        }
        void Select(int index)
        {
            if(busy)return;selected=Mathf.Clamp(index,0,Mathf.Max(0,seasons.Count-1));selectionVersion++;
            var season=Current;bool available=season!=null&&string.IsNullOrEmpty(season.unavailableReason);
            export.interactable=available&&!season.deleted;delete.gameObject.SetActive(!showDeleted);restore.gameObject.SetActive(showDeleted);delete.interactable=available;restore.interactable=available;
            heading.text=season==null?(showDeleted?"RECENTLY DELETED":"SEASON HISTORY"):$"SEASON {season.season} / "+(showDeleted?"RECENTLY DELETED":"FINAL STANDINGS");
            status.text=season==null?"No completed local season archive is available. Older saves without final standings cannot be reconstructed.":available?$"{season.qualified:N0} OFFICIAL / {season.count:N0} COMPETITORS / {season.bytes/1048576.0:F2} MiB":season.unavailableReason;
            GoTo(0);
        }
        void GoTo(int first)
        {
            selectionVersion++;
            var season=Current;int total=season==null||!string.IsNullOrEmpty(season.unavailableReason)?0:filter>0?Math.Min(filter,season.qualified):season.count;
            pageStart=Mathf.Clamp(first,0,Mathf.Max(0,total-1));pageCount=Math.Min(PageSize,total-pageStart);content.sizeDelta=new Vector2(0,pageCount*RowHeight);content.anchoredPosition=Vector2.zero;scroll.verticalNormalizedPosition=1;displayedFirst=-1;
            foreach(var row in rows)row.root.gameObject.SetActive(false);
        }
        void Update()
        {
            if(busy||Current==null||pageCount==0)return;
            int first=Mathf.Clamp(Mathf.FloorToInt(content.anchoredPosition.y/RowHeight),0,Mathf.Max(0,pageCount-1));
            if(pendingRows!=null){
                if(!pendingRows.IsCompleted)return;
                var done=pendingRows;pendingRows=null;
                if(done.IsFaulted){status.text=done.Exception.GetBaseException().Message;return;}
                if(rowVersion==selectionVersion&&requestedFirst==first){Bind(done.Result,first);displayedFirst=first;}
            }
            if(displayedFirst==first)return;
            int season=Current.season;bool deleted=Current.deleted;int offset=pageStart+first,count=Math.Min(rows.Count,pageCount-first);
            requestedFirst=first;rowVersion=selectionVersion;
            pendingRows=Task.Run(()=>{using(var reader=store.Open(season,deleted))return reader.Page(offset,count);});
        }
        void Bind(HistoricalStanding[] data,int first)
        {
            for(int i=0;i<rows.Count;i++){
                var row=rows[i];row.root.gameObject.SetActive(i<data.Length);if(i>=data.Length)continue;var entry=data[i];
                row.root.anchoredPosition=new Vector2(0,-(first+i)*RowHeight);row.position.text=entry.position>0?entry.position.ToString("N0"):"—";
                row.name.text=entry.name+(entry.node<0?" / YOU":"");row.rating.text=entry.placement>=3?entry.mmr.ToString("N0")+" MMR":MmrTierUtility.UnrankedName;
                row.rank.text=entry.placement<3?MmrTierUtility.PlacementLabel(entry.placement):entry.position==0?SeasonQualification.PendingLabel:MmrTierUtility.GetDisplayName(entry.mmr);
                row.titleView.Configure(row.title,entry.title,Current.season,entry.mmr,entry.position);
                row.combo.Configure(new LeagueCompetitor{vehiclePreference=LeagueBotCatalog.Vehicles[entry.vehicle],wheelPreference=LeagueBotCatalog.Wheels[entry.wheels],characterPreference=LeagueBotCatalog.Characters[entry.character],accessoryPreference=LeagueBotCatalog.Accessories[entry.accessory]});
            }
        }
        void Search(string value,bool human)
        {
            if(busy||Current==null||!string.IsNullOrEmpty(Current.unavailableReason))return;
            int season=Current.season,version=selectionVersion;bool deleted=Current.deleted;
            StartCoroutine(Work(Task.Run(()=>{using(var reader=store.Open(season,deleted))return reader.Find(value,human);}),index=>{if(version!=selectionVersion)return;if(index>=0){filter=0;GoTo(Math.Max(0,index-2));}else status.text="No matching historical racer.";}));
        }
        IEnumerator Work<T>(Task<T> task,Action<T> done)
        {
            busy=true;while(!task.IsCompleted)yield return null;busy=false;
            if(task.IsFaulted){status.text=task.Exception.GetBaseException().Message;yield break;}done(task.Result);
        }
        void Export()
        {
            if(busy||Current==null||Current.deleted)return;
            SeasonHistoryDocumentSave.Begin(store,Current.season,message=>{if(this!=null)status.text=message;});
        }
        void ConfirmDelete()
        {
            if(busy||Current==null||Current.deleted)return;int season=Current.season,current=RacingProgressionService.State.currentSeasonNumber;
            StartCoroutine(Work(Task.Run(()=>{lock(store)return store.PrepareDeletion(season,current,DateTime.UtcNow.Ticks);}),confirmed=>{
                var panel=RacingDesign.Panel(root,"Confirm local history deletion",.17f,.28f,.66f,.43f,RacingDesign.Background,8);panel.raycastTarget=true;
                string date=new DateTime(confirmed.earliestDeleteUtc,DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                RacingDesign.Label(panel.transform,$"DELETE LOCAL SEASON {season} STANDINGS?\n{confirmed.bytes:N0} bytes move to Recently Deleted.\nRestore within 30 real elapsed days. Earliest final deletion: {date}.\nDeletion may wait longer while offline or after a clock anomaly.\nTrash uses storage. Career records, earned titles and exported ZIPs stay.",RacingType.Body,.05f,.29f,.90f,.65f,RacingDesign.Ink,TextAnchor.MiddleCenter);
                RacingDesign.Button(panel.transform,"CANCEL",.05f,.07f,.40f,.15f,()=>Destroy(panel.gameObject));
                RacingDesign.Button(panel.transform,"MOVE TO TRASH",.51f,.07f,.44f,.15f,()=>{Destroy(panel.gameObject);StartCoroutine(Work(Task.Run(()=>{lock(store)store.MoveToTrash(confirmed,current,Stopwatch.GetTimestamp()*1000/Stopwatch.Frequency);return true;}),_=>StartCoroutine(Reload())));});
            }));
        }
        void Restore()
        {
            if(busy||Current==null||!Current.deleted)return;int season=Current.season;
            StartCoroutine(Work(Task.Run(()=>{lock(store)return store.Restore(season);}),ok=>{if(ok)StartCoroutine(Reload());else status.text="The trustworthy recovery interval has ended.";}));
        }
    }
}
