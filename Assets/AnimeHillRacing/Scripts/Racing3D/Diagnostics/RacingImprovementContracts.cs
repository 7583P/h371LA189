#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    // Explicit validation only. Real races provide the performance and difficulty evidence.
    public static class RacingImprovementContracts
    {
        [Serializable] public sealed class CurveRow
        {
            public int mmr,samples;public float line,braking,drift,knowledge,execution,items,consistency,awareness,aggression,tricks,chaining,metaProbability,observedMetaRate;
        }
        [Serializable] sealed class Curves { public List<CurveRow> rows=new List<CurveRow>(); }
        public static void Edit(ClosureContractReport checks,string output)
        {
            int[] ratings={0,1000,10000,20000,30000,40000,50000,60000,70000,80000,90000,100000,125000};
            var curves=new Curves();CurveRow previous=null;var highCounts=new HashSet<int>();
            foreach(int rating in ratings)
            {
                var row=new CurveRow{mmr=rating,samples=64,metaProbability=MetaProgressionConfig.EvaluateMetaProbability(rating)};
                bool bounded=true,repeated=true,lobbies=true,diverse=true;int meta=0;
                for(int seed=1;seed<=64;seed++)
                {
                    var p=AiSkillModel.CreateProfile(rating,seed*7919);
                    repeated&=JsonUtility.ToJson(p)==JsonUtility.ToJson(AiSkillModel.CreateProfile(rating,seed*7919));
                    foreach(var field in typeof(AiRacerProfile).GetFields())if(field.FieldType==typeof(float))
                    {float value=(float)field.GetValue(p);bounded&=float.IsFinite(value)&&value>=0&&value<1;}
                    row.line+=p.racingLineSkill/64;row.braking+=p.brakingSkill/64;row.drift+=p.driftSkill/64;
                    row.knowledge+=p.shortcutKnowledge/64;row.execution+=p.shortcutExecution/64;row.items+=p.itemTiming/64;
                    row.consistency+=p.consistency/64;row.awareness+=p.awareness/64;row.aggression+=p.aggression/64;row.tricks+=p.trickSkill/64;row.chaining+=p.chainingSkill/64;
                    var lobby=MatchmakingService.GenerateRankedLobby(rating,"formula-01","wheel-plasma","TEST",seed*7919);
                    var repeat=MatchmakingService.GenerateRankedLobby(rating,"formula-01","wheel-plasma","TEST",seed*7919);
                    var grid=new HashSet<int>();var names=new HashSet<string>();int exact=0;
                    lobbies&=lobby.Length==12&&lobby[0].isHuman;
                    for(int i=0;i<lobby.Length;i++)
                    {
                        grid.Add(lobby[i].gridPosition);names.Add(lobby[i].displayName);
                        repeated&=JsonUtility.ToJson(lobby[i])==JsonUtility.ToJson(repeat[i]);
                        if(i==0)continue;
                        lobbies&=!lobby[i].isHuman&&lobby[i].mmr>=0;
                        if(lobby[i].buildCategory==MetaBuildCategory.Meta)
                        {exact++;lobbies&=lobby[i].vehicleId==MetaProgressionConfig.MetaVehicleId&&lobby[i].wheelId==MetaProgressionConfig.MetaWheelId;}
                        if(rating>=70000)lobbies&=lobby[i].buildCategory!=MetaBuildCategory.Standard;
                    }
                    lobbies&=grid.Count==12&&grid.Contains(1)&&grid.Contains(12)&&names.Count==12;
                    diverse&=exact<=9;meta+=exact;if(rating==100000)highCounts.Add(exact);
                }
                row.observedMetaRate=meta/(64f*11);curves.rows.Add(row);
                checks.Check(bounded,$"{rating} MMR: all skills remain finite and imperfect");
                checks.Check(repeated,$"{rating} MMR: profiles and full lobby reproduce their seed");
                checks.Check(lobbies&&diverse,$"{rating} MMR: twelve unique grid slots, valid builds and diversity guard");
                checks.Check(previous==null||(row.line>previous.line&&row.braking>previous.braking&&row.drift>=previous.drift&&row.consistency>previous.consistency&&row.execution>=previous.execution&&row.items>previous.items&&row.chaining>=previous.chaining&&row.tricks>=previous.tricks&&row.aggression>=previous.aggression),$"{rating} MMR: aggregate driving, tactics and chaining progress");
                previous=row;
            }
            checks.Check(curves.rows[11].line-curves.rows[3].line>.5f&&curves.rows[11].execution-curves.rows[3].execution>.5f,"100k retains a substantial skill gap over 20k");
            checks.Check(highCounts.Count>1,"Exact META count varies across high-MMR seeds without a forced minimum");
            var custom=MatchmakingService.GenerateRankedLobby(100000,"formula-01","wheel-default","TEST",42);
            checks.Check(custom[0].buildCategory!=MetaBuildCategory.Meta,"Formula with default tuning is not mislabeled exact META");
            PlayerPrefs.SetString("AHR.Racing.SelectedVehicle.V1","car-01");PlayerPrefs.SetString("AHR.Racing.SelectedWheel.V1","wheel-default");
            checks.Check(RacingSaveManager.SelectedVehicleId=="car-01"&&RacingSaveManager.SelectedWheelId=="wheel-default","Saved Jeep and default wheels survive selection getters");
            checks.Check(PlayerPrefs.GetString("AHR.Racing.SelectedVehicle.V1")=="car-01"&&PlayerPrefs.GetString("AHR.Racing.SelectedWheel.V1")=="wheel-default","Selection reads do not overwrite saved choices");
            int mmr=RacingSaveManager.CurrentMmr,highest=RacingSaveManager.HighestMmr,races=RacingSaveManager.RacesCount;
            RacingTestSession.Set(125000,42);
            checks.Check(RacingTestSession.Active&&RacingTestSession.Mmr==125000&&RacingSaveManager.CurrentMmr==mmr&&RacingSaveManager.HighestMmr==highest&&RacingSaveManager.RacesCount==races,"Test race override preserves live rating and progression");
            RacingTestSession.Clear();
            File.WriteAllText(Path.Combine(output,"skill-and-meta-curves.json"),JsonUtility.ToJson(curves,true));
        }
        public static void World(ClosureContractReport checks,RaceManager manager)
        {
            var graph=manager.ProgressTracker.TrackGraph;bool physics=true;
            foreach(var car in manager.SpawnedVehicles)
                physics&=JsonUtility.ToJson(car.Stats)==JsonUtility.ToJson(RacingStatsCalculator.Calculate(car.VehicleId,car.WheelId))&&Mathf.Abs(car.Body.mass-car.Stats.WeightKg)<.01f;
            checks.Check(physics,"All nine stats and Rigidbody mass match the same player/AI calculator");
            var overlaps=new Collider[32];
            foreach(var route in graph.Shortcuts)
            {
                checks.Check(Vector3.Distance(route.pathPoints[0],graph.GetNode(route.entryNodeIndex).Position)<.01f&&Vector3.Distance(route.pathPoints[route.pathPoints.Length-1],graph.GetNode(route.exitNodeIndex).Position)<.01f,route.displayName+": exact authored entry and exit");
                bool preservesGates=true;foreach(int gate in graph.CheckpointNodeIndices)preservesGates&=gate<=route.entryNodeIndex||gate>=route.exitNodeIndex;
                checks.Check(preservesGates,route.displayName+": required checkpoint order is preserved");
                float length=0,largestTurn=0;bool decks=true,corridor=true;
                for(int i=0;i<route.pathPoints.Length-1;i++)
                {
                    Vector3 a=route.pathPoints[i],delta=route.pathPoints[i+1]-a;length+=delta.magnitude;
                    if(i<route.pathPoints.Length-2)largestTurn=Mathf.Max(largestTurn,Vector3.Angle(delta,route.pathPoints[i+2]-route.pathPoints[i+1]));
                    if(route.requiresJump&&(i==10||i==11))continue;
                    Vector3 point=a+delta*.5f;
                    bool hit=Physics.Raycast(point+Vector3.up*.6f,Vector3.down,out var floor,1.2f,~0,QueryTriggerInteraction.Ignore);
                    decks&=hit&&floor.normal.y>.6f&&Mathf.Abs(floor.point.y-point.y)<.55f;
                    int count=Physics.OverlapBoxNonAlloc(point+Vector3.up*.9f,new Vector3(.7f,.18f,1.2f),overlaps,Quaternion.LookRotation(delta,Vector3.up),~0,QueryTriggerInteraction.Ignore);
                    for(int c=0;c<count;c++)if(overlaps[c].attachedRigidbody==null&&overlaps[c].GetComponent<RacingSurface>()==null)corridor=false;
                }
                float main=graph.GetNode(route.exitNodeIndex).CumulativeDistance-graph.GetNode(route.entryNodeIndex).CumulativeDistance;
                if(route.shortcutId=="ridge_cut"||route.shortcutId=="overpass_launch")checks.Check(main-length>30,route.displayName+": strong route saves at least thirty physical meters");
                checks.Check(largestTurn<45,route.displayName+$": smooth sampled turns ({largestTurn:F1} degrees maximum)");
                checks.Check(decks,route.displayName+": continuous physical decks outside the jump gap");
                checks.Check(corridor,route.displayName+": car corridor is clear of scenery and tunnel walls");
            }
        }
        public static IEnumerator Menu(ClosureContractReport checks,Action<string> capture)
        {
            SceneManager.LoadScene("RacingSelect");yield return new WaitForSecondsRealtime(.7f);
            int original=RacingSaveManager.CurrentMmr,highest=RacingSaveManager.HighestMmr,races=RacingSaveManager.RacesCount;
            var quality=RacingQualityScope.Current;
            string[] qualityNames={"LOW","MEDIUM","HIGH"};
            for(int i=0;i<3;i++)
            {
                Click(qualityNames[i],checks);yield return null;
                var button=Find(qualityNames[i]);
                checks.Check(RacingQualityScope.Current==(RacingQuality)i&&button!=null&&button.GetComponent<Graphic>().color.g>.4f,"Quality selection has a visible active state: "+qualityNames[i]);
            }
            RacingQualityScope.Select(quality);
            Click("TEST MMR",checks);yield return null;
            string[] presets={"1K","10K","25K","50K","75K","90K","100K","100K+"};
            foreach(string preset in presets)
            {
                Click(preset,checks);yield return null;
                var rows=GameObject.Find("Lobby rows");
                checks.Check(rows!=null&&rows.GetComponentsInChildren<Button>().Length==12,"MMR inspector shows all twelve racers at "+preset);
                checks.Check(RacingSaveManager.CurrentMmr==original&&RacingSaveManager.HighestMmr==highest&&RacingSaveManager.RacesCount==races,"MMR preset preserves saved rank and rewards: "+preset);
            }
            capture?.Invoke("test-mmr-100k-plus");
            Click("DRIVE TEST",checks);yield return new WaitForSecondsRealtime(.6f);
            checks.Check(SceneManager.GetActiveScene().name=="RacingGarage"&&RacingTestSession.Active&&RacingTestSession.Mmr==125000&&RacingSaveManager.CurrentMmr==original,"Visible Drive Test enters Garage with a read-only 100k+ override");
            Click("RACING HUB",checks);yield return new WaitForSecondsRealtime(.6f);
            checks.Check(!RacingTestSession.Active&&RacingSaveManager.CurrentMmr==original,"Returning to Hub clears the test override and preserves rank");
            Click("MAIN MENU",checks);yield return new WaitForSecondsRealtime(.6f);
            checks.Check(Find("ADVENTURE")!=null&&Find("RACING")!=null&&Find("GARAGE")!=null&&Find("SHOP")!=null,"True Main Menu visibly exposes Adventure, Racing, Garage and Shop");
        }
        static Button Find(string name)
        {foreach(var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name==name&&b.gameObject.activeInHierarchy)return b;return null;}
        static void Click(string name,ClosureContractReport checks)
        {var b=Find(name);checks.Check(b!=null&&b.interactable,"Visible UI Button: "+name);if(b!=null&&b.interactable)b.onClick.Invoke();}
    }
}
#endif
