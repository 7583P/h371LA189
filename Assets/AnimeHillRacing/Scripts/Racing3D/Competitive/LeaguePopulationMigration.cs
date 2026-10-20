using System;
using System.IO;

namespace AnimeHillRacing.Racing3D
{
    // Opt-in only. The production 10k default remains the measured release
    // control until real Android budgets authorize deploying this profile.
    public static class LeaguePopulationMigration
    {
        public const int TargetPopulation = 100000;
        public const string Id = "CPPS-POPULATION-100000-1";
        public static LocalLeagueState Expand(LocalLeagueStore store, LocalLeagueState source, LocalLeagueConfiguration config,Action<string> checkpoint=null)
        {
            string invalid = LocalLeagueStore.Validate(source);
            if (invalid != null) throw new InvalidDataException("Migration source is invalid: " + invalid);
            if (source.bots.Length == TargetPopulation && source.ledger.HasMigration(Id)) return source;
            if (source.bots.Length != LocalLeagueEngine.BotCount || source.seasonTransitionCursor >= 0 || source.pendingHumanRace != null)
                throw new InvalidOperationException("Expand a stable 10k league between human races and season transitions.");
            if(!store.Save(source.CopyForSave()))throw new IOException("Commit the 10k source before migration.");
            byte[] baseline = store.ReadNewestJournalBytes();
            if (baseline == null) throw new InvalidOperationException("Commit the 10k source before migration.");
            var saved = store.ParseJournalBytes(baseline, out _, out _);
            if (saved == null || saved.population != source.population || saved.profileId != source.profileId)
                throw new InvalidDataException("Migration requires a verified durable source.");
            string backup = Path.Combine(store.Directory, "population-10000-recovery.journal");
            if (!File.Exists(backup)) SeasonHistoryStore.DurableNew(backup, baseline);
            else if (store.ParseJournalBytes(File.ReadAllBytes(backup), out _, out _)?.profileId != source.profileId)
                throw new IOException("An unrelated migration backup was preserved.");
            checkpoint?.Invoke("backup-verified");

            var candidate = source.CopyForSave();
            // The deterministic born league supplies hidden capabilities only.
            // All 90k newcomers have zero results and 0/3 initial placement.
            var born = LocalLeagueEngine.Create(source.lastLeagueSimulationUtc, source.populationSeed, TargetPopulation, config);
            Array.Copy(source.bots, born.bots, source.bots.Length);
            if (source.botNames != null) Array.Copy(source.botNames, born.botNames, source.botNames.Length);
            else for(int i=0;i<source.bots.Length;i++)born.botNames[i]=LeagueBotCatalog.Name(source,i);
            var names = new BotNameGenerator();
            for (int i = 0; i < source.bots.Length; i++) names.Reserve(LeagueBotCatalog.Name(source, i));
            foreach (var alias in source.botAliases) names.Reserve(alias.displayName);
            for (int i = source.bots.Length; i < TargetPopulation; i++)
            {
                born.botNames[i] = names.Generate(LeagueSimulator.Mix(source.populationSeed, i, 0x4e414d45), LocalLeagueEngine.SkillQuantile(born.bots[i]), out var style);
                born.bots[i].nameStyle = (byte)style;
            }
            candidate.bots = born.bots; candidate.botNames = born.botNames; candidate.population = TargetPopulation;
            candidate.expandedCommunityPresence = true;
            candidate.ledger.appliedMigrations.Add(Id);
            // Do not reseed the original 10k: preserve all pending bot races,
            // presence, RNG/event cursors, cosmetics and identities byte for byte.
            var scheduler = new LocalLeagueEngine(candidate, config).Simulator;
            scheduler.SeedAdditionalPresence(source.bots.Length, candidate.lastLeagueSimulationUtc);
            if (!store.Save(candidate)) throw new IOException("Expanded journal could not be committed.");
            checkpoint?.Invoke("expanded-journal-committed");
            var reloaded = store.Load();
            if (reloaded == null || reloaded.population != TargetPopulation || !reloaded.ledger.HasMigration(Id)) throw new IOException("Expanded journal did not survive durable reload.");
            return reloaded;
        }
    }
}
