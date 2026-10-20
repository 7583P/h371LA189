using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using AnimeHillRacing.Racing3D.Experiments;

namespace CompetitiveQA
{
    // Runs the production 12-bot scheduler, outcome model, rating and quarterly
    // transition against isolated fixtures. This is statistical evidence, not
    // a replacement for physical Unity races or an activation/migration tool.
    public static class MmrEconomyLab
    {
        sealed class Observer : ILeagueSimulationObserver
        {
            public long races, invalidLobbies, invalidConservation, rated, promotions, demotions, positive, negative;
            public long absoluteDelta, finishDelta;
            public void HumanReserved(long ticks,int[] bots) { throw new InvalidOperationException("Statistical lab cannot simulate a human."); }
            public void RaceCompleted(long ticks,long sequence,int[] nodes,RatingParticipant[] before,RatingOutcome[] after,float[] seconds)
            {
                races++;
                if(nodes.Length!=12){invalidLobbies++;return;}
                long reconciled=0;
                for(int k=0;k<12;k++){
                    for(int j=0;j<k;j++)if(nodes[k]==nodes[j])invalidLobbies++;
                    if(!before[k].ranked)continue;
                    rated++;var a=after[k];reconciled+=a.delta-a.floorInjection+a.ceilingRemoval;
                    absoluteDelta+=Math.Abs(a.delta);if(a.delta>0)positive++;else if(a.delta<0)negative++;
                    if(before[k].outcome==ParticipantOutcome.Finished)finishDelta+=a.delta;
                    int old=(int)MmrTierUtility.GetTier(before[k].rating),next=(int)MmrTierUtility.GetTier(a.newRating);
                    if(next>old)promotions++;else if(next<old)demotions++;
                }
                if(reconciled!=0)invalidConservation++;
            }
        }
        public static LocalLeagueConfiguration Configuration(string policy)
        {
            var config=new LocalLeagueConfiguration{maximumCatchupDays=366};
            if(policy=="existing")return config;
            int threshold;double factor;
            if(policy=="threshold-2500-85"){threshold=2500;factor=.85;}
            else if(policy=="threshold-1000-95"){threshold=1000;factor=.95;}
            else throw new ArgumentException("Unknown reset experiment: "+policy);
            // Dense samples preserve the exact linear ratio except rounding at
            // the threshold/cap. The production monotone Hermite interpolator
            // and the existing 10k post-curve ceiling are exercised unchanged.
            var knots=new List<SoftResetPoint>();
            for(int x=0;x<=10000;x+=100)knots.Add(new SoftResetPoint(x,x<=threshold?x:(int)Math.Round(threshold+(x-threshold)*factor,MidpointRounding.AwayFromZero)));
            knots.Add(new SoftResetPoint(CompetitiveRating.MaximumMmr,10000));
            config.resetCurve=knots.ToArray();return config;
        }
        static void Capability(ref CompactLeagueBot b,float skill)
        {
            b.latent=CompactLeagueBot.Pack01(skill);b.learning=0;
            b.consistency=CompactLeagueBot.Pack01(.5f+.48f*skill);
            byte v=CompactLeagueBot.PackByte(skill);b.drift=b.aerial=b.shortcut=b.item=b.racecraft=b.recovery=b.boost=b.adaptation=v;
        }
        static long InitializeFixture(LocalLeagueState s,string scenario)
        {
            long initial=0;
            if(scenario=="organic")return 0;
            if(scenario!="elite"&&scenario!="polarized"&&scenario!="mobility")throw new ArgumentException("Unknown skill fixture.");
            for(int i=0;i<s.bots.Length;i++){
                ref var b=ref s.bots[i];float skill=b.LatentSkill;
                int mmr=scenario=="elite"?8000+(i*37%3001):scenario=="polarized"?(i%2==0?500:9000):(int)(500+skill*8500);
                if(scenario=="polarized")Capability(ref b,i%2==0?.12f:.95f);
                b.placementGames=3;b.currentMmr=b.seasonStartingMmr=b.seasonPeakMmr=b.peakMmr=mmr;b.uncertainty=CompactLeagueBot.Pack01(.3f);initial+=mmr;
            }
            if(scenario=="mobility"){
                initial-=s.bots[0].currentMmr+s.bots[1].currentMmr;
                Capability(ref s.bots[0],.08f);Capability(ref s.bots[1],.98f);
                s.bots[0].currentMmr=s.bots[0].seasonStartingMmr=s.bots[0].seasonPeakMmr=s.bots[0].peakMmr=500;
                s.bots[1].currentMmr=s.bots[1].seasonStartingMmr=s.bots[1].seasonPeakMmr=s.bots[1].peakMmr=9000;initial+=9500;
            }
            return initial;
        }
        static object Describe(IEnumerable<double> values)
        {
            var a=values.OrderBy(x=>x).ToArray();var summary=Stats.Describe(a);
            if(a.Length>0){summary["p10"]=Stats.Percentile(a,.1);summary["p50"]=Stats.Percentile(a,.5);summary["p90"]=Stats.Percentile(a,.9);}
            return summary;
        }
        public static int Run(string output,string[] args)
        {
            int seasons=args.Length>2?int.Parse(args[2]):24,population=args.Length>3?int.Parse(args[3]):360,seed=args.Length>4?int.Parse(args[4]):4207;
            string scenario=args.Length>5?args[5]:"organic",policy=args.Length>6?args[6]:"existing";
            if(seasons<1||seasons>24||population<24||population>100000)throw new ArgumentOutOfRangeException("Bounded QA seasons/population required.");
            string label=$"{scenario}-{policy}-{population}-{seasons}s-{seed}",root=Path.Combine(Path.GetFullPath(output),label);
            Directory.CreateDirectory(root);if(File.Exists(Path.Combine(root,"report.json")))throw new IOException("Preserve completed lab evidence; choose a new output root.");
            var config=Configuration(policy);long start=GlobalSeasonCalendar.EpochTicks;
            var s=LocalLeagueEngine.Create(start,seed,population,config);s.utcOffsetMinutes=-300;
            long initial=InitializeFixture(s,scenario);var observer=new Observer();var c=new CompetitiveCoordinator(s,config);c.Engine.Simulator.Observer=observer;
            if(scenario=="mobility"){
                ref var inactive=ref s.bots[2];inactive.presence=(byte)BotPresence.Offline;inactive.activeRace=LeagueSimulator.NoRace;inactive.nextEventTicks=long.MaxValue;inactive.sessionRacesLeft=0;
                c.Engine.Simulator.Rebuild();
            }
            var store=new LocalLeagueStore(Path.Combine(root,"isolated-save"),Json.To,Json.From<LocalLeagueState>);
            var rows=new List<object>();var timer=Stopwatch.StartNew();var failures=new List<string>();
            TimeSpan cpuStart=Process.GetCurrentProcess().TotalProcessorTime;long peakManagedHeap=GC.GetTotalMemory(false);
            int[] first=s.bots.Select(b=>b.currentMmr).ToArray();double[] originalSkill=s.bots.Select(b=>b.DevelopedSkill).ToArray();
            long lastRaces=0,lastRated=0,lastAbs=0,lastFloor=0,lastReset=0,lastIssue=0;int intervention=0;
            for(int quarter=1;quarter<=seasons;quarter++){
                if(scenario=="mobility"&&quarter==2){Capability(ref s.bots[0],.99f);Capability(ref s.bots[1],.03f);intervention=quarter;}
                int[] before=s.bots.Select(b=>b.currentMmr).ToArray();var skillsBefore=s.bots.Select(b=>new {b.latent,b.learning,b.consistency,b.drift,b.shortcut,b.recovery}).ToArray();
                s.lastKnownUtcTimestamp=s.seasonEndTimestamp-1;c.RunToCompletion(null);
                if(!c.Engine.Simulator.ValidateInvariants(out string invariant))failures.Add("Q"+quarter+": "+invariant);
                long supply=s.human.currentMmr+s.bots.Sum(b=>(long)b.currentMmr),expected=s.ledger.budget.Expected(initial);
                if(supply!=expected)failures.Add("Q"+quarter+": supply mismatch "+supply+" != "+expected);
                int[] final=s.bots.Select(b=>b.currentMmr).ToArray();var mmr=final.Select(x=>(double)x).ToArray();
                bool[] qualified=s.bots.Select(b=>b.Ranked&&(b.flags&SeasonQualification.QualifiedFlag)!=0).ToArray();
                double[] skill=s.bots.Select(b=>b.DevelopedSkill).ToArray(),volume=s.bots.Select(b=>(double)b.races).ToArray();
                var budget=s.ledger.budget;long races=observer.races-lastRaces,rated=observer.rated-lastRated;
                int gainNode=Enumerable.Range(0,population).OrderByDescending(i=>final[i]-before[i]).First(),lossNode=Enumerable.Range(0,population).OrderBy(i=>final[i]-before[i]).First();
                long boundary=s.seasonEndTimestamp;var nodesBefore=s.bots.Select(b=>b.id).ToArray();
                int unified=s.bots.Count(b=>b.Ranked&&(b.flags&SeasonQualification.QualifiedFlag)!=0&&AfterScale.Unified().TryLevel(b.currentMmr,out _));
                int hill=s.bots.Count(b=>b.Ranked&&(b.flags&SeasonQualification.QualifiedFlag)!=0&&AfterScale.HillCompatible().TryLevel(b.currentMmr,out _));
                var preResetBudget=new {floor=budget.floorInjection-lastFloor,placement=budget.placementIssuance-lastIssue,resetFromPrior=budget.softResetRemoval-lastReset};
                s.lastKnownUtcTimestamp=boundary;c.RunToCompletion(()=>{if(!store.Save(s))throw new IOException("Lab checkpoint save failed.");});
                if(!s.bots.Select(b=>b.id).SequenceEqual(nodesBefore)||s.human.placementGames!=0)failures.Add("Reset altered identity or initial human placement.");
                for(int i=0;i<population;i++){
                    var b=s.bots[i];var old=skillsBefore[i];
                    if(b.currentMmr>final[i]||b.currentMmr!=config.ResetMmr(final[i])){failures.Add("Reset increased or mismatched a rating.");break;}
                    if(b.latent!=old.latent||b.learning!=old.learning||b.consistency!=old.consistency||b.drift!=old.drift||b.shortcut!=old.shortcut||b.recovery!=old.recovery){failures.Add("Reset altered capability.");break;}
                }
                rows.Add(new {quarter,globalSeason=s.currentSeasonNumber-1,population,completedRaces=races,totalRaces=observer.races,rating=Describe(mmr),totalSupply=supply,initialSupply=initial,
                    supplyBeforeResetReconciled=supply==expected,supplyAfterReset=s.bots.Sum(b=>(long)b.currentMmr)+s.human.currentMmr,resetRemoved=s.ledger.seasonReceipts.Last().ratingBefore-s.ledger.seasonReceipts.Last().ratingAfter,
                    supplyFlows=preResetBudget,meanAbsoluteMmrDeltaPerRatedStart=rated>0?(observer.absoluteDelta-lastAbs)/(double)rated:0,qualifiedUnifiedAfter=unified,qualifiedHillBeyond=hill,
                    skillRatingSpearman=Stats.Spearman(skill,mmr),volumeRatingSpearman=Stats.Spearman(volume,mmr),
                    promotions=observer.promotions,demotions=observer.demotions,positiveRatedStarts=observer.positive,negativeRatedStarts=observer.negative,
                    skillBands=Enumerable.Range(0,5).Select(q=>new {minimum=q*.22,maximum=(q+1)*.22,mmr=Describe(Enumerable.Range(0,population).Where(i=>skill[i]>=q*.22&&skill[i]<(q+1)*.22).Select(i=>(double)final[i]))}).ToArray(),
                    mostImproved=new {node=gainNode,id=LeagueBotCatalog.Id(seed,gainNode),delta=final[gainNode]-before[gainNode],start=before[gainNode],end=final[gainNode]},
                    largestFall=new {node=lossNode,id=LeagueBotCatalog.Id(seed,lossNode),delta=final[lossNode]-before[lossNode],start=before[lossNode],end=final[lossNode]},
                    tracked=Enumerable.Range(0,3).Select(i=>new {node=i,initialMmr=first[i],preResetMmr=final[i],carry=s.bots[i].currentMmr,mmrFromInitial=final[i]-first[i],originalSkill=originalSkill[i],currentSkill=skill[i],s.bots[i].races,qualified=qualified[i]}).ToArray(),
                    elapsedMilliseconds=timer.Elapsed.TotalMilliseconds,managedHeapBytes=GC.GetTotalMemory(false)});
                lastRaces=observer.races;lastRated=observer.rated;lastAbs=observer.absoluteDelta;lastFloor=budget.floorInjection;lastIssue=budget.placementIssuance;lastReset=budget.softResetRemoval;
                peakManagedHeap=Math.Max(peakManagedHeap,GC.GetTotalMemory(false));
                if(quarter==4||quarter==12||quarter==24)Console.WriteLine($"{label} Q{quarter}: {observer.races} races, mean={mmr.Average():F1}, supply={supply}, elapsed={timer.ElapsedMilliseconds}ms");
            }
            if(observer.invalidLobbies!=0||observer.invalidConservation!=0||s.skippedSimulatedTicks!=0)failures.Add("Invalid lobby, pairwise conservation or skipped scheduler time.");
            if(observer.races==0)failures.Add("Insufficient population/activity to form 12-racer lobbies; no economy conclusion is supported.");
            if(!store.Save(s))throw new IOException("Final lab checkpoint save failed.");
            var reloaded=new LocalLeagueStore(Path.Combine(root,"isolated-save"),Json.To,Json.From<LocalLeagueState>).Load();
            bool reload=reloaded!=null&&reloaded.bots.Select(b=>b.currentMmr).SequenceEqual(s.bots.Select(b=>b.currentMmr))&&reloaded.ledger.budget.Expected(initial)==s.ledger.budget.Expected(initial);
            if(!reload)failures.Add("Durable reload mismatch.");
            File.WriteAllText(Path.Combine(root,"report.json"),Json.Pretty(new {classification="ISOLATED_STATISTICAL_EXPERIMENT_NOT_PHYSICAL_RACE_OR_PRODUCTION_MIGRATION",activated=false,formula=CompetitiveRating.FormulaVersion,seed,population,seasons,scenario,resetPolicy=policy,resetKnots=config.resetCurve,
                initialSupply=initial,syntheticSeededRatingFixture=scenario!="organic",capabilityInterventionQuarter=intervention,totalRaces=observer.races,observer.invalidLobbies,observer.invalidConservation,reloadIdentical=reload,
                humanSimulatedRaces=s.human.races,wallMilliseconds=timer.Elapsed.TotalMilliseconds,cpuMilliseconds=(Process.GetCurrentProcess().TotalProcessorTime-cpuStart).TotalMilliseconds,peakSampledManagedHeapBytes=peakManagedHeap,workingSetBytes=Process.GetCurrentProcess().WorkingSet64,
                checkpoints=rows,finalBudget=s.ledger.budget,finalSupply=s.bots.Sum(b=>(long)b.currentMmr)+s.human.currentMmr,budgetReconciled=s.ledger.budget.Expected(initial)==s.bots.Sum(b=>(long)b.currentMmr)+s.human.currentMmr,failures}));
            Console.WriteLine(label+": "+(failures.Count==0?"PASS":"FAIL")+"; "+observer.races+" actual scheduler settlements; production unchanged.");return failures.Count==0?0:1;
        }
    }
}
