#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingBalanceContracts
    {
        [Serializable] sealed class PositionTable { public int first,last;public ItemDistributionTable.ItemWeight[] weights; }
        [Serializable] sealed class TableEvidence { public PositionTable[] groups;public string note="Nominal weights; existing gap/lap/repeat adjustments retain the position whitelist. Visible route fuel is a shared fixed pickup, not a random item-box roll."; }
        [Serializable] sealed class MatchTimingEvidence {
            public string classification="EDITOR_WARM_10K_ELITE_MATCHMAKING_NOT_RACE_FPS";
            public int bots,calls;public double medianMs,p99Ms,maxMs;public long allocatedBytes;
        }
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--closure-output");
            if(at<0||at+1>=args.Length)throw new ArgumentException("Missing --closure-output");
            string output=args[at+1];Directory.CreateDirectory(output);
            EnsureCoinAssets();var report=new ClosureContractReport();
            var oldScene=SceneManager.GetActiveScene();
            if(Application.isBatchMode&&string.IsNullOrEmpty(oldScene.path))oldScene=EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            var fixture=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try { SceneManager.SetActiveScene(fixture);Tables(report,output);Controls(report);Matchmaking(report,output);Art(report); }
            finally { if(oldScene.IsValid()&&oldScene.isLoaded)SceneManager.SetActiveScene(oldScene);EditorSceneManager.CloseScene(fixture,true); }
            File.WriteAllText(Path.Combine(output,"ranked-balance-contracts.json"),JsonUtility.ToJson(report,true));
            if(report.failed!=0)throw new InvalidOperationException("Ranked balance contracts failed: "+report.failed);
            Debug.Log("RANKED_BALANCE_CONTRACTS passed="+report.passed+" failed="+report.failed);
        }
        static void EnsureCoinAssets()
        {
            const string meshPath="Assets/AnimeHillRacing/Resources/RacingCoin10.asset",materialPath="Assets/AnimeHillRacing/Resources/RacingCoinGold.mat";
            if(AssetDatabase.LoadAssetAtPath<Mesh>(meshPath)==null)AssetDatabase.CreateAsset(RacingCoinArt.CreateMesh(),meshPath);
            if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)==null) {
                var shader=Shader.Find("HillAdventurer/Racing Coin");if(shader==null)throw new InvalidOperationException("Coin shader was not imported.");
                AssetDatabase.CreateAsset(new Material(shader){name="Racing Coin Gold / one opaque pass",enableInstancing=true},materialPath);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static void Tables(ClosureContractReport report,string output)
        {
            var evidence=new TableEvidence{groups=new[]{new PositionTable{first=1,last=1,weights=ItemDistributionTable.WeightsForPosition(1)},new PositionTable{first=2,last=3,weights=ItemDistributionTable.WeightsForPosition(2)},new PositionTable{first=4,last=6,weights=ItemDistributionTable.WeightsForPosition(4)},new PositionTable{first=7,last=12,weights=ItemDistributionTable.WeightsForPosition(7)}}};
            File.WriteAllText(Path.Combine(output,"item-table.json"),JsonUtility.ToJson(evidence,true));
            foreach(var table in evidence.groups)report.Check(table.weights.Sum(x=>x.Weight)==100&&table.weights.All(x=>x.Weight>0&&ItemDistributionTable.IsAllowedAtPosition(x.Type,table.first)),"Authored position table "+table.first+"–"+table.last+" has 100 legal positive nominal weights");
            for(int position=1;position<=12;position++) {
                var rng=new DeterministicRng(4207+position);bool legal=true;var seen=new bool[RaceItemUtility.Count+1];
                var context=new ItemRollContext{position=position,lap=1,totalLaps=3};
                for(int i=0;i<12000;i++) {
                    context.lap=1+i%3;context.gapLeaderSeconds=i%40;
                    var item=ItemDistributionTable.RollItem(context,rng);legal&=item!=RaceItemType.None&&ItemDistributionTable.IsAllowedAtPosition(item,position);seen[(int)item]=true;
                    context.previousPrevious=context.previous;context.previous=item;
                }
                report.Check(legal,"P"+position+": 12000 gap/lap/repeat rolls respect the whitelist");
                if(position<=6)report.Check(seen[(int)RaceItemType.Coin10]&&seen[(int)(position==1?RaceItemType.ElectroSpike:RaceItemType.MagneticShield)],"P"+position+": Coin +10 and its ordinary Rock/Shield alternative are reachable");
            }
            report.Check(ItemDistributionTable.RollItem(new ItemRollContext{position=1,held=RaceItemType.Coin10})==RaceItemType.None,"A held reward cannot be overwritten by another roll");
            var config=Resources.Load<RacingItemBalanceConfiguration>("RacingItemBalanceConfiguration");var original=config.leader;
            try {
                config.leader=new[]{new ItemDistributionTable.ItemWeight{Type=RaceItemType.TurboPulse,Weight=500},new ItemDistributionTable.ItemWeight{Type=RaceItemType.ElectroSpike,Weight=0},new ItemDistributionTable.ItemWeight{Type=RaceItemType.Coin10,Weight=1}};
                var rng=new DeterministicRng(4);bool safe=true;for(int i=0;i<1000;i++)safe&=ItemDistributionTable.RollItem(1,rng)==RaceItemType.Coin10;
                report.Check(safe,"Malformed leader tuning cannot introduce a turbo or select a zero-weight entry");
            } finally {config.leader=original;}
            var hotRng=new DeterministicRng(4207);ItemDistributionTable.RollItem(12,hotRng);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)ItemDistributionTable.RollItem(1+i%12,hotRng);
            report.Check(GC.GetAllocatedBytesForCurrentThread()==before,"10000 warmed item rolls allocate zero managed bytes");
            report.Check(RaceItemUtility.LegacyFeatureFamily(13)==0&&RaceItemUtility.LegacyFeatureFamily(14)==1,"Coin and Mini Turbo use existing non-authoritative model feature families");
            report.Check(!RaceItemUtility.IsShortcutBoost(RaceItemType.MiniTurbo)&&RaceItemUtility.IsShortcutBoost(RaceItemType.TurboPulse),"A short mid-pack turbo is never treated as long-jump route fuel");
            report.Check(RaceItemUtility.IsBoost(RaceItemType.Lancer)&&!RaceItemUtility.IsShortcutBoost(RaceItemType.Lancer)&&RaceItemUtility.LegacyFeatureFamily(15)==1,"Lancer is a rear-pack mobility item, not a reserved shortcut fuel or new authoritative ML input");
        }
        static void Controls(ClosureContractReport report)
        {
            var root=new GameObject("Isolated keyboard contract");root.AddComponent<RacingVehiclePhysics>();var input=root.AddComponent<PlayerRacingInput>();
            var oldKeyboard=Keyboard.current;var keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            try {
                foreach(var shift in new[]{Key.LeftShift,Key.RightShift}) {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(shift));InputSystem.Update();var commands=input.SampleCommands();
                    report.Check(!input.LookBack&&commands.Drift&&!commands.AerialBoost&&!commands.Jump&&!commands.UseItem,shift+" activates drift without rear view, boost, jump or item use");
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();report.Check(!input.SampleCommands().Drift,"Releasing Shift exits drift");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));InputSystem.Update();var drift=input.SampleCommands();report.Check(drift.Drift&&!input.LookBack&&!drift.AerialBoost,"C retains a dedicated drift key without jump or rear-view conflicts");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));InputSystem.Update();report.Check(input.SampleCommands().AerialBoost&&!input.LookBack,"B retains airborne boost independently of rear view");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                input.SetTouchLookBack(true);input.SetTouchDrift(true);report.Check(input.SampleCommands().Drift&&input.LookBack,"Mobile drift and rear-view holds remain independent");
                input.SetInputEnabled(false);report.Check(!input.LookBack&&!input.SampleCommands().Drift,"Pause/focus input disable clears held rear view and drift");
            } finally {InputSystem.RemoveDevice(keyboard);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();UnityEngine.Object.DestroyImmediate(root);}
        }
        static void Matchmaking(ClosureContractReport report,string output)
        {
            foreach(int rating in new[]{9700,10000}) {
                bool tight=true,styles=true;
                for(int seed=1;seed<=64;seed++) {
                    var lobby=MatchmakingService.GenerateSeededTestLobby(rating,MetaProgressionConfig.MetaVehicleId,MetaProgressionConfig.MetaWheelId,"PLAYER",seed);
                    tight&=lobby.Skip(1).All(x=>x.mmr>=9700&&x.mmr<=10000);
                    styles&=lobby.Skip(1).Select(x=>AiSkillModel.CreateProfile(x.mmr,x.personalitySeed).style).Distinct().Count()>=3;
                }
                report.Check(tight,rating+": seeded elite lobbies remain in 9700–10000 across 64 seeds");
                report.Check(styles,rating+": elite personality diversity survives tighter ratings across 64 seeds");
            }
            var state=LocalLeagueEngine.Create(DateTime.UtcNow.Ticks,4207);
            for(int i=0;i<state.bots.Length;i++){var bot=state.bots[i];bot.currentMmr=Math.Max(0,10000-i/2);state.bots[i]=bot;}
            var engine=new LocalLeagueEngine(state);bool tightPersistent=true,ids=true;
            var times=new double[64];int timingAt=0;long matchAllocated=0;
            foreach(int rating in new[]{9700,10000}) {
                state.human.currentMmr=rating;
                engine.SelectOpponents(rating,0,true); // Warm the actual rank index outside timing.
                for(int seed=1;seed<=32;seed++) {
                    long allocatedBefore=GC.GetAllocatedBytesForCurrentThread();
                    long started=System.Diagnostics.Stopwatch.GetTimestamp();
                    var rivals=engine.SelectOpponents(rating,seed,true);
                    times[timingAt++]=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000d/System.Diagnostics.Stopwatch.Frequency;
                    matchAllocated+=GC.GetAllocatedBytesForCurrentThread()-allocatedBefore;
                    tightPersistent&=rivals.All(x=>Math.Abs(x.currentMmr-rating)<=120);
                    ids&=rivals.Length==11&&rivals.Select(x=>x.id).Distinct().Count()==11&&rivals.All(x=>engine.TryIndex(x.id,out _));
                }
            }
            report.Check(tightPersistent,"Mature elite fixture: persistent opponents stay within ±120 at 9700/10000 over 64 matches");
            report.Check(ids,"Tight matchmaking retains eleven distinct real saved bot IDs");
            Array.Sort(times);
            File.WriteAllText(Path.Combine(output,"elite-matchmaking-timing.json"),JsonUtility.ToJson(new MatchTimingEvidence {
                bots=state.bots.Length,calls=timingAt,medianMs=(times[31]+times[32])*.5d,
                p99Ms=times[63],maxMs=times[63],allocatedBytes=matchAllocated },true));
            var sparse=new LocalLeagueEngine(LocalLeagueEngine.Create(DateTime.UtcNow.Ticks,54));
            var fallback=sparse.SelectOpponents(10000,4207,false);
            report.Check(fallback.Length==11&&fallback.Select(x=>x.id).Distinct().Count()==11&&fallback.All(x=>x.currentMmr==0),"Sparse fresh league falls back to real bots instead of fabricating elite ratings");
        }
        static void Art(ClosureContractReport report)
        {
            var mesh=RacingCoinArt.SharedMesh;var material=RacingCoinArt.SharedMaterial;
            report.Check(mesh!=null&&mesh.vertexCount<1000&&mesh.triangles.Length/3<=256,"Native gold coin mesh stays below 1000 vertices / 256 triangles");
            report.Check(material!=null&&material.passCount==1&&material.renderQueue<2500,"Coin material uses one opaque pass with no transparency");
            var a=new GameObject("Coin A");var b=new GameObject("Coin B");RacingCoinArt.Build(a.transform);RacingCoinArt.Build(b.transform);
            var ar=a.GetComponentInChildren<MeshRenderer>();var br=b.GetComponentInChildren<MeshRenderer>();
            report.Check(a.GetComponentsInChildren<MeshRenderer>().Length==1&&ar.sharedMaterial==br.sharedMaterial&&a.GetComponentInChildren<MeshFilter>().sharedMesh==b.GetComponentInChildren<MeshFilter>().sharedMesh,"Coin pickups and rewards share a single mesh/material and one renderer each");
            report.Check(ar.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off&&!ar.receiveShadows,"Coin adds no shadow-render workload");
            UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);
        }
    }
}
#endif
