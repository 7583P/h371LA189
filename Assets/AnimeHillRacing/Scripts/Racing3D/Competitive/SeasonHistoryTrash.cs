using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AnimeHillRacing.Racing3D
{
    public sealed class HistoryDeletionConfirmation
    {
        public int season;
        public long bytes, requestedUtc, earliestDeleteUtc;
        public string checksum;
    }
    public sealed class HistoryStorageUsage
    {
        public long activeBytes, trashBytes, recoverableBytes, transactionBytes;
        public long TotalBytes => activeBytes + trashBytes + transactionBytes;
    }

    public sealed partial class SeasonHistoryStore
    {
        public const int RecoveryDays = 30;
        public const long RecoveryTicks = RecoveryDays * TimeSpan.TicksPerDay;
        const ulong ReceiptMagic = 0x3152535441484148UL;
        readonly string clockSession = Guid.NewGuid().ToString("N");
        sealed class DeletionReceipt
        {
            public int season;
            public byte operation; // 0 delete, 1 restore, 2 purge
            public long bytes, deletedUtc, elapsedTicks, lastWall, lastMonotonic, generation;
            public bool anomaly;
            public string checksum, session;
        }
        internal string ReceiptPath(int season) => Path.ChangeExtension(ArchivePath(season, true), ".receipt");
        bool HasReceipt(int season)=>File.Exists(ReceiptPath(season)+".0")||File.Exists(ReceiptPath(season)+".1");
        IEnumerable<int> ReceiptSeasons(){
            var seen=new HashSet<int>();
            foreach(string path in Directory.GetFiles(trash,"season-*.receipt.*")){
                string name=Path.GetFileName(path);int split=name.IndexOf(".receipt.",StringComparison.Ordinal);
                if(split<7||!int.TryParse(name.Substring(7,split-7),out int season)||season<1||season>=GlobalSeasonCalendar.MaximumSeason)continue;
                if(name!="season-"+season+".receipt.0"&&name!="season-"+season+".receipt.1")continue;
                if(seen.Add(season))yield return season;
            }
        }
        void ClearReceipt(int season,long generation){
            // Remove the older intent first. If power fails between deletions,
            // the surviving restore/purge intent must never revert to delete.
            int newest=(int)(generation&1);string older=ReceiptPath(season)+"."+(1-newest),latest=ReceiptPath(season)+"."+newest;
            if(File.Exists(older))File.Delete(older);TransactionCheckpoint?.Invoke("receipt-old-cleared");
            if(File.Exists(latest))File.Delete(latest);
        }
        public HistoryDeletionConfirmation PrepareDeletion(int season, int currentSeason, long wallUtc)
        {
            if (season >= currentSeason) throw new InvalidOperationException("Only completed seasons can be deleted.");
            string path = ArchivePath(season); using (var reader = Open(season)) { }
            return new HistoryDeletionConfirmation { season = season, requestedUtc = wallUtc,
                earliestDeleteUtc = checked(wallUtc + RecoveryTicks), bytes = new FileInfo(path).Length, checksum = FileHash(path) };
        }
        public void MoveToTrash(HistoryDeletionConfirmation confirmed, int currentSeason, long monotonicMilliseconds)
        {
            if (confirmed == null || confirmed.season >= currentSeason) throw new InvalidOperationException("Confirm a completed season first.");
            int season = confirmed.season; string active = ArchivePath(season), deleted = ArchivePath(season, true);
            if (File.Exists(deleted) && HasReceipt(season)) return;
            if (new FileInfo(active).Length != confirmed.bytes || FileHash(active) != confirmed.checksum) throw new IOException("Archive changed after confirmation.");
            var receipt = new DeletionReceipt { season = season, bytes = confirmed.bytes, deletedUtc = confirmed.requestedUtc,
                lastWall = confirmed.requestedUtc, lastMonotonic = monotonicMilliseconds, session = clockSession, checksum = confirmed.checksum };
            SaveReceipt(receipt); // Intent is durable before moving any archive.
            TransactionCheckpoint?.Invoke("delete-intent");
            File.Move(active, deleted);
        }
        public bool Restore(int season)
        {
            string active = ArchivePath(season), deleted = ArchivePath(season, true), receiptPath = ReceiptPath(season);
            if (!HasReceipt(season)) return File.Exists(active);
            var receipt = ReadReceipt(season);
            if (receipt.elapsedTicks >= RecoveryTicks && !receipt.anomaly) return false;
            receipt.operation = 1; SaveReceipt(receipt);TransactionCheckpoint?.Invoke("restore-intent");CompleteTransaction(receipt); return File.Exists(active);
        }

        // Only observed monotonic elapsed time inside this process is trusted.
        // Closing the game never credits wall-clock-only days. Without an
        // authenticated external clock or a boot identity, cross-restart time
        // cannot prove 30 real days: purge deliberately waits longer.
        public void MaintainTrash(int currentSeason, long wallUtc, long monotonicMilliseconds)
        {
            foreach (int season in ReceiptSeasons())
            {
                if (season >= currentSeason) continue;
                DeletionReceipt r;
                try { r = ReadReceipt(season); } catch (Exception e) when(e is IOException||e is InvalidDataException) { continue; }
                if (r.operation != 0) { CompleteTransaction(r); continue; }
                CompleteTransaction(r); // Resume a delete interrupted before move.
                if (!File.Exists(ArchivePath(season, true))) continue;
                if (r.session == clockSession)
                {
                    long mono = monotonicMilliseconds - r.lastMonotonic, wall = wallUtc - r.lastWall;
                    if (mono < 0 || wall < 0 || Math.Abs(wall / (double)TimeSpan.TicksPerMillisecond - mono) > TimeSpan.FromMinutes(5).TotalMilliseconds)
                        r.anomaly = true;
                    else if (!r.anomaly) r.elapsedTicks = Math.Min(RecoveryTicks, checked(r.elapsedTicks + mono * TimeSpan.TicksPerMillisecond));
                }
                else if (wallUtc < r.lastWall) r.anomaly = true;
                r.session = clockSession; r.lastMonotonic = monotonicMilliseconds; r.lastWall = wallUtc;
                SaveReceipt(r);
                if (r.elapsedTicks < RecoveryTicks || r.anomaly || wallUtc < r.deletedUtc + RecoveryTicks) continue;
                string deleted = ArchivePath(season, true);
                VerifyFile(deleted);
                if (new FileInfo(deleted).Length != r.bytes || FileHash(deleted) != r.checksum) continue;
                r.operation = 2; SaveReceipt(r);TransactionCheckpoint?.Invoke("purge-intent");CompleteTransaction(r);
            }
        }
        void CompleteTransaction(DeletionReceipt r)
        {
            string active = ArchivePath(r.season), deleted = ArchivePath(r.season, true), receipt = ReceiptPath(r.season);
            if (r.operation == 0)
            {
                if (!File.Exists(deleted) && File.Exists(active))
                {
                    if (FileHash(active) != r.checksum) throw new IOException("Interrupted delete no longer matches the confirmed archive.");
                    File.Move(active, deleted);
                }
                // A missing payload is preserved as a recovery receipt, never
                // interpreted as permission to delete another kind of file.
            }
            else if (r.operation == 1)
            {
                if (!File.Exists(active) && File.Exists(deleted))
                {
                    if (FileHash(deleted) != r.checksum) throw new IOException("Restore checksum mismatch.");
                    File.Move(deleted, active);
                }
                if (File.Exists(active) && FileHash(active) == r.checksum) ClearReceipt(r.season,r.generation);
            }
            else if (r.operation == 2 && r.elapsedTicks >= RecoveryTicks && !r.anomaly)
            {
                if (File.Exists(deleted))
                {
                    VerifyFile(deleted);
                    if (FileHash(deleted) != r.checksum) return;
                    File.Delete(deleted);
                }
                ClearReceipt(r.season,r.generation);
            }
        }
        public HistoryStorageUsage StorageUsage()
        {
            var usage = new HistoryStorageUsage();
            foreach (string path in Directory.GetFiles(root, "season-*.hal")) if (TrySeason(path, ".hal", out _)) usage.activeBytes += new FileInfo(path).Length;
            foreach (string path in Directory.GetFiles(trash, "season-*.hal"))
            {
                if (!TrySeason(path, ".hal", out int season)) continue;
                long bytes = new FileInfo(path).Length; usage.trashBytes += bytes;
                try { var receipt = ReadReceipt(season); if (receipt.elapsedTicks < RecoveryTicks || receipt.anomaly) usage.recoverableBytes += bytes; } catch (Exception e) when(e is IOException||e is InvalidDataException) { }
            }
            foreach (string folder in new[] { root, trash }) foreach (string path in Directory.GetFiles(folder))
                if (!path.EndsWith(".hal", StringComparison.Ordinal)) usage.transactionBytes += new FileInfo(path).Length;
            return usage;
        }
        void SaveReceipt(DeletionReceipt r)
        {
            r.generation++;
            string path = ReceiptPath(r.season)+"."+(r.generation&1), temporary = path + ".pending";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var writer = new BinaryWriter(file, Encoding.UTF8, true))
            {
                writer.Write(ReceiptMagic);writer.Write(r.generation);writer.Write(r.season); writer.Write(r.operation); writer.Write(r.bytes); writer.Write(r.deletedUtc);
                writer.Write(r.elapsedTicks); writer.Write(r.lastWall); writer.Write(r.lastMonotonic); writer.Write(r.anomaly);
                WriteText(writer, r.checksum, 64); WriteText(writer, r.session, 32); writer.Flush(); AppendHash(file);
            }
            VerifyFile(temporary);
            // Alternating generations avoid ReplaceFile platform/permission
            // differences. The other verified receipt survives an interrupted
            // replacement; loss can only defer a purge, never accelerate it.
            if(File.Exists(path))File.Delete(path);File.Move(temporary,path);
        }
        DeletionReceipt ReadReceipt(int season)
        {
            DeletionReceipt best=null;
            for(int slot=0;slot<2;slot++){
                string path=ReceiptPath(season)+"."+slot;
                if(!File.Exists(path))continue;
                try{var value=ReadReceiptFile(path,season);if(best==null||value.generation>best.generation)best=value;}catch(Exception e)when(e is IOException||e is InvalidDataException){}
            }
            return best??throw new InvalidDataException("No valid deletion receipt; history preserved.");
        }
        DeletionReceipt ReadReceiptFile(string path,int season)
        {
            VerifyFile(path);
            using (var file = File.OpenRead(path)) using (var r = new BinaryReader(file, Encoding.UTF8, true))
            {
                if (r.ReadUInt64() != ReceiptMagic) throw new InvalidDataException("Invalid deletion receipt.");
                long generation=r.ReadInt64();if(generation<1||r.ReadInt32()!=season)throw new InvalidDataException("Invalid deletion receipt generation.");
                var receipt = new DeletionReceipt { season = season,generation=generation,operation = r.ReadByte(), bytes = r.ReadInt64(), deletedUtc = r.ReadInt64(),
                    elapsedTicks = r.ReadInt64(), lastWall = r.ReadInt64(), lastMonotonic = r.ReadInt64(), anomaly = r.ReadBoolean(), checksum = ReadText(r, 64), session = ReadText(r, 32) };
                if (receipt.operation > 2 || receipt.elapsedTicks < 0 || receipt.elapsedTicks > RecoveryTicks || receipt.bytes <= 0 || receipt.checksum.Length != 64 || receipt.session.Length != 32)
                    throw new InvalidDataException("Invalid deletion receipt values.");
                return receipt;
            }
        }
    }
}
