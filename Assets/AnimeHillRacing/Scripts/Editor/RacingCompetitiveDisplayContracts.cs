#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.EditorTools
{
    [InitializeOnLoad]
    public static class RacingCompetitiveDisplayContracts
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Session="HA.ReleaseDisplayContracts.";
        sealed class Screen {public string name;public GameObject canvas,host;}
        static readonly List<Screen> screens=new List<Screen>();
        static GameObject screenshotCamera;
        static string screenshotOutput;
        static ClosureContractReport activeReport;
        static int screenIndex;
        static double captureAt;
        static RacingCompetitiveDisplayContracts()
        {
            EditorApplication.playModeStateChanged+=Changed;
            EditorApplication.quitting+=RestoreCompany;
        }
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use an isolated batch editor");
            if(Directory.Exists(Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"))))throw new InvalidOperationException("Preserve previous UI run");
            SessionState.SetString(Session+"Company",PlayerSettings.companyName);SessionState.SetBool(Session+"Active",true);
            PlayerSettings.companyName="HillAdventurer.BlacklineQA";
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.playModeStartScene=null;
            EditorApplication.isPlaying=true;
        }
        static void RestoreCompany()
        {
            if(!SessionState.GetBool(Session+"Active",false))return;
            PlayerSettings.companyName=SessionState.GetString(Session+"Company","DefaultCompany");SessionState.SetBool(Session+"Active",false);
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Session+"Active",false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=()=> {
                try {RunFixtures();screenIndex=0;ShowScreen();EditorApplication.update+=CaptureTick;}
                catch(Exception error){Debug.LogException(error);Finish(1);}
            };
            if(state==PlayModeStateChange.EnteredEditMode){int code=SessionState.GetInt(Session+"Exit",1);RestoreCompany();EditorApplication.Exit(code);}
        }
        static void ShowScreen()
        {
            var screen=screens[screenIndex];screen.canvas.SetActive(true);if(screen.host!=null)screen.host.SetActive(true);
            // Allow the real fade/counter Update methods to run. No animation
            // fields or CanvasGroup alpha values are rewritten by the fixture.
            captureAt=EditorApplication.timeSinceStartup+1.7;
        }
        static void CaptureTick()
        {
            if(EditorApplication.timeSinceStartup<captureAt)return;
            try {
                var screen=screens[screenIndex];string path=Path.Combine(screenshotOutput,screen.name+".png");RacingEvidenceCapture.Capture(path);
                var texture=new Texture2D(2,2);int uiPixels=0;
                try {texture.LoadImage(File.ReadAllBytes(path));foreach(var color in texture.GetPixels32())if(color.r<50&&color.g<70&&color.b<100)uiPixels++;}
                finally {UnityEngine.Object.DestroyImmediate(texture);}
                activeReport.Check(uiPixels>50000,screen.name+" renders visible UI after actual animation frames");
                UnityEngine.Object.DestroyImmediate(screen.canvas);if(screen.host!=null)UnityEngine.Object.DestroyImmediate(screen.host);
                screenIndex++;if(screenIndex<screens.Count){ShowScreen();return;}
                File.WriteAllText(Path.Combine(screenshotOutput,"competitive-display-contracts.json"),JsonUtility.ToJson(activeReport,true));
                Finish(activeReport.failed==0?0:1);
            }
            catch(Exception error){activeReport.Check(false,error.ToString());File.WriteAllText(Path.Combine(screenshotOutput,"competitive-display-contracts.json"),JsonUtility.ToJson(activeReport,true));Finish(1);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=CaptureTick;if(screenshotCamera!=null)UnityEngine.Object.DestroyImmediate(screenshotCamera);
            SessionState.SetInt(Session+"Exit",code);EditorApplication.isPlaying=false;
        }
        static void RunFixtures()
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));
            // Runtime startup can create this run's isolated-product-save before
            // the test callback. The entry point already reserved a fresh root.
            if(File.Exists(Path.Combine(output,"competitive-display-contracts.json")))throw new InvalidOperationException("Preserve previous UI contracts");
            Directory.CreateDirectory(output);string company=PlayerSettings.companyName;
            var report=new ClosureContractReport();activeReport=report;screenshotOutput=output;screens.Clear();
            try {
                PlayerSettings.companyName="HillAdventurer.BlacklineQA";
                if(Application.companyName!="HillAdventurer.BlacklineQA")throw new InvalidOperationException("Native QA preferences must be isolated");
                var camera=new GameObject("QA UI camera",typeof(Camera));camera.tag="MainCamera";camera.transform.position=new Vector3(0,1,-10);
                screenshotCamera=camera;
                using(new RacingPlayerPrefsTestScope()) {
                    var engine=RacingProgressionService.Engine;var human=engine.State.human;
                    human.placementGames=3;human.currentMmr=5123;human.qualifiedSeason=engine.State.currentSeasonNumber;engine.RefreshHumanIndex();
                    // Population seeds are fresh. Pick an existing unique name;
                    // duplicate-name priority has its own core contracts.
                    int target=Enumerable.Range(0,engine.Population).GroupBy(engine.DisplayName,StringComparer.OrdinalIgnoreCase).First(g=>g.Count()==1).First();
                    string id=engine.View(target).id,name=engine.DisplayName(target);int expected=Math.Max(0,engine.Rank(id)-3);
                    var board=RacingLeaderboardUI.Open();var input=board.GetComponentInChildren<InputField>(true);
                    void Locate(string query) {
                        typeof(RacingLeaderboardUI).GetMethod("GoTo",Private).Invoke(board,new object[]{0});
                        input.SetTextWithoutNotify(query);
                        input.onEndEdit.Invoke(query);
                    }
                    Locate(id);report.Check((int)typeof(RacingLeaderboardUI).GetField("pageStart",Private).GetValue(board)==expected,"Actual leaderboard input locates an existing persistent ID");
                    Locate(name);report.Check((int)typeof(RacingLeaderboardUI).GetField("pageStart",Private).GetValue(board)==expected,"Actual leaderboard input locates that same persistent name");
                    Locate(name.ToUpperInvariant());report.Check((int)typeof(RacingLeaderboardUI).GetField("pageStart",Private).GetValue(board)==expected,"Persistent name input is case insensitive");
                    int population=engine.Population;Locate("missing-racer-do-not-manufacture");report.Check(engine.Population==population,"Missing UI search never adds a competitor");
                    int numericName=Enumerable.Range(0,engine.Population).Where(i=>int.TryParse(engine.DisplayName(i),out int number)&&engine.Rank(engine.View(i).id)!=number-1).DefaultIfEmpty(-1).First();
                    report.Check(numericName>=0,"Existing population supplies a numeric name without creating a profile");
                    if(numericName>=0) {
                        string numeric=engine.DisplayName(numericName);int numericRank=engine.Rank(engine.View(numericName).id);
                        Locate(numeric);report.Check((int)typeof(RacingLeaderboardUI).GetField("pageStart",Private).GetValue(board)==Math.Max(0,numericRank-3),"Bare numeric name locates the existing racer instead of an unrelated position");
                        Locate("#211");report.Check((int)typeof(RacingLeaderboardUI).GetField("pageStart",Private).GetValue(board)==210,"Explicit hash position remains unambiguous when numeric racer names exist");
                        Locate(numeric);
                    }
                    screens.Add(new Screen{name="leaderboard-name-search",canvas=board.gameObject});board.gameObject.SetActive(false);
                    void Results(string label,int before,int after,int placementBefore,int placementAfter,bool persist,byte state,bool finished=true) {
                        human.currentMmr=after;human.placementGames=placementAfter;human.qualifiedSeason=engine.State.currentSeasonNumber;engine.RefreshHumanIndex();
                        var host=new GameObject("Result fixture / "+label);var manager=host.AddComponent<RaceManager>();manager.enabled=false;manager.PersistResults=persist;
                        var ui=host.AddComponent<RacingResultsUI>();typeof(RacingResultsUI).GetField("raceManager",Private).SetValue(ui,manager);
                        var entry=new RacerResultEntry{racerId=human.id,displayName="PLAYER",isHuman=true,finishPosition=finished?1:12,finished=finished,totalTime=210,
                            oldMmr=before,newMmr=after,mmrDelta=after-before,placementBefore=placementBefore,placementAfter=placementAfter,vehicleId="formula-01",wheelId="wheel-whity"};
                        var result=new RaceResults{rankedRaceId="isolated-ui-fixture-not-a-real-race",settlementState=state,entries=new[]{entry}};
                        long supplyBefore=human.currentMmr+engine.State.bots.Sum(b=>(long)b.currentMmr);ui.ShowResults(result);
                        var canvas=(RectTransform)typeof(RacingResultsUI).GetField("root",Private).GetValue(ui);
                        var table=canvas.Find("Race classification");var texts=table.GetComponentsInChildren<Text>(true).Select(t=>t.text).ToArray();
                        bool official=persist&&state>=(byte)RaceSettlementState.RaceSettled&&state<=(byte)RaceSettlementState.ProfileSaved;
                        int shown=official?after:before,placement=official?placementAfter:placementBefore;
                        report.Check(texts.Contains(placement<3?MmrTierUtility.UnrankedName:shown.ToString("N0")),label+" table uses authoritative final MMR or preserved unrated MMR");
                        if(before!=after&&placement>=3)report.Check(!texts.Contains((official?before:after).ToString("N0")),label+" table does not silently mix old/new MMR");
                        if(persist&&!official)report.Check(canvas.GetComponentsInChildren<Text>(true).Any(t=>t.text=="MMR UNSETTLED / NO CHANGE SHOWN"),label+" does not show a rating change without a final receipt");
                        ui.ShowResults(result);report.Check(human.currentMmr+engine.State.bots.Sum(b=>(long)b.currentMmr)==supplyBefore,label+" repeated display cannot apply a settlement");
                        if(!finished)report.Check(texts.Contains("DNF"),label+" displays DNF without an invented finish time");
                        screens.Add(new Screen{name=label,canvas=canvas.gameObject,host=host});canvas.gameObject.SetActive(false);host.SetActive(false);
                    }
                    Results("settled-ranked",5000,5123,3,3,true,(byte)RaceSettlementState.RaceSettled);
                    Results("third-placement",0,6500,2,3,true,(byte)RaceSettlementState.ProfileSaved);
                    Results("no-receipt",5000,5123,3,3,true,(byte)RaceSettlementState.Started);
                    Results("unrated",5000,5123,3,3,false,(byte)RaceSettlementState.Reserved);
                    Results("settled-dnf",5000,4980,3,3,true,(byte)RaceSettlementState.RaceSettled,false);
                    // Match the real Preparing -> BuildLobby service sequence.
                    // The pure coordinator contracts cannot expose an extra
                    // wall-clock Resume inserted by its Unity host.
                    // Match the scene host: Loading inside Racing3D is external
                    // gameplay, but bounded preparation must still catch up.
                    RacingProgressionService.SetExternalGameplay(true);
                    RacingProgressionService.SetRaceActive(false);
                    bool ready=false;
                    for(int guard=0;guard<1000&&!ready;guard++)ready=RacingProgressionService.PrepareRankedRace();
                    report.Check(RacingProgressionService.Coordinator.CaughtUp,"Unity league is caught up before Preparing");
                    report.Check(RacingProgressionService.IsRaceActive,"Bounded preparation preserves the scene host gameplay pause");
                    int beforeReservation=human.currentMmr;RacingProgressionService.SetRaceActive(true);
                    long acceptedBefore=engine.State.lastKnownUtcTimestamp,simulatedBefore=engine.State.lastLeagueSimulationUtc;
                    System.Threading.Thread.Sleep(4);
                    try {
                        var pending=RacingProgressionService.ReserveRankedRace(4301);
                        File.WriteAllText(Path.Combine(output,"reservation-observation.json"),JsonUtility.ToJson(new ReservationObservation {
                            acceptedBefore=acceptedBefore,simulatedBefore=simulatedBefore,acceptedAfter=engine.State.lastKnownUtcTimestamp,
                            simulatedAfter=engine.State.lastLeagueSimulationUtc,paused=RacingProgressionService.IsRaceActive,
                            reserved=pending!=null,bots=pending==null?0:pending.botIndices.Length},true));
                        report.Check(pending!=null,"Preparing reserves a real ranked receipt without advancing the paused league clock");
                        if(pending!=null) {
                            report.Check(pending.botIndices.Distinct().Count()==11&&pending.botIndices.All(i=>i>=0&&i<engine.Population),"Preparing reserves eleven existing distinct persistent bots");
                            report.Check(!RacingProgressionService.PrepareRankedRace(),"Preparing never restarts simulation during an active physical race");
                            RacingProgressionService.CancelRankedRace(pending.raceId);
                            report.Check(RacingProgressionService.PendingRankedRace==null&&human.currentMmr==beforeReservation,"Before-GO cancellation releases the real reservation without a human rating change");
                        }
                    }
                    finally {RacingProgressionService.SetRaceActive(false);}
                }
            }
            catch(Exception error){report.Check(false,error.ToString());}
            finally {PlayerSettings.companyName=company;}
            File.WriteAllText(Path.Combine(output,"competitive-display-logic.json"),JsonUtility.ToJson(report,true));
            File.WriteAllText(Path.Combine(output,"classification.txt"),"ISOLATED SYNTHETIC UI FIXTURES; NOT REAL RACE RESULTS, RATING ISSUANCE, PURCHASES OR HUMAN APPROVAL.\n");
            if(report.failed>0)throw new InvalidOperationException("Competitive display contracts failed: "+report.failed);
        }
        [Serializable] sealed class ReservationObservation {
            public long acceptedBefore,simulatedBefore,acceptedAfter,simulatedAfter;public bool paused,reserved;public int bots;
        }
    }
}
#endif
