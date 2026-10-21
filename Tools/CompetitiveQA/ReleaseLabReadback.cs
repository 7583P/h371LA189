using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public static class ReleaseLabReadback
    {
        public static int Run(string output,string labs)
        {
            string root=Path.GetFullPath(labs),workspace=Path.GetFullPath(Directory.GetCurrentDirectory())+Path.DirectorySeparatorChar;
            if(!root.StartsWith(workspace,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Only workspace statistical fixtures are allowed");
            string target=Path.Combine(output,"statistical-summary.json");if(File.Exists(target))throw new IOException("Preserve readback evidence");
            var cases=new List<object>();
            foreach(string path in Directory.GetFiles(root,"report.json",SearchOption.AllDirectories).OrderBy(p=>p)) {
                using(var document=JsonDocument.Parse(File.ReadAllText(path))) {
                    var report=document.RootElement;string directory=Path.Combine(Path.GetDirectoryName(path),"isolated-save");
                    var store=new LocalLeagueStore(directory,Json.To,Json.From<LocalLeagueState>);var state=store.Load();
                    if(state==null||store.MigratedLegacy)throw new InvalidOperationException("Expected an existing completed statistical journal");
                    long supply=state.bots.Sum(b=>(long)b.currentMmr)+state.human.currentMmr;
                    if(supply!=report.GetProperty("finalSupply").GetInt64()||state.human.races!=0)throw new InvalidOperationException("Readback differs from the reported bot-only lab");
                    var bands=Enum.GetValues<MmrTier>().Select(tier=>new {tier=MmrTierUtility.GetTierDisplayName(tier),
                        minimumMmr=MmrTierUtility.MinimumRating(tier),count=state.bots.Count(b=>MmrTierUtility.GetTier(b.currentMmr)==tier)}).ToArray();
                    if(bands.Sum(b=>b.count)!=state.bots.Length)throw new InvalidOperationException("Rating-band population mismatch");
                    cases.Add(new {source=path,scenario=report.GetProperty("scenario").GetString(),
                        syntheticSeededRatingFixture=report.GetProperty("syntheticSeededRatingFixture").GetBoolean(),
                        population=state.bots.Length,seed=state.populationSeed,currentSeason=state.currentSeasonNumber,
                        afterCompletedSeasons=report.GetProperty("seasons").GetInt32(),finalSupplyAfterReset=supply,
                        careerRanked=state.bots.Count(b=>b.Ranked),seasonQualified=Enumerable.Range(0,state.bots.Length).Count(i=>SeasonQualification.IsOfficial(state,i)),
                        carryRatingBandsAfterLastReset=bands,
                        checkpoints=report.GetProperty("checkpoints").Clone(),budget=report.GetProperty("finalBudget").Clone(),
                        noSaveWrites=true,noProductionActivation=true});
                }
            }
            if(cases.Count!=3)throw new InvalidOperationException("Expected the three measured labs");
            File.WriteAllText(target,Json.Pretty(new {classification="READBACK_OF_MEASURED_STATISTICAL_FIXTURES_NOT_PHYSICS_OR_100K_LONG_TERM_PROOF",
                limits="Carry-rating bands are after the fourth reset; season qualification is reported separately. Pre-reset percentiles and mobility remain in checkpoints. No synthetic fixture is described as organic.",cases}));
            Console.WriteLine($"Statistical readback: {cases.Count} existing labs; carry-rating bands reconciled; no new profiles or save requests");return 0;
        }
    }
}
