using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public static class ReleasePopulationBench
    {
        // Run each population in a separate host process. Never a real profile,
        // migration request, Android prediction or 100k production activation.
        public static int Run(string output,int population)
        {
            if(population!=10000&&population!=100000)throw new ArgumentOutOfRangeException(nameof(population));
            string root=Path.Combine(Path.GetFullPath(output),"population-"+population+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;long heapBefore=GC.GetTotalMemory(true);
            var timer=Stopwatch.StartNew();long now=new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc).Ticks;
            var state=LocalLeagueEngine.Create(now,4209,population);state.expandedCommunityPresence=population==100000;
            var coordinator=new CompetitiveCoordinator(state,new LocalLeagueConfiguration());double createMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();state.lastKnownUtcTimestamp+=TimeSpan.TicksPerDay;coordinator.RunToCompletion(null);
            double dayMs=timer.Elapsed.TotalMilliseconds;long races=coordinator.Engine.Simulator.CompletedRaces;
            long activeHeap=GC.GetTotalMemory(true)-heapBefore;
            bool invariant=coordinator.Engine.Simulator.ValidateInvariants(out string failure);
            long supply=state.human.currentMmr+state.bots.Sum(b=>(long)b.currentMmr);
            bool budget=state.ledger.budget.Expected(0)==supply;
            var store=new LocalLeagueStore(Path.Combine(root,"isolated-save"),Json.To,Json.From<LocalLeagueState>);
            timer.Restart();bool saved=store.Save(state.CopyForSave());double saveMs=timer.Elapsed.TotalMilliseconds;
            timer.Restart();var loaded=store.Load();double loadMs=timer.Elapsed.TotalMilliseconds;
            bool same=loaded!=null&&loaded.bots.Select(b=>b.currentMmr).SequenceEqual(state.bots.Select(b=>b.currentMmr))&&loaded.botNames.SequenceEqual(state.botNames);
            long bytes=Directory.GetFiles(Path.Combine(root,"isolated-save"),"*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length);
            var report=new{classification="ISOLATED_HOST_1_DAY_ACTUAL_SCHEDULER_NOT_ANDROID_OR_LONG_TERM_DISTRIBUTION",population,seed=4209,
                expandedSyntheticPresence=state.expandedCommunityPresence,createMs,oneDaySchedulerMs=dayMs,actualSettledRaces=races,saveMs,loadMs,
                managedHeapIncreaseBytes=activeHeap,workingSetBytes=process.WorkingSet64,cpuMs=(process.TotalProcessorTime-cpu).TotalMilliseconds,
                journalAndArchiveBytes=bytes,bytesPerCompactBot=LocalLeagueStore.BytesPerBot,invariants=invariant?"none":failure,budgetReconciled=budget,
                totalSupply=supply,humanRaces=state.human.races,reloadIdentical=same,saved,productionActivation=false,isolatedData=root};
            File.WriteAllText(Path.Combine(output,"population-"+population+".json"),Json.Pretty(report));
            Console.WriteLine($"Population {population}: races={races}, create={createMs:F0}ms, day={dayMs:F0}ms, save={saveMs:F0}ms, load={loadMs:F0}ms, bytes={bytes}, invariant={invariant}, budget={budget}");
            return invariant&&budget&&saved&&same&&state.human.races==0?0:1;
        }
    }
}
