using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AnimeHillRacing.Racing3D
{
    // Offline developer/QA transfer of the complete competitive league: human
    // competitive state (Placement, MMR, rank, titles, combo preferences,
    // career), all 10k persistent bots (identities, names, ratings, activity
    // and presence), leaderboard source data, simulation/RNG cursors, global
    // season state and the settlement/clock ledger. The payload is the
    // integrity-checked league journal itself, so nothing is re-encoded.
    //
    // Deliberately excluded: wallet, purchases, VIP/ad-free entitlements and
    // Adventure data (they live outside the league journal and must never be
    // importable).
    //
    // Import never simulates races for the human. The imported league keeps
    // its export instant T0 as accepted time; the normal clock path then
    // advances it to now (T1) across a session boundary, so bots catch up
    // T0 -> T1 deterministically and the current global season wins.
    [Serializable]
    public sealed class LeagueExportHeader
    {
        public const string FormatName = "HA-CPPS-LEAGUE-EXPORT";
        public const int CurrentFormatVersion = 1;
        public string format = FormatName;
        public int formatVersion = CurrentFormatVersion;
        public int stateVersion, competitiveContract, seasonCalendarContract;
        public string exportId, profileId, gameVersion, ratingFormula;
        public long exportedUtc;
        public int populationSeed, population, currentSeason;
        public int humanMmr, humanPlacementGames, humanPeakMmr;
        public string journalSha256;
        public long journalBytes;
    }

    public sealed class LeagueImportResult
    {
        public bool accepted;
        public string failure = "";
        public LeagueExportHeader header;
        public LocalLeagueState state;
        public bool installKeyMatched;
    }

    public static class LeagueTransfer
    {
        static readonly byte[] Magic = Encoding.ASCII.GetBytes("HAEXP1\0");

        public static string NewExportId(long utc, string profileId)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(profileId + ":" + utc + ":" + Guid.NewGuid().ToString("N")));
                return "EX-" + BitConverter.ToString(bytes, 0, 8).Replace("-", "").ToLowerInvariant();
            }
        }

        // journal: newest committed journal bytes of 'state' (written by the store).
        public static byte[] Export(LocalLeagueState state, byte[] journal, long nowUtc, string gameVersion, Func<LeagueExportHeader, string> serialize, out LeagueExportHeader header)
        {
            if (state == null || journal == null || journal.Length == 0) throw new InvalidOperationException("No committed league journal to export.");
            if (state.pendingHumanRace != null && !string.IsNullOrEmpty(state.pendingHumanRace.raceId))
                throw new InvalidOperationException("Finish or leave the ranked race before exporting.");
            var h = state.human;
            header = new LeagueExportHeader {
                stateVersion = state.version, competitiveContract = state.competitiveContract, seasonCalendarContract = GlobalSeasonCalendar.ContractVersion,
                exportId = NewExportId(nowUtc, state.profileId), profileId = state.profileId, gameVersion = gameVersion ?? "", ratingFormula = CompetitiveRating.FormulaVersion,
                exportedUtc = nowUtc, populationSeed = state.populationSeed, population = state.bots.Length, currentSeason = state.currentSeasonNumber,
                humanMmr = h.currentMmr, humanPlacementGames = h.placementGames, humanPeakMmr = h.peakMmr,
                journalSha256 = Hex(journal), journalBytes = journal.Length };
            byte[] headerBytes = Encoding.UTF8.GetBytes(serialize(header));
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    w.Write(Magic); w.Write(headerBytes.Length); w.Write(headerBytes); w.Write(journal.Length); w.Write(journal); w.Flush();
                }
                byte[] body = stream.ToArray();
                using (var sha = SHA256.Create()) { byte[] hash = sha.ComputeHash(body); stream.Write(hash, 0, hash.Length); }
                return stream.ToArray();
            }
        }

        // parseJournal: the store's journal reader (returns null when corrupt).
        public delegate LocalLeagueState JournalParser(byte[] journal, out long generation, out bool installKeyMatched);

        public static LeagueImportResult Read(byte[] package, Func<string, LeagueExportHeader> deserialize, JournalParser parseJournal, LocalLeagueState current)
        {
            var result = new LeagueImportResult();
            try
            {
                if (package == null || package.Length < Magic.Length + 4 + 32) { result.failure = "truncated package"; return result; }
                int bodyLength = package.Length - 32;
                using (var sha = SHA256.Create())
                {
                    byte[] actual = sha.ComputeHash(package, 0, bodyLength);
                    for (int i = 0; i < 32; i++) if (actual[i] != package[bodyLength + i]) { result.failure = "package checksum mismatch"; return result; }
                }
                using (var r = new BinaryReader(new MemoryStream(package, 0, bodyLength)))
                {
                    byte[] magic = r.ReadBytes(Magic.Length);
                    for (int i = 0; i < Magic.Length; i++) if (magic[i] != Magic[i]) { result.failure = "not a league export"; return result; }
                    int headerLength = r.ReadInt32(); if (headerLength <= 0 || headerLength > 1 << 20) { result.failure = "header size"; return result; }
                    var header = deserialize(Encoding.UTF8.GetString(r.ReadBytes(headerLength)));
                    if (header == null || header.format != LeagueExportHeader.FormatName) { result.failure = "unknown format"; return result; }
                    if (header.formatVersion > LeagueExportHeader.CurrentFormatVersion) { result.failure = "export from a newer game version"; return result; }
                    int journalLength = r.ReadInt32(); if (journalLength <= 0 || journalLength != header.journalBytes) { result.failure = "journal size"; return result; }
                    byte[] journal = r.ReadBytes(journalLength);
                    if (Hex(journal) != header.journalSha256) { result.failure = "journal checksum mismatch"; return result; }
                    var state = parseJournal(journal, out _, out bool keyMatched);
                    if (state == null) { result.failure = "journal could not be parsed"; return result; }
                    string invalid = LocalLeagueStore.Validate(state);
                    if (invalid != null) { result.failure = "invalid league: " + invalid; return result; }
                    if (state.competitiveContract < LocalLeagueState.CompetitiveContractCpps) { result.failure = "pre-CPPS league export"; return result; }
                    if (state.profileId != header.profileId || state.bots.Length != header.population || state.populationSeed != header.populationSeed) { result.failure = "header does not describe journal"; return result; }
                    if (current != null && current.ledger.importedExportIds.Contains(header.exportId)) { result.failure = "this export was already imported"; return result; }
                    // Imported time never moves backwards relative to the export instant.
                    state.lastKnownUtcTimestamp = Math.Max(state.lastKnownUtcTimestamp, Math.Min(header.exportedUtc, long.MaxValue));
                    state.ledger.importedExportIds.Add(header.exportId);
                    if (current != null) foreach (var id in current.ledger.importedExportIds) if (!state.ledger.importedExportIds.Contains(id)) state.ledger.importedExportIds.Add(id);
                    while (state.ledger.importedExportIds.Count > 64) state.ledger.importedExportIds.RemoveAt(0);
                    result.header = header; result.state = state; result.installKeyMatched = keyMatched; result.accepted = true;
                    return result;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is EndOfStreamException || exception is FormatException)
            { result.failure = exception.GetType().Name + ": " + exception.Message; return result; }
        }

        static string Hex(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
