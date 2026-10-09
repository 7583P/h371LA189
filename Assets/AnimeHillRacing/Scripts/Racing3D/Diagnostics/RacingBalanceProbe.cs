#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.Racing3D
{
    // Explicit QA flag only. Screenshots, synthetic presentation stages and file IO
    // are confined to this fixture, never a measured race or the normal player.
    public sealed class RacingBalanceProbe:MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string classification="ISOLATED_NATIVE_WALLET_AND_PRESENTATION_FIXTURE_NOT_A_PERFORMANCE_OR_DRIVING_BENCHMARK";
            public string phase,walletKey="AHR.TotalCoins",note="Charge/jump presentation snapshots use controlled event fixtures. Store transactions are not simulated.";
            public int passed,failed,before,after,reloaded,errors;
            public List<string> successes=new List<string>(),failures=new List<string>();
        }
        Report report=new Report();string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"--ranked-balance-test");
            if(at<0||at+1>=args.Length)return;
            if(Application.companyName!="HillAdventurer.BlacklineQA") {Debug.LogError("Ranked balance fixture requires the isolated QA company.");Application.Quit(1);return;}
            int dir=Array.IndexOf(args,"--closure-output");if(dir<0||dir+1>=args.Length){Application.Quit(1);return;}
            var probe=new GameObject("Explicit ranked balance fixture").AddComponent<RacingBalanceProbe>();
            DontDestroyOnLoad(probe.gameObject);probe.output=args[dir+1];probe.report.phase=args[at+1];Directory.CreateDirectory(probe.output);
            Application.logMessageReceived+=probe.Logged;probe.StartCoroutine(probe.Guarded());
        }
        void Logged(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)report.errors++;}
        void Check(bool value,string label){if(value){report.passed++;report.successes.Add(label);}else{report.failed++;report.failures.Add(label);Debug.LogError("RANKED_BALANCE_FAILED "+label);}}
        IEnumerator Guarded()
        {
            var work=report.phase=="write"?Write():Read();
            while(true) {
                bool more=false;Exception error=null;try{more=work.MoveNext();}catch(Exception e){error=e;}
                if(error!=null){Check(false,error.ToString());break;}if(!more)break;yield return work.Current;
            }
            File.WriteAllText(Path.Combine(output,"wallet-"+report.phase+".json"),JsonUtility.ToJson(report,true));
            Application.logMessageReceived-=Logged;
            Application.Quit(report.failed==0&&report.errors==0?0:1);
        }
        void Capture(string name){RacingEvidenceCapture.Capture(Path.Combine(output,name+".png"));Check(File.Exists(Path.Combine(output,name+".png")),"Rendered snapshot: "+name);}
        IEnumerator Write()
        {
            PlayerPrefs.SetInt(report.walletKey,1230);PlayerPrefs.Save();report.before=SaveManager.TotalCoins;
            RacingTestSession.Set(10000,4207);
            SceneManager.LoadScene("Racing3D");RaceManager manager=null;float deadline=Time.realtimeSinceStartup+40;
            while(Time.realtimeSinceStartup<deadline) {
                manager=FindFirstObjectByType<RaceManager>();
                if(manager!=null&&manager.SpawnedVehicles.Count==12&&manager.PlayerVehicle!=null)break;yield return null;
            }
            Check(manager!=null&&manager.SpawnedVehicles.Count==12,"Native racing scene initializes twelve real vehicles");if(manager==null||manager.SpawnedVehicles.Count!=12)yield break;
            manager.StopAllCoroutines();manager.enabled=false;
            var metrics=FindFirstObjectByType<RacingRaceMetrics>();if(metrics!=null){metrics.StopMeasurement();metrics.enabled=false;}
            foreach(var car in manager.SpawnedVehicles) {
                car.SetRaceEnabled(false);car.enabled=false;car.Body.isKinematic=true;
                var recovery=car.GetComponent<VehicleRecovery>();if(recovery!=null)recovery.enabled=false;
                var ai=car.GetComponent<AiVehicleController>();if(ai!=null)ai.enabled=false;
                var telemetry=car.GetComponent<ML.RacingTelemetryRecorder>();if(telemetry!=null)telemetry.enabled=false;
            }
            typeof(RaceManager).GetMethod("SetState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{RaceState.Racing});
            var player=manager.PlayerVehicle;var input=player.GetComponent<PlayerRacingInput>();var inventory=player.GetComponent<ItemInventory>();
            player.SetRaceEnabled(true);input.SetInputEnabled(true);
            string originalStats=JsonUtility.ToJson(player.Stats);float originalMass=player.Body.mass;
            inventory.AcquireItem(RaceItemType.Coin10,false);
            // Let the real introduction finish before checking a racing view.
            yield return new WaitForSeconds(3f);Capture("01-coin10-held-and-shared-wallet");
            var camera=FindFirstObjectByType<RacingCamera3D>();input.SetInputEnabled(true);input.SetTouchLookBack(true);input.SampleCommands();
            yield return new WaitForSeconds(.15f);
            Check(camera!=null&&camera.IsLookingBack&&Vector3.Dot(camera.transform.forward,player.transform.forward)<-.65f,"Rear-view camera faces behind the vehicle");
            var rear=GameObject.Find("Rear view active");Check(rear!=null&&rear.activeInHierarchy,"Rear-view HUD badge is active");Capture("02-shift-mobile-rear-view");
            input.SetTouchLookBack(false);yield return new WaitForSeconds(.15f);Check(!camera.IsLookingBack,"Releasing rear view restores the normal camera");
            bool used=inventory.UseItem();report.after=SaveManager.TotalCoins;
            Check(used&&report.after==report.before+10&&inventory.SharedCoinsAwarded==10,"Human Racing Coin +10 credits exactly ten to the shared Adventure wallet");
            Check(!inventory.UseItem()&&SaveManager.TotalCoins==report.after,"Repeated use callback cannot duplicate the consumed coin");
            Check(JsonUtility.ToJson(player.Stats)==originalStats&&Mathf.Abs(player.Body.mass-originalMass)<.001f&&!player.IsBoosting,"Coin reward changes no engine/grip/mass/boost property");
            yield return new WaitForSeconds(.15f);Capture("03-coin10-consumed");
            var bot=manager.SpawnedVehicles[1];var botInventory=bot.GetComponent<ItemInventory>();bot.SetRaceEnabled(true);
            botInventory.AcquireItem(RaceItemType.Coin10,false);yield return new WaitForSeconds(.2f);
            Check(botInventory.UseItem()&&SaveManager.TotalCoins==report.after,"AI coin consumption never credits the human wallet");
            yield return new WaitForSeconds(.2f);botInventory.AcquireItem(RaceItemType.MiniTurbo,false);
            Check(botInventory.UseItem()&&bot.IsBoosting&&Mathf.Abs(bot.BoostRemainingSeconds-.8f)<.02f&&Mathf.Abs(bot.BoostMultiplier-1.20f)<.001f,"Mini Turbo is the ordinary shared 0.8-second / 1.20 item boost");
            int serial=inventory.AcquisitionSerial;
            var pickup=ItemBox.Create(manager.transform,player.transform.position,RaceItemType.Coin10).GetComponent<ItemBox>();Physics.SyncTransforms();
            Check(pickup.CanCollect(player),"Gold Coin +10 physical pickup is collectible before contact");
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Check(inventory.HeldItem==RaceItemType.Coin10&&inventory.AcquisitionSerial==serial+1,"Multiple chassis colliders collect the known coin only once");
            Check(!pickup.CanCollect(player)&&SaveManager.TotalCoins==report.after,"Collecting/holding another coin does not grant it before consumption");
            Destroy(pickup.gameObject);
            // Controlled stage fixtures inspect the same runtime trails, HUD and
            // shared colors; they are never reported as successful driving.
            Set(player,"isGrounded",true);Set(player,"isDrifting",true);
            foreach(var stage in new[]{MiniTurboStage.MiniTurbo,MiniTurboStage.SuperMiniTurbo,MiniTurboStage.UltraMiniTurbo}) {
                Set(player,"currentMiniTurboStage",stage);Set(player,"driftTime",stage==MiniTurboStage.MiniTurbo?.8f:stage==MiniTurboStage.SuperMiniTurbo?1.7f:2.8f);
                yield return new WaitForSeconds(.15f);Capture("04-drift-stage-"+(int)stage);
            }
            Set(player,"isDrifting",false);Set(player,"currentMiniTurboStage",MiniTurboStage.None);Set(player,"driftTime",0f);
            var vfx=RacingVfxPool.Ensure();int bursts=vfx.BurstCount;player.NotifyJump();
            yield return null;Check(vfx.BurstCount>bursts,"Jump event emits a bounded burst in the existing particle pool");Capture("05-jump-feedback-fixture");
            inventory.FlushCoinRewards();Check(PlayerPrefs.GetInt(report.walletKey)==1240,"Shared wallet is flushed for the independent reload process");
        }
        IEnumerator Read()
        {
            report.reloaded=SaveManager.TotalCoins;
            Check(report.reloaded==1240&&PlayerPrefs.GetInt(report.walletKey)==1240,"Independent process reloads exactly the persisted +10 reward");
            GameSession.SelectedGameMode=GameModeType.Adventure2_5D;GameSession.SelectedMapId="countryside";
            SceneManager.LoadScene("Adventure2_5D");float deadline=Time.realtimeSinceStartup+30;
            while(GameManager.Instance==null&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSeconds(.8f);
            var adventure=GameManager.Instance;
            Check(adventure!=null&&adventure.SavedTotalCoins==1240&&adventure.DisplayCoins==1240,"Real Adventure GameManager reads the same wallet after relaunch");
            var hud=FindFirstObjectByType<AnimeHillRacing.UI.GameplayHUD>();
            bool displayed=false;
            if(hud!=null)foreach(var label in hud.GetComponentsInChildren<Text>(true))displayed|=label.text==1240.ToString("N0");
            Check(displayed,"Existing Adventure gameplay HUD displays the reloaded 1240 coins");
            Capture("06-adventure-shared-wallet-after-relaunch");
            Check(SaveManager.TotalCoins==1240,"Relaunch and Adventure startup do not re-award the racing coin");
        }
        static void Set(object target,string field,object value){typeof(RacingVehiclePhysics).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
    }
}
#endif
