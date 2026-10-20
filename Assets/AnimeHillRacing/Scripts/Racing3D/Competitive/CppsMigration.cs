using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    public sealed class CppsMigrationReport
    {
        public bool applied;
        public string reason = "";
        public int botsReset, legacyArchivesRenumbered, humanPreviousMmr, destinationSeason;
        public long ratingRemoved;
    }

    // One deliberate inaugural transition to CPPS (Competitive Placement &
    // Persistence System). Applied in memory to a state loaded from a pre-CPPS
    // journal and committed by a single new v4 journal generation; the v3
    // slots are left untouched as recovery copies. Its id is recorded in the
    // ledger, so neither relaunches nor future updates can ever re-run it.
    //
    // Reset: competitive MMR -> 0, rank -> UNRANKED, Placement -> 0/3 for the
    // human and every local bot. Preserved: hidden skill and specialties,
    // identities, career counters, historical best MMR, titles, combos and all
    // non-league data (purchases, VIP, ad-free, vehicles, upgrades, currency,
    // settings, Adventure) which live outside this journal and are not read.
    public static class CppsMigration
    {
        public const string Id = LocalLeagueEngine.InauguralMigrationId;
        public const int LegacySeasonOffset = -1000;

        public static string LegacyTitle(string id)
        {
            if (id == null || !id.StartsWith("season:", StringComparison.Ordinal)) return id;
            var parts = id.Split(':');
            return parts.Length == 3 && int.TryParse(parts[1], out int n) && n > 0 ? "legacy-season:" + n + ":" + parts[2] : id;
        }

        public static bool AlreadySatisfied(LocalLeagueState s)
        {
            if (s.competitiveContract >= LocalLeagueState.CompetitiveContractCpps) return true;
            foreach (var m in s.ledger.appliedMigrations) if (m.StartsWith(Id, StringComparison.Ordinal)) return true;
            return false;
        }

        // loadArchiveRecords: materializes an already-saved legacy archive so it
        // can be rewritten under its renumbered legacy season id.
        public static CppsMigrationReport Apply(LocalLeagueState s, LocalLeagueConfiguration c, long nowUtc, Func<LeagueSeasonArchive, CompactBotSeason[]> loadArchiveRecords, LeagueCompetitor watermarkHuman = null)
        {
            var report = new CppsMigrationReport();
            if (AlreadySatisfied(s)) { report.reason = "already satisfied"; return report; }
            var rng = new LeagueRandom(LeagueSimulator.Mix(s.populationSeed, 0x43505053, nowUtc));
            if (string.IsNullOrEmpty(s.profileId)) s.profileId = LocalLeagueEngine.NewProfileId(ref rng);

            // 1. Close an interrupted legacy transition into a complete legacy archive.
            if (s.seasonTransitionCursor >= 0 && s.pendingSeasonArchive?.records != null)
            {
                for (int i = s.seasonTransitionCursor; i < s.bots.Length; i++)
                {
                    var b = s.bots[i];
                    s.pendingSeasonArchive.records[i] = new CompactBotSeason { startingMmr = b.seasonStartingMmr, endingMmr = b.currentMmr, peakMmr = Math.Max(b.currentMmr, b.seasonPeakMmr), nextStartingMmr = 0, races = b.seasonRaces, wins = b.seasonWins, podiums = b.seasonPodiums, bestLap = b.seasonBestLap, selectedTitle = b.selectedTitle, selectedTitleSeason = b.selectedTitleSeason };
                }
                s.archives.Add(s.pendingSeasonArchive);
            }
            s.pendingSeasonArchive = null; s.seasonTransitionCursor = -1;

            // 2. Per-install legacy seasons become distinct legacy ids (n - 1000) so
            //    they can never collide with global season numbers.
            if (s.legacySeasonOffsetApplied == 0)
            {
                foreach (var archive in s.archives)
                {
                    if (archive.seasonNumber <= 0) continue;
                    if (archive.records == null) archive.records = loadArchiveRecords?.Invoke(archive);
                    if (archive.records == null) continue; // unreadable history stays referenced by its old id
                    archive.seasonNumber += LegacySeasonOffset; report.legacyArchivesRenumbered++;
                }
                s.archives.RemoveAll(a => a.seasonNumber > 0 && a.records == null && a.seasonNumber >= 1);
                s.archives.Sort((a, b) => a.seasonNumber.CompareTo(b.seasonNumber));
                foreach (var entry in s.human.history) if (entry.seasonNumber > 0) entry.seasonNumber += LegacySeasonOffset;
                // Earned per-install season titles stay owned under a legacy id that
                // the catalog resolves and season trimming never removes.
                for (int t = 0; t < s.human.unlockedTitleIds.Count; t++) s.human.unlockedTitleIds[t] = LegacyTitle(s.human.unlockedTitleIds[t]);
                s.human.selectedTitleId = LegacyTitle(s.human.selectedTitleId);
                s.legacySeasonOffsetApplied = LegacySeasonOffset;
            }

            // 3. Global season.
            int season = GlobalSeasonCalendar.SeasonAt(nowUtc);
            s.currentSeasonNumber = season; s.lastProcessedSeasonNumber = season - 1; s.highestAcceptedSeason = Math.Max(s.highestAcceptedSeason, season);
            s.seasonStartTimestamp = GlobalSeasonCalendar.Start(season); s.seasonEndTimestamp = GlobalSeasonCalendar.End(season);

            // 4. Bots: competitive state only. Names persist once assigned; v1
            //    names were an unpersisted function and are assigned once here.
            var aliases = new HashSet<int>(); foreach (var a in s.botAliases) aliases.Add(a.index);
            var names = new BotNameGenerator();
            foreach (var a in s.botAliases) names.Reserve(a.displayName);
            if (s.botNames == null || s.botNames.Length != s.bots.Length) s.botNames = new string[s.bots.Length];
            for (int i = 0; i < s.bots.Length; i++)
            {
                ref var b = ref s.bots[i];
                report.ratingRemoved += b.currentMmr;
                if (b.selectedTitle >= 32 && b.selectedTitleSeason > 0) b.selectedTitleSeason += LegacySeasonOffset;
                LocalLeagueEngine.InitializeCompetitive(ref b, ref rng, c, season);
                b.seasonRaces = b.seasonWins = b.seasonPodiums = 0; b.seasonBestLap = 0;
                if ((b.flags & CompactLeagueBot.FlagNameAssigned) == 0)
                {
                    if (aliases.Contains(i)) s.botNames[i] = null; // alias table remains authoritative
                    else { s.botNames[i] = names.Generate(LeagueSimulator.Mix(s.populationSeed, i, 0x4e414d45), LocalLeagueEngine.SkillQuantile(b), out var style); b.nameStyle = (byte)style; }
                    b.flags |= CompactLeagueBot.FlagNameAssigned;
                }
                report.botsReset++;
            }
            s.nameGeneratorVersion = BotNameGenerator.GeneratorVersion;

            // 5. Human.
            var h = s.human; report.humanPreviousMmr = h.currentMmr; report.ratingRemoved += h.currentMmr;
            h.peakMmr = Math.Max(h.peakMmr, h.currentMmr);
            h.currentMmr = 0; h.seasonStartingMmr = 0; h.seasonPeakMmr = 0; h.placementGames = 0; h.provisionalRating = c.placementPrior;
            h.seasonRaces = h.seasonWins = h.seasonPodiums = 0; h.seasonBestLap = 0; h.uncertainty = 1; h.streak = 0;
            if (watermarkHuman != null && watermarkHuman.placementGames >= 0 && !string.IsNullOrEmpty(watermarkHuman.id))
            {
                // Journal loss after a committed inaugural migration: restore the
                // post-migration human competitive state instead of resetting twice.
                h.currentMmr = watermarkHuman.currentMmr; h.placementGames = watermarkHuman.placementGames; h.provisionalRating = watermarkHuman.provisionalRating;
                h.seasonPeakMmr = Math.Max(h.currentMmr, watermarkHuman.seasonPeakMmr); h.seasonStartingMmr = watermarkHuman.seasonStartingMmr; h.streak = watermarkHuman.streak;
                h.uncertainty = watermarkHuman.uncertainty; report.reason = "human restored from committed watermark";
            }

            // 6. Fresh league timeline and transactional markers.
            s.lastLeagueSimulationUtc = Math.Max(s.installationTimestamp, Math.Min(nowUtc, s.lastKnownUtcTimestamp));
            s.activeSimRaces.Clear(); s.pendingHumanRace = null; s.recentOpponentIds.Clear();
            s.ledger.budget.migrationRemoval += report.ratingRemoved;
            s.ledger.appliedMigrations.Add(Id);
            s.version = LocalLeagueState.CurrentVersion; s.competitiveContract = LocalLeagueState.CompetitiveContractCpps;
            report.applied = true; report.destinationSeason = season;
            return report;
        }
    }
}
