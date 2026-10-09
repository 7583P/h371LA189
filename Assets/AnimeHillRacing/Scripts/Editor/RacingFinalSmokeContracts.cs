#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AnimeHillRacing.Ads;
using AnimeHillRacing.Core;
using AnimeHillRacing.Racing3D;
using AnimeHillRacing.Racing3D.ML;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    // Focused closure checks, all disk writes confined to --closure-output.
    // No real or synthetic successful store transaction is claimed.
    public static class RacingFinalSmokeContracts
    {
        static LocalLeagueStore Store(string path)=>new LocalLeagueStore(path,v=>JsonUtility.ToJson(v),JsonUtility.FromJson<LocalLeagueState>);
        public static void Run()
        {
            string output=RacingFrameForensics.Argument("--closure-output");
            if(string.IsNullOrEmpty(output))throw new ArgumentException("Missing isolated output directory");
            Directory.CreateDirectory(output);var report=new ClosureContractReport();
            try { Persistence(report,output);Commercial(report);Telemetry(report,output); }
            catch(Exception error){report.Check(false,error.ToString());}
            File.WriteAllText(Path.Combine(output,"final-smoke.json"),JsonUtility.ToJson(report,true));
            if(report.failed!=0)throw new InvalidOperationException("Final smoke failed: "+report.failed);
            Debug.Log("FINAL_SMOKE passed="+report.passed+" failed="+report.failed);
            if(RacingFrameForensics.Argument("--closure-mode")=="build")RacingSkyHopContracts.Run();
        }
        [Serializable] sealed class GuardCase { public string name;public bool expectedLegal;public RaceTelemetrySession session; }
        [Serializable] sealed class GuardFixture { public string classification="SYNTHETIC_VALIDATOR_UNIT_CASES_NOT_RACES_OR_TRAINING";public List<GuardCase> cases=new List<GuardCase>(); }
        static RaceTelemetrySession Trajectory(float kmh,float start=0,int item=0,float use=0)
        {
            var data=new RaceTelemetrySession();data.metadata.schemaVersion="validator-unit-fixture";data.metadata.source="SYNTHETIC_VALIDATOR_UNIT_FIXTURE";
            float speed=kmh/3.6f;
            for(int i=0;i<6;i++)data.samples.Add(new TelemetrySample{timestamp=start+i*.1f,trackProgress=.1f,trackDistance=i*speed*.1f,
                worldPosition=new Vector3(0,1,i*speed*.1f),velocity=Vector3.forward*speed,speedMps=speed,speedKmh=kmh,throttle=1});
            if(item!=0)data.events.Add(new TelemetryEvent{timestamp=use,eventType=TelemetryEventType.ItemUsed,param1=item});
            return data;
        }
        static void Telemetry(ClosureContractReport report,string output)
        {
            var f=new GuardFixture();
            f.cases.Add(new GuardCase{name="ordinary_36",expectedLegal=true,session=Trajectory(36)});
            f.cases.Add(new GuardCase{name="unmarked_300",expectedLegal=false,session=Trajectory(300)});
            f.cases.Add(new GuardCase{name="golden_rock_is_not_lancer",expectedLegal=false,session=Trajectory(300,0,16)});
            f.cases.Add(new GuardCase{name="observed_lancer_300",expectedLegal=true,session=Trajectory(300,0,15)});
            f.cases.Add(new GuardCase{name="bounded_lancer_coast",expectedLegal=true,session=Trajectory(300,7,15)});
            f.cases.Add(new GuardCase{name="expired_lancer_cannot_justify_300",expectedLegal=false,session=Trajectory(300,8.2f,15)});
            f.cases.Add(new GuardCase{name="lancer_cannot_justify_330",expectedLegal=false,session=Trajectory(330,0,15)});
            f.cases.Add(new GuardCase{name="future_item_cannot_justify_prior_speed",expectedLegal=false,session=Trajectory(300,0,15,4)});
            f.cases.Add(new GuardCase{name="negative_item_time_cannot_justify_speed",expectedLegal=false,session=Trajectory(300,0,15,-1)});
            var teleport=Trajectory(300,0,15);var s=teleport.samples[2];s.worldPosition.z+=400;teleport.samples[2]=s;
            f.cases.Add(new GuardCase{name="lancer_cannot_justify_teleport",expectedLegal=false,session=teleport});
            var acceleration=Trajectory(300,0,15);s=acceleration.samples[2];s.speedMps=30;s.speedKmh=108;acceleration.samples[2]=s;
            f.cases.Add(new GuardCase{name="unexplained_acceleration_still_rejected",expectedLegal=false,session=acceleration});
            var floor=Trajectory(300,0,15);s=floor.samples[2];s.worldPosition.y=-26;floor.samples[2]=s;
            f.cases.Add(new GuardCase{name="under_map_still_rejected",expectedLegal=false,session=floor});
            foreach(var c in f.cases)report.Check(DeterministicLegalityFilter.ValidateTrajectory(c.session.samples,c.session.events).IsLegal==c.expectedLegal,"Telemetry guard: "+c.name);
            File.WriteAllText(Path.Combine(output,"telemetry-guard-fixtures.json"),JsonUtility.ToJson(f,true));
        }
        static void Persistence(ClosureContractReport report,string output)
        {
            var config=Resources.Load<RacingProgressionConfiguration>("RacingProgressionConfiguration").league;
            long epoch=new DateTime(2026,1,31,12,0,0,DateTimeKind.Utc).Ticks;
            var state=LocalLeagueEngine.Create(epoch,4207);
            report.Check(state.bots.Length==10000&&state.version==3&&state.human.currentMmr==0,"Fresh v3 fixture retains exactly 10000 zero-rated persistent bots");
            report.Check(state.seasonEndTimestamp==new DateTime(epoch,DateTimeKind.Utc).AddMonths(3).Ticks,"Season boundary remains three calendar months");
            state.human.currentMmr=10500;state.human.peakMmr=state.human.seasonPeakMmr=20000;
            state.human.selectedTitleId="beyond-apex";state.human.unlockedTitleIds.Add("beyond-apex");
            state.human.vehiclePreference="formula-01";state.human.wheelPreference="wheel-whity";
            state.human.characterPreference="purple-driver";state.human.accessoryPreference="crystal-crest";
            for(int i=0;i<state.bots.Length;i++){state.bots[i].currentMmr=10000+(i%7)*1000;state.bots[i].peakMmr=state.bots[i].seasonPeakMmr=20000;}
            var previous=(CompactLeagueBot[])state.bots.Clone();
            string path=Path.Combine(output,"focused-10k-journal");var store=Store(path);store.Load();
            report.Check(store.Save(state),"Existing migrated v3 fixture saves through the real binary journal");
            var resumed=Store(path).Load();
            report.Check(resumed!=null&&resumed.human.currentMmr==10500&&resumed.bots.Length==10000,"Fresh store instance reloads MMR and all 10000 bots");
            long boundary=resumed.seasonEndTimestamp;
            LocalSeasonEngine.AdvanceChunk(resumed,config,boundary,137);
            var interruptedStore=Store(path);interruptedStore.Load();
            report.Check(resumed.seasonTransitionCursor==137&&interruptedStore.Save(resumed),"Interrupted season commits the exact 137-record transition cursor");
            resumed=Store(path).Load();
            report.Check(resumed.seasonTransitionCursor==137&&resumed.human.currentMmr==10500,"Relaunch resumes the partial transition without an early human reset");
            while(resumed.currentSeasonNumber==1)LocalSeasonEngine.AdvanceChunk(resumed,config,boundary,137);
            report.Check(resumed.human.currentMmr==Math.Min(config.CalculateNormalSeasonReset(10500),10000)&&resumed.bots.All(b=>b.currentMmr<=10000),"Human and 10000 bots apply curve first, then cap prior Beyond at 10000");
            bool identical=true;
            for(int i=0;i<previous.Length;i++){var a=previous[i];var b=resumed.bots[i];identical&=a.id==b.id&&a.vehicle==b.vehicle&&a.wheels==b.wheels&&a.character==b.character&&a.accessory==b.accessory&&a.peakMmr==b.peakMmr;}
            report.Check(identical&&resumed.human.peakMmr==20000,"Season preserves persistent bot IDs, combos and historical peaks");
            report.Check(resumed.human.selectedTitleId=="beyond-apex"&&resumed.human.unlockedTitleIds.Contains("beyond-apex")&&resumed.human.characterPreference=="purple-driver"&&resumed.human.accessoryPreference=="crystal-crest","Human selected title and full combo survive season processing");
            report.Check(!LocalSeasonEngine.AdvanceOne(resumed,config,boundary)&&resumed.archives.Count==1&&resumed.human.history.Count==1,"Completed season processing remains idempotent");
            var completed=Store(path);completed.Load();report.Check(completed.Save(resumed)&&Store(path).Load().human.currentMmr==resumed.human.currentMmr,"Completed season survives another disk relaunch");
            var higher=new LocalLeagueConfiguration{resetCurve=new[]{new SoftResetPoint(0,0),new SoftResetPoint(10500,10200),new SoftResetPoint(20000,14000)}};
            var lower=new LocalLeagueConfiguration{resetCurve=new[]{new SoftResetPoint(0,0),new SoftResetPoint(10100,9600),new SoftResetPoint(10500,9950),new SoftResetPoint(20000,14000)}};
            report.Check(higher.ResetMmr(10500)==10000&&lower.ResetMmr(10500)==9950&&lower.ResetMmr(10100)==9600,"Anti-farm examples: 10500->10200->10000, 10500->9950, 10100->9600");
            var engine=new LocalLeagueEngine(resumed,config);var opponents=engine.SelectOpponents(resumed.human.currentMmr,4207,false);
            report.Check(opponents.Length==11&&opponents.Select(b=>b.id).Distinct().Count()==11&&opponents.All(b=>engine.TryIndex(b.id,out _)),"Focused matchmaking selects eleven distinct persisted IDs");
            report.Check(MmrTierUtility.GetTier(10000)!=MmrTier.ApexBeyond&&MmrTierUtility.GetTier(10001)==MmrTier.ApexBeyond&&MmrTierUtility.GetBeyondCounter(20000)==10,"Exactly 10000 stays normal ranked; Beyond and historical prestige remain strict");
        }
        static void Commercial(ClosureContractReport report)
        {
            var config=Resources.Load<AdEntitlementConfiguration>("AdEntitlementConfiguration");
            var purchases=new RealMoneyEntitlementService(null,null);
            report.Check(config!=null&&config.vipSubscriptionProductId=="vip_monthly"&&purchases.Products.Count(p=>p.ProductId=="vip_monthly"&&p.ProductType==StoreProductType.Subscription)==1,"vip_monthly remains registered exactly once as a subscription");
            string before=JsonUtility.ToJson(AdEntitlementService.Ledger.Data);
            report.Check(!purchases.TryFulfillNewPurchase("vip_monthly","",out _)&&JsonUtility.ToJson(AdEntitlementService.Ledger.Data)==before,"Missing store transaction cannot grant VIP or ad-free entitlement");
            long now=new DateTime(2026,1,31,12,0,0,DateTimeKind.Utc).Ticks;
            var fixture=new AdEntitlementData{adFreeUntilUtc=now+TimeSpan.FromDays(30).Ticks,vipUntilUtc=now+TimeSpan.FromDays(31).Ticks,lastKnownUtcTimestamp=now};
            var restored=new AdEntitlementLedger(JsonUtility.FromJson<AdEntitlementData>(JsonUtility.ToJson(fixture)));
            report.Check(restored.IsVipActive(now)&&restored.IsAdFree(now)&&restored.Data.adFreeUntilUtc==fixture.adFreeUntilUtc,"Existing entitlement serialization preserves VIP expiry and 30-day adFreeUntil; no purchase simulated");
            report.Check(!restored.CompleteQualifyingPurchase("",now)&&restored.Data.handledTransactions.Count==0,"An empty transaction creates neither entitlement nor receipt history");
            bool leader=true,legal=true;var rng=new DeterministicRng(4207);var seen=new bool[17];
            for(int p=1;p<=12;p++)for(int i=0;i<1024;i++){
                var item=ItemDistributionTable.RollItem(p,rng);legal&=ItemDistributionTable.IsAllowedAtPosition(item,p);
                if(p==1){leader&=item==RaceItemType.Coin10||item==RaceItemType.ElectroSpike;seen[(int)item]=true;}
            }
            report.Check(leader&&seen[13]&&seen[3]&&legal&&!ItemDistributionTable.IsAllowedAtPosition(RaceItemType.Lancer,6)&&ItemDistributionTable.IsAllowedAtPosition(RaceItemType.Lancer,12),"Leader gets only Coin +10/ordinary Rock; all position pools stay legal and Lancer stays rear-only");
            bool goldenReachable=true;for(int p=2;p<=12;p++){
                bool found=false;for(int i=0;i<1024;i++)found|=ItemDistributionTable.RollItem(p,rng)==RaceItemType.GoldenRock;
                goldenReachable&=found;
            }
            report.Check(goldenReachable&&!ItemDistributionTable.IsAllowedAtPosition(RaceItemType.GoldenRock,1)&&RaceItemUtility.LegacyFeatureFamily(16)==2&&
                (int)RaceItemType.Lancer==15&&(int)RaceItemType.ElectroSpike==3,"Golden Rock is reachable at P2-P12, excluded at P1, appends ID16 and retains existing ML family/serialized IDs");
        }
    }
}
#endif
