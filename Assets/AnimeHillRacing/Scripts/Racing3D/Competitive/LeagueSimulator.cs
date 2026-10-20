using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    public interface ILeagueSimulationObserver
    {
        void RaceCompleted(long utcTicks, long raceSequence, int[] participants, RatingParticipant[] before, RatingOutcome[] after, float[] seconds);
        void HumanReserved(long utcTicks, int[] bots);
    }

    // Event-driven offline league. Each persistent bot owns exactly one next
    // event (session start, queue entry, queue widening/timeout, cooldown end)
    // in an indexed min-heap; races own their end event. Events are processed
    // in (time, index) order with per-bot/per-race seeded RNG, so results are
    // independent of how processing is chunked across frames, threads or
    // relaunches. Nothing runs while the application is closed: elapsed time
    // is reconstructed when the game is active again.
    public sealed class LeagueSimulator
    {
        public const int LobbySize = 12;
        public const int HumanReservation = -2, NoRace = -1;
        const long TicksPerSecond = TimeSpan.TicksPerSecond;
        readonly LocalLeagueState state;
        readonly LocalLeagueConfiguration config;
        readonly LeagueRankIndex index;
        readonly int n;
        // Indexed min-heap over bots not currently in a match.
        readonly int[] heap, heapPos;
        readonly long[] heapKey; // contiguous keys: avoids touching 172-byte records while sifting
        int heapCount;
        readonly int[] queue, queuePos;
        int queueCount;
        long nextRaceEnd = long.MaxValue;
        readonly int[] best = new int[LobbySize - 1];
        readonly long[] bestDistance = new long[LobbySize - 1];
        readonly int[] lobby = new int[LobbySize];
        readonly RatingParticipant[] participants = new RatingParticipant[LobbySize];
        readonly RatingOutcome[] outcomes = new RatingOutcome[LobbySize];
        readonly double[] scratch = new double[LobbySize];
        readonly float[] seconds = new float[LobbySize];
        readonly bool[] dnf = new bool[LobbySize];
        readonly int[] order = new int[LobbySize];
        readonly int[] shortcutHits = new int[LobbySize], aerialHits = new int[LobbySize];
        public ILeagueSimulationObserver Observer { get; set; }
        public long ProcessedEvents { get; private set; }
        public long CompletedRaces { get; private set; }
        public int QueueCount => queueCount;
        public int ActiveRaces => state.activeSimRaces.Count;

        public LeagueSimulator(LocalLeagueState state, LocalLeagueConfiguration config, LeagueRankIndex index)
        {
            this.state = state; this.config = config; this.index = index; n = state.bots.Length;
            heap = new int[n]; heapPos = new int[n]; heapKey = new long[n]; queue = new int[n]; queuePos = new int[n];
            for (int i = 0; i < n; i++) { heapPos[i] = -1; queuePos[i] = -1; }
            Rebuild();
        }

        // Reconstructs heap/queue/race caches from the persisted records.
        public void Rebuild()
        {
            heapCount = 0; queueCount = 0;
            for (int i = 0; i < n; i++) { heapPos[i] = -1; queuePos[i] = -1; }
            for (int i = 0; i < n; i++)
            {
                var b = state.bots[i];
                if (b.presence == (byte)BotPresence.InQueue) { queuePos[i] = queueCount; queue[queueCount++] = i; }
                if (b.presence != (byte)BotPresence.InMatch && b.nextEventTicks > 0) HeapPush(i);
            }
            RecomputeNextRaceEnd();
        }

        // First-time presence for a league (creation, migration, fast-forward).
        public void SeedPresence(long fromTicks)
            =>SeedAdditionalPresence(0,fromTicks);

        public void SeedAdditionalPresence(int first,long fromTicks)
        {
            for (int i = first; i < n; i++)
            {
                ref var b = ref state.bots[i];
                if (b.activeRace == HumanReservation) continue;
                if (b.presence == (byte)BotPresence.InQueue) QueueRemove(i);
                b.presence = (byte)BotPresence.Offline; b.activeRace = NoRace; b.sessionRacesLeft = 0; b.stateSinceTicks = fromTicks;
                var rng = Rng(i);
                // Bots already inside a session at the seed instant, in proportion to activity.
                var profile = LeagueActivityModel.Parameters((BotActivityProfile)b.profile);
                // Expected share of the day spent in a session (~6 min per race incl. menus).
                double online = Math.Min(.5, profile.sessionsPerDay * b.habit * profile.medianRaces * 6 / 1440.0 *
                    LeagueActivityModel.Intensity((uint)state.populationSeed, fromTicks, state.utcOffsetMinutes, b.chronotypeMinutes));
                if (rng.Unit() < online) { StartSession(i, fromTicks, ref rng, true); }
                else {long start=NextSessionStart(i, fromTicks, ref rng, false);if(state.expandedCommunityPresence)PassivePresence(i,fromTicks,start);else b.nextEventTicks=start;}
                HeapUpdate(i);
            }
        }

        public static uint Mix(int seed, long a, long b)
        {
            ulong x = unchecked((ulong)(uint)seed * 0x9E3779B97F4A7C15UL ^ (ulong)a * 0xBF58476D1CE4E5B9UL ^ (ulong)b * 0x94D049BB133111EBUL);
            x ^= x >> 31; x *= 0xD6E8FEB86659FD93UL; x ^= x >> 32;
            uint v = (uint)x; return v == 0 ? 0x6d2b79f5u : v;
        }
        LeagueRandom Rng(int bot) { ref var b = ref state.bots[bot]; return new LeagueRandom(Mix(state.populationSeed, bot, b.eventCounter++)); }

        public int EffectiveRating(int bot) { var b = state.bots[bot]; return b.Ranked ? b.currentMmr : b.provisionalRating; }

        // ---------------- event loop ----------------
        public long NextEventTicks => Math.Min(heapCount > 0 ? heapKey[0] : long.MaxValue, nextRaceEnd);

        // Processes every event strictly before 'limit' (exclusive) and at most
        // maxEvents events. Returns true when the cursor reached the limit.
        public bool AdvanceBefore(long limit, int maxEvents)
        {
            int processed = 0;
            while (processed < maxEvents)
            {
                long tb = heapCount > 0 ? heapKey[0] : long.MaxValue;
                long t = Math.Min(tb, nextRaceEnd);
                if (t >= limit) { state.lastLeagueSimulationUtc = Math.Max(state.lastLeagueSimulationUtc, limit - 1); return true; }
                state.lastLeagueSimulationUtc = Math.Max(state.lastLeagueSimulationUtc, t);
                if (nextRaceEnd <= tb) FinishEarliestRace(t); else BotEvent(heap[0], t);
                processed++; ProcessedEvents++;
            }
            return false;
        }

        void BotEvent(int i, long t)
        {
            ref var b = ref state.bots[i];
            if((b.flags&ExpandedCommunitySchedule.PassiveFlag)!=0)
            {
                long sessionStart=b.stateSinceTicks;
                if(t<sessionStart){PassivePresence(i,t,sessionStart);HeapUpdate(i);return;}
                var sessionRng=Rng(i);StartSession(i,t,ref sessionRng,false);return;
            }
            var p = LeagueActivityModel.Parameters((BotActivityProfile)b.profile);
            var rng = Rng(i);
            switch ((BotPresence)b.presence)
            {
                case BotPresence.Offline:
                    StartSession(i, t, ref rng, false); break;
                case BotPresence.OnlineIdle:
                    b.presence = (byte)BotPresence.InQueue; b.stateSinceTicks = t; QueueAdd(i);
                    b.nextEventTicks = t + 30 * TicksPerSecond; HeapUpdate(i);
                    TryMatch(i, t); break;
                case BotPresence.InQueue:
                {
                    long waited = t - b.stateSinceTicks;
                    if (waited >= (long)(p.queuePatienceSeconds * TicksPerSecond))
                    {
                        QueueRemove(i);
                        if (rng.Unit() < .5) { b.presence = (byte)BotPresence.OnlineIdle; b.stateSinceTicks = t; b.nextEventTicks = t + (long)((60 + rng.Unit() * 180) * TicksPerSecond); HeapUpdate(i); }
                        else EndSession(i, t, ref rng);
                        break;
                    }
                    if (!TryMatch(i, t)) { b.nextEventTicks = Math.Min(t + 30 * TicksPerSecond, b.stateSinceTicks + (long)(p.queuePatienceSeconds * TicksPerSecond)); HeapUpdate(i); }
                    break;
                }
                case BotPresence.Cooldown:
                {
                    double intensity = LeagueActivityModel.Intensity((uint)state.populationSeed, t, state.expandedCommunityPresence?ExpandedCommunitySchedule.HomeOffset(b,state.utcOffsetMinutes):state.utcOffsetMinutes, b.chronotypeMinutes);
                    double fatigue = .22 * Math.Max(0, 1 - intensity / .6);
                    if (b.sessionRacesLeft > 0 && rng.Unit() >= fatigue)
                    { b.presence = (byte)BotPresence.OnlineIdle; b.stateSinceTicks = t; b.nextEventTicks = t + (long)(Exp(ref rng, p.idleSeconds) * TicksPerSecond) + TicksPerSecond; HeapUpdate(i); }
                    else EndSession(i, t, ref rng);
                    break;
                }
                default:
                    HeapRemove(i); break; // InMatch never owns a bot event
            }
        }

        void StartSession(int i, long t, ref LeagueRandom rng, bool seeded)
        {
            ref var b = ref state.bots[i];
            b.flags&=unchecked((byte)~ExpandedCommunitySchedule.PassiveFlag);
            var p = LeagueActivityModel.Parameters((BotActivityProfile)b.profile);
            double races = Math.Exp(Math.Log(p.medianRaces) + p.racesSpread * rng.Normal());
            b.sessionRacesLeft = (byte)Math.Max(1, Math.Min(30, (int)Math.Round(races)));
            if (seeded) b.sessionRacesLeft = (byte)Math.Max(1, (int)(b.sessionRacesLeft * rng.Unit()) + 1);
            b.presence = (byte)BotPresence.OnlineIdle; b.stateSinceTicks = t;
            b.nextEventTicks = t + (long)(Exp(ref rng, p.idleSeconds * 1.5) * TicksPerSecond) + TicksPerSecond;
            HeapUpdate(i);
        }

        void EndSession(int i, long t, ref LeagueRandom rng)
        {
            ref var b = ref state.bots[i];
            b.presence = (byte)BotPresence.Offline; b.stateSinceTicks = t; b.sessionRacesLeft = 0;
            long next=NextSessionStart(i,t,ref rng,true);
            if(state.expandedCommunityPresence)PassivePresence(i,t,next);else b.nextEventTicks=next;
            HeapUpdate(i);
        }

        void PassivePresence(int i,long t,long sessionStart)
        {
            ref var b=ref state.bots[i];
            b.flags|=ExpandedCommunitySchedule.PassiveFlag;
            b.presence=(byte)(ExpandedCommunitySchedule.Available(state.populationSeed,b,t,out long change,state.utcOffsetMinutes)?BotPresence.OnlineIdle:BotPresence.Offline);
            // While passive, stateSinceTicks stores the next genuine race-session
            // start; nextEventTicks is its minimum with the next availability edge.
            b.stateSinceTicks=sessionStart;b.nextEventTicks=Math.Min(sessionStart,change);
        }

        // Inhomogeneous Poisson session arrivals by thinning against the
        // continuous intensity. No time-of-day bucket or race quota exists.
        long NextSessionStart(int i, long from, ref LeagueRandom rng, bool afterSession)
        {
            var b = state.bots[i];
            var p = LeagueActivityModel.Parameters((BotActivityProfile)b.profile);
            double perDay = Math.Max(.01, p.sessionsPerDay * b.habit);
            double lambdaMax = perDay * LeagueActivityModel.MaximumIntensityFactor;
            long t = from + (afterSession ? (long)(p.minimumOfflineMinutes * (.6 + .8 * rng.Unit()) * TimeSpan.TicksPerMinute) : 0);
            for (int k = 0; k < 400; k++)
            {
                t += (long)(-Math.Log(rng.Unit()) / lambdaMax * TimeSpan.TicksPerDay) + 1;
                double intensity = LeagueActivityModel.Intensity((uint)state.populationSeed, t, state.expandedCommunityPresence?ExpandedCommunitySchedule.HomeOffset(b,state.utcOffsetMinutes):state.utcOffsetMinutes, b.chronotypeMinutes);
                if (rng.Unit() * LeagueActivityModel.MaximumIntensityFactor < intensity) return t;
            }
            return t;
        }

        static double Exp(ref LeagueRandom rng, double mean) => -Math.Log(rng.Unit()) * mean;

        // ---------------- matchmaking among queued bots ----------------
        bool TryMatch(int anchor, long t)
        {
            if (queueCount < LobbySize) return false;
            var a = state.bots[anchor];
            int ra = EffectiveRating(anchor);
            double waitedSeconds = (t - a.stateSinceTicks) / (double)TicksPerSecond;
            long window = (long)Math.Min(6000, 300 + 1200 * waitedSeconds / 60.0);
            int found = 0;
            for (int q = 0; q < queueCount; q++)
            {
                int c = queue[q]; if (c == anchor) continue;
                long d = Math.Abs((long)EffectiveRating(c) - ra);
                if (d > window) continue;
                // Deterministic order: rating distance, then index (order-independent of queue layout).
                long key = d * 4194304L + c;
                if (found < best.Length) { Insert(c, key, found); found++; }
                else if (key < bestDistance[found - 1]) Insert(c, key, found - 1);
            }
            if (found < best.Length) return false;
            lobby[0] = anchor; for (int k = 0; k < best.Length; k++) lobby[k + 1] = best[k];
            StartRace(t);
            return true;
        }

        void Insert(int c, long key, int last)
        {
            int k = last;
            while (k > 0 && bestDistance[k - 1] > key) { bestDistance[k] = bestDistance[k - 1]; best[k] = best[k - 1]; k--; }
            bestDistance[k] = key; best[k] = c;
        }

        void StartRace(long t)
        {
            long sequence = ++state.raceSequence;
            var slot = new SimulatedRaceSlot { raceSequence = sequence, startTicks = t };
            for (int k = 0; k < LobbySize; k++)
            {
                int i = lobby[k]; slot.participants[k] = i;
                ref var b = ref state.bots[i];
                QueueRemove(i); HeapRemove(i);
                b.flags&=unchecked((byte)~ExpandedCommunitySchedule.PassiveFlag);
                b.presence = (byte)BotPresence.InMatch; b.activeRace = (int)(sequence & int.MaxValue); b.stateSinceTicks = t; b.nextEventTicks = 0;
            }
            SimulateResult(slot);
            float last = 0; for (int k = 0; k < LobbySize; k++) if (!dnf[k]) last = Math.Max(last, seconds[k]);
            if (last <= 0) last = config.referenceWinnerSeconds * 1.6f;
            slot.endTicks = t + (long)((config.preRaceSeconds + last + config.resultsSeconds) * TicksPerSecond);
            state.activeSimRaces.Add(slot);
            if (slot.endTicks < nextRaceEnd) nextRaceEnd = slot.endTicks;
        }

        // Statistical race: no Unity scene, no physics. Finishing times follow
        // hidden skill, track profile, consistency, risk, shortcuts, boost,
        // drift, aerial/landing ability, incidents and seeded variation.
        void SimulateResult(SimulatedRaceSlot slot)
        {
            var rng = new LeagueRandom(Mix(state.populationSeed, -1, slot.raceSequence));
            double reference = config.referenceWinnerSeconds;
            double lobbySkill = 0; for (int k = 0; k < LobbySize; k++) lobbySkill += state.bots[slot.participants[k]].DevelopedSkill; lobbySkill /= LobbySize;
            for (int k = 0; k < LobbySize; k++)
            {
                var b = state.bots[slot.participants[k]];
                double core = b.DevelopedSkill, consistency = b.Consistency, risk = b.risk / 255.0, recovery = b.recovery / 255.0;
                double drift = b.drift / 255.0, aerial = b.aerial / 255.0, shortcut = b.shortcut / 255.0, boost = b.boost / 255.0, racecraft = b.racecraft / 255.0, items = b.item / 255.0;
                // Night City Grand Circuit profile: drift-heavy corners, a crest
                // cut (measured ~3.3 s observed saving), jump sections and boosts.
                double pace = reference * (1 + .40 * Math.Pow(Math.Max(0, 1 - core), 1.25));
                pace -= (drift - .5) * 4.0 + (boost - .5) * 3.0 + (aerial - .5) * 2.5 + (racecraft - .5) * 2.0;
                pace += rng.Normal() * reference * (.010 + .028 * (1 - consistency) + .010 * risk);
                pace -= b.RecentForm * 1.5;
                // Two shortcut opportunities: attempt, success saves, failure costs.
                int hits = 0;
                for (int s = 0; s < 2; s++)
                {
                    if (rng.Unit() > .15 + .55 * shortcut + .25 * risk) continue;
                    if (rng.Unit() < .45 + .5 * shortcut * consistency) { pace -= 3.3; hits++; } else pace += 4.0;
                }
                // Jumps/landing chains.
                int air = 0; for (int s = 0; s < 3; s++) if (rng.Unit() < .2 + .7 * aerial) { air++; pace -= .35 * aerial; } else if (rng.Unit() < .25 * (1 - aerial) * (.5 + risk)) pace += 1.6;
                // Incidents scale with risk and inconsistency, cost reduced by recovery.
                double incident = .05 + .14 * (1 - consistency) + .06 * risk;
                while (rng.Unit() < incident) { pace += (4 + 8 * rng.Unit()) * (1.25 - recovery); incident *= .45; }
                // Items: rear competitors receive stronger comeback items; skill decides use.
                double behind = Math.Max(0, lobbySkill - core);
                pace -= rng.Normal() * 1.4 + behind * 6 * (.4 + .6 * items);
                double dnfChance = .003 + .02 * (1 - consistency) * risk;
                dnf[k] = rng.Unit() < dnfChance;
                seconds[k] = (float)Math.Max(reference * .92, pace);
                shortcutHits[k] = hits; aerialHits[k] = air;
            }
        }

        bool Before(int x, int y, SimulatedRaceSlot slot)
        {
            if (dnf[x] != dnf[y]) return !dnf[x];
            if (seconds[x] != seconds[y]) return seconds[x] < seconds[y];
            return slot.participants[x] < slot.participants[y];
        }

        void RecomputeNextRaceEnd() { nextRaceEnd = long.MaxValue; foreach (var r in state.activeSimRaces) if (r.endTicks < nextRaceEnd) nextRaceEnd = r.endTicks; }

        void FinishEarliestRace(long t)
        {
            int at = 0; for (int k = 1; k < state.activeSimRaces.Count; k++) { var r = state.activeSimRaces[k]; var c = state.activeSimRaces[at]; if (r.endTicks < c.endTicks || (r.endTicks == c.endTicks && r.raceSequence < c.raceSequence)) at = k; }
            var slot = state.activeSimRaces[at]; state.activeSimRaces.RemoveAt(at);
            SimulateResult(slot);
            // Allocation-free insertion sort: finishers by time, DNFs last, ties by BotId.
            for (int k = 0; k < LobbySize; k++)
            {
                int j = k; order[k] = k;
                while (j > 0 && Before(order[j], order[j - 1], slot)) { int tmp = order[j]; order[j] = order[j - 1]; order[j - 1] = tmp; j--; }
            }
            for (int place = 0; place < LobbySize; place++)
            {
                int k = order[place]; int i = slot.participants[k]; var b = state.bots[i];
                participants[k] = new RatingParticipant { rating = b.Ranked ? b.currentMmr : b.provisionalRating, ranked = b.Ranked, placementGames = b.placementGames,
                    uncertainty = b.Uncertainty, position = place + 1, outcome = dnf[k] ? ParticipantOutcome.Dnf : ParticipantOutcome.Finished };
            }
            CompetitiveRating.Settle(participants, config, outcomes, scratch);
            var budget = state.ledger.budget; budget.simulatedRaces++;
            for (int k = 0; k < LobbySize; k++)
            {
                int i = slot.participants[k];
                ref var b = ref state.bots[i];
                ApplyOutcome(ref b, participants[k], outcomes[k], seconds[k] / 3f, shortcutHits[k], aerialHits[k], budget, config.postPlacementUncertainty);
                if(participants[k].outcome==ParticipantOutcome.Finished&&slot.startTicks>=state.seasonStartTimestamp&&SeasonQualification.CanQualify(state,state.currentSeasonNumber,t))b.flags|=SeasonQualification.QualifiedFlag;
                index.Update(i, b.currentMmr);
                b.activeRace = NoRace; b.presence = (byte)BotPresence.Cooldown; b.stateSinceTicks = t; b.lastRaceTicks = t;
                if (b.sessionRacesLeft > 0) b.sessionRacesLeft--;
                var rng = Rng(i);
                // Persistent career choices evolve slowly (titles, build preference).
                if ((b.personalitySeed & 3) != 0 && b.races % 19 == 0) {
                    int title=1+rng.Next(LeagueBotCatalog.Titles.Length-1);
                    bool eligible=title!=1||(b.Ranked&&(b.flags&SeasonQualification.QualifiedFlag)!=0&&index.RankOf(i)<1000);
                    if(eligible&&(b.titleFlags&(1<<title))!=0)b.selectedTitle=(ushort)title;
                }
                if (b.races % 64 == 0 && rng.Unit() < .35) LocalLeagueEngine.UpdatePreference(ref b, ref rng);
                if(state.expandedCommunityPresence&&b.sessionRacesLeft==0){EndSession(i,t,ref rng);continue;}
                b.nextEventTicks = t + (long)((8 + Exp(ref rng, LeagueActivityModel.Parameters((BotActivityProfile)b.profile).cooldownSeconds)) * TicksPerSecond);
                HeapUpdate(i);
            }
            CountHour(t);
            CompletedRaces++;
            Observer?.RaceCompleted(t, slot.raceSequence, slot.participants, participants, outcomes, seconds);
            RecomputeNextRaceEnd();
        }

        public static void ApplyOutcome(ref CompactLeagueBot b, RatingParticipant p, RatingOutcome o, float lap, int shortcuts, int aerials, RatingBudget budget, float postPlacementUncertainty)
        {
            if (p.ranked) { budget.pairwiseNet += o.delta + o.ceilingRemoval - o.floorInjection; budget.floorInjection += o.floorInjection; budget.ceilingRemoval += o.ceilingRemoval; b.currentMmr = o.newRating; }
            else
            {
                b.placementGames = (byte)o.newPlacementGames;
                if (o.placementCompleted) { b.currentMmr = o.newRating; b.provisionalRating = o.newRating; budget.placementIssuance += o.placementIssuance; }
                else b.provisionalRating = o.newRating;
            }
            budget.settledParticipants++;
            b.peakMmr = Math.Max(b.peakMmr, b.currentMmr); b.seasonPeakMmr = Math.Max(b.seasonPeakMmr, b.currentMmr);
            b.races++; b.seasonRaces++;
            bool finished = p.outcome == ParticipantOutcome.Finished;
            if(finished&&b.Ranked&&p.position==1&&shortcuts>=3&&aerials>=1)b.titleFlags|=1<<7;
            if (finished && p.position == 1) { b.wins++; b.seasonWins++; }
            if (finished && p.position <= 3) { b.podiums++; b.seasonPodiums++; }
            if (!finished) b.dnfs++;
            b.finishSum += finished ? p.position : LobbySize;
            int sign = p.ranked ? Math.Sign(o.delta) : (finished && p.position <= 6 ? 1 : -1);
            b.streak = (short)(sign > 0 ? Math.Max(1, b.streak + 1) : sign < 0 ? Math.Min(-1, b.streak - 1) : 0);
            if (o.placementCompleted) b.uncertainty = CompactLeagueBot.Pack01(Math.Max(.12f, postPlacementUncertainty));
            else if (p.ranked) b.uncertainty = CompactLeagueBot.Pack01(Math.Max(.12f, b.Uncertainty * .975f));
            float position = finished ? p.position : LobbySize;
            b.form = CompactLeagueBot.PackForm(b.RecentForm * .85f + (6.5f - position) / 5.5f * .15f);
            if (finished && lap > 0 && float.IsFinite(lap)) { if (b.bestLap <= 0 || lap < b.bestLap) b.bestLap = lap; if (b.seasonBestLap <= 0 || lap < b.seasonBestLap) b.seasonBestLap = lap; }
            b.shortcutSuccesses += shortcuts; b.aerialSuccesses += aerials; if (finished) b.landingSuccesses += aerials;
            LeagueBotCatalog.UpdateTitles(ref b);
        }

        // 48-slot ring of completed statistical races per absolute UTC hour.
        void CountHour(long t)
        {
            long hour = t / TimeSpan.TicksPerHour;
            if (state.hourlyRaces == null || state.hourlyRaces.Length != 48) state.hourlyRaces = new int[48];
            if (hour > state.hourlyAnchorHour)
            {
                long clearFrom = Math.Max(state.hourlyAnchorHour + 1, hour - 47);
                for (long h = clearFrom; h <= hour; h++) state.hourlyRaces[(int)(h % 48)] = 0;
                state.hourlyAnchorHour = hour;
            }
            if (hour > state.hourlyAnchorHour - 48) state.hourlyRaces[(int)(hour % 48)]++;
        }

        // ---------------- human physical race reservation ----------------
        public bool IsAvailable(int i, bool includeCooldown = false)
        {
            var b = state.bots[i];
            return b.activeRace == NoRace && (b.presence == (byte)BotPresence.OnlineIdle || b.presence == (byte)BotPresence.InQueue
                || ((includeCooldown||state.expandedCommunityPresence) && b.presence == (byte)BotPresence.Cooldown && b.sessionRacesLeft > 0));
        }

        // Atomic: either all bots move to IN_MATCH with the human reservation or none do.
        public bool Reserve(int[] bots, long t, bool includeCooldown = false)
        {
            foreach (int i in bots) if (i < 0 || i >= n || !IsAvailable(i, includeCooldown)) return false;
            for (int a = 0; a < bots.Length; a++) for (int c = a + 1; c < bots.Length; c++) if (bots[a] == bots[c]) return false;
            foreach (int i in bots)
            {
                ref var b = ref state.bots[i];
                QueueRemove(i); HeapRemove(i);
                b.flags&=unchecked((byte)~ExpandedCommunitySchedule.PassiveFlag);
                b.presence = (byte)BotPresence.InMatch; b.activeRace = HumanReservation; b.stateSinceTicks = t; b.nextEventTicks = 0;
            }
            Observer?.HumanReserved(t, bots);
            return true;
        }

        public void Release(int[] bots, long t, bool raced)
        {
            foreach (int i in bots)
            {
                if (i < 0 || i >= n) continue;
                ref var b = ref state.bots[i];
                if (b.activeRace != HumanReservation) continue;
                var rng = Rng(i);
                b.activeRace = NoRace; b.presence = (byte)BotPresence.Cooldown; b.stateSinceTicks = t;
                if (raced) { b.lastRaceTicks = t; if (b.sessionRacesLeft > 0) b.sessionRacesLeft--; }
                if(state.expandedCommunityPresence&&b.sessionRacesLeft==0){EndSession(i,t,ref rng);continue;}
                b.nextEventTicks = t + (long)((8 + Exp(ref rng, 25)) * TicksPerSecond); HeapUpdate(i);
            }
        }

        // Quiet-hour human queue: real persistent bots whose next session start
        // is soonest (and rating-near) log in now instead of minutes later. Only
        // OFFLINE bots past their minimum offline time qualify; nobody in a race,
        // reserved or in cooldown is touched. Returns how many came online.
        public int BringForwardSessions(int needed, long t, int targetRating)
        {
            if (needed <= 0) return 0;
            var picked = new int[needed]; var score = new double[needed]; int count = 0;
            for (int i = 0; i < n; i++)
            {
                var b = state.bots[i];
                if (b.presence != (byte)BotPresence.Offline || b.activeRace != NoRace) continue;
                var p = LeagueActivityModel.Parameters((BotActivityProfile)b.profile);
                if (t - b.stateSinceTicks < (long)(p.minimumOfflineMinutes * TimeSpan.TicksPerMinute)) continue;
                double soon = Math.Max(0, b.nextEventTicks - t) / (double)TimeSpan.TicksPerHour;
                double s = soon + Math.Abs(EffectiveRating(i) - targetRating) / 1500.0;
                if (count < needed) { picked[count] = i; score[count++] = s; continue; }
                int worst = 0; for (int k = 1; k < needed; k++) if (score[k] > score[worst]) worst = k;
                if (s < score[worst]) { picked[worst] = i; score[worst] = s; }
            }
            for (int k = 0; k < count; k++) { var rng = Rng(picked[k]); StartSession(picked[k], t, ref rng, false); }
            BroughtForwardSessions += count;
            return count;
        }
        public long BroughtForwardSessions { get; private set; }

        // ---------------- bounded catch-up ----------------
        // Ends every in-progress statistical race (they finish within minutes),
        // then re-seeds presence at the new cursor. Skipped time is recorded.
        public void FastForward(long newCursor)
        {
            if (newCursor <= state.lastLeagueSimulationUtc) return;
            while (state.activeSimRaces.Count > 0) { long end = nextRaceEnd; FinishEarliestRace(Math.Min(end, newCursor)); }
            state.skippedSimulatedTicks += newCursor - state.lastLeagueSimulationUtc;
            state.seasonHistoryIncomplete=true;
            state.lastLeagueSimulationUtc = newCursor;
            SeedPresence(newCursor);
        }

        // ---------------- diagnostics ----------------
        public void CountPresence(int[] counts)
        {
            Array.Clear(counts, 0, counts.Length);
            for (int i = 0; i < n; i++) counts[Math.Min(counts.Length - 1, state.bots[i].presence)]++;
        }

        public bool ValidateInvariants(out string failure)
        {
            failure = null;
            var seen = new HashSet<int>();
            foreach (var race in state.activeSimRaces)
                foreach (int i in race.participants)
                {
                    if (!seen.Add(i)) { failure = "Bot " + i + " is in two overlapping races"; return false; }
                    var b = state.bots[i];
                    if (b.presence != (byte)BotPresence.InMatch || b.activeRace != (int)(race.raceSequence & int.MaxValue)) { failure = "Race participant " + i + " is not IN_MATCH for its race"; return false; }
                }
            for (int i = 0; i < n; i++)
            {
                var b = state.bots[i];
                if (b.presence == (byte)BotPresence.InMatch && b.activeRace != HumanReservation && !seen.Contains(i)) { failure = "Bot " + i + " IN_MATCH without a race"; return false; }
                if (b.activeRace == HumanReservation && seen.Contains(i)) { failure = "Bot " + i + " reserved by human and simulated"; return false; }
                if ((b.presence == (byte)BotPresence.InQueue) != (queuePos[i] >= 0)) { failure = "Queue index mismatch " + i; return false; }
                if (b.presence != (byte)BotPresence.InMatch && heapPos[i] < 0) { failure = "Bot " + i + " has no scheduled event"; return false; }
            }
            return true;
        }

        // ---------------- containers ----------------
        void QueueAdd(int i) { if (queuePos[i] >= 0) return; queuePos[i] = queueCount; queue[queueCount++] = i; }
        void QueueRemove(int i)
        {
            int at = queuePos[i]; if (at < 0) return;
            int last = queue[--queueCount]; queue[at] = last; queuePos[last] = at; queuePos[i] = -1;
        }
        // Heap entries (key, bot) are compared by (time, BotId).
        static bool Less(long ka, int a, long kb, int b) => ka < kb || (ka == kb && a < b);
        void HeapPush(int i) { int at = heapCount++; heap[at] = i; heapKey[at] = state.bots[i].nextEventTicks; heapPos[i] = at; Up(at); }
        void HeapRemove(int i)
        {
            int at = heapPos[i]; if (at < 0) return;
            heapPos[i] = -1; int lastAt = --heapCount;
            if (at == lastAt) return;
            int last = heap[lastAt]; heap[at] = last; heapKey[at] = heapKey[lastAt]; heapPos[last] = at; Up(at); Down(heapPos[last]);
        }
        void HeapUpdate(int i)
        {
            if (heapPos[i] < 0) { HeapPush(i); return; }
            int at = heapPos[i]; heapKey[at] = state.bots[i].nextEventTicks; Up(at); Down(heapPos[i]);
        }
        void Up(int at)
        {
            int item = heap[at]; long key = heapKey[at];
            while (at > 0)
            {
                int parent = (at - 1) >> 1;
                if (!Less(key, item, heapKey[parent], heap[parent])) break;
                heap[at] = heap[parent]; heapKey[at] = heapKey[parent]; heapPos[heap[at]] = at; at = parent;
            }
            heap[at] = item; heapKey[at] = key; heapPos[item] = at;
        }
        void Down(int at)
        {
            int item = heap[at]; long key = heapKey[at];
            while (true)
            {
                int l = at * 2 + 1; if (l >= heapCount) break;
                int r = l + 1, c = r < heapCount && Less(heapKey[r], heap[r], heapKey[l], heap[l]) ? r : l;
                if (!Less(heapKey[c], heap[c], key, item)) break;
                heap[at] = heap[c]; heapKey[at] = heapKey[c]; heapPos[heap[at]] = at; at = c;
            }
            heap[at] = item; heapKey[at] = key; heapPos[item] = at;
        }
    }
}
