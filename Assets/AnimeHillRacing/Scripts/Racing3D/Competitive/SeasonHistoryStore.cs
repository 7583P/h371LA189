using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AnimeHillRacing.Racing3D
{
    public sealed class HistoricalStanding
    {
        public int node, mmr, position;
        public byte placement, vehicle, wheels, character, accessory;
        public string name, title;
        public string Identity(int seed) => node < 0 ? CompetitiveCoordinator.HumanId : LeagueBotCatalog.Id(seed, node);
    }

    public sealed class SeasonHistoryInfo
    {
        public int season, population, count, qualified,populationSeed;
        public long startUtc, endUtc, bytes;
        public bool deleted;
        public string unavailableReason;
    }

    // Immutable, independently compressed 256-row blocks. Only an offset table
    // and the requested blocks are loaded. Names/loadouts are captured at season
    // end; later renames do not rewrite history. No profiles or textures are saved.
    public sealed partial class SeasonHistoryStore
    {
        const ulong Magic = 0x31424c4841484148UL;
        const int Version = 1, BlockSize = 256, MaximumBytes = 64 * 1024 * 1024;
        internal readonly string root, trash;
        readonly Dictionary<string, string> verified = new Dictionary<string, string>(StringComparer.Ordinal);
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        public string Root => root;
        public Action<string> TransactionCheckpoint {get;set;}
        public SeasonHistoryStore(string directory)
        {
            root = Path.GetFullPath(directory);
            trash = Path.Combine(root, "recently-deleted");
            Directory.CreateDirectory(root); Directory.CreateDirectory(trash);
        }
        internal string ArchivePath(int season, bool deleted = false)
        {
            if (season < 1 || season >= GlobalSeasonCalendar.MaximumSeason) throw new ArgumentOutOfRangeException(nameof(season));
            return Path.Combine(deleted ? trash : root, "season-" + season.ToString(Invariant) + ".hal");
        }
        string MissingPath(int season) => Path.ChangeExtension(ArchivePath(season), ".unavailable");

        public bool MarkUnavailable(int season,string reason)
        {
            if(string.IsNullOrWhiteSpace(reason)||reason.Length>500)throw new ArgumentException("Bounded history reason required.");
            if(File.Exists(ArchivePath(season))||File.Exists(ArchivePath(season,true))||HasReceipt(season)||File.Exists(MissingPath(season)))return false;
            DurableNew(MissingPath(season),Encoding.UTF8.GetBytes("NOT AVAILABLE: "+reason));return true;
        }

        public void Capture(LocalLeagueState state, LeagueRankIndex order)
        {
            int season = state.currentSeasonNumber;
            string target = ArchivePath(season);
            // A durable receipt is written before the reset. A restart must not
            // recreate a snapshot that the user subsequently moved to trash.
            if (File.Exists(target)) { using (var existing = Open(season)) ValidateSource(existing.Info, state); return; }
            if (File.Exists(ArchivePath(season, true)) || HasReceipt(season) || File.Exists(MissingPath(season))) return;
            if (state.seasonHistoryIncomplete)
            {
                DurableNew(MissingPath(season), Encoding.UTF8.GetBytes("NOT AVAILABLE: bounded offline catch-up skipped part of this season; final standings cannot be reconstructed."));
                return;
            }
            if (state.lastLeagueSimulationUtc < state.seasonEndTimestamp - 1 || state.seasonTransitionCursor >= 0)
                throw new InvalidOperationException("History must be finalized after catch-up and before any reset.");
            string temporary = target + ".pending";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var writer = new BinaryWriter(file, Encoding.UTF8, true))
            {
                int count = state.bots.Length + 1, blocks = (count + BlockSize - 1) / BlockSize;
                writer.Write(Magic); writer.Write(Version); writer.Write(season); writer.Write(state.populationSeed);
                writer.Write(state.bots.Length); writer.Write(count); writer.Write(order.QualifiedCount);
                writer.Write(state.seasonStartTimestamp); writer.Write(state.seasonEndTimestamp);
                writer.Write(BlockSize); writer.Write(blocks);
                long table = file.Position; for (int i = 0; i <= blocks; i++) writer.Write(0L);
                var offsets = new long[blocks + 1];
                for (int block = 0; block < blocks; block++)
                {
                    offsets[block] = file.Position;
                    using (var deflate = new DeflateStream(file, CompressionLevel.Optimal, true))
                    using (var rowWriter = new BinaryWriter(deflate, Encoding.UTF8, true))
                    {
                        int end = Math.Min(count, (block + 1) * BlockSize);
                        for (int rank = block * BlockSize; rank < end; rank++) WriteRow(rowWriter, Snapshot(state, order.AtRank(rank), rank));
                    }
                }
                offsets[blocks] = file.Position; long bodyEnd = file.Position;
                file.Position = table; foreach (long offset in offsets) writer.Write(offset); writer.Flush();
                file.Position = bodyEnd; AppendHash(file);
            }
            using (var check = OpenPath(temporary)) ValidateSource(check.Info, state);
            TransactionCheckpoint?.Invoke("archive-verified");
            File.Move(temporary, target);
        }

        static void ValidateSource(SeasonHistoryInfo info, LocalLeagueState state)
        {
            if (info.season != state.currentSeasonNumber || info.population != state.bots.Length || info.populationSeed!=state.populationSeed || info.endUtc != state.seasonEndTimestamp)
                throw new IOException("Existing history belongs to a different season or population. It was preserved.");
        }
        static HistoricalStanding Snapshot(LocalLeagueState state, int node, int rank)
        {
            bool human = node == state.bots.Length;
            var h = state.human; var b = human ? default : state.bots[node];
            bool official = SeasonQualification.IsOfficial(state, node);
            int position = official ? rank + 1 : 0;
            string title = human ? h.selectedTitleId : LeagueBotCatalog.Title(b);
            if (title == "current-season-rank") title = position > 0 && position <= 1000
                ? "season-top:" + state.currentSeasonNumber + ":" + SeasonalTitleRules.TopBand(position) : "";
            string alias = null;
            if (!human) foreach (var a in state.botAliases) if (a.index == node) { alias = a.displayName; break; }
            return new HistoricalStanding { node = human ? -1 : node, mmr = human ? h.currentMmr : b.currentMmr, position = position,
                placement = human ? (byte)h.placementGames : b.placementGames,
                name = human ? h.displayName : alias ?? LeagueBotCatalog.Name(state, node), title = title ?? "",
                vehicle = human ? LeagueBotCatalog.Lookup(LeagueBotCatalog.Vehicles, h.vehiclePreference) : b.vehicle,
                wheels = human ? LeagueBotCatalog.Lookup(LeagueBotCatalog.Wheels, h.wheelPreference) : b.wheels,
                character = human ? LeagueBotCatalog.Lookup(LeagueBotCatalog.Characters, h.characterPreference) : b.character,
                accessory = human ? LeagueBotCatalog.Lookup(LeagueBotCatalog.Accessories, h.accessoryPreference) : b.accessory };
        }
        static void WriteRow(BinaryWriter w, HistoricalStanding r)
        {
            w.Write(r.node); w.Write(r.mmr); w.Write(r.position); w.Write(r.placement);
            w.Write(r.vehicle); w.Write(r.wheels); w.Write(r.character); w.Write(r.accessory);
            WriteText(w, r.name, 256); WriteText(w, r.title, 128);
        }
        static HistoricalStanding ReadRow(BinaryReader r) => new HistoricalStanding {
            node = r.ReadInt32(), mmr = r.ReadInt32(), position = r.ReadInt32(), placement = r.ReadByte(),
            vehicle = r.ReadByte(), wheels = r.ReadByte(), character = r.ReadByte(), accessory = r.ReadByte(),
            name = ReadText(r, 256), title = ReadText(r, 128) };
        static void WriteText(BinaryWriter writer, string text, int maximum)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
            if (bytes.Length > maximum) throw new InvalidDataException("History text exceeds its bounded format.");
            writer.Write((ushort)bytes.Length); writer.Write(bytes);
        }
        static string ReadText(BinaryReader reader, int maximum)
        {
            int count = reader.ReadUInt16(); if (count > maximum) throw new InvalidDataException("Invalid history text length.");
            byte[] bytes = reader.ReadBytes(count); if (bytes.Length != count) throw new EndOfStreamException();
            return new UTF8Encoding(false, true).GetString(bytes);
        }

        public List<SeasonHistoryInfo> List()
        {
            var result = new List<SeasonHistoryInfo>();
            foreach (bool deleted in new[] { false, true }) foreach (string path in Directory.GetFiles(deleted ? trash : root, "season-*.hal"))
            {
                if (!TrySeason(path, ".hal", out int season)) continue;
                try { using (var reader = OpenPath(path)) { reader.Info.deleted = deleted; result.Add(reader.Info); } }
                catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException) { result.Add(new SeasonHistoryInfo { season = season, deleted = deleted, bytes = new FileInfo(path).Length, unavailableReason = "CORRUPT: " + e.Message }); }
            }
            foreach (string path in Directory.GetFiles(root, "season-*.unavailable"))
                if (TrySeason(path, ".unavailable", out int season)) result.Add(new SeasonHistoryInfo { season = season, unavailableReason = File.ReadAllText(path, Encoding.UTF8), bytes = new FileInfo(path).Length });
            result.Sort((a, b) => b.season.CompareTo(a.season)); return result;
        }
        internal static bool TrySeason(string path, string suffix, out int season) =>
            int.TryParse(Path.GetFileNameWithoutExtension(path).Substring(7), NumberStyles.None, Invariant, out season) &&
            season > 0 && season < GlobalSeasonCalendar.MaximumSeason && Path.GetFileName(path) == "season-" + season.ToString(Invariant) + suffix;
        public HistoryReader Open(int season, bool deleted = false) => OpenPath(ArchivePath(season, deleted));
        HistoryReader OpenPath(string path)
        {
            var info = new FileInfo(path); string stamp = info.Length + ":" + info.LastWriteTimeUtc.Ticks;
            lock (verified) if (!verified.TryGetValue(path, out var old) || old != stamp) { VerifyFile(path); verified[path] = stamp; }
            return new HistoryReader(path);
        }

        public sealed class HistoryReader : IDisposable
        {
            readonly FileStream file;
            readonly long[] offsets;
            public SeasonHistoryInfo Info { get; }
            public int Seed { get; }
            internal HistoryReader(string path)
            {
                file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    using (var r = new BinaryReader(file, Encoding.UTF8, true))
                    {
                        if (r.ReadUInt64() != Magic || r.ReadInt32() != Version) throw new InvalidDataException("Unsupported history format.");
                        Info = new SeasonHistoryInfo { season = r.ReadInt32() }; Seed = r.ReadInt32();Info.populationSeed=Seed;
                        Info.population = r.ReadInt32(); Info.count = r.ReadInt32(); Info.qualified = r.ReadInt32();
                        Info.startUtc = r.ReadInt64(); Info.endUtc = r.ReadInt64(); Info.bytes = file.Length;
                        int blockSize = r.ReadInt32(), blocks = r.ReadInt32();
                        if (Info.population < 12 || Info.population > 1000000 || Info.count != Info.population + 1 ||
                            Info.qualified < 0 || Info.qualified > Info.count || blockSize != BlockSize || blocks != (Info.count + BlockSize - 1) / BlockSize ||
                            Info.startUtc != GlobalSeasonCalendar.Start(Info.season) || Info.endUtc != GlobalSeasonCalendar.End(Info.season)) throw new InvalidDataException("Invalid history header.");
                        offsets = new long[blocks + 1];
                        for (int i = 0; i <= blocks; i++) { offsets[i] = r.ReadInt64(); if (offsets[i] <= 0 || offsets[i] > file.Length - 32 || (i > 0 && offsets[i] <= offsets[i - 1])) throw new InvalidDataException("Invalid block offset."); }
                        if (offsets[0] != file.Position || offsets[blocks] != file.Length - 32) throw new InvalidDataException("Invalid history length.");
                    }
                }
                catch { file.Dispose(); throw; }
            }
            public HistoricalStanding[] Page(int first, int count)
            {
                if (first < 0 || count < 0 || count > 1000 || first > Info.count) throw new ArgumentOutOfRangeException();
                count = Math.Min(count, Info.count - first); var result = new HistoricalStanding[count];
                if (count == 0) return result;
                int last = (first + count - 1) / BlockSize;
                for (int block = first / BlockSize; block <= last; block++)
                {
                    file.Position = offsets[block];
                    using (var bounded = new BoundedReadStream(file, offsets[block + 1] - offsets[block]))
                    using (var deflate = new DeflateStream(bounded, CompressionMode.Decompress, true))
                    using (var r = new BinaryReader(deflate, Encoding.UTF8, true))
                    {
                        int end = Math.Min(Info.count, (block + 1) * BlockSize);
                        for (int index = block * BlockSize; index < end; index++)
                        {
                            var row = ReadRow(r);
                            if (row.node < -1 || row.node >= Info.population || row.mmr < 0 || row.mmr > CompetitiveRating.MaximumMmr || row.placement > 3 ||
                                row.position != (index < Info.qualified ? index + 1 : 0) || (row.position > 0 && row.placement < 3) ||
                                row.vehicle >= LeagueBotCatalog.Vehicles.Length || row.wheels >= LeagueBotCatalog.Wheels.Length || row.character >= LeagueBotCatalog.Characters.Length || row.accessory >= LeagueBotCatalog.Accessories.Length)
                                throw new InvalidDataException("Invalid historical standing.");
                            if (index >= first && index < first + count) result[index - first] = row;
                        }
                        if (deflate.ReadByte() != -1) throw new InvalidDataException("Oversized history block.");
                    }
                }
                return result;
            }
            public int Find(string query, bool humanOnly = false, CancellationToken cancellation = default)
            {
                query = query ?? "";
                for (int first = 0; first < Info.count; first += BlockSize)
                {
                    cancellation.ThrowIfCancellationRequested(); var rows = Page(first, BlockSize);
                    for (int i = 0; i < rows.Length; i++) if (humanOnly ? rows[i].node < 0 : rows[i].name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || rows[i].Identity(Seed).Equals(query, StringComparison.OrdinalIgnoreCase)) return first + i;
                }
                return -1;
            }
            public void Dispose() => file.Dispose();
        }

        const string CsvHeader = "identity,display_name,mmr,official_position,career_placement,vehicle,wheels,character,accessory,title";
        static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        public string Export(int season, string destination, CancellationToken cancellation = default)
        {
            destination = Path.GetFullPath(destination);
            if (!destination.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || File.Exists(destination)) throw new IOException("Choose a new ZIP file.");
            string temporary = destination + ".partial-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var source = Open(season))
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    string csvHash;
                    using (var zip = new ZipArchive(file, ZipArchiveMode.Create, true))
                    {
                        var entry = zip.CreateEntry("leaderboard.csv", CompressionLevel.Optimal);
                        using (var hash = SHA256.Create())
                        {
                            using (var raw = entry.Open())
                            using (var crypto = new CryptoStream(raw, hash, CryptoStreamMode.Write))
                            using (var writer = new StreamWriter(crypto, new UTF8Encoding(false), 32768))
                            {
                                writer.NewLine = "\n"; writer.WriteLine(CsvHeader);
                                for (int first = 0; first < source.Info.count; first += BlockSize)
                                {
                                    cancellation.ThrowIfCancellationRequested();
                                    foreach (var row in source.Page(first, BlockSize)) writer.WriteLine(string.Join(",", Csv(row.Identity(source.Seed)), Csv(row.name), row.mmr.ToString(Invariant), row.position > 0 ? row.position.ToString(Invariant) : "", row.placement.ToString(Invariant), Csv(LeagueBotCatalog.Vehicles[row.vehicle]), Csv(LeagueBotCatalog.Wheels[row.wheels]), Csv(LeagueBotCatalog.Characters[row.character]), Csv(LeagueBotCatalog.Accessories[row.accessory]), Csv(row.title)));
                                }
                            }
                            csvHash = Hex(hash.Hash);
                        }
                        string summary = "{\"schema\":1,\"season\":" + season + ",\"competitors\":" + source.Info.count + ",\"qualified\":" + source.Info.qualified + ",\"localOnly\":true,\"displayNames\":\"captured_at_season_end\",\"startUtc\":\"" + new DateTime(source.Info.startUtc, DateTimeKind.Utc).ToString("O", Invariant) + "\",\"endUtc\":\"" + new DateTime(source.Info.endUtc, DateTimeKind.Utc).ToString("O", Invariant) + "\"}";
                        WriteEntry(zip, "season_summary.json", summary);
                        byte[] summaryBytes = Encoding.UTF8.GetBytes(summary);
                        string manifest = "{\"schema\":1,\"season\":" + season + ",\"rows\":" + source.Info.count + ",\"leaderboardSha256\":\"" + csvHash + "\",\"summarySha256\":\"" + Hash(summaryBytes) + "\",\"archiveSha256\":\"" + FileHash(ArchivePath(season)) + "\"}";
                        WriteEntry(zip, "manifest.json", manifest);
                    }
                    file.Flush(true);
                }
                VerifyExport(season, temporary, cancellation);
                cancellation.ThrowIfCancellationRequested(); File.Move(temporary, destination);
                return FileHash(destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        static void WriteEntry(ZipArchive zip, string name, string text)
        { using (var stream = zip.CreateEntry(name, CompressionLevel.Optimal).Open()) { byte[] data = Encoding.UTF8.GetBytes(text); stream.Write(data, 0, data.Length); } }
        public void VerifyExport(int season, string path, CancellationToken cancellation = default)
        {
            using (var expected = Open(season))
            using (var zip = ZipFile.OpenRead(path))
            {
                if (zip.Entries.Count != 3) throw new InvalidDataException("Unexpected export members.");
                foreach(var entry in zip.Entries)if(entry.Length<0||entry.Length>(entry.FullName=="leaderboard.csv"?expected.Info.count*2048L:16384))throw new InvalidDataException("Export member exceeds its bounded format.");
                string manifest, summary, actualHash;
                using (var reader = new StreamReader(zip.GetEntry("manifest.json").Open(), Encoding.UTF8)) manifest = reader.ReadToEnd();
                using (var reader = new StreamReader(zip.GetEntry("season_summary.json").Open(), Encoding.UTF8)) summary = reader.ReadToEnd();
                using (var raw = zip.GetEntry("leaderboard.csv").Open()) using (var hash = SHA256.Create()) actualHash = Hex(hash.ComputeHash(raw));
                if (!manifest.Contains("\"leaderboardSha256\":\"" + actualHash + "\"") || !manifest.Contains("\"summarySha256\":\"" + Hash(Encoding.UTF8.GetBytes(summary)) + "\"") || !manifest.Contains("\"archiveSha256\":\"" + FileHash(ArchivePath(season)) + "\"")) throw new InvalidDataException("Export checksum mismatch.");
                using (var raw = zip.GetEntry("leaderboard.csv").Open())
                using (var reader = new StreamReader(raw, new UTF8Encoding(false, true)))
                {
                    if (reader.ReadLine() != CsvHeader) throw new InvalidDataException("Invalid CSV header.");
                    for (int first = 0; first < expected.Info.count; first += BlockSize)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        foreach (var row in expected.Page(first, BlockSize))
                        {
                            var values = ReadCsvRow(reader);
                            if (values == null || values.Count != 10 || values[0] != row.Identity(expected.Seed) || values[1] != row.name || values[2] != row.mmr.ToString(Invariant) || values[3] != (row.position > 0 ? row.position.ToString(Invariant) : "") || values[4] != row.placement.ToString(Invariant) || values[5]!=LeagueBotCatalog.Vehicles[row.vehicle] || values[6]!=LeagueBotCatalog.Wheels[row.wheels] || values[7]!=LeagueBotCatalog.Characters[row.character] || values[8]!=LeagueBotCatalog.Accessories[row.accessory] || values[9] != row.title) throw new InvalidDataException("CSV round trip differs from archived standings.");
                        }
                    }
                    if (reader.Read() != -1) throw new InvalidDataException("Extra CSV rows.");
                }
            }
        }
        static List<string> ReadCsvRow(TextReader reader)
        {
            var result = new List<string>(10); var value = new StringBuilder(); bool quoted = false, any = false;
            for (int read; (read = reader.Read()) >= 0;)
            {
                any = true; char c = (char)read;
                if (c == '"') { if (quoted && reader.Peek() == '"') { reader.Read(); value.Append('"'); } else quoted = !quoted; }
                else if (c == ',' && !quoted) { result.Add(value.ToString()); value.Clear(); }
                else if (c == '\n' && !quoted) { result.Add(value.ToString()); return result; }
                else value.Append(c);
                if (value.Length > 2048 || result.Count > 10) throw new InvalidDataException("Oversized CSV row.");
            }
            if (!any) return null; throw new InvalidDataException("Truncated CSV row.");
        }

        internal static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        internal static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes)); }
        public static string FileHash(string path) { using (var file = File.OpenRead(path)) using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(file)); }
        internal static void AppendHash(FileStream file)
        {
            file.Flush(); file.Position = 0; byte[] digest; using (var hash = SHA256.Create()) digest = hash.ComputeHash(file);
            file.Position = file.Length; file.Write(digest, 0, digest.Length); file.Flush(true);
        }
        internal static void VerifyFile(string path)
        {
            using (var file = File.OpenRead(path))
            {
                if (file.Length < 64 || file.Length > MaximumBytes) throw new InvalidDataException("Invalid history file size.");
                long body = file.Length - 32; byte[] actual;
                using (var hash = SHA256.Create()) using (var bounded = new BoundedReadStream(file, body)) actual = hash.ComputeHash(bounded);
                byte[] expected = new byte[32]; if (file.Read(expected, 0, 32) != 32 || !CryptographicOperations.FixedTimeEquals(expected, actual)) throw new InvalidDataException("History checksum failed; original file preserved.");
            }
        }
        internal static void DurableNew(string path, byte[] data)
        {
            string temporary = path + ".pending";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) { file.Write(data, 0, data.Length); file.Flush(true); }
            File.Move(temporary, path);
        }
        internal sealed class BoundedReadStream : Stream
        {
            readonly Stream source; long remaining;
            public BoundedReadStream(Stream source, long count) { this.source = source; remaining = count; }
            public override int Read(byte[] b, int offset, int count) { if (remaining == 0) return 0; int n = source.Read(b, offset, (int)Math.Min(remaining, count)); remaining -= n; if (n == 0) throw new EndOfStreamException(); return n; }
            public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException(); public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { } public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
            public override void SetLength(long v) => throw new NotSupportedException(); public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
        }
    }
}
