"""Install V3 packed journals; old V2 journals are read-only migration inputs."""
from pathlib import Path
path=Path(__file__).resolve().parents[2]/'Assets/AnimeHillRacing/Scripts/Racing3D/MMR/LocalLeagueStore.cs'
if 'HA_LOCAL_COMPACT_V3' in path.read_text(encoding='utf-8-sig'):
    raise SystemExit('Completed one-time migration; preserving the integrated V3 store.')
path.write_text(r'''using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AnimeHillRacing.Racing3D
{
    public sealed class LocalLeagueStore
    {
        const string Magic="HA_LOCAL_COMPACT_V3",ArchiveMagic="HA_SEASON_COMPACT_V3";
        readonly string directory;
        readonly Func<LocalLeagueState,string> serialize;
        readonly Func<string,LocalLeagueState> deserialize;
        readonly Func<string,LegacyLocalLeagueState> legacyReader;
        readonly int configuredPopulation;
        long generation;
        public bool ReadOnlyProtection {get;private set;}
        public bool MigratedLegacy {get;private set;}
        public string RecoveryNotice {get;private set;}="";
        public static int BytesPerBot=>Marshal.SizeOf<CompactLeagueBot>();
        public static int BytesPerSeasonBot=>Marshal.SizeOf<CompactBotSeason>();
        public LocalLeagueStore(string path,Func<LocalLeagueState,string> writer,Func<string,LocalLeagueState> reader,Func<string,LegacyLocalLeagueState> oldReader=null,int population=10000)
        {directory=Path.GetFullPath(path);serialize=writer;deserialize=reader;legacyReader=oldReader;configuredPopulation=population;}
        string Slot(int slot)=>Path.Combine(directory,"league-v3-"+slot+".journal");
        string ArchivePath(int season)=>Path.Combine(directory,"seasons","season-"+season+".bin");
        public LocalLeagueState Load()
        {
            LocalLeagueState best=null;bool existed=false;
            for(int slot=0;slot<2;slot++){
                string path=Slot(slot);if(!File.Exists(path))continue;existed=true;
                try{
                    using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var reader=new BinaryReader(stream,Encoding.UTF8,true)){
                        string magic=reader.ReadString();if(magic!=Magic){if(magic.StartsWith("HA_LOCAL_COMPACT_V",StringComparison.Ordinal))ReadOnlyProtection=true;continue;}
                        if(!Verified(stream))continue;stream.Position=reader.BaseStream.Position=Encoding.UTF8.GetByteCount(Magic)+1;
                        long version=reader.ReadInt64();int recordSize=reader.ReadInt32(),population=reader.ReadInt32(),jsonSize=reader.ReadInt32(),pendingCount=reader.ReadInt32();
                        if(version<1 || recordSize!=BytesPerBot || population<12 || population>1000000 || jsonSize<2 || jsonSize>4000000 || (pendingCount!=0 && pendingCount!=population))continue;
                        string json=Encoding.UTF8.GetString(reader.ReadBytes(jsonSize));var state=deserialize(json);
                        if(state==null || state.version!=3 || state.population!=population)continue;
                        state.bots=new CompactLeagueBot[population];ReadRecords(stream,state.bots);
                        if(pendingCount>0){if(state.pendingSeasonArchive==null)continue;state.pendingSeasonArchive.records=new CompactBotSeason[population];ReadRecords(stream,state.pendingSeasonArchive.records);}
                        if(!Valid(state))continue;
                        if(version>generation){generation=version;best=state;}
                    }
                }catch(Exception e) when(e is IOException || e is ArgumentException || e is FormatException || e is UnauthorizedAccessException){RecoveryNotice=e.Message;}
            }
            if(best==null && !existed && legacyReader!=null)best=LoadLegacy();
            if(ReadOnlyProtection)RecoveryNotice="A newer local progression save was preserved. Updating the game is required to save progression.";
            else if(best==null && existed){ReadOnlyProtection=true;RecoveryNotice="Both compact journal slots are damaged; originals were preserved for recovery.";}
            return best;
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
                    if(!long.TryParse(p[1],out long g) || g<1)continue;var value=legacyReader(p[3]);if(value==null || value.version!=2)continue;
                    if(g>bestGeneration){bestGeneration=g;best=value;}
                }catch(Exception e) when(e is IOException || e is ArgumentException || e is FormatException){RecoveryNotice=e.Message;}
            }
            if(ReadOnlyProtection)return null;
            if(best==null){if(existed){ReadOnlyProtection=true;RecoveryNotice="Legacy journal damage was preserved for recovery.";}return null;}
            var result=LegacyLeagueMigration.Convert(best,configuredPopulation);MigratedLegacy=true;RecoveryNotice="Existing 1000 identities and installation epoch migrated once to the 10k rating scale and compact population.";return result;
        }
        public bool Save(LocalLeagueState state)
        {
            if(ReadOnlyProtection)return false;if(!Valid(state))throw new ArgumentException("Refusing to save invalid compact league state.");
            Directory.CreateDirectory(directory);Directory.CreateDirectory(Path.Combine(directory,"seasons"));
            foreach(var archive in state.archives)if(archive.records!=null)WriteArchive(archive);
            long next=checked(generation+1);string target=Slot((int)(next&1)),temporary=target+".pending";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.ReadWrite,FileShare.None))using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){
                byte[] json=Encoding.UTF8.GetBytes(serialize(state));writer.Write(Magic);writer.Write(next);writer.Write(BytesPerBot);writer.Write(state.bots.Length);writer.Write(json.Length);
                writer.Write(state.pendingSeasonArchive?.records?.Length??0);writer.Write(json);writer.Flush();WriteRecords(stream,state.bots);
                if(state.pendingSeasonArchive?.records!=null)WriteRecords(stream,state.pendingSeasonArchive.records);AppendHash(stream);
            }
            if(File.Exists(target))File.Delete(target);File.Move(temporary,target);generation=next;
            // Only files owned by this store, with a literal parsed season number,
            // are pruned after the new generation is durably committed.
            int minimum=state.archives.Count==0?0:state.archives[0].seasonNumber;
            foreach(string path in Directory.GetFiles(Path.Combine(directory,"seasons"),"season-*.bin")){
                string stem=Path.GetFileNameWithoutExtension(path);if(int.TryParse(stem.Substring(7),out int season) && season<minimum && Path.GetFullPath(path).StartsWith(directory+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))File.Delete(path);
            }
            return true;
        }
        void WriteArchive(LeagueSeasonArchive archive)
        {
            string target=ArchivePath(archive.seasonNumber),temporary=target+".pending";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.ReadWrite,FileShare.None))using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){
                writer.Write(ArchiveMagic);writer.Write(archive.seasonNumber);writer.Write(archive.population);writer.Write(BytesPerSeasonBot);writer.Write(archive.startTimestamp);writer.Write(archive.endTimestamp);writer.Flush();WriteRecords(stream,archive.records);AppendHash(stream);
            }
            if(File.Exists(target))File.Delete(target);File.Move(temporary,target);
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
                        if(reader.ReadString()!=ArchiveMagic || reader.ReadInt32()!=archive.seasonNumber || reader.ReadInt32()!=archive.population || reader.ReadInt32()!=BytesPerSeasonBot)throw new IOException("Invalid season archive header.");
                        reader.ReadInt64();reader.ReadInt64();stream.Position+=botIndex*(long)BytesPerSeasonBot;
                        byte[] bytes=reader.ReadBytes(BytesPerSeasonBot);if(bytes.Length!=BytesPerSeasonBot)throw new IOException("Truncated season record.");row=MemoryMarshal.Read<CompactBotSeason>(bytes);
                    }
                }
                result.Add(new RacingSeasonHistory{seasonNumber=archive.seasonNumber,startingMmr=row.startingMmr,endingMmr=row.endingMmr,peakMmr=row.peakMmr,
                    startingRank=MmrTierUtility.GetDisplayName(row.startingMmr),endingRank=MmrTierUtility.GetDisplayName(row.endingMmr),peakRank=MmrTierUtility.GetDisplayName(row.peakMmr),
                    beyondPeakCounter=MmrTierUtility.GetBeyondCounter(row.peakMmr),nextStartingMmr=row.nextStartingMmr,races=row.races,wins=row.wins,podiums=row.podiums,bestLap=row.bestLap,
                    seasonStartTimestamp=archive.startTimestamp,seasonEndTimestamp=archive.endTimestamp});
            }return result;
        }
        static void WriteRecords<T>(Stream stream,T[] records) where T:struct
        {if(!BitConverter.IsLittleEndian)throw new NotSupportedException("Compact format requires little-endian encoding.");stream.Write(MemoryMarshal.AsBytes(records.AsSpan()));}
        static void ReadRecords<T>(Stream stream,T[] records) where T:struct
        {var bytes=MemoryMarshal.AsBytes(records.AsSpan());int offset=0;while(offset<bytes.Length){int read=stream.Read(bytes.Slice(offset));if(read<=0)throw new EndOfStreamException();offset+=read;}}
        static void AppendHash(FileStream stream){stream.Flush();stream.Position=0;byte[] hash;using(var sha=SHA256.Create())hash=sha.ComputeHash(stream);stream.Position=stream.Length;stream.Write(hash,0,hash.Length);stream.Flush(true);}
        static bool Verified(FileStream stream)
        {
            if(stream.Length<64)return false;long body=stream.Length-32;byte[] expected=new byte[32];stream.Position=body;if(stream.Read(expected,0,32)!=32)return false;stream.Position=0;
            using(var sha=SHA256.Create())using(var limit=new LimitedReadStream(stream,body)){byte[] actual=sha.ComputeHash(limit);return CryptographicOperations.FixedTimeEquals(actual,expected);}
        }
        sealed class LimitedReadStream:Stream
        {
            readonly Stream source;long remaining;public LimitedReadStream(Stream stream,long length){source=stream;remaining=length;}
            public override int Read(byte[] buffer,int offset,int count){int read=source.Read(buffer,offset,(int)Math.Min(count,remaining));remaining-=read;return read;}
            public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
            public override void Flush(){}public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
        }
        public static bool Valid(LocalLeagueState s)
        {
            if(s==null || s.version!=3 || s.ratingScaleMaximum!=10000 || s.bots==null || s.bots.Length<12 || s.bots.Length>1000000 || s.population!=s.bots.Length || s.human==null || s.human.id!="player_human" || s.installationTimestamp<=0 || s.lastKnownUtcTimestamp<s.installationTimestamp || s.lastKnownUtcTimestamp>DateTime.MaxValue.Ticks || s.lastLeagueSimulationUtc<s.installationTimestamp || s.lastLeagueSimulationUtc>s.lastKnownUtcTimestamp || s.currentSeasonNumber<1 || s.lastProcessedSeasonNumber!=s.currentSeasonNumber-1 || s.archives==null || s.archives.Count>20 || s.botAliases==null || s.recentOpponentIds==null)return false;
            try{if(s.seasonStartTimestamp!=LocalSeasonEngine.Boundary(s.installationTimestamp,s.currentSeasonNumber-1) || s.seasonEndTimestamp!=LocalSeasonEngine.Boundary(s.installationTimestamp,s.currentSeasonNumber))return false;}catch(ArgumentOutOfRangeException){return false;}catch(OverflowException){return false;}
            var h=s.human;if(h.currentMmr<0 || h.peakMmr<h.currentMmr || h.seasonPeakMmr<h.currentMmr || h.history==null || h.history.Count>20 || h.unlockedTitleIds==null)return false;
            if(s.seasonTransitionCursor>=0 && (s.pendingSeasonArchive?.records==null || s.pendingSeasonArchive.records.Length!=s.population || s.seasonTransitionCursor>s.population))return false;
            for(int i=0;i<s.bots.Length;i++){var b=s.bots[i];if(b.id!=i || b.currentMmr<0 || b.peakMmr<b.currentMmr || b.seasonPeakMmr<b.currentMmr || b.races<0 || b.wins<0 || b.podiums<0 || b.style>6 || b.vehicle>=LeagueBotCatalog.Vehicles.Length || b.wheels>=LeagueBotCatalog.Wheels.Length || b.character>=LeagueBotCatalog.Characters.Length || b.accessory>=LeagueBotCatalog.Accessories.Length || !float.IsFinite(b.bestLap) || !float.IsFinite(b.seasonBestLap))return false;}
            return true;
        }
    }
}
''',encoding='utf-8')
print('Installed compact V3 journal and archive storage.')
