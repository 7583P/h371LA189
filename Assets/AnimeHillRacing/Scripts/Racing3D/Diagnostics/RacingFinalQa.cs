#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Pickups;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnimeHillRacing.Racing3D
{
    // Explicit closure fixture only. All driving below is automated.
    public sealed class RacingFinalQa : MonoBehaviour
    {
        public readonly ClosureContractReport Checks = new ClosureContractReport();
        readonly HashSet<string> cameraScenarios = new HashSet<string>();
        readonly RaycastHit[] obstructionHits = new RaycastHit[32];
        RaceManager manager;
        Camera camera;
        int cameraFrames, invalidFrames, outsideViewFrames, obstructionFrames;
        string output;
        float nextCameraSample;
        bool observing;

        public void Begin(RaceManager race, string directory)
        {
            manager = race; camera = Camera.main; output = directory;
            observing = true;
            manager.PlayerVehicle.OnLanding += Landing;
        }
        void Landing(float amount) { cameraScenarios.Add("landing"); }
        void LateUpdate()
        {
            if (!observing || manager == null || camera == null || Time.time < nextCameraSample) return;
            nextCameraSample = Time.time + .05f;
            var car = manager.PlayerVehicle;
            if (manager.State == RaceState.Countdown) cameraScenarios.Add("countdown");
            if (manager.State == RaceState.PlayerFinished) cameraScenarios.Add("finish");
            if (manager.State != RaceState.Racing && manager.State != RaceState.PlayerFinished) return;
            var ai = car.GetComponent<AiVehicleController>();
            manager.ProgressTracker.TrackGraph.ProjectPosition(car.transform.position, out int node, out _, out _);
            float turn = ai != null ? Mathf.Abs(ai.UpcomingTurnDegrees) : 0;
            if (turn < 7 && car.SpeedKmh > 40) cameraScenarios.Add("straight");
            if (car.SpeedKmh > 100) cameraScenarios.Add("high speed");
            if (turn >= 10 && turn < 35) cameraScenarios.Add("medium corner");
            if (turn >= 35) cameraScenarios.Add("tight corner");
            if (car.IsDrifting) cameraScenarios.Add("drift");
            if (node >= 48 && node < 64) cameraScenarios.Add("bridge");
            if (node >= 58 && node <= 64 && !car.IsGrounded && cameraScenarios.Add("jump")) Capture("camera-natural-jump");
            if (node >= 24 && node < 36) cameraScenarios.Add("tunnel");
            ObserveCamera();
        }
        void ObserveCamera()
        {
            cameraFrames++;
            Vector3 p = camera.transform.position;
            if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z) || !float.IsFinite(camera.fieldOfView)) invalidFrames++;
            var car = manager.PlayerVehicle;
            Vector3 point = camera.WorldToViewportPoint(car.transform.position + Vector3.up * .5f);
            if (point.z <= camera.nearClipPlane || point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) outsideViewFrames++;
            Vector3 anchor = car.transform.position + Vector3.up;
            Vector3 ray = p - anchor;
            int count = Physics.SphereCastNonAlloc(anchor, .18f, ray.normalized, obstructionHits, Mathf.Max(0, ray.magnitude - .15f), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) if (obstructionHits[i].collider.attachedRigidbody == null && obstructionHits[i].distance > .05f) { obstructionFrames++; break; }
        }
        public IEnumerator AfterRace()
        {
            observing = false;
            Checks.Check(manager.FinalResults != null && manager.FinalResults.GetPlayerEntry().finished, "Automated player completed the race naturally");
            var entry = manager.FinalResults.GetPlayerEntry();
            Checks.Check(RacingSaveManager.CurrentMmr == entry.newMmr && PlayerPrefs.GetInt("AHR.Racing.MMR.V1") == entry.newMmr, "Ranked result persisted its calculated MMR");
            Checks.Check(entry.newMmr == MmrSystem.ApplyDelta(entry.oldMmr, entry.mmrDelta), "Results MMR delta matches the saved rating");
            int completedRaces = RacingSaveManager.RacesCount;
            manager.TransitionToResults();
            Checks.Check(RacingSaveManager.RacesCount == completedRaces, "Repeated Results request does not duplicate ranked rewards");

            // Exercise the real human input component separately from the automated race driver.
            var car = manager.PlayerVehicle;
            var ai = car.GetComponent<AiVehicleController>();
            ai.enabled = false;
            var input = car.GetComponent<PlayerRacingInput>();
            input.SetInputEnabled(true);car.SetCommandProvider(input);car.SetRaceEnabled(true);
            input.SetTouchSteer(1);input.SetTouchGas(true);
            yield return new WaitForSecondsRealtime(.12f);
            Checks.Check(car.LastCommands.Throttle > .9f && car.LastCommands.Steering > .8f, "Player touch throttle and steering reach physics within 120 ms");
            input.SetTouchGas(false);input.SetTouchSteer(0);input.SetTouchBrake(true);
            yield return new WaitForSecondsRealtime(.12f);
            Checks.Check(car.LastCommands.Brake > .9f, "Player brake input reaches physics");
            input.SetTouchBrake(false);
            var hiddenResults = new List<Canvas>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.name.IndexOf("result", StringComparison.OrdinalIgnoreCase) >= 0) { hiddenResults.Add(canvas);canvas.gameObject.SetActive(false); }
            var graph = manager.ProgressTracker.TrackGraph;
            var road = graph.SampleDistance(35);
            car.Body.position = road.Position + Vector3.up * .8f;
            car.Body.rotation = Quaternion.LookRotation(road.Forward, Vector3.up);
            car.ResetVelocities();
            Object.FindFirstObjectByType<RacingCamera3D>().SnapToTarget();
            input.SetTouchBrake(true);
            yield return new WaitForSecondsRealtime(2);
            Checks.Check(car.ForwardSpeedMps < -1.5f, "Real brake input accelerates into reverse");
            cameraScenarios.Add("reverse");ObserveCamera();Capture("camera-reverse");
            input.SetTouchBrake(false);
            // A controlled 180-degree arc stresses camera yaw independently of the
            // driver's skill. This is explicitly a pose fixture, not human driving.
            car.SetRaceEnabled(false);
            for(int i=0;i<=40;i++)
            {
                float angle=i*Mathf.PI/40;
                car.Body.position=road.Position+Vector3.up*.8f+road.Right*(4*(1-Mathf.Cos(angle)))+road.Forward*(4*Mathf.Sin(angle));
                car.Body.rotation=Quaternion.AngleAxis(angle*Mathf.Rad2Deg,Vector3.up)*Quaternion.LookRotation(road.Forward,Vector3.up);
                yield return new WaitForSecondsRealtime(.025f);ObserveCamera();
            }
            cameraScenarios.Add("hairpin (controlled 180-degree pose arc)");Capture("camera-hairpin");
            car.SetRaceEnabled(true);
            var recovery = car.GetComponent<VehicleRecovery>();
            recovery.SetLastSafeTransform(road.Position + Vector3.up * .8f, Quaternion.LookRotation(road.Forward, Vector3.up));
            int before = recovery.RecoveryCount;
            car.Body.position += road.Right * 30;
            recovery.TriggerRecovery("Explicit camera QA fixture");
            yield return null;
            Checks.Check(recovery.RecoveryCount == before + 1 && Vector3.Distance(camera.transform.position, car.transform.position) < 12, "Recovery snaps the camera to the restored racer");
            cameraScenarios.Add("recovery");ObserveCamera();Capture("camera-recovery");
            yield return new WaitForSecondsRealtime(recovery.PenaltySeconds + .1f);
            car.ResetVelocities();
            // Physical wall and racer contacts, without changing driving parameters.
            var wall = new GameObject("Closure camera collision fixture");
            wall.transform.position = car.transform.position + car.transform.forward * 4;
            wall.transform.rotation = car.transform.rotation;
            wall.AddComponent<BoxCollider>().size = new Vector3(8, 4, 1);
            int impacts = 0;Action<float> hit = value => impacts++;car.OnImpact += hit;
            input.SetTouchGas(true);
            yield return new WaitForSecondsRealtime(1.6f);
            input.SetTouchGas(false);
            Checks.Check(impacts > 0, "Camera wall-collision fixture makes a physical contact");
            cameraScenarios.Add("wall collision");ObserveCamera();Capture("camera-wall-collision");
            Object.Destroy(wall);
            yield return null;
            int previousImpacts = impacts;
            var rival = manager.SpawnedVehicles[1];
            // Restore contacts for this explicit fixture after the finish ghost state.
            rival.SetRaceEnabled(true);rival.SetRaceEnabled(false);
            car.Body.position = road.Position + Vector3.up * .8f;car.Body.rotation = Quaternion.LookRotation(road.Forward, Vector3.up);car.ResetVelocities();
            rival.Body.position = car.transform.position + road.Forward * 3.5f;rival.Body.rotation = car.Body.rotation;rival.ResetVelocities();
            input.SetTouchGas(true);
            yield return new WaitForSecondsRealtime(1.6f);
            input.SetTouchGas(false);car.OnImpact -= hit;
            Checks.Check(impacts > previousImpacts, "Camera racer-collision fixture makes a physical contact");
            cameraScenarios.Add("racer collision");ObserveCamera();Capture("camera-racer-collision");
            car.SetRaceEnabled(false);
            foreach(var canvas in hiddenResults)if(canvas!=null)canvas.gameObject.SetActive(true);
            Checks.Check(invalidFrames == 0, "Camera has finite positions and FOV throughout observed scenarios");
            Checks.Check(outsideViewFrames == 0, "Observed camera keeps the racer center in view");
            Checks.Check(obstructionFrames == 0, "Observed camera does not cross static occluders");
            File.WriteAllText(Path.Combine(output, "camera-observations.txt"), "classification=AUTOMATED_NATURAL_RACE_AND_CONTROLLED_PHYSICAL_FIXTURES\nframes=" + cameraFrames + "\ninvalid=" + invalidFrames + "\noutsideView=" + outsideViewFrames + "\nobstructed=" + obstructionFrames + "\nscenarios=" + string.Join(", ", cameraScenarios) + "\n");
        }
        public IEnumerator Adventure()
        {
            Click("MAIN MENU");yield return new WaitForSecondsRealtime(.8f);
            Checks.Check(SceneManager.GetActiveScene().name == "MainMenu", "Racing Results returns through Hub to Main Menu");
            Checks.Check(PlayerPrefs.GetInt("AHR.Racing.MMR.V1") == RacingSaveManager.CurrentMmr, "Ranked save survives scene navigation");
            Click("ADVENTURE");
            yield return new WaitForSecondsRealtime(.8f);
            var cards = Object.FindObjectsByType<TrackSelectionCardUI>(FindObjectsSortMode.None);
            Checks.Check(cards.Length >= 5, "Real Adventure UI displays map selection");Capture("adventure-maps");
            TrackSelectionCardUI selected = null;
            foreach (var card in cards) if (card.IsUnlocked && card.Map.mapId == "countryside") { selected = card; break; }
            if (selected == null) { Checks.Check(false, "Countryside card is available");yield break; }
            selected.GetComponent<Button>().onClick.Invoke();yield return new WaitForSecondsRealtime(.8f);
            Checks.Check(SceneManager.GetActiveScene().name == "Garage" && GameSession.HasPendingAdventureTrack, "Adventure map Button opens preparation Garage");
            Click("ShowSkins");yield return new WaitForSecondsRealtime(.3f);Capture("adventure-skins");
            Checks.Check(GameObject.Find("Skins Panel")!=null, "Adventure skins panel opens through the real Button");
            Click("Shop");yield return new WaitForSecondsRealtime(.6f);Capture("adventure-shop");
            Checks.Check(SceneManager.GetActiveScene().name == "Garage" && GameObject.Find("Store Panel")!=null, "Shop tab opens the real Adventure store panel");
            // Re-select through the UI because the direct shop intentionally clears the pending track.
            Click("Adventure");yield return new WaitForSecondsRealtime(.6f);
            foreach (var card in Object.FindObjectsByType<TrackSelectionCardUI>(FindObjectsSortMode.None))
                if (card.IsUnlocked && card.Map.mapId == "countryside") { card.GetComponent<Button>().onClick.Invoke();break; }
            yield return new WaitForSecondsRealtime(.7f);
            for(int i=0;i<10 && SaveManager.SelectedVehicleId!="car-01";i++) { Click("PreviousVehicle");yield return new WaitForSecondsRealtime(.08f); }
            Checks.Check(SaveManager.SelectedVehicleId=="car-01","Adventure Garage selects the default Jeep through its real controls");
            Capture("adventure-garage");Click("StartAdventure");
            yield return new WaitForSecondsRealtime(1);
            var game = Object.FindFirstObjectByType<GameManager>();
            var car = Object.FindFirstObjectByType<VehiclePhysics2DController>();
            Checks.Check(SceneManager.GetActiveScene().name == "Adventure2_5D" && car != null && game != null, "Adventure vehicle spawns through the real UI");
            if (car == null || game == null) yield break;
            Checks.Check(car.Body2D != null && Physics2D.simulationMode == SimulationMode2D.FixedUpdate && Object.FindFirstObjectByType<TerrainManager>() != null, "Adventure uses live Box2D and authored terrain");
            var input = Object.FindFirstObjectByType<VehicleInputController>();
            Checks.Check(input!=null,"Adventure exposes its shared player input controller");
            if(input==null)yield break;
            float position = car.ProgressPosition;float fuel = car.FuelNormalized;
            // Observe consumption before traversing natural fuel pickups, which
            // can legitimately refill the tank during the acceleration sample.
            yield return new WaitForSecondsRealtime(.3f);
            Checks.Check(car.FuelNormalized < fuel && car.Fuel.CurrentConsumptionNormalizedPerSecond > 0, "Adventure fuel drains during the active run");
            input.SetTouchGas(true);yield return new WaitForSecondsRealtime(2);input.SetTouchGas(false);
            Checks.Check(car.ProgressPosition > position + 1 && car.SpeedMps > .1f, "Adventure touch acceleration moves the spawned vehicle");
            float speed = Mathf.Abs(car.SpeedMps);input.SetTouchBrake(true);yield return new WaitForSecondsRealtime(.8f);input.SetTouchBrake(false);
            Checks.Check(Mathf.Abs(car.SpeedMps) < speed, "Adventure braking reduces speed");Capture("adventure-gameplay");
            int coins = game.RunCoins, gems = SaveManager.TotalGems;
            Collect<CoinPickup>(car);yield return new WaitForSecondsRealtime(.3f);
            Checks.Check(game.RunCoins > coins, "Physical Adventure coin pickup increases run coins");
            Collect<GemPickup>(car);yield return new WaitForSecondsRealtime(.3f);
            Checks.Check(SaveManager.TotalGems > gems, "Physical Adventure gem pickup persists its award");
            car.Fuel.SetNormalized(.4f);Collect<FuelPickup>(car);yield return new WaitForSecondsRealtime(.3f);
            Checks.Check(car.FuelNormalized > .4f, "Physical Adventure fuel pickup replenishes fuel");
            // Controlled physical head contact exercises the production trigger and HUD.
            var head=car.GetComponentInChildren<DriverHeadImpact>();
            Checks.Check(head!=null,"Adventure driver has a physical head-contact component");
            var headContact=new GameObject("Closure physical head-contact fixture");
            if(head!=null)headContact.transform.position=head.GetComponent<Collider2D>()!=null?head.GetComponent<Collider2D>().bounds.center:head.transform.position;
            headContact.AddComponent<CircleCollider2D>().radius=.3f;Physics2D.SyncTransforms();
            yield return new WaitForSecondsRealtime(.2f);
            Checks.Check(game.State == RunState.CrashPending && game.EndReason == RunEndReason.DriverDown, "Physical Driver Down contact starts the real countdown and disables driving");Capture("adventure-driver-down");
            Object.Destroy(headContact);
            yield return new WaitForSecondsRealtime(3.5f);
            Checks.Check(game.State == RunState.Results, "Driver Down reaches Game Over results");Capture("adventure-game-over");
            Click("Retry");yield return new WaitForSecondsRealtime(1);
            game = Object.FindFirstObjectByType<GameManager>();car = Object.FindFirstObjectByType<VehiclePhysics2DController>();
            car.Fuel.SetNormalized(0);yield return new WaitForSecondsRealtime(.2f);
            Checks.Check(game.State == RunState.OutOfFuelPending, "Zero fuel starts the production Out Of Fuel countdown");Capture("adventure-out-of-fuel");
            yield return new WaitForSecondsRealtime(3.5f);
            Checks.Check(game.State == RunState.OutOfFuelFinal || game.State == RunState.Results, "Out Of Fuel reaches its final state");
            PlayerPrefs.Save();Checks.Check(PlayerPrefs.GetInt("AHR.TotalGems") == SaveManager.TotalGems, "Adventure gem save matches persistent PlayerPrefs");
            Click("Menu");yield return new WaitForSecondsRealtime(.7f);
            Checks.Check(SceneManager.GetActiveScene().name == "PreStart", "Adventure returns to Menu through its real Button");
        }
        void Collect<T>(VehiclePhysics2DController car) where T : PickupBase
        {
            var pool=Object.FindFirstObjectByType<PickupPool>();
            PickupKind kind=typeof(T)==typeof(CoinPickup)?PickupKind.Coin:typeof(T)==typeof(GemPickup)?PickupKind.Gem:PickupKind.Fuel;
            var pickup=pool!=null?pool.Spawn(kind,car.Body2D.transform.position,car.ProgressPosition):null;
            Checks.Check(pickup!=null,"Production pool spawns controlled physical " + typeof(T).Name);
            Physics2D.SyncTransforms();
        }
        void Click(string name)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!button.interactable || !button.gameObject.activeInHierarchy) continue;
                bool matches = button.name == name;
                for (int i=0;i<button.onClick.GetPersistentEventCount();i++) matches |= button.onClick.GetPersistentMethodName(i) == name;
                if (matches) { button.onClick.Invoke();return; }
            }
            Checks.Check(false, "Real UI Button found: " + name);
            File.AppendAllText(Path.Combine(output,"qa-buttons.txt"), SceneManager.GetActiveScene().name + " missing=" + name + "\n");
        }
        void Capture(string name) { RacingEvidenceCapture.Capture(Path.Combine(output,name + ".png")); }
        public void Export() { File.WriteAllText(Path.Combine(output,"final-qa-contracts.json"),JsonUtility.ToJson(Checks,true)); }
        public IEnumerator Guarded(IEnumerator routine)
        {
            while(true)
            {
                bool more;object current=null;
                try { more=routine.MoveNext();if(more)current=routine.Current; }
                catch(Exception exception) { File.AppendAllText(Path.Combine(output,"qa-fixture-exceptions.txt"),exception.ToString()+"\n");Checks.Check(false,"QA fixture exception: " + exception.GetType().Name + ": " + exception.Message);more=false; }
                if(!more)break;
                yield return current;
            }
            Export();
        }
        void OnDestroy() { if(manager!=null && manager.PlayerVehicle!=null)manager.PlayerVehicle.OnLanding -= Landing; }
    }
}
#endif
