"""One-time source migration; existing rating/result logic is retained explicitly."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]/'Assets/AnimeHillRacing/Scripts/Racing3D/MMR'
path=ROOT/'LocalLeagueEngine.cs'
old=path.read_text(encoding='utf-8-sig')
if '// compact-v3' in old:raise SystemExit('Compact core already installed.')
rating=old[old.index('        public static void CalculateDeltas'):old.index('        public LeagueCompetitor[] SelectOpponents')]
record=old[old.index('        public static void RecordResult'):old.index('        public static void CalculateDeltas')]
random=old[old.index('    public sealed class LeagueRandom'):].replace('public sealed class LeagueRandom','public struct LeagueRandom')
engine=r'''using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    // compact-v3: only visible profiles and eleven selected opponents become rich views.
    public sealed class LocalLeagueEngine
    {
        public const int BotCount=10000;
        public LocalLeagueState State {get;}
        public LocalLeagueConfiguration Configuration {get;}
        public LeagueRankIndex Index {get;}
        public int Population=>State.bots.Length;
        public int EventsPerHour=>Math.Max(1,(int)Math.Min(int.MaxValue,(long)Configuration.eventsPerHour*Population/1000));
        public long GainCount {get;private set;}
        public long LossCount {get;private set;}
        readonly int[] match=new int[12],mmrs=new int[12],positions=new int[12],deltas=new int[12],candidates=new int[512];
        readonly float[] uncertainty=new float[12];
        readonly double[] performance=new double[12],rawDelta=new double[12],candidateWeights=new double[512];
        readonly Dictionary<int,string> aliases=new Dictionary<int,string>();

        public LocalLeagueEngine(LocalLeagueState state,LocalLeagueConfiguration config=null)
        {
            State=state??throw new ArgumentNullException(nameof(state));Configuration=config??new LocalLeagueConfiguration();
            if(state.bots==null || state.bots.Length<12 || state.bots.Length>1000000 || state.human==null)throw new ArgumentException("Invalid compact population.");
            foreach(var alias in state.botAliases)aliases[alias.index]=alias.displayName;
            Index=new LeagueRankIndex(state);
        }
        public static LocalLeagueState Create(long utcTicks,int seed,int population=BotCount)
        {
            if(population<12 || population>1000000)throw new ArgumentOutOfRangeException(nameof(population));
            var rng=new LeagueRandom((uint)seed);var state=new LocalLeagueState{populationSeed=seed,population=population,
                installationTimestamp=utcTicks,seasonStartTimestamp=utcTicks,seasonEndTimestamp=LocalSeasonEngine.Boundary(utcTicks,1),
                lastKnownUtcTimestamp=utcTicks,lastLeagueSimulationUtc=utcTicks,human=new LeagueCompetitor{id="player_human",displayName="PLAYER"},
                bots=new CompactLeagueBot[population]};
            for(int i=0;i<population;i++){
                float talent=Clamp01(.40f+.19f*(float)rng.Normal());int activityRoll=rng.Next(100);
                state.bots[i]=new CompactLeagueBot{id=i,personalitySeed=(int)(rng.NextUInt()&0x7fffffff),latent=CompactLeagueBot.Pack01(talent),
                    consistency=CompactLeagueBot.Pack01(Clamp01(.68f+talent*.22f+(float)rng.Normal()*.13f)),
                    learning=CompactLeagueBot.Pack01((float)rng.Unit()),uncertainty=ushort.MaxValue,style=(byte)rng.Next(7),
                    activity=(byte)(activityRoll<10?0:activityRoll<35?64:activityRoll<75?160:255),
                    vehicle=(byte)rng.Next(3),wheels=0,character=(byte)rng.Next(4),accessory=(byte)rng.Next(3)};
                LeagueBotCatalog.UpdateTitles(ref state.bots[i]);
            }
            return state;
        }
        public LeagueCompetitor View(int index){if(index==Population)return State.human;aliases.TryGetValue(index,out string name);return LeagueBotCatalog.View(State,index,name);}
        public bool TryIndex(string id,out int index){return LeagueBotCatalog.TryIndex(State.populationSeed,id,out index) && index>=0 && index<Population;}
        public void RefreshHumanIndex()=>Index.Update(Population,State.human.currentMmr);
        public LeagueCompetitor[] Page(int first,int count){RefreshHumanIndex();first=Math.Max(0,Math.Min(Index.Count,first));count=Math.Max(0,Math.Min(count,Index.Count-first));var result=new LeagueCompetitor[count];for(int i=0;i<count;i++)result[i]=View(Index.AtRank(first+i));return result;}
        public int Rank(string id){RefreshHumanIndex();if(id=="player_human")return Index.RankOf(Population);return TryIndex(id,out int index)?Index.RankOf(index):-1;}

        bool Active(int index,ref LeagueRandom rng){var b=State.bots[index];double weight=.06+.94*b.activity/255.0;return rng.Unit()<weight;}
        public void SimulateOne()
        {
            if(State.seasonTransitionCursor>=0)throw new InvalidOperationException("Simulation is frozen during a season transaction.");
            var rng=new LeagueRandom(unchecked((uint)State.populationSeed^(uint)State.simulationEventNumber*0x9e3779b9u^(uint)(State.simulationEventNumber>>32)));
            int anchor;do{anchor=rng.Next(Population);}while(!Active(anchor,ref rng));match[0]=anchor;
            int anchorRank=Index.RankOf(anchor),radius=Math.Max(24,Math.Min(320,Population/35));
            for(int i=1;i<12;i++){
                int candidate,attempts=0;
                do{candidate=rng.Unit()<.16 || attempts++>50?rng.Next(Population):Index.AtRank(Math.Max(0,Math.Min(Population,anchorRank+rng.Next(radius*2+1)-radius)));}
                while(candidate==Population || Contains(match,i,candidate) || !Active(candidate,ref rng));match[i]=candidate;
            }
            for(int i=0;i<12;i++){
                var b=State.bots[match[i]];mmrs[i]=b.currentMmr;uncertainty[i]=b.Uncertainty;
                double developed=Math.Min(1.1,b.LatentSkill+b.LearningPotential*.045*(1-Math.Exp(-b.races/1200.0)));
                double risk=((uint)b.personalitySeed&65535)/65535.0;
                double specialty=Math.Max(0,Math.Min(1,b.LatentSkill+(((uint)b.personalitySeed>>16)/32767.0-.5)*.10));
                double ability=.86*developed+.14*specialty,noise=260+1100*(1-b.Consistency)+280*risk;
                performance[i]=ability*ability*16000+rng.Normal()*noise+b.RecentForm*150;
                if(rng.Unit()<.012+(1-b.Consistency)*.018)performance[i]-=1700+rng.Unit()*1200;
            }
            for(int i=0;i<12;i++){int place=1;for(int j=0;j<12;j++)if(performance[j]>performance[i] || (performance[j]==performance[i] && j<i))place++;positions[i]=place;}
            CalculateDeltas(mmrs,positions,uncertainty,Configuration,deltas,rawDelta);
            long net=0;
            for(int i=0;i<12;i++){
                int index=match[i];var b=State.bots[index];int before=b.currentMmr;
                RecordBot(ref b,positions[i],deltas[i],0);
                if(b.currentMmr>before)GainCount++;if(b.currentMmr<before)LossCount++;net+=b.currentMmr-before;
                if(rng.Unit()<b.LatentSkill*.5)b.shortcutSuccesses++;
                if(rng.Unit()<b.LatentSkill*.4)b.aerialSuccesses++;
                if(rng.Unit()<b.LatentSkill*b.Consistency)b.landingSuccesses++;
                LeagueBotCatalog.UpdateTitles(ref b);
                if((b.personalitySeed&3)!=0 && b.races%19==0){int title=1+rng.Next(6);if((b.titleFlags&(1<<title))!=0)b.selectedTitle=(ushort)title;}
                if(b.races%64==0 && rng.Unit()<.35)UpdatePreference(ref b,ref rng);
                b.lastSimulationBucket=(int)Math.Min(int.MaxValue,State.simulationEventNumber/64);State.bots[index]=b;Index.Update(index,b.currentMmr);
            }
            State.simulationRatingCreatedAtFloor+=Math.Max(0,net);State.simulationEventNumber++;
        }
        static void RecordBot(ref CompactLeagueBot b,int position,int delta,float lap)
        {
            b.currentMmr=(int)Math.Max(0L,Math.Min(int.MaxValue,(long)b.currentMmr+delta));b.peakMmr=Math.Max(b.peakMmr,b.currentMmr);b.seasonPeakMmr=Math.Max(b.seasonPeakMmr,b.currentMmr);
            b.races++;b.seasonRaces++;if(position==1){b.wins++;b.seasonWins++;}if(position<=3){b.podiums++;b.seasonPodiums++;}
            b.uncertainty=CompactLeagueBot.Pack01(Math.Max(.12f,b.Uncertainty*.975f));b.form=CompactLeagueBot.PackForm(b.RecentForm*.85f+(6.5f-position)/5.5f*.15f);
            if(lap>0 && float.IsFinite(lap)){if(b.bestLap<=0 || lap<b.bestLap)b.bestLap=lap;if(b.seasonBestLap<=0 || lap<b.seasonBestLap)b.seasonBestLap=lap;}
        }
        public void RecordLiveResult(int index,int oldMmr,int position,int delta,float lap,int shortcuts,int aerials,int landings)
        {
            var b=State.bots[index];b.currentMmr=oldMmr;RecordBot(ref b,position,delta,lap);b.shortcutSuccesses+=Math.Max(0,shortcuts);b.aerialSuccesses+=Math.Max(0,aerials);b.landingSuccesses+=Math.Max(0,landings);
            LeagueBotCatalog.UpdateTitles(ref b);State.bots[index]=b;Index.Update(index,b.currentMmr);
        }
        static void UpdatePreference(ref CompactLeagueBot b,ref LeagueRandom rng)
        {
            // Persistent adaptation, not a reroll at matchmaking. Even the top
            // bracket retains specialists and near-meta choices.
            double level=Math.Min(1,b.currentMmr/10000.0),metaChance=.03+.89*level*level;
            if(rng.Unit()<metaChance){b.vehicle=3;b.wheels=5;}
            else if(level>.3){byte[] vehicles={2,3,4,6};byte[] wheels={1,2,3,4};b.vehicle=vehicles[rng.Next(4)];b.wheels=wheels[rng.Next(4)];}
        }
'''
engine+=record+rating+r'''
        public LeagueCompetitor[] SelectOpponents(int humanMmr,int seed,bool remember)
        {
            RefreshHumanIndex();var rng=new LeagueRandom((uint)seed^(remember?(uint)State.matchmakingSequence*0x85ebca6bu:0));
            double span=humanMmr>=10000?900:240+Math.Min(10000,humanMmr)*.035;int first=0,count=0;
            for(int step=0;step<7;step++){first=Index.CountHigher((int)Math.Min(int.MaxValue,humanMmr+span));int end=Index.CountHigher((int)Math.Max(0,humanMmr-span),true);count=end-first;if(count>=Math.Min(96,Population))break;span=Math.Min(4500,span*1.55);}
            if(count<12){first=0;count=Index.Count;}
            int candidateCount=Math.Min(candidates.Length,count-1),filled=0;
            while(filled<candidateCount){int candidate=Index.AtRank(first+rng.Next(count));if(candidate==Population || Contains(candidates,filled,candidate))continue;candidates[filled++]=candidate;}
            for(int i=0;i<filled;i++){
                var b=State.bots[candidates[i]];double distance=Math.Abs((double)b.currentMmr-humanMmr)/Math.Max(1,span*.60);
                double activity=.25+.75*b.activity/255.0;candidateWeights[i]=Math.Exp(-distance*distance*.5)*activity*(.9+.2*b.Uncertainty);
                string id=LeagueBotCatalog.Id(State.populationSeed,candidates[i]);if(State.recentOpponentIds.Contains(id))candidateWeights[i]*=.16;
            }
            var result=new LeagueCompetitor[11];
            for(int slot=0;slot<11;slot++){double sum=0;for(int i=0;i<filled;i++)sum+=candidateWeights[i];double roll=rng.Unit()*sum;int chosen=filled-1;for(int i=0;i<filled;i++){roll-=candidateWeights[i];if(roll<0){chosen=i;break;}}
                result[slot]=View(candidates[chosen]);candidateWeights[chosen]=0;}
            if(remember){State.matchmakingSequence++;foreach(var b in result)State.recentOpponentIds.Add(b.id);while(State.recentOpponentIds.Count>44)State.recentOpponentIds.RemoveAt(0);}
            return result;
        }
        static bool Contains(int[] values,int length,int value){for(int i=0;i<length;i++)if(values[i]==value)return true;return false;}
        static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));
    }

'''+random
path.write_text(engine,encoding='utf-8')
(ROOT/'LocalSeasonEngine.cs').write_text(r'''using System;
namespace AnimeHillRacing.Racing3D
{
    public static class LocalSeasonEngine
    {
        public static long Boundary(long ticks,int season)=>new DateTime(ticks,DateTimeKind.Utc).AddMonths(checked(season*3)).Ticks;
        public static long AcceptUtc(LocalLeagueState state,long ticks){long safe=Math.Max(state.installationTimestamp,Math.Max(ticks,state.lastKnownUtcTimestamp));state.lastKnownUtcTimestamp=safe;return safe;}
        public static bool AdvanceChunk(LocalLeagueState state,LocalLeagueConfiguration config,long utc,int maximumRecords,LeagueRankIndex index=null)
        {
            if(utc<state.seasonEndTimestamp)return false;
            if(state.lastProcessedSeasonNumber>=state.currentSeasonNumber)throw new InvalidOperationException("Duplicate season transition.");
            if(state.seasonTransitionCursor<0){state.seasonTransitionCursor=0;state.pendingSeasonArchive=new LeagueSeasonArchive{seasonNumber=state.currentSeasonNumber,population=state.bots.Length,startTimestamp=state.seasonStartTimestamp,endTimestamp=state.seasonEndTimestamp,records=new CompactBotSeason[state.bots.Length]};}
            int end=Math.Min(state.bots.Length,state.seasonTransitionCursor+Math.Max(1,maximumRecords));
            while(state.seasonTransitionCursor<end){int i=state.seasonTransitionCursor;var b=state.bots[i];int reset=config.ResetMmr(b.currentMmr),peak=Math.Max(b.currentMmr,b.seasonPeakMmr);
                state.pendingSeasonArchive.records[i]=new CompactBotSeason{startingMmr=b.seasonStartingMmr,endingMmr=b.currentMmr,peakMmr=peak,nextStartingMmr=reset,races=b.seasonRaces,wins=b.seasonWins,podiums=b.seasonPodiums,bestLap=b.seasonBestLap,selectedTitle=b.selectedTitle,selectedTitleSeason=b.selectedTitleSeason};
                b.peakMmr=Math.Max(b.peakMmr,peak);if(b.seasonRaces>0 && (b.personalitySeed&3)!=0){b.selectedTitle=(ushort)(32+(int)MmrTierUtility.GetTier(peak));b.selectedTitleSeason=state.currentSeasonNumber;}
                b.currentMmr=b.seasonStartingMmr=b.seasonPeakMmr=reset;b.seasonRaces=b.seasonWins=b.seasonPodiums=0;b.seasonBestLap=0;b.uncertainty=CompactLeagueBot.Pack01(Math.Min(1,b.Uncertainty+.12f));
                state.bots[i]=b;index?.Update(i,reset);state.seasonTransitionCursor++;
            }
            if(state.seasonTransitionCursor<state.bots.Length)return true;
            CloseHuman(state.human,state,config);index?.Update(state.bots.Length,state.human.currentMmr);state.archives.Add(state.pendingSeasonArchive);
            while(state.archives.Count>Math.Max(1,Math.Min(20,config.historyLimit)))state.archives.RemoveAt(0);
            state.pendingSeasonArchive=null;state.seasonTransitionCursor=-1;int season=state.currentSeasonNumber;state.lastProcessedSeasonNumber=season;state.currentSeasonNumber=season+1;
            state.seasonStartTimestamp=Boundary(state.installationTimestamp,season);state.seasonEndTimestamp=Boundary(state.installationTimestamp,season+1);return true;
        }
        // Pure harness helper. Production processes bounded chunks and commits
        // each completed archive before starting another missed season.
        public static bool AdvanceOne(LocalLeagueState state,LocalLeagueConfiguration config,long utc)=>utc>=state.seasonEndTimestamp && AdvanceChunk(state,config,utc,state.bots.Length);
        static void CloseHuman(LeagueCompetitor c,LocalLeagueState state,LocalLeagueConfiguration config)
        {
            int reset=config.ResetMmr(c.currentMmr),peak=Math.Max(c.currentMmr,c.seasonPeakMmr);c.peakMmr=Math.Max(c.peakMmr,peak);
            c.history.Add(new RacingSeasonHistory{seasonNumber=state.currentSeasonNumber,startingMmr=c.seasonStartingMmr,endingMmr=c.currentMmr,peakMmr=peak,
                startingRank=MmrTierUtility.GetDisplayName(c.seasonStartingMmr),endingRank=MmrTierUtility.GetDisplayName(c.currentMmr),peakRank=MmrTierUtility.GetDisplayName(peak),
                beyondPeakCounter=MmrTierUtility.GetBeyondCounter(peak),races=c.seasonRaces,wins=c.seasonWins,podiums=c.seasonPodiums,bestLap=c.seasonBestLap,
                nextStartingMmr=reset,seasonStartTimestamp=state.seasonStartTimestamp,seasonEndTimestamp=state.seasonEndTimestamp});
            while(c.history.Count>Math.Max(1,Math.Min(20,config.historyLimit)))c.history.RemoveAt(0);
            if(c.seasonRaces>0)RacingTitleCatalog.Unlock(c,"season:"+state.currentSeasonNumber+":"+(int)MmrTierUtility.GetTier(peak));
            RacingTitleCatalog.TrimSeasonTitles(c,Math.Max(1,state.currentSeasonNumber-config.historyLimit+1));c.currentMmr=c.seasonStartingMmr=c.seasonPeakMmr=reset;
            c.seasonRaces=c.seasonWins=c.seasonPodiums=0;c.seasonBestLap=0;c.uncertainty=Math.Min(1,c.uncertainty+.12f);
        }
    }
}
''',encoding='utf-8')
print('Installed compact engine and bounded season transition core.')
