using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public static class ReleaseLookupContracts
    {
        public static int Run(string output)
        {
            string root=Path.Combine(Path.GetFullPath(output),"lookup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            int passed=0;var failures=new List<string>();var measurements=new List<object>();
            void Check(bool ok,string name){if(ok)passed++;else{failures.Add(name);Console.WriteLine("FAIL "+name);}}
            foreach(int population in new[]{10000,100000}) {
                long now=new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc).Ticks;
                var state=LocalLeagueEngine.Create(now,4207,population);var config=new LocalLeagueConfiguration();
                var coordinator=new CompetitiveCoordinator(state,config);state.lastKnownUtcTimestamp+=3*TimeSpan.TicksPerHour;coordinator.RunToCompletion(null);
                var pending=coordinator.BeginRankedRace(4208,state.lastKnownUtcTimestamp);
                Check(pending!=null&&pending.botIndices.Length==11,"Eleven real reserved opponents at "+population);
                if(pending==null)continue;
                string[] ids=pending.botIndices.Select(i=>LeagueBotCatalog.Id(state.populationSeed,i)).ToArray();
                string[] names=pending.botIndices.Select(i=>coordinator.Engine.DisplayName(i)).ToArray();
                long before=GC.GetTotalMemory(true);var watch=Stopwatch.StartNew();var lookup=new LeagueIdentityLookup(coordinator.Engine);
                double buildMs=watch.Elapsed.TotalMilliseconds;long allocated=GC.GetTotalMemory(true)-before;
                var latencies=new List<double>();
                void Search(LocalLeagueEngine engine,LeagueIdentityLookup index,string phase) {
                    for(int k=0;k<11;k++) {
                        watch.Restart();bool byId=index.TryLocate(ids[k],out int idRank,out int count);latencies.Add(watch.Elapsed.TotalMilliseconds);
                        Check(byId&&count==1&&engine.Index.AtRank(idRank)==pending.botIndices[k],phase+" actual reserved ID "+k);
                        Check(index.TryLocate(names[k].ToUpperInvariant(),out int nameRank,out int matches)&&matches>=1&&engine.DisplayName(engine.Index.AtRank(nameRank)).Equals(names[k],StringComparison.OrdinalIgnoreCase),phase+" actual reserved name "+k);
                    }
                }
                Search(coordinator.Engine,lookup,"before");
                Check(!lookup.TryLocate("missing-profile-do-not-create",out _,out _)&&state.bots.Length==population,"Missing names never manufacture a profile");
                coordinator.MarkStarted(pending.raceId,state.lastKnownUtcTimestamp);
                var results=new PhysicalRacerResult[12];results[0]=new PhysicalRacerResult{racerId=CompetitiveCoordinator.HumanId,isHuman=true,position=1,finished=true};
                for(int k=0;k<11;k++)results[k+1]=new PhysicalRacerResult{racerId=ids[k],position=k+2,finished=true};
                var receipt=coordinator.SettlePhysicalRace(pending.raceId,results,state.lastKnownUtcTimestamp+TimeSpan.TicksPerMinute);
                Check(receipt!=null&&receipt.participantIds.Length==12,"Settlement refers to the same twelve existing identities");
                Search(coordinator.Engine,lookup,"after settlement");
                var store=new LocalLeagueStore(Path.Combine(root,"save-"+population),Json.To,Json.From<LocalLeagueState>);
                Check(store.Save(state.CopyForSave()),"Durable isolated save "+population);
                var loaded=store.Load();var reloaded=new LocalLeagueEngine(loaded,config);var reloadedLookup=new LeagueIdentityLookup(reloaded);
                Search(reloaded,reloadedLookup,"after relaunch");
                Check(ids.All(id=>reloaded.TryIndex(id,out _))&&loaded.populationSeed==state.populationSeed,"No replacement profiles on relaunch");
                measurements.Add(new{population,indexBuildMs=buildMs,indexManagedBytes=allocated,idLookupMs=Stats.Describe(latencies),saveBytes=Directory.GetFiles(Path.Combine(root,"save-"+population),"*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length)});
            }
            var duplicates=LocalLeagueEngine.Create(new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc).Ticks,4209,24);
            duplicates.botNames[0]="Same existing name";duplicates.botNames[1]="Same existing name";
            duplicates.bots[0].placementGames=duplicates.bots[1].placementGames=3;duplicates.bots[0].currentMmr=2000;duplicates.bots[1].currentMmr=3000;
            var duplicateEngine=new LocalLeagueEngine(duplicates);var duplicateLookup=new LeagueIdentityLookup(duplicateEngine);
            Check(duplicateLookup.TryLocate("same existing name",out int best,out int duplicatesCount)&&duplicatesCount==2&&duplicateEngine.Index.AtRank(best)==1,"Duplicate-name fixture preserves both identities and returns their best standing");
            duplicates.bots[0].currentMmr=4000;duplicateEngine.Index.Update(0,4000);
            Check(duplicateLookup.TryLocate("Same existing name",out best,out duplicatesCount)&&duplicatesCount==2&&duplicateEngine.Index.AtRank(best)==0,"Duplicate-name lookup follows live rank changes");
            Check(!duplicateLookup.TryLocate("  ",out _,out _)&&!duplicateLookup.TryLocate(null,out _,out _),"Blank searches do not target an arbitrary bot");
            File.WriteAllText(Path.Combine(output,"release-lookup-contracts.json"),Json.Pretty(new{classification="ISOLATED_CORE_SEARCH_AND_SETTLEMENT_FIXTURES_NOT_PHYSICAL_RACES_OR_ANDROID",passed,failed=failures.Count,failures,measurements,isolatedData=root}));
            Console.WriteLine($"Release lookup: {passed} passed, {failures.Count} failed");return failures.Count==0?0:1;
        }
    }
}
