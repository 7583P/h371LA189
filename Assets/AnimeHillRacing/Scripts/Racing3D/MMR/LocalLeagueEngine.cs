using System;
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
        static readonly byte[] AlternativeVehicles={2,3,4,6},AlternativeWheels={1,2,3,4};

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
                byte Attribute(float center)=>CompactLeagueBot.PackByte(Clamp01(center+(float)rng.Normal()*.13f));
                state.bots[i]=new CompactLeagueBot{id=i,personalitySeed=(int)(rng.NextUInt()&0x7fffffff),latent=CompactLeagueBot.Pack01(talent),
                    consistency=CompactLeagueBot.Pack01(Clamp01(.68f+talent*.22f+(float)rng.Normal()*.13f)),
                    learning=CompactLeagueBot.Pack01((float)rng.Unit()),uncertainty=ushort.MaxValue,style=(byte)rng.Next(7),
                    activity=(byte)(activityRoll<10?0:activityRoll<35?64:activityRoll<75?160:255),
                    vehicle=(byte)rng.Next(3),wheels=0,character=(byte)rng.Next(4),accessory=(byte)rng.Next(3),
                    aggression=CompactLeagueBot.PackByte((float)rng.Unit()),risk=CompactLeagueBot.PackByte((float)rng.Unit()),
                    drift=Attribute(talent),aerial=Attribute(talent),shortcut=Attribute(talent),item=Attribute(talent),racecraft=Attribute(talent),recovery=Attribute(.45f+talent*.4f)};
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
            int nearbyFirst=Math.Max(0,anchorRank-radius),nearbyLast=Math.Min(Population,anchorRank+radius);
            for(int i=1;i<12;i++){
                int candidate,attempts=0;
                // Sample the truncated window uniformly. Clamping an unbounded
                // rank would oversample the best/last bot hundreds of times.
                do{candidate=rng.Unit()<.16 || attempts++>50?rng.Next(Population):Index.AtRank(nearbyFirst+rng.Next(nearbyLast-nearbyFirst+1));}
                while(candidate==Population || Contains(match,i,candidate) || !Active(candidate,ref rng));match[i]=candidate;
            }
            for(int i=0;i<12;i++){
                var b=State.bots[match[i]];mmrs[i]=b.currentMmr;uncertainty[i]=b.Uncertainty;
                double developed=Math.Min(1.1,b.LatentSkill+b.LearningPotential*.045*(1-Math.Exp(-b.races/1200.0)));
                double risk=b.risk/255.0;
                double specialty=(b.drift+b.aerial+b.shortcut+b.item+b.racecraft)/(5*255.0);
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
            else if(level>.3){b.vehicle=AlternativeVehicles[rng.Next(4)];b.wheels=AlternativeWheels[rng.Next(4)];}
        }
        public static void RecordResult(LeagueCompetitor competitor, int position, int delta, float lap)
        {
            competitor.currentMmr = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)competitor.currentMmr + delta));
            competitor.peakMmr = Math.Max(competitor.peakMmr, competitor.currentMmr);
            competitor.seasonPeakMmr = Math.Max(competitor.seasonPeakMmr, competitor.currentMmr);
            competitor.races++; competitor.seasonRaces++;
            if (position == 1) { competitor.wins++; competitor.seasonWins++; }
            if (position <= 3) { competitor.podiums++; competitor.seasonPodiums++; }
            competitor.uncertainty = Math.Max(.12f, competitor.uncertainty * .975f);
            competitor.recentForm = competitor.recentForm * .85f + (6.5f - position) / 5.5f * .15f;
            if (lap > 0 && !float.IsNaN(lap) && !float.IsInfinity(lap)) {
                if (competitor.bestLap <= 0 || lap < competitor.bestLap) competitor.bestLap = lap;
                if (competitor.seasonBestLap <= 0 || lap < competitor.seasonBestLap) competitor.seasonBestLap = lap;
            }
        }

        public static void CalculateDeltas(int[] ratings, int[] places, float[] deviations, LocalLeagueConfiguration config, int[] output, double[] scratch = null)
        {
            int n = ratings.Length;
            if (n < 2 || places.Length != n || output.Length != n || deviations.Length != n) throw new ArgumentException("Invalid rating lobby.");
            scratch = scratch ?? new double[n];
            double uncertaintyMean = 0; for (int i = 0; i < n; i++) uncertaintyMean += deviations[i];
            double k = config.establishedK + config.uncertaintyK * Math.Max(0, Math.Min(1, uncertaintyMean / n));
            int winners = Math.Min(5, Math.Max(1, (n - 1) / 2));
            double positiveWeight=0, negativeWeight=0;
            for (int i = 0; i < n; i++) {
                double residual = 0;
                for (int j = 0; j < n; j++) if (i != j) {
                    double scale=config.ratingScale*(1+.18*Math.Max(0,Math.Min(1,deviations[j])));
                    double expected = 1 / (1 + Math.Pow(10, Math.Max(-20, Math.Min(20, ((double)ratings[j] - ratings[i]) / scale))));
                    double actual = places[i] < places[j] ? 1 : places[i] == places[j] ? .5 : 0;
                    residual += actual - expected;
                }
                bool gain=places[i]<=winners;double sign=gain?1:-1;
                double placement=gain?(winners+1.0-places[i])/winners:(places[i]-winners)/(double)(n-winners);
                double expectationModifier=Math.Max(.15,Math.Min(2,1+sign*residual/(n-1)*1.8));
                double individualUncertainty=.7+.6*Math.Max(0,Math.Min(1,deviations[i]));
                scratch[i]=sign*placement*expectationModifier*individualUncertainty;
                if(gain)positiveWeight+=scratch[i];else negativeWeight-=scratch[i];
            }
            if(positiveWeight<=0 || negativeWeight<=0)throw new ArgumentException("Rating positions must include both placement domains.");
            int budget=Math.Max(n,(int)Math.Round(k,MidpointRounding.AwayFromZero));int positive=0,negative=0;
            for(int i=0;i<n;i++){
                bool gain=scratch[i]>0;scratch[i]=Math.Abs(scratch[i])*budget/(gain?positiveWeight:negativeWeight);
                int magnitude=Math.Max(1,(int)Math.Floor(scratch[i]));output[i]=gain?magnitude:-magnitude;
                if(gain)positive+=magnitude;else negative+=magnitude;
            }
            // Allocate the SAME finite pool on either side. Pairwise expectations
            // change magnitude, never turn sixth place into a gain. Only the
            // explicit zero-MMR floor can inject rating after this calculation.
            for(int domain=0;domain<2;domain++){
                int total=domain==0?positive:negative,sign=domain==0?1:-1;
                while(total!=budget){
                    int direction=total<budget?1:-1,best=-1;double bestResidual=double.NegativeInfinity;
                    for(int i=0;i<n;i++)if(Math.Sign(output[i])==sign && (direction>0 || Math.Abs(output[i])>1)){
                        double residual=(scratch[i]-Math.Abs(output[i]))*direction;
                        if(residual>bestResidual){best=i;bestResidual=residual;}
                    }
                    if(best<0)throw new InvalidOperationException("Cannot conserve placement-domain rating.");
                    output[best]+=sign*direction;total+=direction;
                }
            }
        }


        public LeagueCompetitor[] SelectOpponents(int humanMmr,int seed,bool remember)
        {
            RefreshHumanIndex();var rng=new LeagueRandom((uint)seed^(remember?(uint)State.matchmakingSequence*0x85ebca6bu:0));
            bool elite=humanMmr>=9700&&humanMmr<=10000;
            double span=elite?120:humanMmr>=10000?900:240+Math.Min(10000,humanMmr)*.035;int first=0,count=0;
            // Elite needs a small real neighborhood, not ninety-six distant rivals.
            // Sparse saves still expand/fall back to actual persistent identities;
            // no rating or vehicle is fabricated to fill an elite lobby.
            int neighborhood=elite?24:96;
            for(int step=0;step<7;step++){first=Index.CountHigher((int)Math.Min(int.MaxValue,humanMmr+span));int end=Index.CountHigher((int)Math.Max(0,humanMmr-span),true);count=end-first;if(count>=Math.Min(neighborhood,Population))break;span=Math.Min(4500,span*1.55);}
            if(count<12)
            {
                // A sparse prestige field falls back to the nearest ranked
                // neighborhood, preserving variety without sampling the whole league.
                count=Math.Min(97,Index.Count);
                first=Math.Max(0,Math.Min(Index.Count-count,Index.CountHigher(humanMmr)-count/2));
            }
            int humanRank=Index.RankOf(Population);
            int available=count-(humanRank>=first && humanRank<first+count?1:0);
            int candidateCount=Math.Min(candidates.Length,available),filled=0;
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

    public struct LeagueRandom
    {
        uint state;
        public LeagueRandom(uint seed) { state = seed == 0 ? 0x6d2b79f5u : seed; }
        public uint NextUInt() { uint x = state; x ^= x << 13; x ^= x >> 17; x ^= x << 5; return state = x; }
        public double Unit() => (NextUInt() + .5) / 4294967296.0;
        public int Next(int count) => (int)(Unit() * count);
        public double Normal() => Math.Sqrt(-2 * Math.Log(Unit())) * Math.Cos(2 * Math.PI * Unit());
    }
}
