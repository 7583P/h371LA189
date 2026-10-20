using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public static class SeasonScaleBench
    {
        public static int Run(string output)
        {
            string run=Path.Combine(Path.GetFullPath(output),"scale-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(run);
            long start=new DateTime(2026,10,18,0,0,0,DateTimeKind.Utc).Ticks;
            var timer=Stopwatch.StartNew();var s=LocalLeagueEngine.Create(start,4209,100000);s.expandedCommunityPresence=true;s.utcOffsetMinutes=-300;
            double createMs=timer.Elapsed.TotalMilliseconds;var c=new CompetitiveCoordinator(s,new LocalLeagueConfiguration());s.lastKnownUtcTimestamp+=TimeSpan.TicksPerDay;c.RunToCompletion(null);
            var store=new LocalLeagueStore(Path.Combine(run,"save"),Json.To,Json.From<LocalLeagueState>);timer.Restart();store.Save(s.CopyForSave());double saveMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();var loaded=store.Load();double loadMs=timer.Elapsed.TotalMilliseconds;
            c=new CompetitiveCoordinator(loaded,new LocalLeagueConfiguration());loaded.lastKnownUtcTimestamp+=30*TimeSpan.TicksPerHour;
            var steps=new List<double>();timer.Restart();
            while(true){var step=Stopwatch.StartNew();var result=c.Step(256);steps.Add(step.Elapsed.TotalMilliseconds);if(result==CatchupStatus.CaughtUp)break;}
            double catchupMs=timer.Elapsed.TotalMilliseconds;bool noHuman=loaded.human.races==0;bool valid=c.Engine.Simulator.ValidateInvariants(out string failure);
            var match=new List<double>();var gaps=new List<double>();
            // This high-tier query fixture measures honest scarcity. It is never
            // saved or included in any population/rating-distribution report.
            var matchmaking=loaded.CopyForSave();matchmaking.human.placementGames=3;matchmaking.human.currentMmr=10000;
            var matches=new CompetitiveCoordinator(matchmaking,new LocalLeagueConfiguration());
            int widened=0;
            for(int i=0;i<128;i++){
                var w=Stopwatch.StartNew();var p=matches.BeginRankedRace(4207+i,matchmaking.lastKnownUtcTimestamp);match.Add(w.Elapsed.TotalMilliseconds);
                if(p==null)throw new InvalidOperationException("100k matchmaking unavailable");
                if(matches.Engine.LastMatchmaking.widened)widened++;
                gaps.AddRange(p.botIndices.Select(index=>Math.Abs(10000.0-matchmaking.bots[index].currentMmr)));
                matches.CancelReservation(p.raceId,matchmaking.lastKnownUtcTimestamp);
            }
            // Storage/latency fixture: identical observed records across forty
            // labeled fixture seasons. This is not manufactured game history.
            var history=new SeasonHistoryStore(Path.Combine(run,"STORAGE_FIXTURE_NOT_REAL_HISTORY"));
            var fixture=loaded.CopyForSave();var indexOrder=new LeagueRankIndex(fixture,true);
            var archiveSamples=new List<object>();timer.Restart();
            for(int season=1;season<=40;season++){
                fixture.currentSeasonNumber=season;fixture.seasonStartTimestamp=GlobalSeasonCalendar.Start(season);fixture.seasonEndTimestamp=GlobalSeasonCalendar.End(season);
                fixture.lastKnownUtcTimestamp=fixture.seasonEndTimestamp;fixture.lastLeagueSimulationUtc=fixture.seasonEndTimestamp-1;
                history.Capture(fixture,indexOrder);
                if(season==1||season==10||season==20||season==40)archiveSamples.Add(new{seasons=season,actualBytes=history.StorageUsage().activeBytes,elapsedMs=timer.Elapsed.TotalMilliseconds,managedBytes=GC.GetTotalMemory(true)});
            }
            timer.Restart();using(var reader=history.Open(1)){for(int i=0;i<100;i++)reader.Page(i*997%99900,14);}double pagesMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();string publicExport=Path.Combine(run,"season-fixture-1.zip");history.Export(1,publicExport);double exportMs=timer.Elapsed.TotalMilliseconds;
            var report=new{classification="HOST_ONLY_100K_SCALE_AND_STORAGE_FIXTURES_NOT_ANDROID_VALIDATION",run,createMs,saveMs,loadMs,catchup30HoursMs=catchupMs,catchupStep256Ms=Stats.Describe(steps),
                matchmakingMs=Stats.Describe(match),highTierAbsoluteGap=Stats.Describe(gaps),highTierWidenedQueries=widened,archiveSamples,oneHundred14RowPagesMs=pagesMs,
                verifiedExportMs=exportMs,verifiedExportBytes=new FileInfo(publicExport).Length,managedBytes=GC.GetTotalMemory(true),workingSetBytes=Process.GetCurrentProcess().WorkingSet64,noHumanRaces=noHuman,invariants=valid?"none":failure};
            File.WriteAllText(Path.Combine(output,"scale-bench.json"),Json.Pretty(report));Console.WriteLine($"100k scale: catch-up30h={catchupMs:F0}ms, archives40={history.StorageUsage().activeBytes}B, export={exportMs:F0}ms, invariants={valid}");return valid&&noHuman?0:1;
        }
    }
}
