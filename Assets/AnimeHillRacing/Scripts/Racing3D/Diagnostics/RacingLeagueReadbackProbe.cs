#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    // Explicit QA only: reopen a COPY of a completed physical-race journal and
    // drive the real leaderboard input in a fresh native player process.
    public sealed class RacingLeagueReadbackProbe : MonoBehaviour
    {
        [Serializable] sealed class Row {public string id,name;public bool existing,byId,byName;public int nameMatches;}
        [Serializable] sealed class Report {
            public string classification="NATIVE_UI_RESTART_OF_COPIED_REAL_QA_RACE_JOURNAL_NOT_HUMAN_APPROVAL",raceId,error;
            public int seed,bots,population;public bool ratingsUntouched,passed;
            public List<Row> opponents=new List<Row>();
        }
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--league-readback-player")<0)return;
            if(Application.companyName!="HillAdventurer.BlacklineQA")throw new InvalidOperationException("Native readback requires isolated QA preferences");
            var host=new GameObject("Explicit native league UI readback");DontDestroyOnLoad(host);host.AddComponent<RacingLeagueReadbackProbe>().StartCoroutine("Run");
        }
        IEnumerator Run()
        {
            string output=Path.GetFullPath(RacingFrameForensics.Argument("--closure-output"));
            string target=Path.Combine(output,"native-league-readback.json");
            if(File.Exists(target))throw new IOException("Preserve existing native readback");
            var report=new Report();bool initialized=false;
            try {
                report.seed=int.Parse(RacingFrameForensics.Argument("--league-readback-seed","4301"));
                if(!Directory.Exists(Path.Combine(output,"isolated-product-save")))throw new IOException("An existing copied QA journal is required; do not create opponents");
                initialized=true;
            }
            catch(Exception error){report.error=error.ToString();}
            if(!initialized){Finish(target,report);yield break;}
            yield return SceneManager.LoadSceneAsync("RacingSelect");
            yield return new WaitForSecondsRealtime(.5f);
            var engine=RacingProgressionService.Engine;var state=engine.State;
            var receipt=state.ledger.recentSettlements.LastOrDefault(r=>r.seed==report.seed&&r.origin==(byte)RaceOrigin.Physical&&r.participantIds.Contains(CompetitiveCoordinator.HumanId));
            if(receipt==null){report.error="The existing journal contains no physical receipt for this seed";Finish(target,report);yield break;}
            report.raceId=receipt.raceId;report.population=engine.Population;
            int humanMmr=state.human.currentMmr,humanRaces=state.human.races;long physicalRaces=state.ledger.budget.physicalRaces;
            RacingLeaderboardUI board=null;InputField input=null;
            try {board=RacingLeaderboardUI.Open();input=board.GetComponentInChildren<InputField>(true);}
            catch(Exception error){report.error=error.ToString();Finish(target,report);yield break;}
            foreach(string id in receipt.participantIds) {
                if(id==CompetitiveCoordinator.HumanId)continue;
                var row=new Row{id=id};report.opponents.Add(row);
                try {
                    row.existing=engine.TryIndex(id,out int index);
                    if(!row.existing)throw new InvalidOperationException("Missing raced identity: "+id);
                    row.name=engine.DisplayName(index);int expected=Math.Max(0,engine.Rank(id)-3);
                    input.onEndEdit.Invoke(id);
                    row.byId=(int)typeof(RacingLeaderboardUI).GetField("pageStart",Private).GetValue(board)==expected&&Visible(board,row.name);
                    int best=Enumerable.Range(0,engine.Population).Where(i=>engine.DisplayName(i).Equals(row.name,StringComparison.OrdinalIgnoreCase)).Select(i=>engine.Index.RankOf(i)).DefaultIfEmpty(-1).Min();
                    row.nameMatches=Enumerable.Range(0,engine.Population).Count(i=>engine.DisplayName(i).Equals(row.name,StringComparison.OrdinalIgnoreCase));
                    input.onEndEdit.Invoke(row.name);
                    row.byName=best>=0&&(int)typeof(RacingLeaderboardUI).GetField("pageStart",Private).GetValue(board)==Math.Max(0,best-3)&&Visible(board,row.name);
                }
                catch(Exception error){report.error=error.ToString();break;}
                yield return null;
            }
            report.bots=report.opponents.Count;
            report.ratingsUntouched=state.human.currentMmr==humanMmr&&state.human.races==humanRaces&&state.ledger.budget.physicalRaces==physicalRaces;
            report.passed=string.IsNullOrEmpty(report.error)&&report.bots==11&&report.opponents.All(r=>r.existing&&r.byId&&r.byName)&&report.ratingsUntouched;
            try {
                if(Camera.main==null){var camera=new GameObject("Explicit readback UI camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);}
                RacingEvidenceCapture.Capture(Path.Combine(output,"native-leaderboard-after-restart.png"));
            }
            catch(Exception error){report.error=error.ToString();report.passed=false;}
            Finish(target,report);
        }
        static bool Visible(RacingLeaderboardUI board,string name)=>board.GetComponentsInChildren<Text>().Any(t=>t.isActiveAndEnabled&&t.text==name);
        static void Finish(string target,Report report) {
            File.WriteAllText(target,JsonUtility.ToJson(report,true));
            Debug.Log("Native physical league UI readback: bots="+report.bots+" passed="+report.passed);
            Application.Quit(report.passed?0:1);
        }
    }
}
#endif
