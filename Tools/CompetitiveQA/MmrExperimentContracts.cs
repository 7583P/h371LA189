using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;
using AnimeHillRacing.Racing3D.Experiments;
using AnimeHillRacing.Racing3D.ML;

namespace CompetitiveQA
{
    public static class MmrExperimentContracts
    {
        public static int Run(string output)
        {
            var pass=new List<string>();var fail=new List<string>();
            void Check(bool ok,string label){if(ok)pass.Add(label);else{fail.Add(label);Console.WriteLine("FAIL "+label);}}
            var unified=AfterScale.Unified();int[] ratings={2999,3000,3099,3100,3199,3200,3999,4000,5000,10000,100000};int[] levels={-1,0,0,1,1,2,9,10,20,70,970};
            string[] offArgs={"--racing-ml-mode","Off"};
            var forced=RacingMlExecutionPolicy.QaOverride(true,"HillAdventurer.BlacklineQA",offArgs);
            Check(forced==MlExecutionMode.Off&&RacingMlExecutionPolicy.Effective(MlExecutionMode.Shadow,forced)==MlExecutionMode.Off,"Explicit QA Off survives later spawner default Shadow");
            Check(RacingMlExecutionPolicy.QaOverride(false,"HillAdventurer.BlacklineQA",offArgs)==null&&RacingMlExecutionPolicy.QaOverride(true,"Production",offArgs)==null,"QA command line cannot override a production player or another company");
            Check(RacingMlExecutionPolicy.QaOverride(true,"HillAdventurer.BlacklineQA",new[]{"--racing-ml-mode","99"})==null&&RacingMlExecutionPolicy.QaOverride(true,"HillAdventurer.BlacklineQA",new[]{"--racing-ml-mode"})==null,"Undefined/missing ML modes cannot establish an override");
            foreach(var mode in new[]{MlExecutionMode.Shadow,MlExecutionMode.Assist,MlExecutionMode.Authority})Check(RacingMlExecutionPolicy.Effective(mode,mode)==MlExecutionMode.Shadow,"Unapproved ML mode retains Shadow authority gate: "+mode);
            for(int i=0;i<ratings.Length;i++){bool exists=unified.TryLevel(ratings[i],out int level);Check(exists==(levels[i]>=0)&&level==levels[i],"Optional AFTER boundary "+ratings[i]+" => "+levels[i]);}
            for(int mmr=0;mmr<=100000;mmr+=100)Check(AfterScale.HillCompatible().TryLevel(mmr,out int level)==(mmr>10000)&&(mmr<=10000||level==MmrTierUtility.GetBeyondCounter(mmr)),"Hill compatibility "+mmr);
            foreach(string policy in new[]{"existing","threshold-2500-85","threshold-1000-95"}){
                var config=MmrEconomyLab.Configuration(policy);bool ordered=true;int prior=-1;
                for(int mmr=0;mmr<=CompetitiveRating.MaximumMmr;mmr++){
                    int carry=config.ResetMmr(mmr);if(carry<0||carry>mmr||carry<prior||(mmr>10000&&carry>10000)){ordered=false;break;}prior=carry;
                }
                Check(ordered,"Every integer rating through 999999 has monotone downward reset: "+policy);
            }
            var participants=new RatingParticipant[12];var outcomes=new RatingOutcome[12];var scratch=new double[12];var rules=new LocalLeagueConfiguration();
            for(int i=0;i<12;i++)participants[i]=new RatingParticipant{ranked=true,placementGames=3,rating=3000,position=i+1,uncertainty=.2f};
            CompetitiveRating.Settle(participants,rules,outcomes,scratch);int fair=outcomes[0].delta;
            participants[0].rating=6000;CompetitiveRating.Settle(participants,rules,outcomes,scratch);
            Check(outcomes[0].delta<fair,"Beating weaker opponents awards less than an equal-rating win");
            participants[0].position=12;participants[11].position=1;CompetitiveRating.Settle(participants,rules,outcomes,scratch);
            Check(outcomes[0].delta<0&&outcomes.Sum(x=>x.delta-x.floorInjection+x.ceilingRemoval)==0,"Loss to weaker rivals is penalized and reconciles exactly");
            for(int i=0;i<12;i++){participants[i].rating=3000;participants[i].position=1;}CompetitiveRating.Settle(participants,rules,outcomes,scratch);
            Check(outcomes.All(x=>x.delta==0),"A twelve-way tie at equal rating creates no points");
            for(int i=0;i<12;i++){participants[i].position=i+1;participants[i].outcome=ParticipantOutcome.Dnf;}CompetitiveRating.Settle(participants,rules,outcomes,scratch);
            Check(outcomes.Sum(x=>x.delta-x.floorInjection+x.ceilingRemoval)==0,"DNF distance order conserves rating without a participation bonus");

            long now=GlobalSeasonCalendar.EpochTicks+TimeSpan.TicksPerDay;
            var state=LocalLeagueEngine.Create(now,4210,360);var coordinator=new CompetitiveCoordinator(state,rules);state.lastKnownUtcTimestamp+=TimeSpan.TicksPerHour*6;coordinator.RunToCompletion(null);
            var journal=AfterExperimentJournal.Create(CompetitiveCoordinator.HumanId,1,unified);RaceSettlementRecord last=null;
            for(int race=0;race<3;race++){
                var pending=coordinator.BeginRankedRace(4207+race,state.lastKnownUtcTimestamp);coordinator.MarkStarted(pending.raceId,state.lastKnownUtcTimestamp);
                var results=new PhysicalRacerResult[12];results[0]=new PhysicalRacerResult{racerId=state.human.id,isHuman=true,finished=true,position=1,bestLap=66};
                for(int i=1;i<12;i++)results[i]=new PhysicalRacerResult{racerId=LeagueBotCatalog.Id(state.populationSeed,pending.botIndices[i-1]),finished=true,position=i+1,bestLap=70};
                last=coordinator.SettlePhysicalRace(pending.raceId,results,state.lastKnownUtcTimestamp+TimeSpan.TicksPerMinute);
                Check(journal.Observe(last,state.human.qualifiedSeason),"Real coordinator receipt observed: career race "+(race+1));
                Check((journal.peakQualifiedMmr>=0)==(race==2),"Only career-ranked qualified settlement can record prestige: "+(race+1));
            }
            Check(!journal.Observe(last,1),"Duplicate raceId cannot change current or historical prestige");
            int current=journal.currentMmr,peak=journal.peakQualifiedMmr;
            var malformed=last.Copy();malformed.raceId+="-duplicate-node";malformed.participantIds[1]=malformed.participantIds[0];
            Check(!journal.Observe(malformed,1)&&journal.currentMmr==current,"Duplicate participants cannot award a title");
            malformed=last.Copy();malformed.raceId+="-partial";malformed.partial=true;malformed.abandonment=true;
            Check(journal.Observe(malformed,1)&&journal.peakQualifiedMmr==peak,"Settled abandonment updates current rating without awarding a prestige peak");
            malformed=last.Copy();malformed.raceId+="-wrong-season";malformed.seasonId=2;Check(!journal.Observe(malformed,2),"Stale/future season receipt cannot rewrite history");
            malformed=last.Copy();malformed.raceId+="-cancelled";malformed.state=(byte)RaceSettlementState.Cancelled;Check(!journal.Observe(malformed,1),"Cancelled state is never treated as settled");
            malformed=last.Copy();malformed.raceId+="-sim-human";malformed.origin=(byte)RaceOrigin.Simulated;Check(!journal.Observe(malformed,1),"Statistical bot race cannot generate a human title");
            // Synthetic receipt mutations below exercise boundaries only. They
            // are never submitted to the coordinator, saves or runtime rating.
            var fixture=last.Copy();fixture.raceId+="-boundary-fixture";fixture.finalMmr[0]=4000;
            Check(journal.Observe(fixture,1)&&journal.CurrentLevel(out int top)&&top==10,"Qualified synthetic boundary fixture renders AFTER 10");
            fixture=fixture.Copy();fixture.raceId+="-loss";fixture.finalMmr[0]=2999;fixture.partial=true;fixture.abandonment=true;
            Check(journal.Observe(fixture,1)&&!journal.CurrentLevel(out _)&&journal.peakQualifiedMmr==4000,"Current diamond disappears after a loss while legitimate observed peak remains");
            var title=journal.CloseSeason();Check(title!=null&&title.Title=="Season 1 AFTER 10"&&journal.CloseSeason().level==10&&journal.titles.Count==1,"Season title is frozen and closing is idempotent");
            string path=Path.Combine(Path.GetFullPath(output),"experimental-after-only.json");File.WriteAllText(path,Json.Pretty(journal));
            var loaded=Json.From<AfterExperimentJournal>(File.ReadAllText(path));loaded.Validate();
            loaded.BeginSeason(2,2500,AfterScale.HillCompatible());Check(loaded.titles[0].Title=="Season 1 AFTER 10"&&!loaded.CurrentLevel(out _)&&loaded.peakQualifiedMmr==-1,"Export/reload and a new scale retain original title without recalculation");
            var pendingReceipt=last.Copy();pendingReceipt.raceId+="-new-season-fixture";pendingReceipt.seasonId=2;pendingReceipt.createdUtc=GlobalSeasonCalendar.Start(2)+1;pendingReceipt.settledUtc=pendingReceipt.createdUtc+1;pendingReceipt.finalMmr[0]=20000;
            Check(loaded.Observe(pendingReceipt,0)&&loaded.peakQualifiedMmr==-1&&!loaded.CurrentLevel(out _),"Unqualified carry/import cannot manufacture a historical peak");
            Check(loaded.CloseSeason()==null&&loaded.titles.Count==1,"A season with no qualified settlement grants no title");
            Check(state.human.currentMmr==current&&state.human.seasonPeakMmr==peak&&state.human.history.Count==0,"Prestige experiments never write the real player, rating, rank or season archive");
            var engine=coordinator.Engine;var original=engine.View(0);int capability=original.drivingRating;
            int savedMmr=state.bots[0].currentMmr;state.bots[0].currentMmr=100000;
            Check(engine.View(0).drivingRating==capability&&engine.View(0).currentMmr==100000,"Visible rating changes cannot change persisted driving capability");
            state.bots[0].currentMmr=savedMmr;
            var missing=new SeasonHistoryStore(Path.Combine(output,"legacy-history-"+Guid.NewGuid().ToString("N")));
            Check(missing.MarkUnavailable(1,"legacy final standings were not captured")&&!missing.MarkUnavailable(1,"must not overwrite"),"Legacy missing standings get an immutable explicit unavailable marker");
            Check(missing.List().Count==1&&missing.List()[0].unavailableReason.Contains("NOT AVAILABLE"),"Unavailable historical seasons remain visible without inventing standings");
            File.WriteAllText(Path.Combine(output,"mmr-experiment-contracts.json"),Json.Pretty(new{passed=pass.Count,failed=fail.Count,checks=pass,failures=fail,experimentalOnly=true,activated=false}));
            Console.WriteLine($"MMR experimental contracts: {pass.Count} PASS, {fail.Count} FAIL; no production activation.");return fail.Count==0?0:1;
        }
    }
}
