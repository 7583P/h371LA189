using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    public static class PhysicalLeagueReadback
    {
        public static int Run(string output,string saveDirectory,string metricsPath)
        {
            string workspace=Path.GetFullPath(Directory.GetCurrentDirectory())+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(saveDirectory).StartsWith(workspace,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Read only a QA save inside this workspace");
            string destination=Path.Combine(output,"physical-league-readback.json");if(File.Exists(destination))throw new IOException("Preserve readback evidence");
            using(var metrics=JsonDocument.Parse(File.ReadAllText(metricsPath))) {
                int seed=metrics.RootElement.GetProperty("seed").GetInt32();var racers=metrics.RootElement.GetProperty("racers").EnumerateArray().ToArray();
                var store=new LocalLeagueStore(saveDirectory,Json.To,Json.From<LocalLeagueState>);var state=store.Load();
                if(state==null||store.MigratedLegacy)throw new InvalidOperationException("Expected an existing current-format QA journal; no profile replacement is allowed");
                var engine=new LocalLeagueEngine(state);var lookup=new LeagueIdentityLookup(engine);
                var receipt=state.ledger.recentSettlements.LastOrDefault(r=>r.seed==seed&&r.origin==(byte)RaceOrigin.Physical&&r.participantIds.Contains(CompetitiveCoordinator.HumanId));
                if(receipt==null)throw new InvalidOperationException("No actual persisted physical-race receipt matches this seed");
                var rows=new List<object>();var failures=new List<string>();int bots=0;
                for(int k=0;k<receipt.participantIds.Length;k++) {
                    string id=receipt.participantIds[k];if(id==CompetitiveCoordinator.HumanId)continue;bots++;
                    bool exists=engine.TryIndex(id,out int index);string name=exists?engine.DisplayName(index):"MISSING";
                    var actual=racers.Where(r=>r.TryGetProperty("racerId",out var direct)?direct.GetString()==id:r.GetProperty("name").GetString()==name).ToArray();
                    bool byId=lookup.TryLocate(id,out int rank,out int idCount)&&idCount==1&&engine.Index.AtRank(rank)==index;
                    bool byName=lookup.TryLocate(name,out int nameRank,out int nameCount)&&nameCount>0&&engine.DisplayName(engine.Index.AtRank(nameRank)).Equals(name,StringComparison.OrdinalIgnoreCase);
                    bool physical=actual.Length==1&&actual[0].GetProperty("position").GetInt32()==receipt.positions[k]&&actual[0].GetProperty("finished").GetBoolean()==((ParticipantOutcome)receipt.outcomes[k]==ParticipantOutcome.Finished);
                    if(!exists||!byId||!byName||!physical)failures.Add("Existing identity/physical result/lookup mismatch: "+id);
                    rows.Add(new{id,name,exists,byId,byName,nameMatches=nameCount,uniquePhysicalRacer=actual.Length==1,physicalResultMatchesReceipt=physical,finalVisibleMmr=receipt.finalMmr[k],seasonQualified=exists&&SeasonQualification.IsOfficial(state,index)});
                }
                int human=Array.IndexOf(receipt.participantIds,CompetitiveCoordinator.HumanId);
                bool humanMatches=human>=0&&state.human.currentMmr==receipt.finalMmr[human];
                bool settled=receipt.state>=(byte)RaceSettlementState.RaceSettled&&receipt.state<=(byte)RaceSettlementState.ProfileSaved;
                if(bots!=11||receipt.participantIds.Distinct().Count()!=12||!humanMatches||!settled)failures.Add("Twelve identities and final human settlement were not preserved");
                File.WriteAllText(destination,Json.Pretty(new{classification="FRESH_PROCESS_READBACK_OF_REAL_UNITY_RANKED_QA_RACE_SCRIPTED_PLAYER_NOT_HUMAN_APPROVAL",seed,populationSeed=state.populationSeed,
                    population=state.bots.Length,profileId=state.profileId,raceId=receipt.raceId,receipt.state,bots,settled,humanMatchesReceipt=humanMatches,noProfilesCreated=true,noSaveWrites=true,
                    currentSeason=state.currentSeasonNumber,lookup=rows,failures,passed=failures.Count==0,sourceMetrics=metricsPath,sourceSave=saveDirectory}));
                Console.WriteLine($"Physical ranked readback: {bots} real opponents; failures={failures.Count}; human receipt={humanMatches}; no profile writes");return failures.Count==0?0:1;
            }
        }
    }
}
