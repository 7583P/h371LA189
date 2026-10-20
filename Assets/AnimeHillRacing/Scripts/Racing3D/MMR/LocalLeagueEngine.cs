using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    public sealed class MatchmakingDiagnostics
    {
        public int availableBots, candidatePool, window, searchSeconds;
        public int onlineBots, queuedBots, inMatchBots, cooldownBots, offlineBots;
        public bool widened, relaxedCooldown;
    }

    // compact-v4: only visible profiles and eleven selected opponents become rich views.
    public sealed class LocalLeagueEngine
    {
        public const int BotCount=10000;
        public const string InauguralMigrationId="CPPS-INAUGURAL-1";
        public LocalLeagueState State {get;}
        public LocalLeagueConfiguration Configuration {get;}
        public LeagueRankIndex Index {get;}
        public LeagueSimulator Simulator {get;}
        public int Population=>State.bots.Length;
        public MatchmakingDiagnostics LastMatchmaking {get;private set;}=new MatchmakingDiagnostics();
        readonly Dictionary<int,string> aliases=new Dictionary<int,string>();
        readonly int[] candidates;
        readonly double[] candidateWeights;
        static readonly byte[] AlternativeVehicles={2,3,4,6},AlternativeWheels={1,2,3,4};

        public LocalLeagueEngine(LocalLeagueState state,LocalLeagueConfiguration config=null)
        {
            State=state??throw new ArgumentNullException(nameof(state));Configuration=config??new LocalLeagueConfiguration();
            if(state.bots==null || state.bots.Length<12 || state.bots.Length>1000000 || state.human==null)throw new ArgumentException("Invalid compact population.");
            foreach(var alias in state.botAliases)aliases[alias.index]=alias.displayName;
            candidates=new int[state.bots.Length];candidateWeights=new double[state.bots.Length];
            Index=new LeagueRankIndex(state,true);
            Simulator=new LeagueSimulator(state,Configuration,Index);
        }

        public static string NewProfileId(ref LeagueRandom rng)=>"hp-"+rng.NextUInt().ToString("x8")+rng.NextUInt().ToString("x8");

        // A league born under CPPS: every identity starts UNRANKED 0/3 with an
        // independent hidden skill. The inaugural reset is recorded as already
        // satisfied, so it can never run against this profile.
        public static LocalLeagueState Create(long utcTicks,int seed,int population=BotCount,LocalLeagueConfiguration config=null)
        {
            if(population<12 || population>1000000)throw new ArgumentOutOfRangeException(nameof(population));
            config=config??new LocalLeagueConfiguration();
            var rng=new LeagueRandom((uint)seed);int season=GlobalSeasonCalendar.SeasonAt(utcTicks);
            var state=new LocalLeagueState{populationSeed=seed,population=population,installationTimestamp=utcTicks,
                currentSeasonNumber=season,lastProcessedSeasonNumber=season-1,highestAcceptedSeason=season,
                seasonStartTimestamp=GlobalSeasonCalendar.Start(season),seasonEndTimestamp=GlobalSeasonCalendar.End(season),
                lastKnownUtcTimestamp=utcTicks,lastLeagueSimulationUtc=utcTicks,competitiveContract=LocalLeagueState.CompetitiveContractCpps,
                human=new LeagueCompetitor{id="player_human",displayName="PLAYER",placementGames=0,provisionalRating=config.placementPrior},
                bots=new CompactLeagueBot[population],botNames=new string[population],nameGeneratorVersion=BotNameGenerator.GeneratorVersion,
                qualificationContract=SeasonQualification.ContractVersion,qualificationSeason=season};
            state.profileId=NewProfileId(ref rng);
            var names=new BotNameGenerator();
            for(int i=0;i<population;i++){
                float talent=Clamp01(.40f+.19f*(float)rng.Normal());int activityRoll=rng.Next(100);
                byte Attribute(float center)=>CompactLeagueBot.PackByte(Clamp01(center+(float)rng.Normal()*.13f));
                var b=new CompactLeagueBot{id=i,personalitySeed=(int)(rng.NextUInt()&0x7fffffff),latent=CompactLeagueBot.Pack01(talent),
                    consistency=CompactLeagueBot.Pack01(Clamp01(.68f+talent*.22f+(float)rng.Normal()*.13f)),
                    learning=CompactLeagueBot.Pack01((float)rng.Unit()),uncertainty=ushort.MaxValue,style=(byte)rng.Next(7),
                    activity=(byte)(activityRoll<10?0:activityRoll<35?64:activityRoll<75?160:255),
                    vehicle=(byte)rng.Next(3),wheels=0,character=(byte)rng.Next(4),accessory=(byte)rng.Next(3),
                    aggression=CompactLeagueBot.PackByte((float)rng.Unit()),risk=CompactLeagueBot.PackByte((float)rng.Unit()),
                    drift=Attribute(talent),aerial=Attribute(talent),shortcut=Attribute(talent),item=Attribute(talent),racecraft=Attribute(talent),recovery=Attribute(.45f+talent*.4f)};
                InitializeCompetitive(ref b,ref rng,config,season);
                state.botNames[i]=names.Generate(LeagueSimulator.Mix(seed,i,0x4e414d45),SkillQuantile(b),out var style);b.nameStyle=(byte)style;b.flags|=CompactLeagueBot.FlagNameAssigned;
                LeagueBotCatalog.UpdateTitles(ref b);state.bots[i]=b;
            }
            state.ledger.appliedMigrations.Add(InauguralMigrationId+":born");
            return state;
        }

        // Approximate population quantile of hidden latent skill (N(.40,.19)).
        public static double SkillQuantile(CompactLeagueBot b){double z=(b.LatentSkill-.40)/.19;return .5*(1+Erf(z/Math.Sqrt(2)));}
        static double Erf(double x){double t=1/(1+.3275911*Math.Abs(x));double y=1-(((((1.061405429*t-1.453152027)*t)+1.421413741)*t-.284496736)*t+.254829592)*t*Math.Exp(-x*x);return x>=0?y:-y;}

        // CPPS competitive and activity fields. Hidden skill is untouched.
        public static void InitializeCompetitive(ref CompactLeagueBot b,ref LeagueRandom rng,LocalLeagueConfiguration config,int season)
        {
            float talent=b.LatentSkill;
            if(b.boost==0)b.boost=CompactLeagueBot.PackByte(Clamp01(talent+(float)rng.Normal()*.13f));
            if(b.adaptation==0)b.adaptation=CompactLeagueBot.PackByte(Clamp01(.3f+talent*.5f+(float)rng.Normal()*.15f));
            b.profile=(byte)LeagueActivityModel.RollProfile(ref rng,SkillQuantile(b));
            double habit=Math.Exp(rng.Normal()*.45-.45*.45/2);b.habit=(float)Math.Max(.2,Math.Min(3.5,habit));
            b.chronotypeMinutes=(short)Math.Max(-240,Math.Min(240,(int)Math.Round(rng.Normal()*75)));
            b.placementGames=0;b.provisionalRating=config.placementPrior;b.currentMmr=0;b.seasonStartingMmr=0;b.seasonPeakMmr=0;
            b.presence=(byte)BotPresence.Offline;b.activeRace=LeagueSimulator.NoRace;b.nextEventTicks=0;b.sessionRacesLeft=0;b.streak=0;
            b.uncertainty=ushort.MaxValue;if(b.firstSeason<=0)b.firstSeason=season;
        }

        public string DisplayName(int index){if(aliases.TryGetValue(index,out string alias))return alias;return LeagueBotCatalog.Name(State,index);}
        public LeagueCompetitor View(int index){if(index==Population)return State.human;aliases.TryGetValue(index,out string name);return LeagueBotCatalog.View(State,index,name);}
        public bool TryIndex(string id,out int index){return LeagueBotCatalog.TryIndex(State.populationSeed,id,out index) && index>=0 && index<Population;}
        public void RefreshHumanIndex()=>Index.Update(Population,State.human.currentMmr);
        public LeagueCompetitor[] Page(int first,int count){RefreshHumanIndex();first=Math.Max(0,Math.Min(Index.Count,first));count=Math.Max(0,Math.Min(count,Index.Count-first));var result=new LeagueCompetitor[count];for(int i=0;i<count;i++)result[i]=View(Index.AtRank(first+i));return result;}
        public int Rank(string id){RefreshHumanIndex();if(id=="player_human")return Index.RankOf(Population);return TryIndex(id,out int index)?Index.RankOf(index):-1;}
        public int OfficialPosition(string id){int node=id==CompetitiveCoordinator.HumanId?Population:TryIndex(id,out int i)?i:-1;return node>=0&&SeasonQualification.IsOfficial(State,node)?Rank(id)+1:0;}
        public int RankedCount(){int c=State.human.Ranked?1:0;foreach(var b in State.bots)if(b.Ranked)c++;return c;}

        public static void RecordResult(LeagueCompetitor competitor, int position, int delta, float lap)
        {
            competitor.currentMmr = (int)Math.Max(0L, Math.Min(CompetitiveRating.MaximumMmr, (long)competitor.currentMmr + delta));
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

        // Legacy v3 placement-domain allocator, kept only so old fixtures and
        // diagnostics can compare against it. Settlement uses CompetitiveRating.
        public static void CalculateDeltas(int[] ratings, int[] places, float[] deviations, LocalLeagueConfiguration config, int[] output, double[] scratch = null)
        {
            int n = ratings.Length;
            if (n < 2 || places.Length != n || output.Length != n || deviations.Length != n) throw new ArgumentException("Invalid rating lobby.");
            var p = new RatingParticipant[n]; var o = new RatingOutcome[n]; var s = new double[n];
            for (int i = 0; i < n; i++) p[i] = new RatingParticipant { rating = Math.Max(0, ratings[i]), ranked = true, placementGames = CompetitiveRating.PlacementRaces, uncertainty = deviations[i], position = places[i], outcome = ParticipantOutcome.Finished };
            CompetitiveRating.Settle(p, config, o, s);
            for (int i = 0; i < n; i++) output[i] = o[i].delta;
        }

        public int HumanEffectiveRating=>State.human.Ranked?State.human.currentMmr:State.human.provisionalRating;

        // Human ranked matchmaking over the persistent population. Only bots that
        // are ONLINE_IDLE or IN_QUEUE right now may be chosen; OFFLINE, IN_MATCH,
        // COOLDOWN and already-reserved bots are never eligible. The returned bots
        // are reserved atomically (reserve=true) for this race id.
        public int[] SelectAvailableOpponents(int seed,long utcTicks,bool reserve,bool includeCooldown=false)
        {
            var sim=Simulator;var rng=new LeagueRandom((uint)seed^(uint)State.matchmakingSequence*0x85ebca6bu);
            int target=HumanEffectiveRating,available=0;var diag=new MatchmakingDiagnostics();
            var presence=new int[5];sim.CountPresence(presence);
            diag.offlineBots=presence[0];diag.onlineBots=presence[1];diag.queuedBots=presence[2];diag.inMatchBots=presence[3];diag.cooldownBots=presence[4];
            for(int i=0;i<Population;i++)if(sim.IsAvailable(i,includeCooldown))available++;
            diag.availableBots=available;diag.relaxedCooldown=includeCooldown;
            if(available<11){LastMatchmaking=diag;return null;}
            long window=State.human.Ranked?300:900;int pool=0;int wanted=Math.Min(available,48);
            for(int step=0;step<12;step++){
                pool=0;
                for(int i=0;i<Population;i++)if(sim.IsAvailable(i,includeCooldown) && Math.Abs((long)sim.EffectiveRating(i)-target)<=window)candidates[pool++]=i;
                if(pool>=wanted)break;
                window=Math.Min(20000,(long)(window*1.6));diag.widened=true;
            }
            if(pool<11){pool=0;for(int i=0;i<Population;i++)if(sim.IsAvailable(i,includeCooldown))candidates[pool++]=i;window=int.MaxValue;}
            diag.candidatePool=pool;diag.window=(int)Math.Min(int.MaxValue,window);
            // A smaller online pool means a longer (simulated) search, not fabricated rivals.
            diag.searchSeconds=(int)Math.Round(Math.Max(2,Math.Min(25,2+600.0/Math.Max(1,available)+(diag.widened?3:0))));
            var styles=new int[8];
            // IDs are stable integers in the compact store. Formatting a string
            // for every one of 100k candidates dominated allocation/latency;
            // decode only the bounded recent-opponent list once per query.
            var recent=new HashSet<int>();foreach(string id in State.recentOpponentIds)if(TryIndex(id,out int prior))recent.Add(prior);
            for(int i=0;i<pool;i++){
                var b=State.bots[candidates[i]];double distance=Math.Abs((double)sim.EffectiveRating(candidates[i])-target)/Math.Max(150,window*.6);
                double weight=Math.Exp(-distance*distance*.5)*(.9+.2*b.Uncertainty);
                if(!State.human.Ranked){double skillGap=Math.Abs(LeagueSkillMapping.DrivingRating(b.DevelopedSkill)-target)/4000.0;weight*=Math.Exp(-skillGap*skillGap);}
                if(recent.Contains(candidates[i]))weight*=.16;
                candidateWeights[i]=Math.Max(1e-9,weight);
            }
            var result=new int[11];
            for(int slot=0;slot<11;slot++){
                double sum=0;for(int i=0;i<pool;i++)sum+=candidateWeights[i];double roll=rng.Unit()*sum;int chosen=-1;
                for(int i=0;i<pool;i++){if(candidateWeights[i]<=0)continue;roll-=candidateWeights[i];if(roll<0){chosen=i;break;}}
                if(chosen<0)for(int i=pool-1;i>=0;i--)if(candidateWeights[i]>0){chosen=i;break;}
                result[slot]=candidates[chosen];candidateWeights[chosen]=0;
                // Driving-style diversity: soften further picks of an already-present style.
                int style=Math.Min(7,(int)State.bots[candidates[chosen]].style);styles[style]++;
                for(int i=0;i<pool;i++)if(candidateWeights[i]>0 && State.bots[candidates[i]].style==style)candidateWeights[i]*=.8;
            }
            LastMatchmaking=diag;
            if(reserve){
                if(!sim.Reserve(result,utcTicks,includeCooldown))throw new InvalidOperationException("Atomic reservation failed for an available lobby.");
                State.matchmakingSequence++;foreach(int i in result)State.recentOpponentIds.Add(LeagueBotCatalog.Id(State.populationSeed,i));
                while(State.recentOpponentIds.Count>44)State.recentOpponentIds.RemoveAt(0);
            }
            return result;
        }

        // Compatibility wrapper used by legacy fixtures: selection without reservation.
        public LeagueCompetitor[] SelectOpponents(int humanMmr,int seed,bool remember)
        {
            var picked=SelectAvailableOpponents(seed,State.lastLeagueSimulationUtc,false);
            if(picked==null){
                // No live presence yet (fixtures that never simulated): nearest ranked identities.
                RefreshHumanIndex();int count=Math.Min(97,Index.Count),first=Math.Max(0,Math.Min(Index.Count-count,Index.CountHigher(humanMmr)-count/2));
                var list=new List<int>(11);var rng=new LeagueRandom((uint)seed);
                while(list.Count<11){int c=Index.AtRank(first+rng.Next(count));if(c!=Population && !list.Contains(c))list.Add(c);}
                picked=list.ToArray();
            }
            var result=new LeagueCompetitor[11];for(int i=0;i<11;i++)result[i]=View(picked[i]);
            if(remember){State.matchmakingSequence++;foreach(var b in result)State.recentOpponentIds.Add(b.id);while(State.recentOpponentIds.Count>44)State.recentOpponentIds.RemoveAt(0);}
            return result;
        }

        public static void UpdatePreference(ref CompactLeagueBot b,ref LeagueRandom rng)
        {
            // Persistent adaptation, not a reroll at matchmaking. Even the top
            // bracket retains specialists and near-meta choices.
            double level=Math.Min(1,b.currentMmr/10000.0),metaChance=.03+.89*level*level;
            if(rng.Unit()<metaChance){b.vehicle=3;b.wheels=5;}
            else if(level>.3){b.vehicle=AlternativeVehicles[rng.Next(4)];b.wheels=AlternativeWheels[rng.Next(4)];}
        }
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
