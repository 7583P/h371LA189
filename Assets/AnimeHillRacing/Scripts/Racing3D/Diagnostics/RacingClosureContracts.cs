#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    [Serializable] public sealed class ClosureContractReport
    {
        public int passed, failed;
        public List<string> successes = new List<string>(), failures = new List<string>();
        public void Check(bool condition, string description)
        {
            if (condition) { passed++; successes.Add(description); }
            else { failed++; failures.Add(description); Debug.LogError("CLOSURE_CONTRACT_FAILED " + description); }
        }
    }
    public static class RacingClosureContracts
    {
        sealed class Commands : IVehicleCommandProvider
        {
            public VehicleCommands value;
            public VehicleCommands GetCommands() => value;
        }
        public static void Progress(ClosureContractReport report)
        {
            var root = new GameObject("Closure track contract");
            var graph = root.AddComponent<TrackSplineGraph>();
            var nodes = new TrackNode[4];
            Vector3[] points = { Vector3.zero, new Vector3(0,0,30), new Vector3(30,0,30), new Vector3(30,0,0) };
            for (int i=0;i<4;i++) nodes[i] = new TrackNode { Position=points[i], Forward=(points[(i+1)%4]-points[i]).normalized, Up=Vector3.up, Right=Vector3.Cross(Vector3.up,(points[(i+1)%4]-points[i]).normalized), TrackWidth=20 };
            graph.InitializeFromNodes(nodes);
            graph.ProjectPosition(new Vector3(10,0,0),out int node,out float along,out _);
            report.Check(node==3 && Mathf.Abs(along-110)<.001f,"Track projection includes the incoming closing segment");
            var tracker = root.AddComponent<TrackProgressTracker>();
            tracker.Initialize(graph,3,6);
            var carRoot = new GameObject("Closure progress racer");
            var car = carRoot.AddComponent<RacingVehiclePhysics>();
            var progress = tracker.RegisterRacer(new RacerIdentity("test","Test","car-01","wheel-default",1000,true,1,1),car,null);
            tracker.NotifyCheckpoint(car,2,false,1);
            report.Check(progress.CheckpointVisits==0,"Checkpoint skipping is rejected");
            tracker.NotifyCheckpoint(car,0,true,2);
            tracker.NotifyCheckpoint(car,0,true,3);
            report.Check(progress.CheckpointVisits==1 && progress.CurrentLap==1,"Start and duplicate finish do not create a lap");
            tracker.NotifyCheckpoint(car,1,false,4);
            tracker.NotifyCheckpoint(car,0,true,5);
            report.Check(progress.CurrentLap==1 && !progress.Finished,"Early finish cannot bypass remaining gates");
            float time=10;
            for(int lap=0;lap<3;lap++)
            {
                for(int gate=lap==0?2:1;gate<6;gate++) tracker.NotifyCheckpoint(car,gate,false,time+=10);
                tracker.NotifyCheckpoint(car,0,true,time+=10);
            }
            report.Check(progress.Finished && progress.CheckpointVisits==19 && progress.FinishTime>0,"Three complete laps require exactly 19 gate visits");
            UnityEngine.Object.DestroyImmediate(carRoot); UnityEngine.Object.DestroyImmediate(root);
            var first=RacingArt.Bevel(new Vector3(.255f,.18f,.045f),.018f);
            var second=RacingArt.Bevel(new Vector3(.345f,.18f,.045f),.018f);
            report.Check(Mathf.Abs(first.bounds.size.x-.255f)<.001f && Mathf.Abs(second.bounds.size.x-.345f)<.001f,"Wheel mesh cache preserves distinct tyre widths without rounding");
        }
        public static IEnumerator ItemsAndDrift(ClosureContractReport report,string output)
        {
            SceneManager.LoadScene("Racing3D");
            yield return new WaitForSecondsRealtime(2);
            var manager = UnityEngine.Object.FindFirstObjectByType<RaceManager>();
            manager.enabled=false;
            manager.StopAllCoroutines();
            foreach(var car in manager.SpawnedVehicles) car.SetRaceEnabled(false);
            RacingImprovementContracts.World(report,manager);
            var floor=new GameObject("Isolated item and drift test surface");
            var floorCollider=floor.AddComponent<BoxCollider>();floorCollider.size=new Vector3(250,1,350);
            floor.transform.position=new Vector3(5000,99.5f,0);floor.AddComponent<RacingSurface>();
            var command=new Commands();
            var owner=Create("Test owner",new Vector3(5000,100.6f,0),command);
            var rival=Create("Test rival",new Vector3(5000,100.6f,8),new Commands());
            var inventory=owner.GetComponent<ItemInventory>();var defense=rival.GetComponent<ItemInventory>();
            var pool=RacingItemPool.ForScene();
            var entities=pool.GetComponentsInChildren<ItemEntityBase>(true);
            var ids=new HashSet<int>();foreach(var entity in entities)ids.Add(entity.GetInstanceID());
            yield return new WaitForSeconds(.4f);
            Use(inventory,RaceItemType.TurboPulse,report);
            report.Check(owner.IsBoosting,"TurboPulse activates the shared vehicle boost");
            yield return new WaitForSeconds(2.3f);
            report.Check(!owner.IsBoosting,"TurboPulse expires");
            Use(inventory,RaceItemType.HyperCharge,report);
            report.Check(owner.IsBoosting&&inventory.HasShield,"HyperCharge activates boost and protection");
            yield return new WaitForSeconds(3.1f);
            report.Check(!owner.IsBoosting&&!inventory.HasShield,"HyperCharge clears both states");
            Use(defense,RaceItemType.MagneticShield,report);
            report.Check(defense.HasShield,"MagneticShield becomes active");
            Place(owner,new Vector3(5000,100.6f,0));Place(rival,new Vector3(5000,100.6f,8));
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Use(inventory,RaceItemType.KineticMissile,report);
            float deadline=Time.time+2;
            while(pool.ActiveCount>0&&Time.time<deadline)yield return null;
            report.Check(!defense.HasShield&&!rival.IsSpunOut&&pool.ActiveCount==0,"Missile impact consumes shield and returns the entity to its pool");
            yield return new WaitForSeconds(.3f);
            Place(owner,new Vector3(5000,100.6f,0));Place(rival,new Vector3(5000,100.6f,8));
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Use(inventory,RaceItemType.KineticMissile,report);
            deadline=Time.time+2;
            while(!rival.IsSpunOut&&Time.time<deadline)yield return null;
            report.Check(rival.IsSpunOut&&pool.ActiveCount==0,"Unshielded missile produces a real hit and returns to pool");
            rival.ResetVelocities();owner.ResetVelocities();
            yield return new WaitForSeconds(.3f);
            Place(owner,new Vector3(5000,100.6f,0));Place(rival,new Vector3(5000,100.6f,8));
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Use(inventory,RaceItemType.ElectroSpike,report);
            yield return new WaitForSeconds(.55f);
            Place(rival,new Vector3(5000,100.6f,-2.1f));
            deadline=Time.time+1;
            while(!rival.IsSpunOut&&Time.time<deadline)yield return null;
            report.Check(rival.IsSpunOut&&pool.ActiveCount==0,"ElectroSpike arms, hits and returns to pool");
            rival.ResetVelocities();owner.ResetVelocities();
            rival.Body.position=new Vector3(5004,100.6f,0);Physics.SyncTransforms();
            yield return new WaitForSeconds(.3f);
            Use(inventory,RaceItemType.ShockwavePulse,report);
            report.Check(rival.IsSpunOut,"ShockwavePulse affects a nearby rival");
            yield return new WaitForSeconds(.3f);
            Use(defense,RaceItemType.MagneticShield,report);
            rival.ResetVelocities();
            Use(inventory,RaceItemType.ShockwavePulse,report);
            report.Check(!defense.HasShield&&!rival.IsSpunOut,"Shield also protects against shockwave");
            yield return new WaitForSeconds(.3f);
            Use(defense,RaceItemType.MagneticShield,report);
            yield return new WaitForSeconds(5.8f);
            report.Check(!defense.HasShield,"MagneticShield expires without a hit");
            bool reusedExisting=true;
            for(int i=0;i<100;i++)
            {
                var missile=pool.Fire(owner,new Vector3(5000,110,0),Quaternion.identity);
                reusedExisting &= missile!=null&&ids.Contains(missile.GetInstanceID());
                if(missile!=null)pool.Release(missile);
            }
            report.Check(reusedExisting,"100 pool requests reuse prewarmed instance IDs");
            report.Check(pool.ActiveCount==0&&pool.GetComponentsInChildren<ItemEntityBase>(true).Length==entities.Length&&pool.DroppedRequests==0,"Pool capacity is unchanged after repeated item use");
            owner.Body.position=new Vector3(5000,100.6f,0);owner.Body.rotation=Quaternion.identity;
            owner.ResetVelocities();rival.SetRaceEnabled(false);
            Physics.SyncTransforms();command.value=new VehicleCommands{Throttle=1};
            deadline=Time.time+10;
            while(owner.SpeedKmh<65&&Time.time<deadline)yield return new WaitForFixedUpdate();
            report.Check(owner.SpeedKmh>=65,"Shared physics reaches drift entry speed on throttle");
            command.value=new VehicleCommands{Throttle=1,Steering=.65f,Drift=true};
            float largestTurn=0;Quaternion previous=owner.Body.rotation;
            for(int step=0;step<180;step++)
            {
                yield return new WaitForFixedUpdate();
                largestTurn=Mathf.Max(largestTurn,Quaternion.Angle(previous,owner.Body.rotation));previous=owner.Body.rotation;
            }
            report.Check(owner.IsDrifting&&owner.CurrentMiniTurboStage!=MiniTurboStage.None,"Sustained real drift charges a mini turbo");
            report.Check(largestTurn<8,"Drift changes orientation continuously without a snap");
            command.value=new VehicleCommands{Throttle=1,Steering=.2f};
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            report.Check(!owner.IsDrifting&&owner.IsBoosting,"Drift release produces mini turbo");
            // Isolated real physics verifies hop -> trick -> landing, tactical holds,
            // repeated acquisitions, physical missile occlusion and finish ghosts.
            Place(owner,new Vector3(5000,100.6f,0));Place(rival,new Vector3(5000,100.6f,8));
            command.value=VehicleCommands.Idle;yield return new WaitForSeconds(.3f);
            int jumps=owner.JumpsPerformed,tricks=owner.TricksPerformed,landings=owner.LandingBoosts;
            command.value=new VehicleCommands{Jump=true};yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            command.value=VehicleCommands.Idle;
            report.Check(owner.JumpsPerformed==jumps+1&&!owner.IsGrounded,"Player hop leaves the ground and fires exactly one jump");
            deadline=Time.time+1;while(owner.AirborneTime<.18f&&Time.time<deadline)yield return new WaitForFixedUpdate();
            command.value=new VehicleCommands{Trick=true};yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();command.value=VehicleCommands.Idle;
            report.Check(owner.TricksPerformed==tricks+1,"A real airborne hop accepts its trick");
            deadline=Time.time+2;while(!owner.IsGrounded&&Time.time<deadline)yield return new WaitForFixedUpdate();
            report.Check(owner.LandingBoosts==landings+1&&owner.IsBoosting,"The trick produces a shared landing boost on physical contact");
            Place(owner,new Vector3(5000,100.6f,0));Place(rival,new Vector3(5000,100.6f,8));yield return new WaitForSeconds(.3f);
            manager.ProgressTracker.Initialize(manager.ProgressTracker.TrackGraph,3,6);
            manager.ProgressTracker.RegisterRacer(new RacerIdentity("fixture-owner","Fixture owner","car-01","wheel-default",100000,true,1,1),owner,null);
            manager.ProgressTracker.RegisterRacer(new RacerIdentity("fixture-rival","Fixture rival","car-01","wheel-default",100000,false,2,2),rival,null);
            typeof(RaceManager).GetField("currentState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(manager,RaceState.Racing);
            var gate=new GameObject("Stationary checkpoint crossing fixture");gate.transform.position=new Vector3(5000,101,1);
            var volume=gate.AddComponent<BoxCollider>();volume.isTrigger=true;volume.size=new Vector3(6,3,6);
            var checkpoint=gate.AddComponent<CheckpointTrigger3D>();checkpoint.Configure(0,true);checkpoint.Bind(manager.ProgressTracker,manager);
            Physics.SyncTransforms();yield return new WaitForSeconds(.2f);
            var crossing=manager.ProgressTracker.FindRacer(owner);
            report.Check(crossing.CheckpointVisits==0,"Entering a gate while stationary cannot create a checkpoint");
            command.value=new VehicleCommands{Throttle=1};deadline=Time.time+2;
            while(crossing.CheckpointVisits==0&&Time.time<deadline)yield return new WaitForFixedUpdate();
            report.Check(crossing.CheckpointVisits==1,"Accelerating through an already occupied checkpoint is registered by real trigger Stay");
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            report.Check(crossing.CheckpointVisits==1,"Multiple chassis colliders and trigger Stay do not duplicate a gate");
            checkpoint.Configure(1,false);command.value=new VehicleCommands{Brake=1};owner.Body.linearVelocity=-Vector3.forward*8;
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            report.Check(crossing.CheckpointVisits==1,"A physical reverse crossing is rejected");
            UnityEngine.Object.Destroy(gate);command.value=VehicleCommands.Idle;
            Place(owner,new Vector3(5000,100.6f,0));Place(rival,new Vector3(5000,100.6f,8));yield return new WaitForSeconds(.3f);
            var tactical=new AiItemAgent(AiSkillModel.CreateProfile(100000,42),inventory,owner,manager.ProgressTracker.TrackGraph,new AiDecisionLogger());
            inventory.AcquireItem(RaceItemType.TurboPulse,false);yield return new WaitForSeconds(.25f);
            report.Check(!tactical.ShouldUseItem(new AiItemContext{reserveBoost=true}),"Elite AI holds boost for a known shortcut");
            report.Check(tactical.ShouldUseItem(new AiItemContext{reserveBoost=true,shortcutEntryReady=true}),"Elite AI releases its reserved boost at a controlled route entry");
            inventory.UseItem();int serial=inventory.AcquisitionSerial;inventory.AcquireItem(RaceItemType.TurboPulse,false);
            report.Check(inventory.AcquisitionSerial==serial+1&&!tactical.ShouldUseItem(new AiItemContext{shortcutEntryReady=true}),"Reacquiring the same item starts a fresh reaction interval");
            yield return new WaitForSeconds(.3f);inventory.UseItem();yield return new WaitForSeconds(.3f);
            Place(owner,new Vector3(5000,100.6f,0));Place(rival,new Vector3(5000,100.6f,8));yield return new WaitForSeconds(.3f);
            inventory.AcquireItem(RaceItemType.KineticMissile,false);yield return new WaitForSeconds(.25f);
            var wall=new GameObject("Missile occlusion fixture");wall.transform.position=new Vector3(5000,101,4);wall.AddComponent<BoxCollider>().size=new Vector3(4,2,1);Physics.SyncTransforms();
            report.Check(!tactical.ShouldUseItem(new AiItemContext{targetAhead=true}),"Elite missile timing rejects a physically occluded rival");
            UnityEngine.Object.Destroy(wall);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            report.Check(tactical.ShouldUseItem(new AiItemContext{targetAhead=true}),"Elite missile timing accepts a visible rival in range");
            inventory.UseItem();yield return new WaitForSeconds(1.5f);rival.ResetVelocities();
            inventory.AcquireItem(RaceItemType.MagneticShield,false);yield return new WaitForSeconds(.25f);
            report.Check(!tactical.ShouldUseItem(new AiItemContext()),"Elite AI preserves a shield without a threat");
            report.Check(tactical.ShouldUseItem(new AiItemContext{projectileThreat=true}),"Elite AI defends against a projectile threat");
            inventory.UseItem();inventory.AcquireItem(RaceItemType.MagneticShield,false);yield return new WaitForSeconds(.3f);
            report.Check(!tactical.ShouldUseItem(new AiItemContext{projectileThreat=true}),"Elite AI avoids wasting a shield while protection is active");
            inventory.UseItem();yield return new WaitForSeconds(.3f);inventory.AcquireItem(RaceItemType.ShockwavePulse,false);yield return new WaitForSeconds(.25f);
            report.Check(!tactical.ShouldUseItem(new AiItemContext{rivalsInPulseRadius=0})&&tactical.ShouldUseItem(new AiItemContext{rivalsInPulseRadius=1}),"Elite shockwave waits for a rival inside the actual pulse radius");
            inventory.UseItem();
            rival.SetRaceEnabled(true);
            Place(owner,new Vector3(4999,100.6f,0));Place(rival,new Vector3(5001,100.6f,0));
            var supply=ItemBox.CreateBoostSupply(floor.transform,new Vector3(5000,101.1f,0),6);
            int ownerUses=inventory.ItemsUsed,rivalUses=defense.ItemsUsed;
            Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            report.Check(inventory.HeldItem==RaceItemType.TurboPulse&&defense.HeldItem==RaceItemType.TurboPulse,"A visible route boost pickup supplies the same item to human and bot collectors");
            report.Check(inventory.ItemsUsed==ownerUses&&defense.ItemsUsed==rivalUses,"Route boost pickups stay held until the ordinary use command");
            yield return new WaitForSeconds(.3f);inventory.UseItem();
            report.Check(!supply.CanCollect(owner),"Route boost pickup cooldown prevents immediate repeated collection by the same racer");
            UnityEngine.Object.Destroy(supply.gameObject);owner.SetFinished();
            bool ghost=true;foreach(var collider in owner.GetComponents<Collider>())ghost&=!collider.enabled;
            report.Check(ghost&&owner.Body.isKinematic&&!owner.RaceEnabled,"Finished racers cannot drive, attack or block the active pack");
            owner.SetRaceEnabled(true);bool restored=true;foreach(var collider in owner.GetComponents<Collider>())restored&=collider.enabled;
            report.Check(restored&&!owner.Body.isKinematic,"Restarting a finished vehicle restores its physical contacts");
            rival.SetRaceEnabled(false);
            Place(rival,new Vector3(5080,100.6f,-100));
            Place(owner,new Vector3(5030,100.6f,-12));
            var recovery=owner.gameObject.AddComponent<VehicleRecovery>();
            command.value=new VehicleCommands{Throttle=.8f};
            yield return null;
            // The isolated floor is intentionally outside the production track.
            typeof(VehicleRecovery).GetField("graph",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(recovery,null);
            recovery.SetLastSafeTransform(owner.transform.position,Quaternion.identity);
            int recoveryCount=recovery.RecoveryCount;
            yield return new WaitForSeconds(5);
            File.WriteAllText(Path.Combine(output,"recovery-free-drive.txt"),$"classification=REAL_UNITY_FREE_DRIVE\nrecoveries={recovery.RecoveryCount-recoveryCount}\nspeed={owner.SpeedMps}\nposition={owner.transform.position}\n");
            report.Check(recovery.RecoveryCount==recoveryCount&&owner.SpeedMps>5,"Shared recovery does not interrupt normal forward driving");
            var recoveryWall=new GameObject("Oscillating contact recovery fixture");
            recoveryWall.transform.position=new Vector3(5000,101.5f,3);
            recoveryWall.AddComponent<BoxCollider>().size=new Vector3(8,4,1);
            Place(owner,new Vector3(5000,100.6f,0));
            recovery.SetLastSafeTransform(new Vector3(5000,100.6f,-8),Quaternion.identity);
            deadline=Time.time+9;
            while(recovery.RecoveryCount==recoveryCount&&Time.time<deadline)
            {
                var oscillating=owner.Body.linearVelocity;
                oscillating.x=Mathf.Sin(Time.time*9)*1.4f;
                owner.Body.linearVelocity=oscillating;
                yield return new WaitForSeconds(.1f);
            }
            report.Check(recovery.RecoveryCount==recoveryCount+1&&recovery.LastRecoveryReason=="Vehicle stuck","Oscillating wall contact recovers despite intermittent nonzero speed");
            owner.SetRaceEnabled(false);
            UnityEngine.Object.Destroy(recoveryWall);
            File.WriteAllText(Path.Combine(output,"item-drift-contracts.json"),JsonUtility.ToJson(report,true));
            UnityEngine.Object.Destroy(owner.gameObject);UnityEngine.Object.Destroy(rival.gameObject);UnityEngine.Object.Destroy(floor);
        }
        static RacingVehiclePhysics Create(string name,Vector3 position,IVehicleCommandProvider provider)
        {
            var root=RacingVehicleFactory.Create(new RacerIdentity(name,name,"car-01","wheel-default",1000,false,42,1));
            root.transform.position=position;var car=root.GetComponent<RacingVehiclePhysics>();car.Initialize("car-01","wheel-default",provider);
            root.AddComponent<ItemInventory>().SetSeed(42);return car;
        }
        static void Place(RacingVehiclePhysics car,Vector3 position)
        {
            // Interpolated bodies retain their rendered pose until a physics step.
            // ItemInventory fires from Transform, so synchronize both test poses.
            car.ResetVelocities();car.transform.SetPositionAndRotation(position,Quaternion.identity);
            car.Body.position=position;car.Body.rotation=Quaternion.identity;Physics.SyncTransforms();
        }
        static void Use(ItemInventory inventory,RaceItemType item,ClosureContractReport report)
        { inventory.AcquireItem(item,false);report.Check(inventory.UseItem(),item+" can be acquired and used"); }
    }
}
#endif
