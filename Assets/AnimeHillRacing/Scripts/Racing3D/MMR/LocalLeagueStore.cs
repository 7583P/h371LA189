using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AnimeHillRacing.Racing3D
{
    // Integrity terms used precisely:
    //  * SHA-256 trailer: detects accidental corruption/truncation only. Anyone
    //    who can rewrite the file can rewrite the hash.
    //  * HMAC-SHA256 trailer: authenticity relative to a per-install random key
    //    that is never in source and is stored outside the journal directory.
    //    It detects casual edits; it is not server authority. A missing or
    //    rotated key never destroys progression: the journal is accepted on its
    //    checksum and an anomaly is recorded.
    public interface IIntegrityKeyProvider { byte[] Key(); }

    public sealed class LocalLeagueStore
    {
        const string MagicV3="HA_LOCAL_COMPACT_V3",Magic="HA_LOCAL_COMPACT_V4",ArchiveMagic="HA_SEASON_COMPACT_V3",SessionMagic="HA_RANKED_SESSION_V1";
        readonly string directory;
        readonly Func<LocalLeagueState,string> serialize;
        readonly Func<string,LocalLeagueState> deserialize;
        readonly Func<string,LegacyLocalLeagueState> legacyReader;
        readonly int configuredPopulation;
        long generation;
        int lastSavedArchiveMinimum;
        readonly Dictionary<string, (long length, long modified)> verifiedArchives = new Dictionary<string, (long, long)>();
        public IIntegrityKeyProvider KeyProvider {get;set;}
        public bool ReadOnlyProtection {get;private set;}
        public bool MigratedLegacy {get;private set;}
        public bool LoadedPreCpps {get;private set;}
        public bool MacMismatch {get;private set;}
        public bool RecoveredOlderGeneration {get;private set;}
        public bool JournalExisted {get;private set;}
        public long Generation=>generation;
        public string Directory=>directory;
        public string RecoveryNotice {get;private set;}="";
        public static int BytesPerBot=>Marshal.SizeOf<CompactLeagueBot>();
        public static int BytesPerBotV3=>Marshal.SizeOf<CompactLeagueBotV3>();
        public static int BytesPerSeasonBot=>Marshal.SizeOf<CompactBotSeason>();
        public LocalLeagueStore(string path,Func<LocalLeagueState,string> writer,Func<string,LocalLeagueState> reader,Func<string,LegacyLocalLeagueState> oldReader=null,int population=10000)
        {directory=Path.GetFullPath(path);serialize=writer;deserialize=reader;legacyReader=oldReader;configuredPopulation=population;}
        string Slot(int slot)=>Path.Combine(directory,"league-v4-"+slot+".journal");
        string SlotV3(int slot)=>Path.Combine(directory,"league-v3-"+slot+".journal");
        string ArchivePath(int season)=>Path.Combine(directory,"seasons","season-"+season+".bin");
        public string SessionPath=>Path.Combine(directory,"ranked-session.bin");

        public LocalLeagueState Load()
        {
            generation=0;
            LocalLeagueState best=null;bool existed=false;int validSlots=0;long highestSeen=0;
            for(int slot=0;slot<2;slot++){
                string path=Slot(slot);if(!File.Exists(path))continue;existed=true;
                try{
                    var state=ReadJournal(path,Magic,BytesPerBot,out long version,out bool macOk);
                    if(state==null)continue;highestSeen=Math.Max(highestSeen,version);validSlots++;
                    if(version>generation){generation=version;best=state;MacMismatch=!macOk;}
                }catch(Exception e) when(e is IOException || e is ArgumentException || e is FormatException || e is UnauthorizedAccessException || e is EndOfStreamException){RecoveryNotice=e.Message;}
            }
            // A newer slot that exists but failed validation means we fell back.
            if(best!=null && existed && validSlots<2 && File.Exists(Slot((int)((generation+1)&1))))RecoveredOlderGeneration=true;
            if(best==null && !ReadOnlyProtection){
                bool v3Existed=false;
                for(int slot=0;slot<2;slot++){
                    string path=SlotV3(slot);if(!File.Exists(path))continue;v3Existed=existed=true;
                    try{var state=ReadJournal(path,MagicV3,BytesPerBotV3,out long version,out _);if(state!=null && version>generation){generation=version;best=state;}}
                    catch(Exception e) when(e is IOException || e is ArgumentException || e is FormatException || e is UnauthorizedAccessException || e is EndOfStreamException){RecoveryNotice=e.Message;}
                }
                if(best!=null){LoadedPreCpps=true;RecoveryNotice="Pre-CPPS v3 journal loaded; v3 slots are preserved unchanged.";}
                else if(!v3Existed && !existed && legacyReader!=null){best=LoadLegacy();if(best!=null)LoadedPreCpps=true;}
            }
            JournalExisted=existed;
            if(ReadOnlyProtection)RecoveryNotice="A newer local progression save was preserved. Updating the game is required to save progression.";
            else if(best==null && existed){ReadOnlyProtection=true;RecoveryNotice="All compact journal slots are damaged; originals were preserved for recovery.";}
            lastSavedArchiveMinimum = best != null && best.archives.Count > 0 ? best.archives[0].seasonNumber : 0;
            return best;
        }

        LocalLeagueState ReadJournal(string path,string magic,int recordSize,out long version,out bool macOk)
        {
            version=0;macOk=true;
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var reader=new BinaryReader(stream,Encoding.UTF8,true)){
                string found=reader.ReadString();
                if(found!=magic){if(found.StartsWith("HA_LOCAL_COMPACT_V",StringComparison.Ordinal) && string.CompareOrdinal(found,Magic)>0)ReadOnlyProtection=true;return null;}
                bool v4=magic==Magic;
                if(!Verified(stream,v4,out macOk))return null;
                stream.Position=Encoding.UTF8.GetByteCount(magic)+1;
                version=reader.ReadInt64();int size=reader.ReadInt32(),population=reader.ReadInt32(),jsonSize=reader.ReadInt32(),pendingCount=reader.ReadInt32();
                if(version<1 || size!=recordSize || population<12 || population>1000000 || jsonSize<2 || jsonSize>8000000 || (pendingCount!=0 && pendingCount!=population))return null;
                string json=Encoding.UTF8.GetString(reader.ReadBytes(jsonSize));var state=deserialize(json);
                if(state!=null && state.version>LocalLeagueState.CurrentVersion){ReadOnlyProtection=true;return null;}
                if(state==null || state.version!=(v4?4:3) || state.population!=population)return null;
                if(v4){state.bots=new CompactLeagueBot[population];ReadRecords(stream,state.bots);}
                else{var old=new CompactLeagueBotV3[population];ReadRecords(stream,old);state.bots=new CompactLeagueBot[population];for(int i=0;i<population;i++)state.bots[i]=old[i].Upgrade();}
                if(pendingCount>0){if(state.pendingSeasonArchive==null)return null;state.pendingSeasonArchive.records=new CompactBotSeason[population];ReadRecords(stream,state.pendingSeasonArchive.records);}
                else if(state.pendingSeasonArchive!=null && state.seasonTransitionCursor<0)state.pendingSeasonArchive=null;
                if(v4){
                    int names=reader.ReadInt32();if(names!=0 && names!=population)return null;
                    if(names>0){state.botNames=new string[population];for(int i=0;i<population;i++){string name=reader.ReadString();state.botNames[i]=name.Length==0?null:name;}}
                }
                Normalize(state);
                if(v4 && !Valid(state))return null;
                if(!v4 && !ValidV3(state))return null;
                return state;
            }
        }

        // JsonUtility materializes absent nested classes as empty objects.
        public static void Normalize(LocalLeagueState s)
        {
            if(s.pendingHumanRace!=null && string.IsNullOrEmpty(s.pendingHumanRace.raceId))s.pendingHumanRace=null;
            if(s.ledger==null)s.ledger=new CompetitiveLedger();
            if(s.ledger.budget==null)s.ledger.budget=new RatingBudget();
            if(s.activeSimRaces==null)s.activeSimRaces=new List<SimulatedRaceSlot>();
            if(s.hourlyRaces==null || s.hourlyRaces.Length!=48)s.hourlyRaces=new int[48];
            if(s.recentOpponentIds==null)s.recentOpponentIds=new List<string>();
            if(s.botAliases==null)s.botAliases=new List<LegacyBotAlias>();
            if(s.archives==null)s.archives=new List<LeagueSeasonArchive>();
        }

        LocalLeagueState LoadLegacy()
        {
            LegacyLocalLeagueState best=null;long bestGeneration=0;bool existed=false;
            for(int i=0;i<2;i++){
                string path=Path.Combine(directory,"league-"+i+".journal");if(!File.Exists(path))continue;existed=true;
                try{
                    string[] p=File.ReadAllText(path,Encoding.UTF8).Split(new[]{'\n'},4);
                    if(p.Length!=4 || p[0]!="HA_LOCAL_LEAGUE_V2"){if(p.Length>0 && p[0].StartsWith("HA_LOCAL_LEAGUE_V",StringComparison.Ordinal))ReadOnlyProtection=true;continue;}
                    using(var hash=SHA256.Create()){string digest=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(p[3]))).Replace("-","").ToLowerInvariant();if(p[2]!=digest)continue;}
                    if(!long.TryParse(p[1],out long g) || g<1)continue;var value=legacyReader(p[3]);if(value!=null && value.version>2)ReadOnlyProtection=true;if(value==null || value.version!=2)continue;
                    if(g>bestGeneration){bestGeneration=g;best=value;}
                }catch(Exception e) when(e is IOException || e is ArgumentException || e is FormatException){RecoveryNotice=e.Message;}
            }
            if(ReadOnlyProtection)return null;
            if(best==null){if(existed){ReadOnlyProtection=true;RecoveryNotice="Legacy journal damage was preserved for recovery.";}return null;}
            var result=LegacyLeagueMigration.Convert(best,configuredPopulation);MigratedLegacy=true;RecoveryNotice="Existing 1000 identities and installation epoch migrated once to the 10k rating scale and compact population.";return result;
        }

        public bool Save(LocalLeagueState state)
        {
            if(ReadOnlyProtection)return false;Normalize(state);{string invalid=Validate(state);if(invalid!=null)throw new ArgumentException("Refusing to save invalid compact league state: "+invalid);}
            System.IO.Directory.CreateDirectory(directory);System.IO.Directory.CreateDirectory(Path.Combine(directory,"seasons"));
            foreach(var archive in state.archives)if(archive.records!=null)WriteArchive(archive);
            long next=checked(generation+1);string target=Slot((int)(next&1)),temporary=target+".pending";
            state.savedRevision=next;
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.ReadWrite,FileShare.None))using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){
                byte[] json=Encoding.UTF8.GetBytes(serialize(state));writer.Write(Magic);writer.Write(next);writer.Write(BytesPerBot);writer.Write(state.bots.Length);writer.Write(json.Length);
                writer.Write(state.pendingSeasonArchive?.records?.Length??0);writer.Write(json);writer.Flush();WriteRecords(stream,state.bots);
                if(state.pendingSeasonArchive?.records!=null)WriteRecords(stream,state.pendingSeasonArchive.records);
                bool names=state.botNames!=null && state.botNames.Length==state.bots.Length;
                writer.Write(names?state.botNames.Length:0);
                if(names)foreach(string name in state.botNames)writer.Write(name??"");
                writer.Flush();AppendIntegrity(stream,true);
            }
            if(File.Exists(target))File.Delete(target);File.Move(temporary,target);generation=next;
            int newestMinimum=state.archives.Count==0?0:state.archives[0].seasonNumber;
            // The other journal is still a recovery generation. Preserve every
            // archive referenced by that generation, including large offline jumps.
            int minimum=Math.Min(newestMinimum,lastSavedArchiveMinimum);
            lastSavedArchiveMinimum=newestMinimum;
            // Completed history is retained until an explicit, separate history
            // lifecycle transaction. A save never silently deletes old seasons.
            return true;
        }

        // ---------- ranked session journal (tiny, written at GO / settlement) ----------
        [Serializable] public sealed class RankedSessionFile { public PendingRankedRace pending; public RaceSettlementRecord settlement; public string profileId; }
        public void SaveSession(RankedSessionFile session,Func<RankedSessionFile,string> writer)
        {
            System.IO.Directory.CreateDirectory(directory);
            string temporary=SessionPath+".pending";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.ReadWrite,FileShare.None))using(var w=new BinaryWriter(stream,Encoding.UTF8,true)){
                w.Write(SessionMagic);w.Write(writer(session));w.Flush();AppendIntegrity(stream,true);
            }
            if(File.Exists(SessionPath))File.Delete(SessionPath);File.Move(temporary,SessionPath);
        }
        public RankedSessionFile LoadSession(Func<string,RankedSessionFile> reader)
        {
            if(!File.Exists(SessionPath))return null;
            try{
                using(var stream=new FileStream(SessionPath,FileMode.Open,FileAccess.Read,FileShare.Read))using(var r=new BinaryReader(stream,Encoding.UTF8,true)){
                    if(!Verified(stream,true,out _))return null;stream.Position=0;
                    if(r.ReadString()!=SessionMagic)return null;return reader(r.ReadString());
                }
            }catch(Exception e) when(e is IOException || e is ArgumentException || e is FormatException || e is EndOfStreamException){RecoveryNotice=e.Message;return null;}
        }
        public void ClearSession(){try{if(File.Exists(SessionPath))File.Delete(SessionPath);}catch(IOException){}}

        // ---------- archives ----------
        void WriteArchive(LeagueSeasonArchive archive)
        {
            string target=ArchivePath(archive.seasonNumber),temporary=target+".pending";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.ReadWrite,FileShare.None))using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){
                writer.Write(ArchiveMagic);writer.Write(archive.seasonNumber);writer.Write(archive.population);writer.Write(BytesPerSeasonBot);writer.Write(archive.startTimestamp);writer.Write(archive.endTimestamp);writer.Flush();WriteRecords(stream,archive.records);AppendIntegrity(stream,false);
            }
            if(File.Exists(target))File.Delete(target);File.Move(temporary,target);
        }
        // Loads a complete saved archive (used once by the CPPS renumbering).
        public CompactBotSeason[] LoadArchiveRecords(LeagueSeasonArchive archive)
        {
            string path=ArchivePath(archive.seasonNumber);if(!File.Exists(path))return null;
            try{
                using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var reader=new BinaryReader(stream,Encoding.UTF8,true)){
                    if(!Verified(stream,false,out _))return null;stream.Position=0;
                    if(reader.ReadString()!=ArchiveMagic || reader.ReadInt32()!=archive.seasonNumber || reader.ReadInt32()!=archive.population || reader.ReadInt32()!=BytesPerSeasonBot)return null;
                    if(reader.ReadInt64()!=archive.startTimestamp || reader.ReadInt64()!=archive.endTimestamp)return null;
                    var records=new CompactBotSeason[archive.population];ReadRecords(stream,records);return records;
                }
            }catch(Exception e) when(e is IOException || e is EndOfStreamException){RecoveryNotice=e.Message;return null;}
        }
        public List<RacingSeasonHistory> ReadHistory(LocalLeagueState state,int botIndex)
        {
            var result=new List<RacingSeasonHistory>(20);
            foreach(var archive in state.archives){
                if(botIndex<0 || botIndex>=archive.population)continue;CompactBotSeason row;
                if(archive.records!=null)row=archive.records[botIndex];
                else{
                    string path=ArchivePath(archive.seasonNumber);if(!File.Exists(path))continue;
                    using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var reader=new BinaryReader(stream,Encoding.UTF8,true)){
                        var info=new FileInfo(path); bool cached;
                        lock(verifiedArchives) cached=verifiedArchives.TryGetValue(path,out var stamp) && stamp.length==info.Length && stamp.modified==info.LastWriteTimeUtc.Ticks;
                        if(!cached){if(!Verified(stream,false,out _))throw new IOException("Season archive checksum failed; damaged history was preserved.");lock(verifiedArchives)verifiedArchives[path]=(info.Length,info.LastWriteTimeUtc.Ticks);}
                        stream.Position=0;
                        if(reader.ReadString()!=ArchiveMagic || reader.ReadInt32()!=archive.seasonNumber || reader.ReadInt32()!=archive.population || reader.ReadInt32()!=BytesPerSeasonBot)throw new IOException("Invalid season archive header.");
                        if(reader.ReadInt64()!=archive.startTimestamp || reader.ReadInt64()!=archive.endTimestamp)throw new IOException("Invalid season archive epoch.");stream.Position+=botIndex*(long)BytesPerSeasonBot;
                        byte[] bytes=reader.ReadBytes(BytesPerSeasonBot);if(bytes.Length!=BytesPerSeasonBot)throw new IOException("Truncated season record.");row=MemoryMarshal.Read<CompactBotSeason>(bytes);
                    }
                }
                result.Add(new RacingSeasonHistory{seasonNumber=archive.seasonNumber,startingMmr=row.startingMmr,endingMmr=row.endingMmr,peakMmr=row.peakMmr,
                    startingRank=MmrTierUtility.GetDisplayName(row.startingMmr),endingRank=MmrTierUtility.GetDisplayName(row.endingMmr),peakRank=MmrTierUtility.GetDisplayName(row.peakMmr),
                    beyondPeakCounter=MmrTierUtility.GetBeyondCounter(row.peakMmr),selectedTitleId=LeagueBotCatalog.Title(new CompactLeagueBot{selectedTitle=row.selectedTitle,selectedTitleSeason=row.selectedTitleSeason}),nextStartingMmr=row.nextStartingMmr,races=row.races,wins=row.wins,podiums=row.podiums,bestLap=row.bestLap,
                    seasonStartTimestamp=archive.startTimestamp,seasonEndTimestamp=archive.endTimestamp});
            }return result;
        }

        // ---------- raw journal bytes (developer export/import) ----------
        public byte[] ReadNewestJournalBytes(){for(int k=0;k<2;k++){string path=Slot((int)((generation-k)&1));if(File.Exists(path))return File.ReadAllBytes(path);}return null;}
        public LocalLeagueState ParseJournalBytes(byte[] bytes,out long version,out bool macOk)
        {
            string temporary=Path.Combine(Path.GetTempPath(),"ha-import-"+Guid.NewGuid().ToString("N")+".journal");
            try{File.WriteAllBytes(temporary,bytes);bool v3=bytes.Length>20 && Encoding.UTF8.GetString(bytes,1,Math.Min(MagicV3.Length,bytes.Length-1))==MagicV3;
                return ReadJournal(temporary,v3?MagicV3:Magic,v3?BytesPerBotV3:BytesPerBot,out version,out macOk);}
            finally{try{File.Delete(temporary);}catch(IOException){}}
        }
        public void ForceGeneration(long minimum){if(minimum>generation)generation=minimum;}

        static void WriteRecords<T>(Stream stream,T[] records) where T:struct
        {if(!BitConverter.IsLittleEndian)throw new NotSupportedException("Compact format requires little-endian encoding.");stream.Write(MemoryMarshal.AsBytes(records.AsSpan()));}
        static void ReadRecords<T>(Stream stream,T[] records) where T:struct
        {var bytes=MemoryMarshal.AsBytes(records.AsSpan());int offset=0;while(offset<bytes.Length){int read=stream.Read(bytes.Slice(offset));if(read<=0)throw new EndOfStreamException();offset+=read;}}

        // Layout: body | SHA-256(body) [| HMAC-SHA256(key, body)]
        void AppendIntegrity(FileStream stream,bool mac)
        {
            stream.Flush();stream.Position=0;byte[] hash;using(var sha=SHA256.Create())hash=sha.ComputeHash(stream);
            byte[] tag=new byte[32];
            if(mac){byte[] key=SafeKey();if(key!=null){stream.Position=0;using(var h=new HMACSHA256(key))tag=h.ComputeHash(stream);}}
            stream.Position=stream.Length;stream.Write(hash,0,hash.Length);if(mac)stream.Write(tag,0,tag.Length);stream.Flush(true);
        }
        byte[] SafeKey(){try{var k=KeyProvider?.Key();return k!=null && k.Length>=16?k:null;}catch(Exception){return null;}}
        bool Verified(FileStream stream,bool mac,out bool macOk)
        {
            macOk=true;int trailer=mac?64:32;
            if(stream.Length<trailer+32)return false;long body=stream.Length-trailer;byte[] expected=new byte[32];stream.Position=body;if(stream.Read(expected,0,32)!=32)return false;stream.Position=0;
            using(var sha=SHA256.Create())using(var limit=new LimitedReadStream(stream,body)){byte[] actual=sha.ComputeHash(limit);if(!CryptographicOperations.FixedTimeEquals(actual,expected))return false;}
            if(!mac)return true;
            byte[] tag=new byte[32];stream.Position=body+32;if(stream.Read(tag,0,32)!=32)return false;
            byte[] key=SafeKey();if(key==null){macOk=false;return true;}
            stream.Position=0;using(var h=new HMACSHA256(key))using(var limit=new LimitedReadStream(stream,body)){macOk=CryptographicOperations.FixedTimeEquals(h.ComputeHash(limit),tag);}
            return true;
        }
        sealed class LimitedReadStream:Stream
        {
            readonly Stream source;long remaining;public LimitedReadStream(Stream stream,long length){source=stream;remaining=length;}
            public override int Read(byte[] buffer,int offset,int count){int read=source.Read(buffer,offset,(int)Math.Min(count,remaining));remaining-=read;return read;}
            public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
            public override void Flush(){}public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
        }

        static bool ValidV3(LocalLeagueState s)=>s!=null && s.bots!=null && s.bots.Length>=12 && s.population==s.bots.Length && s.human!=null && s.human.id=="player_human" && s.installationTimestamp>0;

        public static bool Valid(LocalLeagueState s)=>Validate(s)==null;
        public static string Validate(LocalLeagueState s)
        {
            if(s==null)return "null state";
            if(s.version!=LocalLeagueState.CurrentVersion)return "version";
            if(s.ratingScaleMaximum!=10000)return "rating scale";
            if(s.bots==null || s.bots.Length<12 || s.bots.Length>1000000 || s.population!=s.bots.Length)return "population";
            if(s.human==null || s.human.id!="player_human")return "human";
            if(s.installationTimestamp<=0 || s.lastKnownUtcTimestamp<s.installationTimestamp || s.lastKnownUtcTimestamp>DateTime.MaxValue.Ticks)return "clock";
            if(s.lastLeagueSimulationUtc<s.installationTimestamp || s.lastLeagueSimulationUtc>s.lastKnownUtcTimestamp)return "simulation cursor";
            if(s.currentSeasonNumber<1 || s.lastProcessedSeasonNumber!=s.currentSeasonNumber-1)return "season counters";
            if(s.archives==null || s.archives.Count>GlobalSeasonCalendar.MaximumSeason || s.botAliases==null || s.recentOpponentIds==null || s.ledger==null)return "collections";
            try{if(s.seasonStartTimestamp!=GlobalSeasonCalendar.Start(s.currentSeasonNumber) || s.seasonEndTimestamp!=GlobalSeasonCalendar.End(s.currentSeasonNumber))return "season calendar";}catch(ArgumentOutOfRangeException){return "season range";}catch(OverflowException){return "season overflow";}
            var h=s.human;if(h.currentMmr<0 || h.currentMmr>CompetitiveRating.MaximumMmr || h.peakMmr<h.currentMmr || h.seasonPeakMmr<h.currentMmr || h.history==null || h.history.Count>GlobalSeasonCalendar.MaximumSeason || h.unlockedTitleIds==null || h.placementGames<0 || h.placementGames>3)return "human values";
            if(!h.Ranked && h.currentMmr!=0)return "unranked human with visible MMR";
            if(s.seasonTransitionCursor>=0 && (s.pendingSeasonArchive?.records==null || s.pendingSeasonArchive.records.Length!=s.population || s.seasonTransitionCursor>s.population))return "season transition";
            if(s.botNames!=null && s.botNames.Length!=s.bots.Length)return "names";
            for(int i=0;i<s.bots.Length;i++){var b=s.bots[i];
                if(b.id!=i || b.currentMmr<0 || b.currentMmr>CompetitiveRating.MaximumMmr || b.peakMmr<b.currentMmr || b.seasonPeakMmr<b.currentMmr || b.races<0 || b.wins<0 || b.podiums<0 || b.style>6 || b.vehicle>=LeagueBotCatalog.Vehicles.Length || b.wheels>=LeagueBotCatalog.Wheels.Length || b.character>=LeagueBotCatalog.Characters.Length || b.accessory>=LeagueBotCatalog.Accessories.Length || !float.IsFinite(b.bestLap) || !float.IsFinite(b.seasonBestLap) || !float.IsFinite(b.habit))return "bot "+i;
                if(b.placementGames>3 || b.presence>4 || b.profile>3 || (!b.Ranked && b.currentMmr!=0))return "bot competitive "+i;
                if(b.presence==(byte)BotPresence.InMatch && b.activeRace==LeagueSimulator.NoRace)return "bot presence "+i;
            }
            return null;
        }
    }
}
