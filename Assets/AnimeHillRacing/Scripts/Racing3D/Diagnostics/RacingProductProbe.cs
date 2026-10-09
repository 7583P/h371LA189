#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingProductProbe : MonoBehaviour
    {
        [Serializable] sealed class MenuPerformance
        {
            public string scope="Native Unity Editor menu scheduler, 10000 compact persistent bots; loading/capture frames excluded. Desktop race performance is measured separately.";
            public int schedulerFrames, matches, savedStateBytes;
            public double seconds, meanPumpMs, maxPumpMs, saveMs, saveWorkerMs;
            public bool gcCounterAvailable;
            public long totalGcBytes, maxGcBytes;
            public float meanFrameMs, p99FrameMs;
            public int bytesPerBot, journalBytes;
            public double startupMs;
            public long frozenPumpAllocatedBytes;
        }
        string output;int errors;readonly ClosureContractReport report=new ClosureContractReport();
        public void Begin(string directory) {output=directory;Directory.CreateDirectory(output);DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Logged;StartCoroutine(RunGuarded());}
        void Logged(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception)errors++;}
        void Capture(string name){RacingEvidenceCapture.Capture(Path.Combine(output,name+".png"));report.Check(File.Exists(Path.Combine(output,name+".png")),"Native rendered evidence: "+name);}
        IEnumerator WaitForCombos()
        {
            float deadline=Time.realtimeSinceStartup+8;
            while(Time.realtimeSinceStartup<deadline)
            {
                bool ready=true;
                foreach(var preview in FindObjectsByType<RacingComboThumbnail>(FindObjectsSortMode.None))ready &= preview.Texture!=null;
                if(ready){yield return null;yield break;}
                yield return null;
            }
            report.Check(false,"Requested visible combo bitmaps complete within the menu timeout");
        }
        IEnumerator ComboContracts(RacingLeaderboardUI league)
        {
            yield return WaitForCombos();
            var first=new LeagueCompetitor {id="thumbnail-fixture-a",vehiclePreference=MetaProgressionConfig.MetaVehicleId,wheelPreference=MetaProgressionConfig.MetaWheelId,characterPreference="cobalt-driver",accessoryPreference="none"};
            var second=new LeagueCompetitor {id="thumbnail-fixture-b",vehiclePreference=first.vehiclePreference,wheelPreference=first.wheelPreference,characterPreference=first.characterPreference,accessoryPreference=first.accessoryPreference};
            var a=RacingComboThumbnail.Create(league.transform,2,0,.1f,.1f);a.Configure(first);
            var b=RacingComboThumbnail.Create(league.transform,2,.2f,.1f,.1f);b.Configure(second);
            var changed=RacingComboThumbnail.Create(league.transform,2,.4f,.1f,.1f);changed.Configure(first.vehiclePreference,first.wheelPreference,first.characterPreference,"crystal-crest");
            yield return WaitForCombos();
            report.Check(a.Texture!=null && ReferenceEquals(a.Texture,b.Texture),"Different racer identities reuse the same full 3D combo bitmap");
            report.Check(changed.Texture!=null && changed.Texture!=a.Texture,"A visible accessory change produces a distinct combo capture");
            report.Check(a.Texture!=null && a.Texture.width==192 && a.Texture.height==108,"Native preview uses a small 192x108 bitmap");
            report.Check(league.GetComponentsInChildren<MeshRenderer>().Length==0 && league.GetComponentsInChildren<RacingVehicleVisuals>().Length==0,"Leaderboard rows contain cached UI images and no live 3D vehicle hierarchy");
            var studio=FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(camera=>camera.name=="Studio camera");
            report.Check(studio!=null && !studio.enabled,"Opaque leaderboard suspends the showroom camera underneath it");
            int generated=RacingComboThumbnailCache.GeneratedCount;
            var pinned=a.Texture;
            string[] vehicles={"hill-adventurer-01","car-01","rally-01","formula-01"};
            string[] wheels={"wheel-default","wheel-whity","wheel-plasma"};
            string[] drivers={"cobalt-driver","purple-driver","cyan-driver","ruby-driver"};
            string[] accessories={"none","signal-aerial","crystal-crest"};
            foreach(string vehicle in vehicles)foreach(string wheel in wheels)foreach(string driver in drivers)foreach(string accessory in accessories)
                changed.Configure(vehicle,wheel,driver,accessory);
            report.Check(RacingComboThumbnailCache.EntryCount<=RacingComboThumbnailCache.Capacity && a.Texture==pinned && b.Texture==pinned,"Rapid combo browsing stays bounded and cannot evict images still visible in other views");
            yield return WaitForCombos();
            report.Check(RacingComboThumbnailCache.GeneratedCount<=generated+1,"Abandoned browsing requests do not construct or render unused 3D combinations");
            Destroy(a.transform.parent.gameObject);Destroy(b.transform.parent.gameObject);Destroy(changed.transform.parent.gameObject);
            yield return null;
        }
        IEnumerator RunGuarded() {
            var work=Run();
            while(true) {
                object next;bool more;
                try {more=work.MoveNext();next=more?work.Current:null;}
                catch(Exception exception) {
                    Debug.LogException(exception);File.WriteAllText(Path.Combine(output,"native-product-exception.txt"),exception.ToString());
                    File.WriteAllText(Path.Combine(output,"native-product-tests.json"),JsonUtility.ToJson(report,true));
                    SessionState.SetInt("AHR.Racing.Closure.Exit",1);SessionState.SetBool("AHR.Racing.Closure.Done",true);yield break;
                }
                if(!more)yield break;yield return next;
            }
        }
        IEnumerator Run() {
            Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync("RacingGarage");yield return new WaitForSecondsRealtime(1);
            var state=RacingProgressionService.State;
            report.Check(state.bots.Length==10000 && state.bots.All(b=>b.currentMmr==0),"Fresh native local population has 10000 zero-MMR bots before activity");
            Capture("01-garage-current-rank");
            RacingSeasonUI.ShowRanks();yield return null;yield return null;Capture("02-native-rank-ladder");
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.name.IndexOf("rank ladder",StringComparison.OrdinalIgnoreCase)>=0)Destroy(canvas.gameObject);
            yield return null;
            var league=RacingLeaderboardUI.Open();yield return null;yield return ComboContracts(league);
            report.Check(league.EntityCount==10001 && league.PooledRowCount<=16,"Native leaderboard displays 10001 identities using at most sixteen pooled rows");Capture("03-fresh-leaderboard");
            Destroy(league.GetComponentInParent<Canvas>().gameObject);yield return null;
            RacingProgressionService.DiagnosticPauseScheduler(false);
            RacingProgressionService.DiagnosticSimulate(10000);RacingProgressionService.SetRaceActive(true);long events=state.simulationEventNumber;
            int capturesBeforeRace=RacingComboThumbnailCache.GeneratedCount;
            var deferred=RacingComboThumbnail.Create(transform,2,0,.1f,.1f);
            deferred.Configure("sport-bike-01","wheel-whity","ruby-driver","signal-aerial");
            long frozenBefore=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<10000;i++)RacingProgressionService.Pump();
            long frozenAllocation=GC.GetAllocatedBytesForCurrentThread()-frozenBefore;
            report.Check(frozenAllocation==0,"Ten thousand actual frozen production Pump calls allocate zero league bytes");
            for(int i=0;i<4;i++){RacingProgressionService.Pump();yield return null;}
            report.Check(state.simulationEventNumber==events,"League diagnostics and offline catch-up cannot advance during a race");
            report.Check(deferred.Texture==null && capturesBeforeRace==RacingComboThumbnailCache.GeneratedCount,"A requested combo during racing queues no mesh construction or rendering");
            RacingProgressionService.SetRaceActive(false);yield return WaitForCombos();
            report.Check(deferred.Texture!=null,"Deferred preview becomes available after gameplay ends");Destroy(deferred.transform.parent.gameObject);yield return null;
            var performance=new MenuPerformance();var frameTimes=new System.Collections.Generic.List<float>();
            performance.frozenPumpAllocatedBytes=frozenAllocation;performance.startupMs=RacingProgressionService.StartupMilliseconds;performance.bytesPerBot=LocalLeagueStore.BytesPerBot;
            var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            performance.gcCounterAvailable=gc.Valid;double startTime=Time.realtimeSinceStartupAsDouble;
            float deadline=Time.realtimeSinceStartup+60;
            while(RacingProgressionService.PendingDiagnosticEvents>0 && Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                performance.schedulerFrames++;performance.meanPumpMs+=RacingProgressionService.LastPumpMilliseconds;
                performance.maxPumpMs=Math.Max(performance.maxPumpMs,RacingProgressionService.LastPumpMilliseconds);
                frameTimes.Add(Time.unscaledDeltaTime*1000);
                if(gc.Valid){performance.totalGcBytes+=gc.LastValue;performance.maxGcBytes=Math.Max(performance.maxGcBytes,gc.LastValue);}
            }
            performance.seconds=Time.realtimeSinceStartupAsDouble-startTime;performance.matches=(int)(state.simulationEventNumber-events);
            gc.Dispose();
            performance.meanPumpMs/=Math.Max(1,performance.schedulerFrames);
            frameTimes.Sort();if(frameTimes.Count>0){performance.meanFrameMs=frameTimes.Average();performance.p99FrameMs=frameTimes[Math.Min(frameTimes.Count-1,(int)(frameTimes.Count*.99f))];}
            RacingProgressionService.MarkChanged();RacingProgressionService.Save();performance.saveMs=RacingProgressionService.LastSaveMilliseconds;
            while(RacingProgressionService.HasPendingSave){yield return null;RacingProgressionService.Save();}
            performance.saveWorkerMs=RacingProgressionService.LastSaveWorkerMilliseconds;
            performance.savedStateBytes=System.Text.Encoding.UTF8.GetByteCount(JsonUtility.ToJson(state));
            performance.journalBytes=(int)Directory.GetFiles(Path.Combine(output,"isolated-product-save"),"league-v3-*.journal").Sum(p=>new FileInfo(p).Length);
            File.WriteAllText(Path.Combine(output,"native-menu-performance.json"),JsonUtility.ToJson(performance,true));
            report.Check(RacingProgressionService.PendingDiagnosticEvents==0 && state.simulationEventNumber>=10000,"Ten thousand matches complete through the production menu-frame scheduler");
            league=RacingLeaderboardUI.Open();yield return null;yield return WaitForCombos();Capture("04-evolved-leaderboard");Destroy(league.GetComponentInParent<Canvas>().gameObject);yield return null;
            var lobby=MatchmakingService.GenerateRankedLobby(state.human.currentMmr,RacingSaveManager.SelectedVehicleId,RacingSaveManager.SelectedWheelId,"QA PLAYER",4207);
            report.Check(lobby.Length==12 && lobby.Count(x=>x.isHuman)==1 && lobby.Where(x=>!x.isHuman).All(x=>RacingProgressionService.Find(x.racerId)!=null),"Native production matchmaking selects eleven persistent league identities");
            report.Check(lobby.Where(x=>!x.isHuman).All(x=>{var saved=RacingProgressionService.Find(x.racerId);return x.vehicleId==saved.vehiclePreference && x.wheelId==saved.wheelPreference && x.characterId==saved.characterPreference && x.accessoryId==saved.accessoryPreference;}),"The eleven selected racers retain every saved combo component without rerolling or forced META");
            var garage=FindFirstObjectByType<RacingGarageUI>();garage.NextDriver();garage.NextAccessory();
            report.Check(state.human.characterPreference!="cobalt-driver" && state.human.accessoryPreference!="none","Real Garage buttons persist selected driver and accessory");
            yield return null;yield return null;Capture("08-native-selected-loadout");
            var competitors=lobby.Select(x=>RacingProgressionService.Find(x.racerId)).ToArray();var ratings=competitors.Select(x=>x.currentMmr).ToArray();var positions=Enumerable.Range(1,12).ToArray();var uncertainty=competitors.Select(x=>x.uncertainty).ToArray();var deltas=new int[12];
            LocalLeagueEngine.CalculateDeltas(ratings,positions,uncertainty,RacingProgressionService.Engine.Configuration,deltas);
            var results=new RaceResults{entries=lobby.Select((x,i)=>new RacerResultEntry {racerId=x.racerId,isHuman=x.isHuman,oldMmr=ratings[i],mmrDelta=deltas[i],finishPosition=i+1,bestLapTime=x.isHuman?75f:0,shortcutSuccesses=x.isHuman?1:0,aerialSuccesses=x.isHuman?1:0,landingSuccesses=x.isHuman?1:0}).ToArray()};
            int humanRaces=state.human.races;RacingProgressionService.RecordRankedResults(results);
            report.Check(state.human.races==humanRaces+1 && state.human.shortcutSuccesses==1 && state.human.aerialSuccesses==1 && state.human.landingSuccesses==1,"Explicit synthetic QA result fixture exercises persistent human counters without claiming driving or expert telemetry");
            RacingProgressionService.SetHumanMmr(12000);report.Check(RacingProgressionService.SelectHumanTitle("beyond-apex"),"Native title inventory selects an unlocked neon title");
            var profile=RacingProfileUI.Open();yield return null;yield return WaitForCombos();Capture("05-native-title-inventory");Destroy(profile.GetComponentInParent<Canvas>().gameObject);yield return null;
            File.WriteAllText(Path.Combine(output,"combo-cache.txt"),$"generated={RacingComboThumbnailCache.GeneratedCount}\nhits={RacingComboThumbnailCache.CacheHits}\nentries={RacingComboThumbnailCache.EntryCount}\ntextureBytes={RacingComboThumbnailCache.TextureBytes}\ncapacity={RacingComboThumbnailCache.Capacity}\n");
            long start=state.seasonEndTimestamp;RacingProgressionService.DiagnosticAdvance(TimeSpan.FromTicks(start-state.lastKnownUtcTimestamp));
            deadline=Time.realtimeSinceStartup+60;while(state.currentSeasonNumber==1 && Time.realtimeSinceStartup<deadline)yield return null;
            report.Check(state.currentSeasonNumber==2 && state.human.history.Count==1 && state.human.currentMmr==10000 && state.human.peakMmr==12000,"Native offline transition applies the exact >10k reset and retains historical prestige");
            var season=RacingSeasonUI.Open();yield return null;yield return null;Capture("06-native-season-history");Destroy(season.GetComponentInParent<Canvas>().gameObject);yield return null;
            // Dedicated native 3D showroom: actual low-poly models, no flat board image.
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.renderMode!=RenderMode.WorldSpace)canvas.enabled=false;
            var oldCamera=Camera.main;if(oldCamera!=null){oldCamera.enabled=false;oldCamera.tag="Untagged";}
            var cameraRoot=new GameObject("Native item showcase camera");var camera=cameraRoot.AddComponent<Camera>();cameraRoot.tag="MainCamera";
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.005f,.012f,.035f);camera.orthographic=true;camera.orthographicSize=3.65f;camera.transform.position=new Vector3(5000,101,-10);camera.transform.LookAt(new Vector3(5000,101,0));
            foreach(var existing in FindObjectsByType<Light>(FindObjectsSortMode.None))existing.enabled=false;
            RenderSettings.ambientLight=new Color(.12f,.15f,.20f);RenderSettings.ambientIntensity=.6f;
            var light=new GameObject("Item showcase light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.9f;light.transform.rotation=Quaternion.Euler(30,-30,0);
            var labels=RacingDesign.Canvas("Native twelve item showcase",150);RacingDesign.Label(labels,"NIGHT CITY / TWELVE ITEMS",RacingType.Title,.05f,.90f,.9f,.065f,RacingDesign.Cyan);
            for(int i=0;i<12;i++) {
                var item=(RaceItemType)(i+1);var model=new GameObject(RaceItemUtility.DisplayName(item));model.transform.position=new Vector3(4997+(i%4)*2,103.15f-(i/4)*2,0);model.transform.rotation=Quaternion.Euler(0,35,8);model.transform.localScale=Vector3.one*.72f;RacingItemArt.Build(model.transform,item);
                RacingDesign.Label(labels,RaceItemUtility.DisplayName(item),RacingType.Caption,.055f+(i%4)*.245f,.64f-(i/4)*.27f,.22f,.045f,RaceItemUtility.ColorFor(item),TextAnchor.MiddleCenter);
            }
            yield return null;yield return null;Capture("07-native-twelve-item-meshes");
            File.WriteAllText(Path.Combine(output,"native-product-tests.json"),JsonUtility.ToJson(report,true));
            File.WriteAllText(Path.Combine(output,"native-product-state.json"),JsonUtility.ToJson(state,true));
            RacingProgressionService.Save(true);
            var stored=new LocalLeagueStore(Path.Combine(output,"isolated-product-save"),value=>JsonUtility.ToJson(value),JsonUtility.FromJson<LocalLeagueState>).Load();
            report.Check(stored!=null && stored.currentSeasonNumber==state.currentSeasonNumber && stored.human.selectedTitleId==state.human.selectedTitleId && stored.human.peakMmr==state.human.peakMmr && stored.bots.Select(b=>b.currentMmr).SequenceEqual(state.bots.Select(b=>b.currentMmr)),"Background native binary journal commits reload complete season, title, prestige and all 10000 compact ratings");
            yield return RacingVipShopProbe.Verify(report,output);
            File.WriteAllText(Path.Combine(output,"native-product-tests.json"),JsonUtility.ToJson(report,true));
            Application.logMessageReceived-=Logged;
#if UNITY_EDITOR
            SessionState.SetInt("AHR.Racing.Closure.Exit",report.failed==0 && errors==0?0:1);SessionState.SetBool("AHR.Racing.Closure.Done",true);
#else
            Application.Quit(report.failed==0 && errors==0?0:1);
#endif
        }
    }
}
#endif
